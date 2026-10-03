#if UNITY_EDITOR
using System;
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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    public static class CardBattleFeedbackChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;

        [MenuItem("LAC/Tests/Check Card Battle Feedback (Play Mode)")]
        public static async void Run()
        {
            if (_running) return;
            Require(Application.isPlaying && NetworkServer.active && PlayerRegistry.Count == 1, "Cần host một người.");
            _running = true;
            var run = RunManager.Instance;
            var player = PlayerRegistry.All[0];
            var health = player.GetComponent<PlayerHealth>();
            var ui = CardSelectionController.Instance;
            var feedback = ui.GetComponent<CardBattleFeedback>();
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var config = Resources.Load<CardBattleFeedbackData>("CardBattleFeedback");
            var dash = player.GetComponent<PlayerDash>();
            CharacterId originalCharacter = player.CharacterId;
            bool originalPlayerEnabled = player.enabled;
            bool originalPlayerActive = player.gameObject.activeSelf;
            bool originalFeedbackEnabled = feedback.enabled;
            string dataBefore = JsonUtility.ToJson(config);
            var definitions = Get<CardDefinition[]>(run, "_cardDefinitions");
            try
            {
                await Restart();
                int healed = feedback.Presented(CardBattleEffect.Kind.Heal);
                int upgraded = feedback.Presented(CardBattleEffect.Kind.Upgrade);
                int oldHealth = health.Health;
                await Select(CardId.SinhLuc);
                Require(feedback.Presented(CardBattleEffect.Kind.Upgrade) == upgraded + 1, "Thiếu/trùng vòng nhận thẻ.");
                Require(feedback.Presented(CardBattleEffect.Kind.Heal) == healed + 1
                    && feedback.LastHealAmount == health.Health - oldHealth, "Sinh Lực không báo đúng máu thực nhận.");
                typeof(PlayerHealth).GetMethod("OnHealthChanged", Private).Invoke(health, new object[] { health.Health, health.Health });
                await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Heal) == healed + 1, "Hook cùng máu phát hiệu ứng trùng.");

                int shields = feedback.Presented(CardBattleEffect.Kind.Shield);
                Hit(); await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Shield) == shields, "Không có Thiết Bích vẫn hiện khiên.");
                await Select(CardId.ThietBich);
                Hit(); await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Shield) == shields + 1, "Thiếu dấu khiên sau sát thương thật.");
                Require(!DamageSystem.ApplyToPlayer(player, 1, player.transform.position), "Cửa sổ bảo vệ bị đổi.");
                await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Shield) == shields + 1, "Đòn bị chặn vẫn phát dấu khiên.");
                Set(health, "_invulnerableUntil", Time.time + 10000f);

                healed = feedback.Presented(CardBattleEffect.Kind.Heal);
                await Select(CardId.HoiXuan);
                Require(feedback.Presented(CardBattleEffect.Kind.Heal) == healed + 1
                    && feedback.LastHealAmount == 1, "Hồi Xuân không báo đúng một máu đầu đợt.");
                DamageSystem.HealPlayer(player, 999); await Task.Delay(80);
                healed = feedback.Presented(CardBattleEffect.Kind.Heal);
                DamageSystem.HealPlayer(player, 999); await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Heal) == healed, "Đầy máu vẫn báo hồi.");

                await Select(CardId.KhinhThan);
                Require(Keyboard.current != null, "Cần keyboard thử di chuyển.");
                int wind = feedback.Presented(CardBattleEffect.Kind.Wind);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
                await Task.Delay(650);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                await Task.Delay(100);
                int emitted = feedback.Presented(CardBattleEffect.Kind.Wind) - wind;
                Require(emitted >= 1 && emitted <= 4, "Vệt gió không theo di chuyển/giới hạn nhịp.");
                wind = feedback.Presented(CardBattleEffect.Kind.Wind);
                await Task.Delay(350);
                Require(feedback.Presented(CardBattleEffect.Kind.Wind) == wind, "Đứng yên vẫn phát gió.");
                Time.timeScale = 0f;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
                await Task.Delay(300);
                Require(feedback.Presented(CardBattleEffect.Kind.Wind) == wind, "Pause vẫn phát gió.");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Time.timeScale = 1f;
                await Task.Delay(100);

                await CheckDashTail();
                await CheckLifecycle();

                var emit = typeof(CardBattleFeedback).GetMethod("Emit", Private);
                object[] emission = { CardBattleEffect.Kind.Upgrade, player, Vector2.zero, 0 };
                for (int i = 0; i < 1000; i++) emit.Invoke(feedback, emission);
                Require(feedback.PooledEffects == config.Capacity && feedback.ActiveEffects == config.Capacity,
                    "Pool phát sinh thêm đối tượng khi đầy.");
                await Task.Delay(1200);
                Require(feedback.ActiveEffects == 0, "Hiệu ứng không tự trả pool.");
                upgraded = feedback.Presented(CardBattleEffect.Kind.Upgrade);
                healed = feedback.Presented(CardBattleEffect.Kind.Heal);
                await Restart(); await Task.Delay(150);
                Require(feedback.ActiveEffects == 0 && feedback.Presented(CardBattleEffect.Kind.Upgrade) == upgraded
                    && feedback.Presented(CardBattleEffect.Kind.Heal) == healed, "Restart phát buff/hồi giả.");
                Require(JsonUtility.ToJson(config) == dataBefore, "Sửa asset hiệu ứng khi chạy.");
                Debug.Log("[CardBattle] HOST ALL PASSED: actual accepted cards, Sinh Luc/duplicate hook, Hoi Xuan/full HP, shield only on real damage with card, movement/idle/pause, owned dash/tail/resumed walk, inactive target, empty-observer disable, character change clears live/pending FX, reenable subscriptions, 1000 requests capped at 24, lifetime, restart, immutable data.");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                if (Application.isPlaying)
                {
                    Time.timeScale = 1f;
                    if (player != null)
                    {
                        player.gameObject.SetActive(originalPlayerActive);
                        player.enabled = originalPlayerEnabled;
                        if (NetworkServer.active && player.CharacterId != originalCharacter)
                            player.SetCharacter(originalCharacter);
                    }
                    if (feedback != null) feedback.enabled = originalFeedbackEnabled;
                    if (run != null) Set(run, "_cardDefinitions", definitions);
                    if (health != null) Set(health, "_invulnerableUntil", 0f);
                }
                _running = false;
            }

            async Task CheckDashTail()
            {
                Require(dash != null && player.isOwned, "Cần dash của người chơi local để kiểm vệt gió.");
                double readyDeadline = Time.realtimeSinceStartupAsDouble + 3d;
                while ((dash.CooldownRemaining > 0f || dash.IsDashing)
                    && Time.realtimeSinceStartupAsDouble < readyDeadline) await Task.Delay(20);
                Require(dash.CooldownRemaining <= 0f && !dash.IsDashing, "Dash chưa sẵn sàng trong thời hạn kiểm.");
                float startedAt = -1f;
                Action observedDash = () => startedAt = Time.time;
                dash.Dashed += observedDash;
                int before = feedback.Presented(CardBattleEffect.Kind.Wind);
                Key walk = player.transform.position.x > 0f ? Key.A : Key.D;
                try
                {
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(walk, Key.Space));
                    double deadline = Time.realtimeSinceStartupAsDouble + 2d;
                    while (startedAt < 0f && Time.realtimeSinceStartupAsDouble < deadline) await Task.Delay(10);
                    Require(startedAt >= 0f, "Input không kích hoạt dash thật.");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(walk));
                    float quietUntil = startedAt + player.Data.DashDuration + config.WindDashQuietSeconds;
                    while (Time.time < quietUntil - .03f && Time.realtimeSinceStartupAsDouble < deadline)
                    {
                        Require(feedback.Presented(CardBattleEffect.Kind.Wind) == before,
                            "Bước cuối dash hoặc đuôi dash phát nhầm vệt đi bộ.");
                        await Task.Delay(10);
                    }
                    Require(Time.time >= quietUntil - .03f, "Thời gian gameplay không tiến trong phép thử dash.");
                    deadline = Time.realtimeSinceStartupAsDouble + 2d;
                    while (feedback.Presented(CardBattleEffect.Kind.Wind) == before
                        && Time.realtimeSinceStartupAsDouble < deadline) await Task.Delay(20);
                    Require(!dash.IsDashing && feedback.Presented(CardBattleEffect.Kind.Wind) > before,
                        "Đi bộ sau dash không khôi phục vệt gió.");
                }
                finally
                {
                    dash.Dashed -= observedDash;
                    if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                }
                await Task.Delay(1000);
            }

            async Task CheckLifecycle()
            {
                var pool = Get<ObjectPool<CardBattleEffect>>(feedback, "_pool");
                Require(pool != null && feedback.ActiveEffects == 0, "Cần pool nhàn rỗi trước thử lifecycle.");
                BorrowPreview();
                player.enabled = false;
                await Task.Delay(80);
                Require(feedback.ActiveEffects == 0, "Disable PlayerCharacter không thu hồi hiệu ứng.");
                Require(Get<System.Collections.IList>(feedback, "_players").Count == 0,
                    "Observer của component bị disable chưa được tháo.");

                // Dựng trạng thái pool có hiệu ứng nhưng observer rỗng, trước LateUpdate kế tiếp.
                BorrowPreview();
                feedback.enabled = false;
                Require(feedback.ActiveEffects == 0, "Disable Feedback bỏ sót pool khi observer đã rỗng.");
                feedback.enabled = true;
                player.enabled = true;
                await Task.Delay(80);

                BorrowPreview();
                player.gameObject.SetActive(false);
                await Task.Delay(80);
                Require(feedback.ActiveEffects == 0, "Ẩn GameObject vẫn để hiệu ứng hiển thị.");
                player.gameObject.SetActive(true);
                await Task.Delay(80);

                CharacterData replacement = null;
                var registry = Get<CharacterRegistry>(player, "_registry");
                for (int i = 0; i < registry.Count; i++)
                {
                    var candidate = registry.GetByIndex(i);
                    if (candidate != null && candidate.Id != originalCharacter) { replacement = candidate; break; }
                }
                Require(replacement != null, "Thiếu nhân vật khác để kiểm đổi danh tính hiệu ứng.");
                Hit();
                Set(health, "_invulnerableUntil", Time.time + 10000f);
                await Task.Delay(450);
                int beforeHeal = feedback.Presented(CardBattleEffect.Kind.Heal);
                DamageSystem.HealPlayer(player, 1);
                BorrowPreview();
                player.SetCharacter(replacement.Id);
                await Task.Delay(80);
                Require(feedback.ActiveEffects == 0 && feedback.Presented(CardBattleEffect.Kind.Heal) == beforeHeal,
                    "Đổi nhân vật còn hiệu ứng sống hoặc phát hồi máu chờ của nhân vật cũ.");
                player.SetCharacter(originalCharacter);
                await Task.Delay(80);

                Require(ObserverSubscribers(health, "HealthChanged") == 1
                    && ObserverSubscribers(player.Upgrades, "Changed") == 1
                    && ObserverSubscribers(dash, "Dashed") == 1,
                    "Disable/enable làm mất hoặc nhân đôi subscription của observer.");
                int beforeShield = feedback.Presented(CardBattleEffect.Kind.Shield);
                Hit();
                Set(health, "_invulnerableUntil", Time.time + 10000f);
                await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Shield) == beforeShield + 1,
                    "Sau enable dấu khiên không phát đúng một lần.");
                beforeHeal = feedback.Presented(CardBattleEffect.Kind.Heal);
                DamageSystem.HealPlayer(player, 1);
                await Task.Delay(80);
                Require(feedback.Presented(CardBattleEffect.Kind.Heal) == beforeHeal + 1
                    && feedback.LastHealAmount == 1, "Sau enable hồi máu không phát đúng một lần.");
                await Task.Delay(1000);

                void BorrowPreview()
                {
                    pool.Get(player.transform.position, Quaternion.identity)
                        .Play(pool, config, CardBattleEffect.Kind.Heal, player, Vector2.zero, 1);
                    Require(feedback.ActiveEffects > 0, "Không dựng được hiệu ứng lifecycle.");
                }
            }

            async Task Restart()
            {
                Time.timeScale = 1f;
                if (!run.IsOver) run.ReportPlayerDown();
                await Task.Delay(100);
                run.RestartRun();
                Set(health, "_invulnerableUntil", Time.time + 10000f);
                await Task.Delay(150);
            }
            async Task Select(CardId id)
            {
                Set(health, "_invulnerableUntil", Time.time + 10000f);
                Set(run, "_cardDefinitions", new[] { cards.Single(c => c.Id == id) });
                var waves = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
                Set(waves, "_waveStartedAt", Time.time - 1000f);
                typeof(WaveManager).GetMethod("ReleaseDueSpawns", Private).Invoke(waves, null);
                for (int i = EnemyRegistry.Count - 1; i >= 0; i--)
                    DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
                await Task.Delay(150);
                Require(run.State == RunState.CardSelection, "Không mở đề nghị thật.");
                var slot = Get<Array>(ui.GetComponent<CardSelectionView>(), "_slots").GetValue(0);
                ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
                double deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (run.State == RunState.CardSelection && Time.realtimeSinceStartupAsDouble < deadline)
                    await Task.Delay(30);
                await Task.Delay(80);
                Require(run.State == RunState.WaveActive && player.Upgrades.GetStacks(id) > 0, "Thẻ chưa được host nhận.");
            }
            void Hit()
            {
                Set(health, "_invulnerableUntil", 0f);
                Require(DamageSystem.ApplyToPlayer(player, 1, player.transform.position), "Đòn thử không trừ máu.");
            }
        }

        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static int ObserverSubscribers(object target, string eventName) =>
            Get<Delegate>(target, eventName)?.GetInvocationList().Count(callback =>
                callback.Target != null && callback.Target.GetType().FullName == "LAC.Cards.CardBattleObserver") ?? 0;
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException("[CardBattle] " + message); }
    }
}
#endif
