#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Director.Editor
{
    /// <summary>Kiểm tra luật/ngữ cảnh bằng dữ liệu thật; không thay cho Play Mode/co-op.</summary>
    public static class DirectorRuleChecks
    {
        private static int _checks;

        [MenuItem("LAC/Validation/Director Rules")]
        public static void RunMenu() => Debug.Log(RunAll());

        public static string RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("RunAll chỉ chạy trong Edit Mode.");
            _checks = 0;
            WaveTable table = AssetDatabase.LoadAssetAtPath<WaveTable>("Assets/_LAC/Data/Waves/WaveTable_CoDinh.asset");
            DirectorRulesData rules = Resources.Load<DirectorRulesData>("Director/DirectorRules");
            Check(table != null && table.TotalWaves == 10, "Thiếu bảng 10 wave");
            Check(rules != null && rules.TryValidate(out _), "Thiếu/invalid DirectorRules asset");
            string tableBefore = EditorJsonUtility.ToJson(table);
            string rulesBefore = EditorJsonUtility.ToJson(rules);
            var director = new AIDirector();
            WaveSpec baseline = table.BuildSpec(6, new RandomStream(711, "baseline"));
            DirectorContext healthy = Context(.9f, .05f);

            DirectorDecision d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.3f, 0f), rules, table, new RandomStream(71, "director"));
            Check(d.State == DirectorState.Relief && !d.IsFallback, "HP ngưỡng vào Relief");
            Check(Close(d.Applied.SpawnInterval, baseline.SpawnInterval * 1.25f) && d.Applied.Burst == 2, "Relief không áp đúng nhịp/burst");
            Check(SameMix(d.Applied, baseline), "Relief thay thành phần");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.49f, .1f), rules, table, null);
            Check(d.State == DirectorState.Relief, "Hysteresis thoát sớm theo HP");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.5f, .20f), rules, table, null);
            Check(d.State == DirectorState.Balanced, "Hysteresis không thoát tại ngưỡng bằng");
            director.Reset();
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.9f, .35f), rules, table, null);
            Check(d.State == DirectorState.Relief, "Damage ngưỡng vào Relief");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.9f, .201f), rules, table, null);
            Check(d.State == DirectorState.Relief, "Hysteresis thoát sớm theo damage");
            director.Reset();
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.9f, .05f, 1), rules, table, null);
            Check(d.State == DirectorState.Relief, "Down không ưu tiên người yếu nhất");
            director.Reset();
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.7f, .10f), rules, table, new RandomStream(71, "director"));
            Check(d.State == DirectorState.Variety && !d.IsFallback, "Variety không vào tại ngưỡng");
            Check(d.Applied.SpawnInterval == baseline.SpawnInterval && d.Applied.Burst == baseline.Burst, "Variety tăng nhịp");
            Check(d.Applied.Duration == baseline.Duration && SafetyConstraints.PopulationBudget(d.Applied) <= SafetyConstraints.PopulationBudget(baseline), "Variety phá budget/Duration");
            Check(!Close(d.Applied.Directions[0], baseline.Directions[0]), "Variety chưa đổi hướng");
            director.Reset();
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.699f, .1f), rules, table, null);
            Check(d.State == DirectorState.Balanced, "Variety vào sai bên ngưỡng HP");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.9f, .101f), rules, table, null);
            Check(d.State == DirectorState.Balanced, "Variety vào sai bên ngưỡng damage");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, Context(.9f, 1.8f), rules, table, null);
            Check(d.State == DirectorState.Relief && d.Context.DamageRatioMax == 1f && Close(d.Context.DamageRatioMaxRaw, 1.8f), "Damage thô bị mất/kẹp sai");

            d = director.Decide(DirectorMode.Fixed, 6, baseline, DirectorContext.Missing("no_data"), null, table, null);
            Check(d.IsApplicable && !d.IsFallback && d.Source == "fixed_table" && SameSpec(d.Applied, baseline), "Fixed bị context/rules làm thay đổi");
            foreach (int protectedWave in new[] { 1, 10 })
            {
                WaveSpec protectedSpec = table.BuildSpec(protectedWave, new RandomStream(9, "baseline"));
                d = director.Decide(DirectorMode.Adaptive, protectedWave, protectedSpec, DirectorContext.Missing("no_data"), null, table, null);
                Check(d.IsApplicable && !d.IsFallback && d.Source == "fixed_table" && SameSpec(d.Applied, protectedSpec), "Wave mở đầu/boss bị sửa");
            }
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, DirectorContext.Missing("no_participants"), rules, table, null);
            Check(d.IsFallback && d.FallbackReason == "no_participants" && SameSpec(d.Applied, baseline), "Missing context không fallback");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, healthy, null, table, null);
            Check(d.IsFallback && d.FallbackReason == "missing_rules", "Missing rules không fallback");
            d = director.Decide(DirectorMode.Adaptive, 6, baseline, healthy, rules, table, null);
            Check(d.IsFallback && d.FallbackReason == "missing_director_random", "Missing RunRandom không fallback");
            d = director.Decide((DirectorMode)99, 6, baseline, healthy, rules, table, null);
            Check(d.IsFallback && d.FallbackReason == "invalid_mode", "Invalid mode không fallback");
            foreach (DirectorContext invalid in new[] { Context(float.NaN, 0f), Context(.5f, float.PositiveInfinity), Context(.5f, -1f), Context(.5f, .1f, -1), new DirectorContext(true,.8f,0f,0,1,1,1) })
            {
                d = director.Decide(DirectorMode.Adaptive, 6, baseline, invalid, rules, table, new RandomStream(71, "director"));
                Check(d.IsFallback && !d.Context.IsValid && SameSpec(d.Applied, baseline), "Context sai vẫn chạy Adaptive/báo metadata hợp lệ");
            }

            Check(!ReferenceEquals(d.Applied.Mix, baseline.Mix) && !ReferenceEquals(d.Applied.Directions, baseline.Directions), "Fallback không clone mảng");
            float directionBefore = baseline.Directions[0];
            d.Applied.Directions[0] = .123f;
            Check(baseline.Directions[0] == directionBefore, "Đụng mảng baseline");
            WaveSpec.Group[] malformedMix = (WaveSpec.Group[])baseline.Mix.Clone();
            malformedMix[0] = new WaveSpec.Group(malformedMix[0].Enemy, int.MaxValue);
            foreach (WaveSpec malformed in new[] {
                new WaveSpec(baseline.Mix, new[] { float.NaN }, 1f, 2, 45f),
                new WaveSpec(baseline.Mix, new[] { 1f }, 1f, 2, 45f),
                new WaveSpec(baseline.Mix, baseline.Directions, float.PositiveInfinity, 2, 45f),
                new WaveSpec(baseline.Mix, baseline.Directions, 1f, 2, float.MaxValue),
                new WaveSpec(malformedMix, baseline.Directions, 1f, 2, 45f),
                new WaveSpec(null, null, 1f, 1, 45f),
                table.BuildSpec(10, null) })
            {
                d = director.Decide(DirectorMode.Adaptive, 6, malformed, healthy, rules, table, new RandomStream(71, "director"));
                Check(!d.IsApplicable && d.Source == "invalid_baseline", "Baseline hỏng bị tạo fallback giả");
            }
            Check(!SafetyConstraints.TryValidateApplied(new WaveSpec(baseline.Mix,baseline.Directions,baseline.SpawnInterval,baseline.Burst,baseline.Duration+1f),baseline,6,table,DirectorState.Variety,out _), "Duration mới được chấp nhận");
            Check(!SafetyConstraints.TryValidateApplied(new WaveSpec(baseline.Mix,baseline.Directions,baseline.SpawnInterval*.9f,baseline.Burst,baseline.Duration),baseline,6,table,DirectorState.Variety,out _), "Tăng nhịp lọt hàng rào");
            EnemyData future = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_LAC/Data/Enemies/QuyNho.asset");
            Check(!SafetyConstraints.TryValidateBaseline(new WaveSpec(new[] {new WaveSpec.Group(future,1)},new[]{0f},1f,1,25f),2,table,out _), "Quái chưa giới thiệu được sinh");

            for (int wave = 1; wave <= 10; wave++)
            {
                WaveSpec original = table.BuildSpec(wave, new RandomStream(913 + wave, "baseline"));
                Check(SafetyConstraints.TryValidateBaseline(original, wave, table, out string reason), "Bảng gốc invalid wave " + wave + ": " + reason);
                for (int seed = 1; seed <= 40; seed++)
                {
                    var a = new AIDirector(); var b = new AIDirector();
                    DirectorDecision da = a.Decide(DirectorMode.Adaptive,wave,original,healthy,rules,table,new RandomStream(seed,"director"));
                    DirectorDecision db = b.Decide(DirectorMode.Adaptive,wave,original,healthy,rules,table,new RandomStream(seed,"director"));
                    Check(da.IsApplicable && !da.IsFallback && SameSpec(da.Applied,db.Applied), "Cùng seed khác quyết định");
                    Check(SafetyConstraints.TryValidateApplied(da.Applied,original,wave,table,da.State,out _), "Đặc tả AI phá tầng an toàn");
                }
            }
            director.Reset(); Check(director.State == DirectorState.Balanced, "State xuyên qua reset");
            Check(EditorJsonUtility.ToJson(table) == tableBefore && EditorJsonUtility.ToJson(rules) == rulesBefore, "AI sửa asset trong ván");
            CheckContexts();
            string result = "PASS: " + _checks + " assertions (Director rules + team context, Edit Mode; NOT Play Mode/Internet).";
            Debug.Log("[DirectorRuleChecks] " + result);
            return result;
        }

        private static void CheckContexts()
        {
            Check(PlayerRegistry.Count == 0, "Context fixture cần Edit Mode không có player");
            var objects = new List<GameObject>();
            var context = new ContextAggregator();
            try
            {
                PlayerCharacter a = Player(objects, "DirectorCheck_A", 10, 10);
                PlayerCharacter b = Player(objects, "DirectorCheck_B", 6, 6);
                context.BeginWave(); SetHealth(a, 9, 10); context.RecordDamage(a, 1);
                SetHealth(b, 1, 6); context.RecordDamage(b, 5);
                DirectorContext snapshot = context.Capture();
                Check(snapshot.IsValid && Close(snapshot.HealthMin,1f/6f) && Close(snapshot.DamageRatioMaxRaw,5f/6f) && snapshot.ConnectedPlayers==2, "Team không ưu tiên người yếu nhất");
                SetHealth(a, 10, 10); SetHealth(b, 6, 6);
                Check(Close(context.Capture().HealthMin,1f/6f), "Thẻ/hồi máu viết lại snapshot cuối wave");
                context.Leave(b); PlayerRegistry.Unregister(b);
                snapshot=context.Capture();
                Check(snapshot.IsValid && Close(snapshot.HealthMin,.9f) && snapshot.ConnectedPlayers==1 && Close(snapshot.DamageRatioMaxRaw,5f/6f), "Rời khi chọn thẻ còn ép HP thấp hoặc mất tổn thất");
                context.BeginWave(); SetHealth(a,0,10); context.RecordDamage(a,10);
                SetHealth(a,5,10); SetHealth(a,0,10); context.RecordDamage(a,5);
                snapshot=context.Capture();
                Check(snapshot.IsValid && snapshot.HealthMin==0f && snapshot.Downs==2 && Close(snapshot.DamageRatioMaxRaw,1.5f), "Down/revive che tổn thất hoặc mất raw >1");
                SetHealth(a,16,16); context.BeginWave(); SetHealth(a,8,16); context.RecordDamage(a,8);
                snapshot=context.Capture();
                Check(snapshot.Downs==0 && Close(snapshot.DamageRatioMaxRaw,.5f), "Reset wave không cập nhật mẫu số upgrade");
                PlayerRegistry.Register(b); context.RefreshParticipants();
                snapshot=context.Capture();
                Check(!snapshot.IsValid && snapshot.MissingReason=="incomplete_late_join" && snapshot.LateJoiners==1, "Vào khi chọn thẻ giả tạo lịch sử");
                SetHealth(a,16,16); context.BeginWave();
                PlayerCharacter late=Player(objects,"DirectorCheck_Late",8,8); context.RefreshParticipants();
                snapshot=context.Capture();
                Check(!snapshot.IsValid && snapshot.LateJoiners==1 && snapshot.Participants==3, "Vào giữa wave không fallback");
                foreach (GameObject obj in objects) PlayerRegistry.Unregister(obj.GetComponent<PlayerCharacter>());
                context.BeginWave(); snapshot=context.Capture();
                Check(!snapshot.IsValid && snapshot.MissingReason=="no_participants", "Đội rỗng vẫn hợp lệ");
                context.Reset(); Check(context.Capture().MissingReason=="missing_wave", "Context xuyên qua reset run");
            }
            finally
            {
                foreach (GameObject obj in objects)
                {
                    if (obj == null) continue;
                    PlayerRegistry.Unregister(obj.GetComponent<PlayerCharacter>());
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        private static PlayerCharacter Player(List<GameObject> objects,string name,int hp,int max)
        {
            var obj=new GameObject(name); objects.Add(obj);
            PlayerCharacter player=obj.AddComponent<PlayerCharacter>(); obj.AddComponent<PlayerHealth>();
            SetHealth(player,hp,max); PlayerRegistry.Register(player); return player;
        }
        private static void SetHealth(PlayerCharacter player,int hp,int max)
        {
            PlayerHealth health=player.GetComponent<PlayerHealth>();
            typeof(PlayerHealth).GetField("_health",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(health,hp);
            typeof(PlayerHealth).GetField("_maxHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(health,max);
        }
        private static DirectorContext Context(float hp,float damage,int downs=0) => new DirectorContext(true,hp,damage,downs,2,2);
        private static bool Close(float a,float b) => Mathf.Abs(a-b)<.00001f;
        private static bool SameMix(WaveSpec a,WaveSpec b)
        {
            if(a.Mix.Length!=b.Mix.Length) return false;
            for(int i=0;i<a.Mix.Length;i++) if(a.Mix[i].Enemy!=b.Mix[i].Enemy || a.Mix[i].Count!=b.Mix[i].Count) return false;
            return true;
        }
        private static bool SameSpec(WaveSpec a,WaveSpec b)
        {
            if(!SameMix(a,b) || a.Burst!=b.Burst || a.SpawnInterval!=b.SpawnInterval || a.Duration!=b.Duration || a.Directions.Length!=b.Directions.Length) return false;
            for(int i=0;i<a.Directions.Length;i++) if(a.Directions[i]!=b.Directions[i]) return false;
            return true;
        }
        private static void Check(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); _checks++; }
    }
}
#endif
