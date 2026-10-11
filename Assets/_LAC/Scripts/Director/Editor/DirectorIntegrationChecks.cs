#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace LAC.Director.Editor
{
    public static class DirectorIntegrationChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string LastResult { get; private set; } = "Not run";
        public static string CsvDirectory { get; private set; }
        private static int _assertions;

        // -executeMethod cho bản sao TEMP; mở scene rõ ràng thay vì tin cache __Backupscenes.
        public static void StartClientEditor()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-telemetry-client") < 0)
                throw new InvalidOperationException("Bootstrap chỉ dành cho Editor client có cờ kiểm thử.");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_LAC/Scenes/Arena.unity");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("LAC/Tests/Check Director Integration (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count == 0)
                throw new InvalidOperationException("Cần Mirror host đang Play trong Arena.");
            if (LastResult == "RUNNING") return;
            LastResult = "RUNNING";
            _assertions = 0;
            var run = RunManager.Instance;
            var player = PlayerRegistry.All.First();
            var health = player.GetComponent<PlayerHealth>();
            var waveManager = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
            var weapons = PlayerRegistry.All.Select(p => p.GetComponent<WeaponAuto>()).ToArray();
            bool[] enabled = weapons.Select(w => w.enabled).ToArray();
            var rules = typeof(RunManager).GetField("_directorRules", Private).GetValue(run);
            try
            {
                Check(run.DirectorMode == DirectorMode.Adaptive, "Chọn Adaptive trước host để chạy fixture này");
                Check(player.Data.MaxHealth == 10, "Fixture cần Warrior host, MaxHealth=10");
                foreach (var weapon in weapons) weapon.enabled = false;
                EnemySpawner.Instance.ResetRun();
                RestoreAll();
                ProtectAll();
                run.StartRun(450146);
                CsvDirectory = run.Telemetry.DirectoryPath;
                await Task.Delay(150);
                Check(run.CurrentWave == 1 && run.DirectorSource == "fixed_table", "Wave 1 được bảo vệ");
                Check(!run.TrySetDirectorMode(DirectorMode.Fixed), "Không đổi mode giữa trận");

                Hit(player, 1);
                Check(!DamageSystem.ApplyToPlayer(player, 999, Vector2.zero), "Bỏ sát thương trong i-frame");
                DamageSystem.HealPlayer(player, 1);
                Hit(player, 4);
                await CompleteTimedWave(run);
                DamageSystem.HealPlayer(player, 3); // Hồi trong nghỉ không được sửa snapshot HP=6.
                await NextWave(run);
                Check(run.CurrentWave == 2 && run.DirectorState == DirectorState.Relief, "Tổn thất thật -> Relief");
                Near(run.DirectorHealthMin, .6f, "Snapshot trước hồi máu");
                Near(run.DirectorDamageRatioMax, .5f, "Tính cả HP đã hồi, chỉ sát thương chấp nhận");
                Near(run.CurrentDirectorDecision.Applied.SpawnInterval, 1.75f, "Nhịp wave 2 x1.25");
                AssertSchedule(waveManager, run.CurrentDirectorDecision.Applied);

                DamageSystem.HealPlayer(player, 100);
                Hit(player, 3);
                await CompleteTimedWave(run);
                await NextWave(run);
                Check(run.CurrentWave == 3 && run.DirectorState == DirectorState.Relief, "Khoảng trễ giữ Relief với tổn thất 30%");
                Near(run.CurrentDirectorDecision.Applied.SpawnInterval, 1.625f, "Relief wave 3");
                Check(run.CurrentDirectorDecision.Applied.Burst == 1, "floor(burst x0.75)");

                DamageSystem.HealPlayer(player, 100);
                await CompleteTimedWave(run);
                await NextWave(run);
                Check(run.CurrentWave == 4 && run.DirectorState == DirectorState.Variety, "Hồi phục -> Variety");
                Check(!run.CurrentDirectorDecision.Applied.Directions.SequenceEqual(run.CurrentDirectorDecision.Baseline.Directions), "Variety thay hướng thật");
                Check(SafetyConstraints.PopulationBudget(run.CurrentDirectorDecision.Applied) <= SafetyConstraints.PopulationBudget(run.CurrentDirectorDecision.Baseline), "Giữ ngân sách kể cả quái con");
                AssertSchedule(waveManager, run.CurrentDirectorDecision.Applied);

                Hit(player, 2);
                await CompleteTimedWave(run);
                await NextWave(run);
                Check(run.CurrentWave == 5 && run.DirectorState == DirectorState.Balanced, "Ngữ cảnh trung gian -> Balanced");
                Near(run.CurrentDirectorDecision.Applied.SpawnInterval, run.CurrentDirectorDecision.Baseline.SpawnInterval, "Balanced baseline");

                DamageSystem.HealPlayer(player, 100);
                await CompleteTimedWave(run);
                typeof(RunManager).GetField("_directorRules", Private).SetValue(run, null);
                await NextWave(run);
                Check(run.CurrentWave == 6 && run.DirectorSource == "fixed_fallback" && run.DirectorFallbackReason == "missing_rules", "Fallback baseline khi thiếu rules");
                AssertSchedule(waveManager, run.CurrentDirectorDecision.Baseline);
                typeof(RunManager).GetField("_directorRules", Private).SetValue(run, rules);

                for (int wave = 6; wave <= 9; wave++)
                {
                    ProtectAll();
                    if (wave == 9)
                    {
                        typeof(RunManager).GetField("_waveEndsAt", Private).SetValue(run, NetworkTime.time + 1d);
                        await Task.Delay(150);
                        int reserved = EnemyRegistry.Alive.Sum(e => e.Data.PopulationCost);
                        Check(reserved <= 40 && reserved > 0, "Spawn áp dụng trần 40, có quái sinh thực");
                    }
                    await CompleteTimedWave(run);
                    await NextWave(run);
                }
                Check(run.CurrentWave == 10 && !run.IsTimedWave && run.DirectorSource == "fixed_table", "Boss baseline, không đổi Duration");
                typeof(WaveManager).GetField("_waveStartedAt", Private).SetValue(waveManager, Time.time - 1000f);
                await Task.Delay(250);
                Check(EnemyRegistry.Alive.Any(e => e.IsBoss), "Boss đã sinh");
                foreach (var enemy in EnemyRegistry.Alive.ToArray()) DamageSystem.ApplyToEnemy(enemy, 99999, player.transform.position);
                await Task.Delay(250);
                Check(run.State == RunState.Victory, "Victory cuối wave 10");
                string csv = TelemetryChecks.ValidateDirectorCsv(CsvDirectory, DirectorMode.Adaptive, 10);
                Check(csv.StartsWith("ALL PASSED"), csv);
                Check(TelemetryChecks.Read(Path.Combine(CsvDirectory, "directors.csv")).Count == 10, "Một quyết định mỗi wave");

                run.RestartRun();
                await Task.Delay(200);
                Check(run.CurrentWave == 1 && run.DirectorState == DirectorState.Balanced && !run.DirectorContextValid, "Restart reset FSM/ngữ cảnh");
                foreach (var p in PlayerRegistry.All.ToArray()) Hit(p, 999);
                Check(run.State == RunState.Defeat, "Gục hết -> Defeat");
                var snapshot = (DirectorContext)typeof(RunManager).GetField("_previousDirectorContext", Private).GetValue(run);
                Check(snapshot.Downs == PlayerRegistry.Count && snapshot.DamageRatioMaxRaw >= 1f, "Đòn chí tử được ghi trước EndRun");
                run.RestartRun();
                await Task.Delay(150);
                Check(run.DirectorMode == DirectorMode.Adaptive && run.CurrentWave == 1 && run.DirectorDowns == 0, "Chơi lại không rò dữ liệu/không đổi mode");
                LastResult = $"ALL PASSED: {_assertions} assertions; real host FSM/HP/heal/hysteresis/applied spawn/CSV/boss/Restart ({PlayerRegistry.Count} players). Deadline accelerated; not balance playtest.";
                Debug.Log("[DirectorIntegration] " + LastResult + " CSV=" + CsvDirectory);
            }
            catch (Exception e) { LastResult = e.ToString(); Debug.LogException(e); }
            finally
            {
                if (run != null) typeof(RunManager).GetField("_directorRules", Private).SetValue(run, rules);
                for (int i = 0; i < weapons.Length; i++) if (weapons[i] != null) weapons[i].enabled = enabled[i];
                if (Application.isPlaying && NetworkServer.active) RestoreAll();
            }
        }

        private static async Task CompleteTimedWave(RunManager run)
        {
            ProtectAll();
            Check(run.State == RunState.WaveActive && run.IsTimedWave, "Đang wave tính giờ");
            run.ReportWaveCleared();
            Check(run.State == RunState.WaveActive, "AI không bỏ bảo vệ kết thúc sớm");
            typeof(RunManager).GetField("_waveEndsAt", Private).SetValue(run, NetworkTime.time - 1d);
            run.ReportWaveCleared();
            await Task.Delay(100);
            Check(run.State == RunState.CardSelection && EnemyRegistry.Count == 0, "Hết giờ, cleanup, pha thẻ");
        }

        private static async Task NextWave(RunManager run)
        {
            run.ReportCardSelectionComplete();
            await Task.Delay(160);
            Check(run.State == RunState.WaveActive && run.CurrentDirectorDecision.IsApplicable, "Đặc tả wave được áp dụng");
        }

        private static void AssertSchedule(WaveManager manager, WaveSpec spec)
        {
            var schedule = (IList)typeof(WaveManager).GetField("_schedule", Private).GetValue(manager);
            Check(schedule.Count > spec.Burst, "Có lịch sinh đầy đủ");
            object next = schedule[spec.Burst];
            float delay = (float)next.GetType().GetField("Delay").GetValue(next);
            Near(delay, spec.SpawnInterval, "WaveManager dùng Applied thay vì bảng cố định");
        }

        private static void Hit(PlayerCharacter player, int amount)
        {
            typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(player.GetComponent<PlayerHealth>(), 0f);
            Check(DamageSystem.ApplyToPlayer(player, amount, Vector2.zero), "Host chấp nhận sát thương thử");
        }

        private static void ProtectAll()
        {
            foreach (var p in PlayerRegistry.All)
                typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(p.GetComponent<PlayerHealth>(), Time.time + 1000f);
        }

        private static void RestoreAll()
        {
            foreach (var p in PlayerRegistry.All)
                typeof(PlayerHealth).GetMethod("ServerRestore", Private).Invoke(p.GetComponent<PlayerHealth>(), null);
        }

        private static void Near(float actual, float expected, string name) => Check(Mathf.Abs(actual - expected) < .001f, name + $" ({actual} != {expected})");
        private static void Check(bool value, string reason)
        {
            _assertions++;
            if (!value) throw new InvalidOperationException("DirectorIntegration: " + reason);
        }
    }
}
#endif
