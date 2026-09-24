#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using LAC.VFX;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    /// <summary>Kiểm tra hồi quy trên host thật; chủ động bắt đầu lại ván thử, không sửa asset.</summary>
    public static class CardSelectionPlayModeChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;

        [MenuItem("LAC/Demo/Check Card Resume (Play Mode - restarts run)")]
        public static async void Run()
        {
            if (_running) return;
            if (!Application.isPlaying || !NetworkServer.active || PlayerRegistry.Count != 1)
            {
                Debug.LogWarning("[CardResume] Mở Arena và chạy host một người trước khi kiểm tra.");
                return;
            }

            _running = true;
            var run = RunManager.Instance;
            var controller = UnityEngine.Object.FindAnyObjectByType<CardSelectionController>();
            var player = PlayerRegistry.All[0];
            var input = player.GetComponent<PlayerInputReader>();
            var health = player.GetComponent<PlayerHealth>();
            var waveManager = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
            float originalScale = Time.timeScale;
            bool originalInput = input.enabled;
            float originalInvulnerability = Get<float>(health, "_invulnerableUntil");
            try
            {
                var cards = Resources.LoadAll<CardDefinition>("Cards");
                Require(cards.Length == 7, "Thiếu định nghĩa thẻ.");
                foreach (var card in cards)
                {
                    await Restart(run, health);
                    // Giữ đúng asset thẻ, chỉ giới hạn bể rút của đối tượng chạy để thử đủ 7 loại.
                    var definitions = Get<CardDefinition[]>(run, "_cardDefinitions");
                    Set(run, "_cardDefinitions", new[] { card });
                    try
                    {
                        ClearWave(waveManager);
                        await Task.Delay(250);
                        Require(Get<float>(controller, "_previousTimeScale") == 1f,
                            "Đã lưu nhầm timeScale của hit-stop.");
                        await Task.Delay(100);
                        Require(Time.timeScale == 0f && !input.enabled, "Giao tranh chạy khi bảng còn mở.");
                        HitStop.Request(.03f);
                        HitStop.Tick();
                        Require(Time.timeScale == 0f, "Hit-stop chiếm pause của thẻ.");
                        var button = FirstCardButton(controller);
                        button.onClick.Invoke();
                        button.onClick.Invoke();
                        Require(!input.enabled, "Hiệu ứng thẻ bật input trước khi đóng bảng.");
                        await Task.Delay(Mathf.CeilToInt(CardHoverVisual.ConsumeDuration * 1000f) + 650);
                        Require(player.Upgrades.GetStacks(card.Id) == 1, "Nhấp đôi nhận nhiều thẻ.");
                        Require(run.State == RunState.WaveActive && run.CurrentWave == 2,
                            "Không chuyển đúng một đợt sau chọn.");
                        Require(Time.timeScale == 1f && !CardSelectionController.CombatInputLocked,
                            "Game vẫn bị pause sau chọn.");
                        Require(input.enabled && Get<InputActionMap>(input, "_map").enabled,
                            "Input/action map không được khôi phục.");
                        Require(EnemyRegistry.Count > 0, "Đợt mới không sinh quái.");
                        float before = Time.fixedTime;
                        await Task.Delay(100);
                        Require(Time.fixedTime > before, "Mô phỏng vật lý không tiếp tục.");
                        Debug.Log($"[CardResume] PASS {card.DisplayName}: pause, double-click, wave 2, input, physics.");
                    }
                    finally { Set(run, "_cardDefinitions", definitions); }
                }

                foreach (float scale in new[] { .5f, 0f })
                {
                    await Restart(run, health);
                    input.enabled = false;
                    HitStop.Cancel();
                    Time.timeScale = scale;
                    ClearWave(waveManager);
                    await Task.Delay(250);
                    FirstCardButton(controller).onClick.Invoke();
                    await Task.Delay(Mathf.CeilToInt(CardHoverVisual.ConsumeDuration * 1000f) + 650);
                    Require(Time.timeScale == scale && !input.enabled,
                        "Không bảo toàn pause/input đã có trước màn thẻ.");
                }

                await Restart(run, health);
                ClearWave(waveManager);
                await Task.Delay(250);
                FirstCardButton(controller).onClick.Invoke();
                run.ReportPlayerDown();
                await Task.Delay(80);
                run.RestartRun();
                await Task.Delay(350);
                Require(run.State == RunState.WaveActive && run.CurrentWave == 1,
                    "Coroutine chọn cũ chuyển đợt của ván mới.");
                Require(!Get<bool>(controller, "_committing") && Get<int>(controller, "_rerollsRemaining") == 2,
                    "Trạng thái lựa chọn không reset.");
                foreach (var card in cards) Require(player.Upgrades.GetStacks(card.Id) == 0, "Còn thẻ ván cũ.");
                Debug.Log("[CardResume] ALL PASSED: seven cards, kill hit-stop overlap, preserved pause/input, cancelled selection and run reset.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (Application.isPlaying && run != null)
                {
                    HitStop.Cancel();
                    run.ReportPlayerDown();
                    run.RestartRun();
                    Time.timeScale = originalScale;
                    if (input != null) input.enabled = originalInput;
                    if (health != null) Set(health, "_invulnerableUntil", originalInvulnerability);
                }
                _running = false;
            }
        }

        private static async Task Restart(RunManager run, PlayerHealth health)
        {
            HitStop.Cancel();
            if (!run.IsOver) run.ReportPlayerDown();
            // Cho WaveManager quan sát trạng thái kết thúc trước khi khởi động lại.
            await Task.Delay(80);
            run.RestartRun();
            Time.timeScale = 1f;
            health.GetComponent<PlayerInputReader>().enabled = true;
            Set(health, "_invulnerableUntil", Time.time + 1000f);
            await Task.Delay(100);
        }

        private static void ClearWave(WaveManager waves)
        {
            int count = EnemyRegistry.Count;
            Require(count > 0, "Đợt thử không có quái.");
            for (int i = count - 1; i >= 0; i--)
                DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
            // Cùng khung với quái cuối chết: ép đúng thứ tự đã gây lỗi, không phụ thuộc FPS.
            typeof(WaveManager).GetMethod("Update", Private).Invoke(waves, null);
            Require(RunManager.Instance.State == RunState.CardSelection, "Không mở chọn sau khi hết quái.");
        }

        private static Button FirstCardButton(CardSelectionController controller)
        {
            var slots = Get<Array>(controller.GetComponent<CardSelectionView>(), "_slots");
            return (Button)slots.GetValue(0).GetType().GetField("Button").GetValue(slots.GetValue(0));
        }

        private static T Get<T>(object target, string name) =>
            (T)target.GetType().GetField(name, Private).GetValue(target);

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[CardResume] " + message);
        }
    }
}
#endif
