using System;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Cards
{
    /// <summary>Chỉ số nâng cấp của một người chơi trong đúng một ván.</summary>
    public sealed class PlayerUpgradeState : MonoBehaviour
    {
        private readonly int[] _stacks = new int[7];

        public event Action Changed;

        public float DamageMultiplier => 1f + 0.2f * GetStacks(CardId.CuongCong);
        public float AttackSpeedMultiplier => 1f + 0.15f * GetStacks(CardId.LienKich);
        public int MaxHealthBonus => 25 * GetStacks(CardId.SinhLuc);
        public float DashCooldownMultiplier => GetStacks(CardId.BoPhap) > 0 ? 0.8f : 1f;
        public int ProjectileCount => GetStacks(CardId.SongTien) > 0 ? 2 : 1;
        public float ProjectileDamageMultiplier => ProjectileCount > 1 ? 0.7f : 1f;
        public int ProjectileHitLimit => GetStacks(CardId.XuyenTam) > 0 ? 3 : 1;
        public bool Explodes => GetStacks(CardId.BocPha) > 0;
        public float ProjectileSpreadDegrees { get; private set; } = 7f;
        public float ExplosionRadius { get; private set; } = 1.75f;
        public float ExplosionDamageRatio { get; private set; } = 0.3f;

        public int GetStacks(CardId id) => _stacks[(int)id];

        public void ConfigureProjectileEffects(float spreadDegrees, float explosionRadius)
        {
            ProjectileSpreadDegrees = Mathf.Max(spreadDegrees, 0f);
            ExplosionRadius = Mathf.Max(explosionRadius, 0f);
        }

        public int DamageFromBase(int baseDamage, bool projectile)
        {
            float value = baseDamage * DamageMultiplier;
            if (projectile) value *= ProjectileDamageMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        public float AttackIntervalFromBase(float baseInterval) =>
            baseInterval / AttackSpeedMultiplier;

        public bool Apply(CardDefinition card, PlayerHealth health)
        {
            if (card == null || GetStacks(card.Id) >= card.MaxStacks) return false;
            _stacks[(int)card.Id]++;

            if (card.Id == CardId.SinhLuc && health != null && NetworkServer.active)
                health.ServerApplyMaxHealthBonus(MaxHealthBonus, 25);

            Changed?.Invoke();
            return true;
        }

        public void ResetRun()
        {
            Array.Clear(_stacks, 0, _stacks.Length);
            Changed?.Invoke();
        }
    }
}
