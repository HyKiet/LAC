using System;
using System.Collections.Generic;
using LAC.Combat;
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
        private CardUpgradePreview _modifiers = new CardUpgradePreview(0);
        private bool _replayingCards;
        private readonly List<CardEvolutionData> _evolutions = new List<CardEvolutionData>();
        public IReadOnlyList<CardEvolutionData> Evolutions => _evolutions;
        public bool HasEvolution(string id)
        {
            foreach (CardEvolutionData recipe in _evolutions) if (recipe.Id == id) return true;
            return false;
        }
        public event Action Changed;
        public CharacterData CharacterData => _character != null ? _character.Data : null;
        public float DamageMultiplier => _modifiers.DamageMultiplier;
        public float AttackSpeedMultiplier => _modifiers.AttackSpeedMultiplier;
        public float DashCooldownMultiplier => _modifiers.DashCooldownMultiplier;
        public float MoveSpeedMultiplier => _modifiers.MoveSpeedMultiplier;
        public float AttackRangeMultiplier => _modifiers.AttackRangeMultiplier;
        public float HitInvulnerabilityMultiplier => _modifiers.HitInvulnerabilityMultiplier;
        public int WaveHeal => _modifiers.WaveHeal;
        public int ProjectileCount => _modifiers.ProjectileCount;
        public float ProjectileDamageMultiplier => _modifiers.ProjectileDamageMultiplier;
        public int ProjectileHitLimit => _modifiers.ProjectileHitLimit;
        public bool Explodes => ExplosionRadius > 0f && ExplosionDamageRatio > 0f;
        public float ProjectileSpreadDegrees => _modifiers.ProjectileSpreadDegrees;
        public float ExplosionRadius => _modifiers.ExplosionRadius;
        public float ExplosionDamageRatio => _modifiers.ExplosionDamageRatio;
        public int MaxHealthBonus => MaxHealthBonusFromBase(_character != null && _character.Data != null
            ? _character.Data.MaxHealth : 0);

        private void Awake() => _character = GetComponent<PlayerCharacter>();
        public int GetStacks(CardId id) => (uint)(int)id < _stacks.Length ? _stacks[(int)id] : 0;
        public int MaxHealthBonusFromBase(int baseHealth) => _modifiers.MaxHealthBonusFromBase(baseHealth);
        public float DamageFromBase(int baseDamage, bool projectile) =>
            baseDamage * DamageMultiplier * (projectile ? ProjectileDamageMultiplier : 1f);
        public float AttackIntervalFromBase(float baseInterval) => baseInterval / Mathf.Max(.1f, AttackSpeedMultiplier);

        public bool Apply(CardDefinition card, PlayerHealth health)
        {
            if (card == null || (uint)(int)card.Id >= _stacks.Length || GetStacks(card.Id) >= card.MaxStacks) return false;
            int oldHealthBonus = MaxHealthBonus;
            _stacks[(int)card.Id]++;
            _modifiers.ApplyEffects(card, card.StatScaleAtStack(GetStacks(card.Id)));
            WeaponShape shape = _character != null && _character.Data != null ? _character.Data.WeaponShape : WeaponShape.Circle;
            foreach (CardEvolutionData recipe in CardEvolutionCatalog.Recipes)
            {
                if (!recipe.IsReady(this, shape)) continue;
                _evolutions.Add(recipe);
                _modifiers.ApplyEffects(recipe.Bonus, 1f);
            }
            if (MaxHealthBonus != oldHealthBonus && health != null && NetworkServer.active)
                health.ServerApplyMaxHealthBonus(MaxHealthBonus, Mathf.Max(0, MaxHealthBonus - oldHealthBonus));
            if (!_replayingCards) Changed?.Invoke();
            return true;
        }

        // Client dựng lại cùng lịch sử nhưng HUD chỉ được thấy trạng thái cuối, không thấy reset tạm.
        internal void ReplayCards(IReadOnlyList<CardDefinition> history)
        {
            bool previousReplay = _replayingCards;
            _replayingCards = true;
            try
            {
                ResetRun();
                if (history != null)
                    for (int i = 0; i < history.Count; i++) Apply(history[i], null);
            }
            finally { _replayingCards = previousReplay; }
            if (!_replayingCards) Changed?.Invoke();
        }

        public CardUpgradePreview ProjectCard(CardDefinition card)
        {
            var preview = new CardUpgradePreview(_modifiers, CharacterData != null ? CharacterData.MaxHealth : 0);
            if (card == null || (uint)(int)card.Id >= _stacks.Length || GetStacks(card.Id) >= card.MaxStacks)
                return preview;
            preview.ApplyEffects(card, card.StatScaleAtStack(GetStacks(card.Id) + 1));
            WeaponShape shape = CharacterData != null ? CharacterData.WeaponShape : WeaponShape.Circle;
            foreach (var recipe in CardEvolutionCatalog.Recipes)
            {
                if (HasEvolution(recipe.Id) || recipe.Bonus == null || !recipe.Bonus.Supports(shape)) continue;
                bool ready = recipe.Ingredients.Count >= 2;
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Card == null || ingredient.Stacks < 1 || !ingredient.Card.Supports(shape)
                        || GetStacks(ingredient.Card.Id) + (ingredient.Card.Id == card.Id ? 1 : 0) < ingredient.Stacks)
                    { ready = false; break; }
                }
                if (ready) preview.AddEvolution(recipe);
            }
            return preview;
        }

        public void ResetRun()
        {
            Array.Clear(_stacks, 0, _stacks.Length);
            _evolutions.Clear();
            _modifiers = new CardUpgradePreview(0);
            if (!_replayingCards) Changed?.Invoke();
        }
    }
}
