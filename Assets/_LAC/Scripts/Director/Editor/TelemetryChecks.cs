#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public static class TelemetryChecks
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        public static string LastResult { get; private set; } = "Not run";
        public static string LastDirectory { get; private set; }

        [MenuItem("LAC/Tests/Check T43 Telemetry (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count == 0 || LastResult == "RUNNING") return;
            LastResult = "RUNNING";
            try
            {
                Application.runInBackground = true;
                var run = RunManager.Instance;
                UnityEngine.Object.FindAnyObjectByType<WaveManager>().enabled = false;
                EnemySpawner.Instance.ResetRun();
                var players = PlayerRegistry.All.ToArray();
                foreach (var p in players)
                {
                    p.GetComponent<WeaponAuto>().enabled = false;
                    p.GetComponent<PlayerMovement>().enabled = false;
                    typeof(PlayerHealth).GetMethod("ServerRestore", Private).Invoke(p.GetComponent<PlayerHealth>(), null);
                }
                run.StartRun(431);
                LastDirectory = run.Telemetry.DirectoryPath;
                var first = players[0];
                int initialHp = first.GetComponent<PlayerHealth>().Health;
                Check(DamageSystem.ApplyToPlayer(first, 1, Vector2.zero), "First damage rejected");
                Check(!DamageSystem.ApplyToPlayer(first, 999, Vector2.zero), "I-frame did not reject damage");
                var catalog = Resources.Load<EnemyCatalogData>("Enemies/EnemyCatalog");
                EnemySpawner.Instance.BeginWave(1);
                EnemySpawner.Instance.Spawn(catalog.Get(0), new Vector2(10, 7));
                foreach (var target in EnemyRegistry.Alive.ToArray()) DamageSystem.ApplyToEnemy(target, 999, Vector2.zero);
                run.ReportSoulCollected(3);
                run.ReportWaveCleared();
                var firstWave = Read(Path.Combine(LastDirectory, "waves.csv"));
                Check(firstWave.Count == players.Length, "Wave summary duplicated/missing");
                Check(firstWave[0]["health_lost"] == "1" && firstWave[0]["hits_taken"] == "1", "Rejected hit counted");
                Check(firstWave[0]["team_kills"] == "1" && firstWave[0]["team_souls"] == "3", "Team counters wrong");
                Check(firstWave[0]["result"] == "cleared", "Cleared wave result");
                await Task.Delay(150);
                run.ReportCardSelectionComplete();
                await Task.Delay(100);

                if (players.Length == 2)
                {
                    // Tránh collider Trống Đồng ở tâm sân đẩy hai người ra ngoài vùng cứu.
                    for (int i = 0; i < players.Length; i++)
                        players[i].GetComponent<PlayerMovement>().ServerRespawn(new Vector2(4, i));
                    await Task.Delay(500);
                    Down(players[1]);
                    await Task.Delay(3500);
                    Check(players[1].IsAlive, "Real 3-second revival failed");
                }
                foreach (var p in players) Down(p);
                Check(run.State == RunState.Defeat, "Expected defeat");
                var summary = Read(Path.Combine(LastDirectory, "runs.csv"));
                var events = Read(Path.Combine(LastDirectory, "events.csv"));
                Check(summary.Count == players.Length && summary.All(r => r["result"] == "defeat"), "Run summary mismatch");
                Check(summary[0]["health_lost"] == initialHp.ToString(), "Lethal damage missing or overkill counted");
                Check(summary[0]["downs"] == "1", "Down counted twice");
                if (players.Length == 2) Check(summary[1]["revives"] == "1" && summary[1]["downs"] == "2", "Revival counters wrong");
                Check(events.Count(r => r["event"] == "run_ended") == 1, "Duplicate run end");
                run.Telemetry.Finish("aborted");
                Check(Read(Path.Combine(LastDirectory, "runs.csv")).Count == players.Length, "Finish is not idempotent");

                run.RestartRun();
                Check(run.Telemetry.DirectoryPath != LastDirectory, "Restart overwrote previous data");
                string victoryDirectory = run.Telemetry.DirectoryPath;
                for (int wave = 1; wave <= run.TotalWaves; wave++)
                {
                    run.ReportWaveCleared();
                    if (!run.IsOver) run.ReportCardSelectionComplete();
                }
                var victoryWaves = Read(Path.Combine(victoryDirectory, "waves.csv"));
                Check(victoryWaves.Count == run.TotalWaves * players.Length, "Final wave missing/duplicated");
                Check(Read(Path.Combine(victoryDirectory, "runs.csv")).All(r => r["result"] == "victory" && r["downs"] == "0"), "Counters leaked across restart");
                run.RestartRun();
                string abortedDirectory = run.Telemetry.DirectoryPath;
                NetworkManager.singleton.StopHost();
                var aborted = Read(Path.Combine(abortedDirectory, "runs.csv"));
                Check(aborted.Count == players.Length && aborted.All(r => r["result"] == "aborted"), "StopHost not recorded as aborted");
                LastResult = "ALL PASSED (" + players.Length + " players)";
                Debug.Log("[TelemetryChecks] " + LastResult + " CSV: " + LastDirectory);
            }
            catch (Exception e) { LastResult = e.ToString(); Debug.LogException(e); }
        }

        private static void Down(PlayerCharacter player)
        {
            typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(player.GetComponent<PlayerHealth>(), 0f);
            Check(DamageSystem.ApplyToPlayer(player, 999, Vector2.zero), "Down failed");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("T43: " + message);
        }

        // Đọc CSV có dấu phẩy, dấu nháy và newline trong ô; dùng cả cho kiểm tra bằng MCP.
        public static List<Dictionary<string, string>> Read(string path)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            string text;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8)) text = reader.ReadToEnd();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (!quoted && (c == ',' || c == '\n'))
                {
                    row.Add(cell.ToString()); cell.Clear();
                    if (c == '\n') { rows.Add(row); row = new List<string>(); }
                }
                else if (quoted || c != '\r') cell.Append(c);
            }
            Check(!quoted && row.Count == 0 && cell.Length == 0, "Truncated CSV");
            var result = new List<Dictionary<string, string>>();
            foreach (var data in rows.Skip(1))
            {
                Check(data.Count == rows[0].Count, "CSV column mismatch");
                var entry = new Dictionary<string, string>();
                for (int i = 0; i < data.Count; i++) entry.Add(rows[0][i], data[i]);
                result.Add(entry);
            }
            return result;
        }
    }
}
#endif
