#if UNITY_EDITOR
using System;
using System.Linq;
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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    public static class CardBattleCoopChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static CardDefinition[] _originalCards;
        private static readonly Dictionary<PlayerHealth, float> OriginalProtection = new Dictionary<PlayerHealth, float>();
        private static bool _prepared, _running;

        [MenuItem("LAC/Tests/Card Battle/Prepare Late Join (Host)")]
        public static async void PrepareLateJoin()
        {
            if (_running || _prepared) return;
            Require(Application.isPlaying && NetworkServer.active && PlayerRegistry.Count == 1, "Cần host một người.");
            _running = true;
            var run = RunManager.Instance;
            _originalCards = Get<CardDefinition[]>(run, "_cardDefinitions");
            try
            {
                if (!run.IsOver) run.ReportPlayerDown();
                await Task.Delay(100);
                run.RestartRun();
                ProtectPlayers();
                await Task.Delay(200);
                await OpenOffer(CardId.KhinhThan, true);
                PickHost();
                await Wait(() => run.CurrentWave == 2 && run.State == RunState.WaveActive);
                await OpenOffer(CardId.SinhLuc, true);
                // Chừa thời gian khởi chạy client; riêng fixture, không đổi cấu hình 10 giây.
                Set(run, "_cardDeadline", NetworkTime.time + 300d);
                var selections = Get<System.Collections.IDictionary>(run, "_cardSelections");
                var local = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
                typeof(RunManager).GetMethod("SendCardOffer", Private).Invoke(run,
                    new[] { (object)local.connectionToClient, selections[local.connectionToClient.connectionId] });
                _prepared = true;
                Debug.Log("[CardBattleCoop] READY: launch CardClient.exe --lac-card-battle-probe during wave 2 selection.");
            }
            catch (Exception e) { Debug.LogException(e); RestoreCards(); RestoreProtection(); }
            finally { _running = false; }
            if (_prepared) ContinueWhenClientJoins();
        }

        private static async void ContinueWhenClientJoins()
        {
            try { await Wait(() => PlayerRegistry.Count == 2, 300d); Complete(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                RestoreCards(); RestoreProtection(); _prepared = false;
            }
        }

        [MenuItem("LAC/Tests/Card Battle/Complete Late Join And Movement")]
        public static async void Complete()
        {
            if (_running) return;
            Require(_prepared && Application.isPlaying && NetworkServer.active && PlayerRegistry.Count == 2,
                "Chuẩn bị host trước rồi kết nối client --lac-card-battle-probe.");
            _running = true;
            var run = RunManager.Instance;
            var local = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
            var remote = PlayerRegistry.All.First(p => p != local);
            var feedback = CardSelectionController.Instance.GetComponent<CardBattleFeedback>();
            var dash = remote.GetComponent<PlayerDash>();
            bool remoteDashed = false;
            int beforeRemoteDash = 0;
            Action onDash = () => { remoteDashed = true; beforeRemoteDash = feedback.Presented(CardBattleEffect.Kind.Wind); };
            dash.Dashed += onDash;
            try
            {
                ProtectPlayers();
                await Task.Delay(1000);
                Require(run.State == RunState.CardSelection && local.Upgrades.GetStacks(CardId.KhinhThan) == 1,
                    "Client không vào đúng màn chọn đang có lịch sử.");
                int upgraded = feedback.Presented(CardBattleEffect.Kind.Upgrade);
                int healed = feedback.Presented(CardBattleEffect.Kind.Heal);
                PickHost();
                await Wait(() => run.CurrentWave == 3 && run.State == RunState.WaveActive);
                await Task.Delay(500);
                Require(feedback.Presented(CardBattleEffect.Kind.Upgrade) == upgraded + 1
                    && feedback.Presented(CardBattleEffect.Kind.Heal) == healed + 1
                    && remote.Upgrades.GetStacks(CardId.SinhLuc) == 0, "Người vào muộn nhận nhầm thẻ/hiệu ứng.");
                Debug.Log("[CardBattleCoop] LATE JOIN PASSED: inherited history, only existing participant upgraded/healed.");

                await OpenOffer(CardId.KhinhThan, false);
                int beforeRemoteWalk = feedback.Presented(CardBattleEffect.Kind.Wind);
                PickHost();
                await Wait(() => run.CurrentWave == 4 && run.State == RunState.WaveActive);
                Require(local.Upgrades.GetStacks(CardId.KhinhThan) == 2 && remote.Upgrades.GetStacks(CardId.KhinhThan) == 1,
                    "Chưa áp Khinh Thân cho đúng hai người.");
                await Wait(() => remoteDashed);
                int walking = beforeRemoteDash - beforeRemoteWalk;
                await Task.Delay(1000);
                Require(walking >= 2 && walking <= 8 && feedback.Presented(CardBattleEffect.Kind.Wind) == beforeRemoteDash,
                    "Máy host thiếu gió của đồng đội hoặc phát gió ở đuôi dash.");
                await Task.Delay(2200);
                int remoteTotal = feedback.Presented(CardBattleEffect.Kind.Wind) - beforeRemoteWalk;
                Require(remoteTotal > walking && remoteTotal <= 14, "Gió không tiếp tục khi đồng đội đi lại.");
                Debug.Log($"[CardBattleCoop] REMOTE MOVEMENT PASSED walk={walking} dash=0 total={remoteTotal} pool={feedback.PooledEffects}");

                int wind = feedback.Presented(CardBattleEffect.Kind.Wind);
                Require(Keyboard.current != null, "Thiếu keyboard host.");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
                await Task.Delay(650);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                await Task.Delay(700);
                Require(feedback.Presented(CardBattleEffect.Kind.Wind) > wind, "Host không có vệt đi bộ.");
                int upgradesBeforeReset = feedback.Presented(CardBattleEffect.Kind.Upgrade);
                int healsBeforeReset = feedback.Presented(CardBattleEffect.Kind.Heal);
                run.ReportPlayerDown(); run.ReportPlayerDown();
                await Task.Delay(150);
                run.RestartRun();
                ProtectPlayers();
                await Task.Delay(500);
                Require(feedback.ActiveEffects == 0 && feedback.Presented(CardBattleEffect.Kind.Upgrade) == upgradesBeforeReset
                    && feedback.Presented(CardBattleEffect.Kind.Heal) == healsBeforeReset, "Restart co-op phát hiệu ứng cũ.");
                Debug.Log("[CardBattleCoop] ALL PASSED: late join during selection, remote walk/dash/idle/resume, host walk, restart.");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                if (dash != null) dash.Dashed -= onDash;
                if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                RestoreCards();
                RestoreProtection();
                _prepared = _running = false;
            }
        }

        private static async Task OpenOffer(CardId id, bool solo)
        {
            var run = RunManager.Instance;
            Set(run, "_cardDefinitions", new[] { Resources.LoadAll<CardDefinition>("Cards").Single(c => c.Id == id) });
            var waves = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
            if (solo)
            {
                Set(waves, "_waveStartedAt", Time.time - 1000f);
                typeof(WaveManager).GetMethod("ReleaseDueSpawns", Private).Invoke(waves, null);
            }
            else
            {
                // Hai máy phải giữ cùng lịch/ID quái; không đẩy riêng đồng hồ host.
                await Wait(() => Get<int>(waves, "_spawnedWave") == run.CurrentWave
                    && Get<System.Collections.ICollection>(waves, "_schedule").Count > 0
                    && Get<int>(waves, "_nextSpawnIndex") >= Get<System.Collections.ICollection>(waves, "_schedule").Count);
                await Task.Delay(500);
            }
            for (int i = EnemyRegistry.Count - 1; i >= 0; i--)
                DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
            await Wait(() => run.State == RunState.CardSelection);
        }
        private static void PickHost()
        {
            var view = CardSelectionController.Instance.GetComponent<CardSelectionView>();
            var slot = Get<Array>(view, "_slots").GetValue(0);
            ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
        }
        private static void ProtectPlayers()
        {
            for (int i = 0; i < PlayerRegistry.Count; i++)
            {
                var health = PlayerRegistry.All[i].GetComponent<PlayerHealth>();
                if (!OriginalProtection.ContainsKey(health)) OriginalProtection.Add(health, Get<float>(health, "_invulnerableUntil"));
                Set(health, "_invulnerableUntil", Time.time + 10000f);
            }
        }
        private static void RestoreProtection()
        {
            foreach (var entry in OriginalProtection)
                if (entry.Key != null) Set(entry.Key, "_invulnerableUntil", entry.Value);
            OriginalProtection.Clear();
        }
        private static void RestoreCards()
        {
            if (RunManager.Instance != null) Set(RunManager.Instance, "_cardDefinitions", _originalCards);
        }
        private static async Task Wait(Func<bool> predicate, double seconds = 35d)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + seconds;
            while (!predicate())
            {
                if (!Application.isPlaying || Time.realtimeSinceStartupAsDouble > deadline)
                    throw new InvalidOperationException("[CardBattleCoop] Hết thời gian chờ.");
                await Task.Delay(30);
            }
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Require(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("[CardBattleCoop] " + message); }
    }
}
#endif
