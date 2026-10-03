using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAC.Cards
{
    public static class CardEvolutionCatalog
    {
        private static CardEvolutionData[] _recipes;
        public static IReadOnlyList<CardEvolutionData> Recipes => _recipes ??= Load();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _recipes = null;

        private static CardEvolutionData[] Load()
        {
            CardEvolutionData[] recipes = Resources.LoadAll<CardEvolutionData>("Evolutions");
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardEvolutionData recipe in recipes)
            {
                if (!recipe.Validate(cards, out string error) || !ids.Add(recipe.Id))
                {
                    // Cùng từ chối cả danh mục; không phụ thuộc thứ tự Resources.LoadAll trên từng máy.
                    Debug.LogError($"[Evolution] Danh mục không hợp lệ: {recipe.name}: {error ?? "ID trùng"}");
                    return Array.Empty<CardEvolutionData>();
                }
            }
            Array.Sort(recipes, (a, b) => string.CompareOrdinal(a.Id, b.Id));
            return recipes;
        }

        public static CardEvolutionData Find(string id)
        {
            foreach (CardEvolutionData recipe in Recipes) if (recipe.Id == id) return recipe;
            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Fixture trong bộ nhớ, không ghi vào asset và không đi vào bản phát hành.
        public static void EditorSetTestRecipes(CardEvolutionData[] recipes) => _recipes = recipes;
#endif
    }
}
