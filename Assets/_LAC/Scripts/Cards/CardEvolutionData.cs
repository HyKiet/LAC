using System;
using System.Collections.Generic;
using LAC.Combat;
using UnityEngine;

namespace LAC.Cards
{
    [CreateAssetMenu(fileName = "Evolution", menuName = "LAC/Cards/Evolution Recipe")]
    public sealed class CardEvolutionData : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            [SerializeField] private CardDefinition _card;
            [SerializeField, Min(1)] private int _stacks;
            public CardDefinition Card => _card;
            public int Stacks => _stacks;
            public Ingredient(CardDefinition card, int stacks) { _card = card; _stacks = stacks; }
        }

        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 5)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Ingredient[] _ingredients = Array.Empty<Ingredient>();
        // Chỉ phần thưởng thêm, không lặp lại hiệu ứng nguyên liệu. Đặt ngoài Resources/Cards.
        [SerializeField] private CardDefinition _bonus;
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public CardDefinition Bonus => _bonus;
        public IReadOnlyList<Ingredient> Ingredients => _ingredients;

        public bool Validate(IReadOnlyList<CardDefinition> cards, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(_id) || _id != _id.Trim() || string.IsNullOrWhiteSpace(_displayName))
                error = "Thiếu ID/tên hoặc ID có khoảng trắng ở đầu/cuối.";
            else if (_ingredients == null || _ingredients.Length < 2) error = "Cần ít nhất hai loại nguyên liệu.";
            else if (_bonus == null) error = "Thiếu dữ liệu phần thưởng tiến hoá.";
            else if (cards == null) error = "Thiếu danh mục thẻ nền.";
            if (error != null) return false;
            var ids = new HashSet<CardId>();
            foreach (Ingredient ingredient in _ingredients)
            {
                if (ingredient.Card == null || ingredient.Stacks < 1 || ingredient.Stacks > ingredient.Card.MaxStacks
                    || !ids.Add(ingredient.Card.Id))
                { error = "Nguyên liệu trùng, thiếu hoặc vượt giới hạn cộng dồn."; return false; }
                bool found = false;
                foreach (CardDefinition card in cards) if (card == ingredient.Card) found = true;
                if (!found) { error = "Nguyên liệu không thuộc bộ thẻ nền."; return false; }
            }
            foreach (CardDefinition card in cards)
                if (card == _bonus) { error = "Phần thưởng không được nằm trong bể thẻ nền."; return false; }
            return true;
        }

        public bool IsReady(PlayerUpgradeState state, WeaponShape shape)
        {
            if (state == null || state.HasEvolution(Id) || _bonus == null || !_bonus.Supports(shape)
                || _ingredients == null || _ingredients.Length < 2) return false;
            foreach (Ingredient ingredient in _ingredients)
                if (ingredient.Card == null || ingredient.Stacks < 1 || !ingredient.Card.Supports(shape)
                    || state.GetStacks(ingredient.Card.Id) < ingredient.Stacks) return false;
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void EditorConfigure(string id, string displayName, string description,
            CardDefinition bonus, params Ingredient[] ingredients)
        { _id = id; _displayName = displayName; _description = description; _bonus = bonus; _ingredients = ingredients; }
#endif
    }
}
