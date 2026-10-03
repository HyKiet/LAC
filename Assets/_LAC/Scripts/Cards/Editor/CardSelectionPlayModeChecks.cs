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
            CharacterId originalCharacter = player.CharacterId;
            try
            {
                var cards = Resources.LoadAll<CardDefinition>("Cards");
                Require(cards.Length == 12, "Thiếu định nghĩa thẻ.");
                foreach (var card in cards)
                {
                    await Restart(run, health);
                    // Thẻ đạn được thử trên Tấm; các thẻ chung thử trên Gióng.
                    player.SetCharacter(card.RequiresProjectile ? CharacterId.Tam : CharacterId.Giong);
                    typeof(PlayerHealth).GetMethod("ServerRestore", Private).Invoke(health, null);
                    if (card.Id == CardId.HoiXuan)
                        DamageSystem.ApplyToPlayer(player, 2, player.transform.position);
                    Set(health, "_invulnerableUntil", Time.time + 1000f);
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
                        CheckRuntimeEffect(card, player, health);
                        float before = Time.fixedTime;
                        await Task.Delay(100);
                        Require(Time.fixedTime > before, "Mô phỏng vật lý không tiếp tục.");
                        Debug.Log($"[CardResume] PASS {card.DisplayName}: pause, double-click, wave 2, input, physics.");
                    }
                    finally { Set(run, "_cardDefinitions", definitions); }
                }

                await CheckCountdown(run, health, controller, waveManager);

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
                Debug.Log("[CardResume] ALL PASSED: twelve cards, kill hit-stop overlap, preserved pause/input, cancelled selection and run reset.");
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
                    if (player != null) player.SetCharacter(originalCharacter);
                }
                _running = false;
            }
        }

        private static async Task CheckCountdown(RunManager run, PlayerHealth health,
            CardSelectionController controller, WaveManager waveManager)
        {
            await Restart(run, health);
            ClearWave(waveManager);
            await Task.Delay(250);
            Require(run.State == RunState.CardSelection, "Chưa mở lượt kiểm bộ đếm.");
            var view = controller.GetComponent<CardSelectionView>();
            var status = Get<Text>(view, "_statusText");
            int token = Get<int>(controller, "_token"), revision = Get<int>(controller, "_revision");
            int rerolls = Get<int>(controller, "_rerollsRemaining");
            double deadline = Get<double>(controller, "_deadline");
            var slots = Get<Array>(view, "_slots");
            var ids = new System.Collections.Generic.List<CardId>();
            foreach (object slot in slots)
            {
                var root = (GameObject)slot.GetType().GetField("Root").GetValue(slot);
                if (root.activeSelf) ids.Add(((CardDefinition)slot.GetType().GetField("Card").GetValue(slot)).Id);
            }
            var update = (Action)Delegate.CreateDelegate(typeof(Action), controller,
                typeof(CardSelectionController).GetMethod("Update", Private));
            // Cố định cùng giây chỉ trong kiểm UI; deadline host không bị sửa.
            try
            {
                Set(controller, "_deadline", NetworkTime.time + 5.5);
                update();
                string unchanged = status.text;
                for (int i = 0; i < 10000; i++) update();
                Require(ReferenceEquals(unchanged, status.text), "Bộ đếm tạo lại chuỗi trong cùng giây.");
                int seconds = Get<int>(controller, "_displayedCountdown");
                double localDeadline = Get<double>(controller, "_deadline");
                view.SetStatus("TEST OFFER RESET");
                controller.ReceiveOffer(token, revision, ids.ToArray(), rerolls, localDeadline);
                update();
                Require(Get<int>(controller, "_displayedCountdown") == seconds
                    && status.text != "TEST OFFER RESET", "Đề nghị cùng giây không reset cache.");
                Set(controller, "_deadline", NetworkTime.time - 1);
                update();
                Require(Get<int>(controller, "_displayedCountdown") == 0 && status.text == "ĐANG TỰ CHỌN…",
                    "Đề nghị quá hạn không hiển thị tự chọn.");
                foreach (var button in view.GetComponentsInChildren<Button>())
                    Require(!button.interactable, "Quá hạn chưa khoá nút.");
            }
            finally
            {
                controller.ReceiveOffer(token, revision, ids.ToArray(), rerolls, deadline);
                update();
            }
            FirstCardButton(controller).onClick.Invoke();
            await Task.Delay(Mathf.CeilToInt(CardHoverVisual.ConsumeDuration * 1000f) + 650);
            Require(run.State == RunState.WaveActive, "Không phục hồi sau kiểm bộ đếm.");
            Debug.Log("[CardResume] COUNTDOWN PASS: 10,000 same-second updates retain string; same-second offer reset, expired offer locks buttons, next wave resumes.");
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

        private static void CheckRuntimeEffect(CardDefinition card, PlayerCharacter player, PlayerHealth health)
        {
            switch (card.Id)
            {
                case CardId.CuongCong:
                    Require(Mathf.Approximately(player.Upgrades.DamageFromBase(player.Data.BaseDamage, false),
                        player.Data.BaseDamage * 1.3f), "Cường Công cấp đầu chưa tăng 30% sát thương thật.");
                    break;
                case CardId.LienKich:
                    Require(Mathf.Approximately(player.Upgrades.AttackIntervalFromBase(player.Data.AttackInterval),
                        player.Data.AttackInterval / 1.225f), "Liên Kích cấp đầu chưa tăng 22,5% tốc độ đánh.");
                    break;
                case CardId.SinhLuc:
                    Require(health.MaxHealth == player.Data.MaxHealth + Mathf.CeilToInt(player.Data.MaxHealth * .2f), "Sinh Lực sai mức máu.");
                    break;
                case CardId.KhinhThan:
                    float speed = (float)typeof(PlayerMovement).GetProperty("MoveSpeed", Private).GetValue(player.GetComponent<PlayerMovement>());
                    Require(Mathf.Approximately(speed, player.Data.MoveSpeed * 1.12f), "Khinh Thân chưa nối vào di chuyển.");
                    break;
                case CardId.AmVang:
                    float range = (float)typeof(WeaponAuto).GetProperty("AttackRange", Private).GetValue(player.GetComponent<WeaponAuto>());
                    Require(Mathf.Approximately(range, player.Data.AttackRange * 1.15f), "Âm Vang chưa nối vào vũ khí.");
                    break;
                case CardId.HoiXuan:
                    Require(health.Health == health.MaxHealth - 1, "Hồi Xuân chưa hồi đúng 1 máu khi sang đợt.");
                    break;
                case CardId.ThietBich:
                    Set(health, "_invulnerableUntil", 0f);
                    DamageSystem.ApplyToPlayer(player, 1, player.transform.position);
                    Require(Mathf.Abs(Get<float>(health, "_invulnerableUntil") - Time.time - Get<float>(health, "_hitInvulnerability") * 1.225f) < .001f,
                        "Thiết Bích chưa tăng thời gian bảo vệ.");
                    Set(health, "_invulnerableUntil", Time.time + 1000f);
                    break;
                case CardId.CuongNo:
                    Require(Mathf.Approximately(player.Upgrades.DamageMultiplier, 1.375f)
                        && Mathf.Approximately(player.Upgrades.AttackSpeedMultiplier, .88f), "Cuồng Nộ thiếu đánh đổi.");
                    break;
            }
        }

        private static void ClearWave(WaveManager waves)
        {
            // T-44 sinh theo lịch: đưa hết quái đang chờ vào sân trước khi dọn đợt thử.
            Set(waves, "_waveStartedAt", Time.time - 1000f);
            typeof(WaveManager).GetMethod("ReleaseDueSpawns", Private).Invoke(waves, null);
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
