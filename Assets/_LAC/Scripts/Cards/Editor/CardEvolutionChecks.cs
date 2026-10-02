#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    public static class CardEvolutionChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;

        [MenuItem("LAC/Tests/Validate Evolution Rules")]
        public static void Validate()
        {
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var recipes = CardEvolutionTestFixture.Install();
            var go = new GameObject("EvolutionRulesTest");
            var second = new GameObject("EvolutionReplayTest");
            var invalid = ScriptableObject.CreateInstance<CardEvolutionData>();
            try
            {
                var state = go.AddComponent<PlayerUpgradeState>();
                var replay = second.AddComponent<PlayerUpgradeState>();
                var power = Array.Find(cards, c => c.Id == CardId.CuongCong);
                var speed = Array.Find(cards, c => c.Id == CardId.LienKich);
                string before = JsonUtility.ToJson(power);
                foreach (var recipe in recipes) Require(recipe.Validate(cards, out _), "Fixture không hợp lệ.");
                Require(!invalid.Validate(cards, out _), "Nhận công thức rỗng.");
                invalid.EditorConfigure("bad", "Bad", "", recipes[0].Bonus,
                    new CardEvolutionData.Ingredient(power, 1), new CardEvolutionData.Ingredient(power, 1));
                Require(!invalid.Validate(cards, out _), "Nhận nguyên liệu trùng.");
                invalid.EditorConfigure("bad", "Bad", "", recipes[0].Bonus,
                    new CardEvolutionData.Ingredient(power, 99), new CardEvolutionData.Ingredient(speed, 1));
                Require(!invalid.Validate(cards, out _), "Nhận mức nguyên liệu không thể đạt.");
                state.Apply(power, null);
                Require(state.Evolutions.Count == 0, "Tiến hoá khi thiếu nguyên liệu.");
                state.Apply(speed, null);
                Require(state.Evolutions.Count == 2 && Mathf.Approximately(state.DamageMultiplier, 1.4f)
                    && Mathf.Approximately(state.AttackRangeMultiplier, 1.1f), "Không áp hai phần thưởng độc lập.");
                Require(replay.Evolutions.Count == 0, "Tiến hoá bị dùng chung giữa người chơi.");
                state.Apply(power, null);
                Require(state.Evolutions.Count == 2 && Mathf.Approximately(state.DamageMultiplier, 1.55f), "Áp phần thưởng lần hai.");
                for (int i = 0; i < 3; i++)
                {
                    replay.ResetRun(); replay.Apply(power, null); replay.Apply(speed, null); replay.Apply(power, null);
                    Require(replay.Evolutions.Count == 2 && Mathf.Approximately(replay.DamageMultiplier, state.DamageMultiplier),
                        "Dựng lại lịch sử làm lệch chỉ số.");
                }
                Require(JsonUtility.ToJson(power) == before, "Sửa ScriptableObject nền.");
                state.ResetRun();
                Require(state.Evolutions.Count == 0 && state.DamageMultiplier == 1f, "Reset còn tiến hoá cũ.");
                state.Apply(speed, null); state.Apply(power, null);
                Require(state.Evolutions.Count == 2, "Ván mới không tiến hoá được.");
                invalid.EditorConfigure("threshold", "Threshold", "", recipes[0].Bonus,
                    new CardEvolutionData.Ingredient(power, 3), new CardEvolutionData.Ingredient(speed, 2));
                Require(invalid.Validate(cards, out _) && !invalid.IsReady(state, WeaponShape.Circle), "Bỏ qua số cấp nguyên liệu.");
                state.Apply(power, null); state.Apply(power, null);
                Require(!invalid.IsReady(state, WeaponShape.Circle), "Chỉ kiểm một loại nguyên liệu.");
                state.Apply(speed, null);
                Require(invalid.IsReady(state, WeaponShape.Circle), "Không nhận đủ nguyên liệu tại ngưỡng.");
                var projectile = Array.Find(cards, c => c.Id == CardId.SongTien);
                invalid.EditorConfigure("projectile", "Projectile", "", recipes[0].Bonus,
                    new CardEvolutionData.Ingredient(projectile, 1), new CardEvolutionData.Ingredient(power, 1));
                state.Apply(projectile, null);
                Require(!invalid.IsReady(state, WeaponShape.Arc) && invalid.IsReady(state, WeaponShape.Line), "Không lọc vũ khí.");
                Debug.Log("[Evolution] RULES ALL PASSED: validation, missing ingredients, simultaneous, once-only, per-player, replay, reset, immutable assets.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(invalid); CardEvolutionTestFixture.Remove(recipes);
            }
        }

        [MenuItem("LAC/Tests/Check Evolution Play Mode (restarts run)")]
        public static async void Run()
        {
            if (_running || !Application.isPlaying || !NetworkServer.active) return;
            _running = true;
            var recipes = CardEvolutionTestFixture.Install();
            var run = RunManager.Instance;
            var ui = CardSelectionController.Instance;
            var local = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
            var originalDefinitions = Get<CardDefinition[]>(run, "_cardDefinitions");
            var protectedPlayers = new System.Collections.Generic.Dictionary<PlayerHealth, float>();
            try
            {
                foreach (var player in PlayerRegistry.All)
                {
                    var health = player.GetComponent<PlayerHealth>();
                    protectedPlayers[health] = Get<float>(health, "_invulnerableUntil");
                }
                run.ReportPlayerDown(); run.ReportPlayerDown();
                await Task.Delay(150); run.RestartRun(); await Task.Delay(300);
                foreach (var health in protectedPlayers.Keys) Set(health, "_invulnerableUntil", Time.time + 10000f);
                var cards = Resources.LoadAll<CardDefinition>("Cards");
                var power = Array.Find(cards, c => c.Id == CardId.CuongCong);
                var speed = Array.Find(cards, c => c.Id == CardId.LienKich);
                await SelectRound(run, ui, power, 2);
                Require(local.Upgrades.Evolutions.Count == 0, "Tiến hoá sớm.");
                await Task.Delay(300);
                Set(run, "_cardDefinitions", new[] { speed });
                ClearWave(); await Task.Delay(450); Pick(ui); Pick(ui);
                await Task.Delay(1500);
                Require(Get<GameObject>(ui.GetComponent<CardSelectionView>(), "_evolutionPanel").activeSelf,
                    "Không hiển thị thông báo khi timeScale=0.");
                Require(run.State == RunState.CardSelection && Time.timeScale == 0f && local.Upgrades.Evolutions.Count == 2,
                    "Đợt mới chạy trước khi đọc thông báo hoặc tiến hoá trùng.");
                Debug.Log("[Evolution] DISPLAY READY: two notifications, real accepted card, paused gameplay.");
                await Wait(() => run.CurrentWave == 3 && run.State == RunState.WaveActive, 18000);
                // Cho client ghi chữ ký sau khi nhận trạng thái đợt mới, trước khi test reset ván.
                await Task.Delay(600);
                foreach (var player in PlayerRegistry.All)
                    Require(player.Upgrades.Evolutions.Count == 2 && Mathf.Approximately(player.Upgrades.DamageMultiplier, 1.4f),
                        "Sai tiến hoá của người chơi.");
                Require(Time.timeScale == 1f && !CardSelectionController.CombatInputLocked, "Kẹt input/pause.");
                run.ReportPlayerDown(); run.ReportPlayerDown(); await Task.Delay(150); run.RestartRun(); await Task.Delay(300);
                Require(local.Upgrades.Evolutions.Count == 0
                    && !Get<GameObject>(ui.GetComponent<CardSelectionView>(), "_evolutionPanel").activeSelf,
                    "Restart giữ tiến hoá/thông báo.");
                Debug.Log($"[Evolution] PLAY ALL PASSED: players={PlayerRegistry.Count}, double-click, queued UI, ACK barrier, reset.");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                Set(run, "_cardDefinitions", originalDefinitions);
                foreach (var pair in protectedPlayers) if (pair.Key != null) Set(pair.Key, "_invulnerableUntil", pair.Value);
                CardEvolutionTestFixture.Remove(recipes); _running = false;
            }
        }

        private static async Task SelectRound(RunManager run, CardSelectionController ui, CardDefinition card, int nextWave)
        {
            Set(run, "_cardDefinitions", new[] { card }); ClearWave();
            await Task.Delay(450); Pick(ui);
            await Wait(() => run.CurrentWave == nextWave && run.State == RunState.WaveActive, 18000);
        }
        private static void ClearWave()
        {
            var waves = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
            Set(waves, "_waveStartedAt", Time.time - 1000f);
            typeof(WaveManager).GetMethod("ReleaseDueSpawns", Private).Invoke(waves, null);
            for (int i = EnemyRegistry.Count - 1; i >= 0; i--)
                DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
        }
        private static void Pick(CardSelectionController ui)
        {
            var slot = Get<Array>(ui.GetComponent<CardSelectionView>(), "_slots").GetValue(0);
            ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
        }
        private static async Task Wait(Func<bool> condition, int timeout)
        {
            double end = Time.realtimeSinceStartupAsDouble + timeout / 1000d;
            while (!condition())
            { Require(Application.isPlaying && Time.realtimeSinceStartupAsDouble < end, "Hết thời gian chờ."); await Task.Delay(50); }
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("[Evolution] " + message); }
    }
}
#endif
