using System;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Cards
{
    /// <summary>Bản sao chỉ số của ván; nội dung hiệu ứng lấy từ CardDefinition.</summary>
    public sealed class PlayerUpgradeState : MonoBehaviour
    {
        private readonly int[] _stacks = new int[Enum.GetValues(typeof(CardId)).Length];
        private PlayerCharacter _character;
        private float _healthRatio;
        public event Action Changed;
        public float DamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;
        public float DashCooldownMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float AttackRangeMultiplier { get; private set; } = 1f;
        public float HitInvulnerabilityMultiplier { get; private set; } = 1f;
        public int WaveHeal { get; private set; }
        public int ProjectileCount { get; private set; } = 1;
        public float ProjectileDamageMultiplier { get; private set; } = 1f;
        public int ProjectileHitLimit { get; private set; } = 1;
        public bool Explodes => ExplosionRadius > 0f && ExplosionDamageRatio > 0f;
        public float ProjectileSpreadDegrees { get; private set; }
        public float ExplosionRadius { get; private set; }
        public float ExplosionDamageRatio { get; private set; }
        public int MaxHealthBonus => MaxHealthBonusFromBase(_character != null && _character.Data != null
            ? _character.Data.MaxHealth : 0);

        private void Awake() => _character = GetComponent<PlayerCharacter>();
        public int GetStacks(CardId id) => (uint)(int)id < _stacks.Length ? _stacks[(int)id] : 0;
        public int MaxHealthBonusFromBase(int baseHealth) => Mathf.CeilToInt(baseHealth * _healthRatio - 0.00001f);
        public float DamageFromBase(int baseDamage, bool projectile) =>
            baseDamage * DamageMultiplier * (projectile ? ProjectileDamageMultiplier : 1f);
        public float AttackIntervalFromBase(float baseInterval) => baseInterval / Mathf.Max(.1f, AttackSpeedMultiplier);

        public bool Apply(CardDefinition card, PlayerHealth health)
        {
            if (card == null || (uint)(int)card.Id >= _stacks.Length || GetStacks(card.Id) >= card.MaxStacks) return false;
            int oldHealthBonus = MaxHealthBonus;
            _stacks[(int)card.Id]++;
            DamageMultiplier += card.DamageBonus;
            AttackSpeedMultiplier += card.AttackSpeedBonus;
            _healthRatio += card.HealthBonusRatio;
            DashCooldownMultiplier -= card.DashCooldownReduction;
            MoveSpeedMultiplier += card.MoveSpeedBonus;
            AttackRangeMultiplier += card.AttackRangeBonus;
            HitInvulnerabilityMultiplier += card.HitInvulnerabilityBonus;
            WaveHeal += card.WaveHeal;
            ProjectileCount += card.ExtraProjectiles;
            ProjectileDamageMultiplier *= card.ProjectileDamageRatio;
            ProjectileSpreadDegrees = Mathf.Max(ProjectileSpreadDegrees, card.ProjectileSpread);
            ProjectileHitLimit += card.ExtraPierces;
            ExplosionRadius = Mathf.Max(ExplosionRadius, card.ExplosionRadius);
            ExplosionDamageRatio = Mathf.Max(ExplosionDamageRatio, card.ExplosionDamageRatio);
            if (card.HealthBonusRatio > 0f && health != null && NetworkServer.active)
                health.ServerApplyMaxHealthBonus(MaxHealthBonus, MaxHealthBonus - oldHealthBonus);
            Changed?.Invoke();
            return true;
        }

        public void ResetRun()
        {
            Array.Clear(_stacks, 0, _stacks.Length);
            DamageMultiplier = AttackSpeedMultiplier = DashCooldownMultiplier = 1f;
            MoveSpeedMultiplier = AttackRangeMultiplier = HitInvulnerabilityMultiplier = 1f;
            ProjectileCount = ProjectileHitLimit = 1;
            ProjectileDamageMultiplier = 1f;
            _healthRatio = ProjectileSpreadDegrees = ExplosionRadius = ExplosionDamageRatio = 0f;
            WaveHeal = 0;
            Changed?.Invoke();
        }
    }
}
