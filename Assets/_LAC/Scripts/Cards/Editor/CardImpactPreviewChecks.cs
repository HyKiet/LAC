#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LAC.Combat;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardImpactPreviewChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("LAC/Tests/Validate Card Impact Preview (Edit Mode)")]
        public static void Validate()
        {
            Require(!Application.isPlaying, "Thoát Play Mode trước khi kiểm bản xem trước.");
            CardEvolutionCatalog.EditorSetTestRecipes(null);
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var recipes = CardEvolutionCatalog.Recipes.ToArray();
            var characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Require(cards.Length == 12 && recipes.Length == 8 && characters.Length == 3, "Thiếu danh mục thật.");
            var assets = cards.Cast<UnityEngine.Object>().Concat(recipes)
                .Concat(recipes.Select(r => r.Bonus)).Concat(characters).ToArray();
            var originals = assets.Select(EditorJsonUtility.ToJson).ToArray();
            var go = new GameObject("CardImpactPreviewChecks") { hideFlags = HideFlags.HideAndDontSave };
            var invalid = ScriptableObject.CreateInstance<CardDefinition>();
            invalid.hideFlags = HideFlags.HideAndDontSave;
            invalid.EditorConfigure((CardId)999, "Invalid", "", 1, 1f, Color.white);
            int checkedChoices = 0;
            try
            {
                var character = go.AddComponent<PlayerCharacter>();
                var state = go.GetComponent<PlayerUpgradeState>() ?? go.AddComponent<PlayerUpgradeState>();
                Set(state, "_character", character);
                foreach (CharacterData data in characters)
                {
                    Set(character, "_data", data);
                    foreach (CardDefinition card in cards)
                    {
                        if (!card.Supports(data.WeaponShape)) continue;
                        state.ResetRun();
                        for (int rank = 0; rank < card.MaxStacks; rank++) CheckNext(card, true);
                        CheckNext(card, false);
                    }
                    foreach (CardEvolutionData recipe in recipes)
                    {
                        if (!recipe.Bonus.Supports(data.WeaponShape)
                            || recipe.Ingredients.Any(i => !i.Card.Supports(data.WeaponShape))) continue;
                        var history = recipe.Ingredients.SelectMany(i => Enumerable.Repeat(i.Card, i.Stacks)).ToArray();
                        foreach (bool reverse in new[] { false, true })
                        {
                            state.ResetRun();
                            if (reverse) Array.Reverse(history);
                            for (int i = 0; i < history.Length - 1; i++) state.Apply(history[i], null);
                            CardUpgradePreview preview = state.ProjectCard(history.Last());
                            Require(preview.NewEvolutions.Any(r => r.Id == recipe.Id), "Không báo tiến hoá kế tiếp: " + recipe.Id);
                            CheckNext(history.Last(), true);
                        }
                    }
                    CheckNext(null, false);
                    CheckNext(invalid, false);
                }

                Set(character, "_data", characters.Single(c => c.WeaponShape == WeaponShape.Arc));
                state.ResetRun();
                Grant(CardId.CuongCong, 3); Grant(CardId.SinhLuc, 2); Grant(CardId.ThietBich, 1);
                CardDefinition protection = cards.Single(c => c.Id == CardId.ThietBich);
                var simultaneous = state.ProjectCard(protection);
                Require(simultaneous.NewEvolutions.Count == 2
                    && simultaneous.NewEvolutions.Any(r => r.Id == "ThanhGiong")
                    && simultaneous.NewEvolutions.Any(r => r.Id == "KimCang"), "Thiếu tiến hoá đồng thời.");
                string compound = CardImpactPreview.Metrics(protection, state, simultaneous);
                Require(compound.Contains("Thánh Gióng") && compound.Contains("Kim Cang"), "Footer thiếu tên tiến hoá đồng thời.");
                CheckNext(protection, true);

                Set(character, "_data", null);
                state.ResetRun();
                foreach (CardDefinition card in cards)
                {
                    string text = CardImpactPreview.Metrics(card, state, state.ProjectCard(card));
                    Require(!string.IsNullOrEmpty(text) && !text.Contains("NaN") && !text.Contains("Infinity"), "Fallback thiếu dữ liệu: " + card.Id);
                    if (card.Id == CardId.SinhLuc) Require(text.Contains("máu gốc"), "Không được hiển thị máu thật khi chưa có nhân vật.");
                    if (card.Id == CardId.CuongCong) Require(text.Contains("Hệ số"), "Fallback sát thương phải ghi rõ hệ số.");
                }
                for (int i = 0; i < assets.Length; i++)
                    Require(originals[i] == EditorJsonUtility.ToJson(assets[i]), "Xem trước sửa asset: " + assets[i].name);
                Debug.Log($"[CardImpactPreview] ALL PASSED: {checkedChoices} choices, all next ranks / 3 characters / 8 evolution thresholds in both orders; simultaneous evolutions; exact projected vs applied stats; repeated previews without Changed, state/health/asset mutation; capped/null/invalid and character-null fallback.");

                void Grant(CardId id, int count)
                {
                    CardDefinition card = cards.Single(c => c.Id == id);
                    for (int i = 0; i < count; i++) state.Apply(card, null);
                }

                void CheckNext(CardDefinition card, bool accepted)
                {
                    string before = StateSignature(state);
                    string oldEvolutions = string.Join(",", state.Evolutions.Select(r => r.Id));
                    int notifications = 0;
                    Action notification = () => notifications++;
                    state.Changed += notification;
                    CardUpgradePreview preview = null;
                    try
                    {
                        for (int repeat = 0; repeat < 10; repeat++)
                        {
                            preview = state.ProjectCard(card);
                            if (card != null && accepted)
                            {
                                Require(CardImpactPreview.Description(card, state).Length > 0, "Thiếu mô tả: " + card.Id);
                                Require(CardImpactPreview.Metrics(card, state, preview).Contains("→"), "Thiếu trước → sau: " + card.Id);
                            }
                            Require(before == StateSignature(state) && notifications == 0,
                                "Xem trước thay đổi state/sự kiện: " + (card != null ? card.Id.ToString() : "null"));
                        }
                        Require(state.Apply(card, null) == accepted, "Sai chấp nhận thẻ: " + (card != null ? card.Id.ToString() : "null"));
                        Require(notifications == (accepted ? 1 : 0), "Apply phát sai sự kiện.");
                        Require(Stats(state) == Stats(preview), "Xem trước lệch kết quả Apply: " + (card != null ? card.Id.ToString() : "null"));
                        var oldIds = oldEvolutions.Split(',');
                        var actualNew = state.Evolutions.Where(r => !oldIds.Contains(r.Id)).Select(r => r.Id);
                        Require(string.Join(",", actualNew) == string.Join(",", preview.NewEvolutions.Select(r => r.Id)),
                            "Danh sách tiến hoá xem trước khác Apply.");
                        checkedChoices++;
                    }
                    finally { state.Changed -= notification; }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(invalid); }
        }

        private static string StateSignature(PlayerUpgradeState state)
        {
            var result = new StringBuilder(Stats(state));
            foreach (CardId id in Enum.GetValues(typeof(CardId))) result.Append('/').Append(state.GetStacks(id));
            foreach (var recipe in state.Evolutions) result.Append('/').Append(recipe.Id);
            return result.ToString();
        }

        private static string Stats(object state)
        {
            string[] names = { "DamageMultiplier", "AttackSpeedMultiplier", "DashCooldownMultiplier", "MoveSpeedMultiplier",
                "AttackRangeMultiplier", "HitInvulnerabilityMultiplier", "WaveHeal", "ProjectileCount", "ProjectileDamageMultiplier",
                "ProjectileHitLimit", "ProjectileSpreadDegrees", "ExplosionRadius", "ExplosionDamageRatio", "MaxHealthBonus" };
            return string.Join("/", names.Select(name => Convert.ToString(state.GetType().GetProperty(name).GetValue(state), CultureInfo.InvariantCulture)));
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[CardImpactPreview] " + message);
        }
    }
}
#endif
