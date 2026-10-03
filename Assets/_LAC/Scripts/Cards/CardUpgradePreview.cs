using System.Collections.Generic;
using UnityEngine;

namespace LAC.Cards
{
    // Dùng chung phép cộng hiệu ứng cho gameplay và bản xem trước độc lập, không sửa state/asset.
    public sealed class CardUpgradePreview
    {
        private readonly int _baseHealth;
        private float _healthRatio;
        private readonly List<CardEvolutionData> _newEvolutions = new List<CardEvolutionData>();
        public IReadOnlyList<CardEvolutionData> NewEvolutions => _newEvolutions;
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
        public float ProjectileSpreadDegrees { get; private set; }
        public float ExplosionRadius { get; private set; }
        public float ExplosionDamageRatio { get; private set; }
        public int MaxHealthBonus => MaxHealthBonusFromBase(_baseHealth);

        internal CardUpgradePreview(int baseHealth) => _baseHealth = baseHealth;

        internal CardUpgradePreview(CardUpgradePreview source, int baseHealth)
        {
            _baseHealth = baseHealth;
            _healthRatio = source._healthRatio;
            DamageMultiplier = source.DamageMultiplier;
            AttackSpeedMultiplier = source.AttackSpeedMultiplier;
            DashCooldownMultiplier = source.DashCooldownMultiplier;
            MoveSpeedMultiplier = source.MoveSpeedMultiplier;
            AttackRangeMultiplier = source.AttackRangeMultiplier;
            HitInvulnerabilityMultiplier = source.HitInvulnerabilityMultiplier;
            WaveHeal = source.WaveHeal;
            ProjectileCount = source.ProjectileCount;
            ProjectileDamageMultiplier = source.ProjectileDamageMultiplier;
            ProjectileHitLimit = source.ProjectileHitLimit;
            ProjectileSpreadDegrees = source.ProjectileSpreadDegrees;
            ExplosionRadius = source.ExplosionRadius;
            ExplosionDamageRatio = source.ExplosionDamageRatio;
        }

        internal int MaxHealthBonusFromBase(int baseHealth) => Mathf.CeilToInt(baseHealth * _healthRatio - .00001f);

        internal void ApplyEffects(CardDefinition card, float statScale)
        {
            DamageMultiplier += card.DamageBonus * statScale;
            AttackSpeedMultiplier += card.AttackSpeedBonus * statScale;
            _healthRatio += card.HealthBonusRatio * statScale;
            DashCooldownMultiplier -= card.DashCooldownReduction * statScale;
            MoveSpeedMultiplier += card.MoveSpeedBonus * statScale;
            AttackRangeMultiplier += card.AttackRangeBonus * statScale;
            HitInvulnerabilityMultiplier += card.HitInvulnerabilityBonus * statScale;
            WaveHeal += card.WaveHeal;
            ProjectileCount += card.ExtraProjectiles;
            ProjectileDamageMultiplier *= card.ProjectileDamageRatio;
            ProjectileSpreadDegrees = Mathf.Max(ProjectileSpreadDegrees, card.ProjectileSpread);
            ProjectileHitLimit += card.ExtraPierces;
            ExplosionRadius = Mathf.Max(ExplosionRadius, card.ExplosionRadius);
            ExplosionDamageRatio = Mathf.Max(ExplosionDamageRatio, card.ExplosionDamageRatio);
        }

        internal void AddEvolution(CardEvolutionData recipe)
        {
            _newEvolutions.Add(recipe);
            ApplyEffects(recipe.Bonus, 1f);
        }
    }
}
