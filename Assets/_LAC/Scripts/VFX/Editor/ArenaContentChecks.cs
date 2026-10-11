#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.VFX.Editor
{
    public static class ArenaContentChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string LastResult { get; private set; } = "Not run";
        public static string CsvDirectory { get; private set; }

        [MenuItem("LAC/Tests/Check 10 Wave Content (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying || !Mirror.NetworkServer.active)
                throw new InvalidOperationException("Cần host đang Play trong Arena.");
            LastResult = "Running";
            var run = RunManager.Instance;
            var waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            var decor = UnityEngine.Object.FindFirstObjectByType<ArenaStageVisuals>();
            var player = PlayerRegistry.All.First();
            var weapon = player.GetComponent<WeaponAuto>();
            var health = player.GetComponent<PlayerHealth>();
            bool weaponWasEnabled = weapon.enabled;
            try
            {
                Check(run.TotalWaves == 10, "Expected 10 waves");
                Check(decor != null && decor.GetComponentsInChildren<Collider2D>(true).Length == 0, "Decor collision");
                var giong = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
                Check(giong.AnimationSet.Get(AnimState.Idle).FrameCount == 8, "Giong idle frames");
                Check(giong.AnimationSet.Get(AnimState.Walk).FrameCount == 6, "Giong run frames");
                Check(giong.AnimationSet.Get(AnimState.Attack).FrameCount == 4, "Giong attack frames");
                weapon.enabled = false;
                run.StartRun(1504);
                CsvDirectory = run.Telemetry.DirectoryPath;
                typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(health, Time.time + 300);
                var selections = new HashSet<int>();
                bool sawBoss = false;
                double deadline = UnityEditor.EditorApplication.timeSinceStartup + 100;
                while (!run.IsOver && UnityEditor.EditorApplication.timeSinceStartup < deadline)
                {
                    if (!Application.isPlaying) throw new InvalidOperationException("Play stopped during check");
                    int wave = run.CurrentWave;
                    Check(decor.CurrentStage == Mathf.Min((wave - 1) / 3, 2), "Stage differs from wave " + wave);
                    if (run.State == RunState.CardSelection)
                    {
                        selections.Add(wave);
                        Check(wave < 10, "Card selection after boss");
                        if (wave == 1) await Task.Delay(150); // Đợi timeout và ACK hiệu ứng chọn thẻ thật.
                        else run.ReportCardSelectionComplete();
                        await Task.Delay(150);
                        continue;
                    }
                    if (run.IsTimedWave)
                    {
                        // Dồn các lượt sinh còn lại để kiểm tra cap và hết giờ khi quái còn sống.
                        typeof(RunManager).GetField("_waveEndsAt", Private).SetValue(run, Mirror.NetworkTime.time + 1);
                        await Task.Delay(150);
                        int reserved = EnemyRegistry.Alive.Sum(e => e.Data.PopulationCost);
                        Check(reserved <= 40, "Population cap including splitter children");
                        Check(run.State == RunState.WaveActive, "Ended early with time remaining");
                        run.ReportWaveCleared();
                        Check(run.State == RunState.WaveActive, "Early clear bypassed timer");
                        typeof(RunManager).GetField("_waveEndsAt", Private).SetValue(run, Mirror.NetworkTime.time - 1);
                        await Task.Delay(150);
                        Check(run.State == RunState.CardSelection && EnemyRegistry.Count == 0, "Timeout did not clear combat");
                        continue;
                    }
                    typeof(WaveManager).GetField("_waveStartedAt", Private).SetValue(waves, Time.time - 1000);
                    await Task.Delay(150);
                    foreach (var enemy in EnemyRegistry.Alive.ToArray())
                    {
                        if (enemy.IsBoss)
                        {
                            Check(wave == 10 && run.IsFinalWave, "Boss at wrong wave");
                            sawBoss = true;
                        }
                        DamageSystem.ApplyToEnemy(enemy, 99999, player.transform.position);
                    }
                    await Task.Delay(150);
                }
                Check(run.State == RunState.Victory && run.CurrentWave == 10, "Victory must be wave 10");
                Check(selections.Count == 9 && sawBoss, "Expected 9 selections and final boss");
                var summary = LAC.Director.Editor.TelemetryChecks.Read(Path.Combine(CsvDirectory, "runs.csv"));
                var rows = LAC.Director.Editor.TelemetryChecks.Read(Path.Combine(CsvDirectory, "waves.csv"));
                Check(rows.Count == 10 && summary[0]["waves_cleared"] == "10", "Telemetry wave count");
                Check(summary[0]["result"] == "victory", "Telemetry victory");
                run.RestartRun();
                await Task.Delay(300);
                Check(run.CurrentWave == 1 && decor.CurrentStage == 0, "Restart did not reset stage");
                LastResult = "ALL PASSED: 10 waves, 9 selections, timer guard, population cap, timeout cleanup, boss, telemetry, stages, restart";
                Debug.Log("[ArenaContentChecks] " + LastResult);
            }
            catch (Exception e) { LastResult = e.ToString(); Debug.LogException(e); }
            finally { if (weapon != null) weapon.enabled = weaponWasEnabled; }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("ArenaContent: " + message);
        }
    }
}
#endif
