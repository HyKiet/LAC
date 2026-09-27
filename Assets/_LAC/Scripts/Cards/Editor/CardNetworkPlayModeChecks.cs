#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    public static class CardNetworkPlayModeChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;

        [MenuItem("LAC/Tests/Build Card Network Client")]
        public static void BuildClient()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_LAC/Scenes/Arena.unity" },
                locationPathName = "Builds/CardNetworkTests/CardClient.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Debug.Log($"[CardNet] BUILD {report.summary.result}");
        }

        [MenuItem("LAC/Tests/Check Cards With Remote Test Client")]
        public static async void Run()
        {
            if (_running) return;
            if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count != 2)
            {
                Debug.LogWarning("[CardNet] Cần host Editor và CardClient.exe --lac-card-test-client.");
                return;
            }
            _running = true;
            var run = RunManager.Instance;
            var ui = CardSelectionController.Instance;
            var local = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
            var remote = PlayerRegistry.All[0] == local ? PlayerRegistry.All[1] : PlayerRegistry.All[0];
            var originalInvulnerability = new Dictionary<PlayerHealth, float>();
            try
            {
                run.ReportPlayerDown(); run.ReportPlayerDown();
                await Task.Delay(200);
                run.RestartRun();
                foreach (var player in PlayerRegistry.All)
                {
                    var health = player.GetComponent<PlayerHealth>();
                    originalInvulnerability[health] = Get<float>(health, "_invulnerableUntil");
                    Set(health, "_invulnerableUntil", Time.time + 10000f);
                }
                await Task.Delay(700);
                ClearWave();
                await Task.Delay(600);
                int token = Get<int>(ui, "_token");
                int revision = Get<int>(ui, "_revision");
                run.CmdPickCard(token, revision, (CardId)999);
                run.CmdPickCard(token - 1, revision, CardId.CuongCong);
                await Task.Delay(300);
                Require(Total(local) == 0, "Chấp nhận thẻ sai hoặc token cũ.");
                PickFirst(ui);
                PickFirst(ui);
                await Task.Delay(1600);
                Require(Total(local) == 1 && Total(remote) == 0, "Lựa chọn không độc lập hoặc nhận trùng.");
                Require(run.State == RunState.CardSelection && Time.timeScale == 0f,
                    "Chuyển đợt trước khi người thứ hai chọn.");
                Debug.Log("[CardNet] PASS waiting, invalid ID/token, double click.");
                await Wait(() => run.CurrentWave == 2 && run.State == RunState.WaveActive, 8000);
                Require(Total(local) == 1 && Total(remote) == 1, "Không áp thẻ lên đúng hai người.");
                var rerolls = Get<Dictionary<uint, int>>(run, "_cardRerolls");
                Require(rerolls[local.netId] == 2 && rerolls[remote.netId] == 0, "Lượt đổi bị dùng chung.");
                Debug.Log("[CardNet] PASS remote choice, independent rerolls, both players upgraded.");

                await Task.Delay(600);
                ClearWave();
                double openedAt = Time.realtimeSinceStartupAsDouble;
                await Task.Delay(600);
                // Hai yêu cầu cùng revision: chỉ một lượt đổi được trừ.
                token = Get<int>(ui, "_token"); revision = Get<int>(ui, "_revision");
                run.CmdRerollCards(token, revision); run.CmdRerollCards(token, revision);
                await Task.Delay(400);
                Require(rerolls[local.netId] == 1, "Đổi trùng trừ hai lượt.");
                run.CmdRerollCards(token, Get<int>(ui, "_revision"));
                await Task.Delay(400);
                revision = Get<int>(ui, "_revision");
                run.CmdRerollCards(token, revision);
                await Task.Delay(300);
                Require(rerolls[local.netId] == 0 && Get<int>(ui, "_revision") == revision, "Cho đổi quá hai lượt.");
                PickFirst(ui);
                await Task.Delay(2000);
                Require(run.State == RunState.CardSelection && Total(remote) == 1, "Tự chọn quá sớm.");
                await Wait(() => run.CurrentWave == 3 && run.State == RunState.WaveActive, 12000);
                double elapsed = Time.realtimeSinceStartupAsDouble - openedAt;
                Require(elapsed >= 10d && elapsed < 15d && Total(remote) == 2 && Total(local) == 2,
                    "Sai thời hạn tự chọn hoặc số thẻ.");
                Debug.Log($"[CardNet] PASS timeout {elapsed:F2}s, duplicate reroll, per-run limit.");

                await Task.Delay(600);
                ClearWave();
                await Task.Delay(600);
                PickFirst(ui);
                await Task.Delay(1500);
                remote.connectionToClient.Disconnect();
                await Wait(() => run.State == RunState.WaveActive && run.CurrentWave == 4, 4000);
                Require(!CardSelectionController.CombatInputLocked && Time.timeScale == 1f, "Mất kết nối gây kẹt pause.");
                Debug.Log("[CardNet] PASS disconnect releases barrier.");
                run.ReportPlayerDown();
                await Task.Delay(200);
                run.RestartRun();
                await Task.Delay(300);
                Require(Total(local) == 0 && run.CurrentWave == 1, "Không reset nâng cấp khi chơi lại.");
                Debug.Log("[CardNet] ALL PASSED (two processes, LatencySimulation).");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                foreach (var entry in originalInvulnerability)
                    if (entry.Key != null) Set(entry.Key, "_invulnerableUntil", entry.Value);
                _running = false;
            }
        }

        private static int Total(PlayerCharacter player)
        {
            int count = 0;
            foreach (CardId id in Enum.GetValues(typeof(CardId))) count += player.Upgrades.GetStacks(id);
            return count;
        }
        private static void ClearWave()
        {
            for (int i = EnemyRegistry.Count - 1; i >= 0; i--)
                DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
        }
        private static void PickFirst(CardSelectionController ui)
        {
            var slot = Get<Array>(ui.GetComponent<CardSelectionView>(), "_slots").GetValue(0);
            ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
        }
        private static async Task Wait(Func<bool> condition, int timeout)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeout / 1000d;
            while (!condition())
            {
                if (!Application.isPlaying || Time.realtimeSinceStartupAsDouble > deadline)
                    throw new Exception("[CardNet] Hết thời gian chờ.");
                await Task.Delay(50);
            }
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("[CardNet] " + message); }
    }
}
#endif
