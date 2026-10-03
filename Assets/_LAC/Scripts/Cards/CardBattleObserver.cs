using LAC.Core;
using LAC.Player;
using UnityEngine;

namespace LAC.Cards
{
    internal sealed class CardBattleObserver
    {
        private readonly CardBattleFeedback _feedback;
        private readonly PlayerUpgradeState _state;
        private readonly PlayerHealth _health;
        private readonly PlayerMovement _movement;
        private readonly PlayerDash _dash;
        private readonly CardDefinition[] _cards;
        private readonly CardBattleFeedbackData _data;
        private readonly int[] _stacks;
        private CharacterData _characterData;
        private Vector2 _lastPosition;
        private int _lastHealth, _pendingHeal;
        private bool _pendingUpgrade, _pendingShield, _selectionObserved;
        private float _nextWindAt, _healExpiresAt;
        public PlayerCharacter Player { get; }
        public bool Available => Player != null && Player.isActiveAndEnabled && _state != null && _health != null;

        public CardBattleObserver(CardBattleFeedback feedback, PlayerCharacter player, CardDefinition[] cards,
            CardBattleFeedbackData data)
        {
            _feedback = feedback; Player = player; _cards = cards; _data = data;
            _state = player.Upgrades; _health = player.GetComponent<PlayerHealth>();
            _movement = player.GetComponent<PlayerMovement>(); _dash = player.GetComponent<PlayerDash>();
            _stacks = new int[cards.Length];
            Reset();
            _state.Changed += OnUpgrades;
            if (_health != null) _health.HealthChanged += OnHealth;
            if (_dash != null) _dash.Dashed += OnDash;
        }

        public void Reset()
        {
            _characterData = Player.Data;
            _lastHealth = _health != null ? _health.Health : 0;
            _lastPosition = Player.transform.position;
            _pendingHeal = 0; _pendingUpgrade = _pendingShield = _selectionObserved = false;
            _nextWindAt = _healExpiresAt = 0f;
            for (int i = 0; i < _cards.Length; i++) _stacks[i] = _state.GetStacks(_cards[i].Id);
        }

        private void OnUpgrades()
        {
            bool increased = false, decreased = false;
            for (int i = 0; i < _cards.Length; i++)
            {
                int count = _state.GetStacks(_cards[i].Id);
                increased |= count > _stacks[i]; decreased |= count < _stacks[i];
                _stacks[i] = count;
            }
            if (decreased) { Reset(); return; }
            var run = RunManager.Instance;
            // Snapshot khi mới vào giữa ván không phải một lần vừa nhận thẻ.
            if (increased && run != null && !run.IsOver
                && (_selectionObserved || run.State == RunState.CardSelection)) _pendingUpgrade = true;
        }

        private void OnHealth(int health, int max)
        {
            int delta = health - _lastHealth;
            bool wasAlive = _lastHealth > 0;
            _lastHealth = health;
            var run = RunManager.Instance;
            if (health <= 0) { _pendingHeal = 0; _pendingUpgrade = _pendingShield = false; return; }
            if (!wasAlive || health <= 0 || run == null || run.IsOver) return;
            // SyncVar máu có thể tới trước replay thẻ phía client; giữ delta thật rồi xét sau.
            if (delta > 0) { _pendingHeal += delta; _healExpiresAt = Time.unscaledTime + .75f; }
            if (delta < 0 && _state.HitInvulnerabilityMultiplier > 1f) _pendingShield = true;
        }

        private void OnDash()
        {
            // RPC lướt có thể kết thúc trước đuôi nội suy vị trí trên máy quan sát.
            float duration = Player.Data != null ? Player.Data.DashDuration : 0f;
            _nextWindAt = Mathf.Max(_nextWindAt, Time.time + duration + _data.WindDashQuietSeconds);
        }

        public void Tick(RunManager run)
        {
            if (_characterData != Player.Data) Reset();
            Vector2 position = Player.transform.position;
            Vector2 displacement = position - _lastPosition;
            _lastPosition = position;
            if (run.State == RunState.CardSelection) _selectionObserved = true;
            if (run.State != RunState.WaveActive || Time.deltaTime <= 0f || !Player.IsAlive) return;
            if (_pendingUpgrade) { _feedback.Emit(CardBattleEffect.Kind.Upgrade, Player, Vector2.zero); _pendingUpgrade = false; }
            if (_pendingHeal > 0)
            {
                if (_state.WaveHeal > 0 || _state.MaxHealthBonus > 0)
                { _feedback.Emit(CardBattleEffect.Kind.Heal, Player, Vector2.zero, _pendingHeal); _pendingHeal = 0; }
                else if (Time.unscaledTime >= _healExpiresAt) _pendingHeal = 0;
            }
            if (_pendingShield) { _feedback.Emit(CardBattleEffect.Kind.Shield, Player, Vector2.zero); _pendingShield = false; }
            if (_state.MoveSpeedMultiplier <= 1f || _movement == null || !_movement.IsMoving
                || (_dash != null && _dash.IsDashing) || Time.time < _nextWindAt) return;
            // Bỏ rung mạng khi đứng yên và bước nhảy vị trí khi hồi sinh/teleport.
            float distance = displacement.sqrMagnitude;
            if (distance < Mathf.Pow(.35f * Time.deltaTime, 2) || distance > 2.25f) return;
            float dt = Mathf.Max(Time.deltaTime, Time.fixedDeltaTime);
            float maxStep = Player.Data.MoveSpeed * _state.MoveSpeedMultiplier * _data.WindMaxWalkSpeedRatio * dt;
            if (distance > maxStep * maxStep) return;
            _nextWindAt = Time.time + _data.WindInterval;
            _feedback.Emit(CardBattleEffect.Kind.Wind, Player, displacement.normalized);
        }

        public void Dispose()
        {
            if (_state != null) _state.Changed -= OnUpgrades;
            if (_health != null) _health.HealthChanged -= OnHealth;
            if (_dash != null) _dash.Dashed -= OnDash;
        }
    }
}
