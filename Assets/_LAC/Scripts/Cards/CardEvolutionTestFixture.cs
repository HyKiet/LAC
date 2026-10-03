#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace LAC.Cards
{
    // Chỉ cài khi được yêu cầu rõ ràng; không có công thức thử trong Resources/build release.
    public static class CardEvolutionTestFixture
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallForClient()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-evolution-test-client") >= 0) Install();
        }

        public static CardEvolutionData[] Install()
        {
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            CardDefinition power = Array.Find(cards, c => c.Id == CardId.CuongCong);
            CardDefinition speed = Array.Find(cards, c => c.Id == CardId.LienKich);
            var damage = ScriptableObject.CreateInstance<CardDefinition>();
            damage.EditorConfigureEffects(damage: .1f);
            var range = ScriptableObject.CreateInstance<CardDefinition>();
            range.EditorConfigureEffects(range: .1f);
            var first = ScriptableObject.CreateInstance<CardEvolutionData>();
            first.EditorConfigure("test-damage", "CỘNG HƯỞNG — KIỂM THỬ", "+10% sát thương gốc. Công thức thử T-25, không phải nội dung chính thức.", damage,
                new CardEvolutionData.Ingredient(power, 1), new CardEvolutionData.Ingredient(speed, 1));
            var second = ScriptableObject.CreateInstance<CardEvolutionData>();
            second.EditorConfigure("test-range", "DƯ ÂM — KIỂM THỬ", "+10% tầm đánh gốc. Giữ nguyên hiệu ứng các thẻ nền đã nhận.", range,
                new CardEvolutionData.Ingredient(power, 1), new CardEvolutionData.Ingredient(speed, 1));
            var recipes = new[] { first, second };
            CardEvolutionCatalog.EditorSetTestRecipes(recipes);
            return recipes;
        }

        public static void Remove(CardEvolutionData[] recipes)
        {
            CardEvolutionCatalog.EditorSetTestRecipes(null);
            foreach (var recipe in recipes)
            {
                if (Application.isPlaying) { UnityEngine.Object.Destroy(recipe.Bonus); UnityEngine.Object.Destroy(recipe); }
                else { UnityEngine.Object.DestroyImmediate(recipe.Bonus); UnityEngine.Object.DestroyImmediate(recipe); }
            }
        }
    }
}
#endif
