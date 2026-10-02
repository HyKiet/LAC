#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    public static class CardHistoryReplayChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("LAC/Tests/Validate Atomic Card History Replay (Edit Mode)")]
        public static void Validate()
        {
            Require(!Application.isPlaying, "Thoát Play Mode trước khi kiểm replay dữ liệu.");
            CardEvolutionCatalog.EditorSetTestRecipes(null);
            CardDefinition[] cards = Resources.LoadAll<CardDefinition>("Cards");
            CardEvolutionData[] recipes = CardEvolutionCatalog.Recipes.ToArray();
            CharacterData[] characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Require(cards.Length == 12 && recipes.Length == 8 && characters.Length == 3,
                "Cần danh mục thật gồm 12 thẻ, 8 tiến hoá và 3 nhân vật.");
            UnityEngine.Object[] assets = cards.Cast<UnityEngine.Object>().Concat(recipes)
                .Concat(recipes.Select(r => r.Bonus)).Concat(characters).ToArray();
            string[] originals = assets.Select(EditorJsonUtility.ToJson).ToArray();
            var baselineGo = new GameObject("CardHistoryBaseline") { hideFlags = HideFlags.HideAndDontSave };
            var replayGo = new GameObject("CardHistoryReplay") { hideFlags = HideFlags.HideAndDontSave };
            var invalidCard = ScriptableObject.CreateInstance<CardDefinition>();
            invalidCard.hideFlags = HideFlags.HideAndDontSave;
            invalidCard.EditorConfigure((CardId)999, "Fixture", "", 1, 1f, Color.white);
            int checkedHistories = 0;
            int checkedEvents = 0;
            try
            {
                PlayerCharacter baselineCharacter = baselineGo.AddComponent<PlayerCharacter>();
                PlayerUpgradeState baseline = baselineGo.GetComponent<PlayerUpgradeState>() ?? baselineGo.AddComponent<PlayerUpgradeState>();
                PlayerCharacter replayCharacter = replayGo.AddComponent<PlayerCharacter>();
                PlayerUpgradeState replay = replayGo.GetComponent<PlayerUpgradeState>() ?? replayGo.AddComponent<PlayerUpgradeState>();
                Set(baseline, "_character", baselineCharacter);
                Set(replay, "_character", replayCharacter);
                var replayCards = (Action<IReadOnlyList<CardDefinition>>)typeof(PlayerUpgradeState)
                    .GetMethod("ReplayCards", Private).CreateDelegate(typeof(Action<IReadOnlyList<CardDefinition>>), replay);
                foreach (CharacterData character in characters)
                {
                    Set(baselineCharacter, "_data", character);
                    Set(replayCharacter, "_data", character);
                    foreach (CardEvolutionData recipe in recipes)
                    {
                        var history = new List<CardDefinition>();
                        foreach (CardEvolutionData.Ingredient ingredient in recipe.Ingredients)
                            for (int i = 0; i < ingredient.Stacks; i++) history.Add(ingredient.Card);
                        CheckReplay(history, character.name + "/" + recipe.Id);
                        bool supported = recipe.Bonus.Supports(character.WeaponShape)
                            && recipe.Ingredients.All(i => i.Card.Supports(character.WeaponShape));
                        Require(replay.HasEvolution(recipe.Id) == supported, "Sai lọc tiến hoá: " + recipe.Id);
                        history.Reverse();
                        CheckReplay(history, character.name + "/reverse/" + recipe.Id);
                    }

                    var capped = new List<CardDefinition> { null, invalidCard };
                    foreach (CardDefinition card in cards)
                        for (int i = 0; i < card.MaxStacks + 2; i++) capped.Add(card);
                    capped.Add(null);
                    CheckReplay(capped, character.name + "/caps/null/invalid ID");
                    foreach (CardDefinition card in cards)
                        Require(replay.GetStacks(card.Id) == card.MaxStacks, "Replay vượt cấp: " + card.Id);
                    CheckReplay(Array.Empty<CardDefinition>(), character.name + "/empty");
                    CheckReplay(null, character.name + "/null clears");
                }

                CharacterData arc = characters.Single(c => c.WeaponShape == WeaponShape.Arc);
                Set(baselineCharacter, "_data", arc);
                Set(replayCharacter, "_data", arc);
                var simultaneous = new List<CardDefinition>();
                Add(CardId.CuongCong, 3); Add(CardId.SinhLuc, 2); Add(CardId.ThietBich, 2);
                CheckReplay(simultaneous, "simultaneous evolutions");
                Require(replay.HasEvolution("ThanhGiong") && replay.HasEvolution("KimCang")
                    && replay.Evolutions.Count == 2, "Thiếu tiến hoá đồng thời sau replay.");

                CardDefinition power = cards.Single(c => c.Id == CardId.CuongCong);
                int notifications = 0;
                Action countChanged = () => notifications++;
                replay.Changed += countChanged;
                try
                {
                    replay.ResetRun();
                    Require(notifications == 1, "ResetRun thường phải phát Changed.");
                    Require(replay.Apply(power, null) && notifications == 2, "Apply thường phải phát Changed.");
                    Require(!replay.Apply(null, null) && !replay.Apply(invalidCard, null) && notifications == 2,
                        "Apply bị từ chối không được phát Changed.");
                    bool threw = false;
                    try { replayCards(new ThrowingHistory(power)); }
                    catch (InvalidOperationException exception)
                    {
                        threw = exception.Message == ThrowingHistory.Message;
                    }
                    Require(threw && notifications == 2, "Replay lỗi phải truyền exception và không phát sự kiện thành công.");
                    replay.ResetRun();
                    Require(notifications == 3, "Exception làm kẹt suppression ở ResetRun.");
                    Require(replay.Apply(power, null) && notifications == 4, "Exception làm kẹt suppression ở Apply.");
                    replayCards(new[] { power });
                    Require(notifications == 5, "Replay sau exception không phục hồi Changed.");
                }
                finally { replay.Changed -= countChanged; }

                for (int i = 0; i < assets.Length; i++)
                    Require(originals[i] == EditorJsonUtility.ToJson(assets[i]), "Replay sửa asset: " + assets[i].name);
                Debug.Log($"[CardHistoryReplay] ALL PASSED: {checkedHistories} real-catalog histories / 3 characters / 8 evolutions; {checkedEvents} final-only events; repeats, reverse order, caps, empty/null, simultaneous, normal notifications, exception recovery, immutable assets.");

                void Add(CardId id, int count)
                {
                    CardDefinition card = cards.Single(c => c.Id == id);
                    for (int i = 0; i < count; i++) simultaneous.Add(card);
                }

                void CheckReplay(IReadOnlyList<CardDefinition> history, string label)
                {
                    baseline.ResetRun();
                    if (history != null)
                        for (int i = 0; i < history.Count; i++) baseline.Apply(history[i], null);
                    string expected = Signature(baseline);
                    // Replay phải thay thế cả một build đã có, không chỉ làm đúng trên state rỗng.
                    replay.Apply(cards.Single(c => c.Id == CardId.CuongCong), null);
                    int events = 0;
                    Action checkFinal = () =>
                    {
                        events++;
                        Require(Signature(replay) == expected, "Changed thấy trạng thái trung gian: " + label);
                    };
                    replay.Changed += checkFinal;
                    try
                    {
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            replayCards(history);
                            Require(events == attempt + 1, "Replay không phát đúng một Changed: " + label);
                            Require(Signature(replay) == expected, "Replay khác ResetRun + Apply tuần tự: " + label);
                        }
                        checkedHistories++;
                        checkedEvents += events;
                    }
                    finally { replay.Changed -= checkFinal; }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baselineGo);
                UnityEngine.Object.DestroyImmediate(replayGo);
                UnityEngine.Object.DestroyImmediate(invalidCard);
            }
        }

        private static string Signature(PlayerUpgradeState state)
        {
            var signature = new StringBuilder();
            foreach (CardId id in Enum.GetValues(typeof(CardId))) signature.Append(state.GetStacks(id)).Append(',');
            foreach (CardEvolutionData recipe in state.Evolutions) signature.Append(recipe.Id).Append(',');
            Append(state.DamageMultiplier); Append(state.AttackSpeedMultiplier); Append(state.DashCooldownMultiplier);
            Append(state.MoveSpeedMultiplier); Append(state.AttackRangeMultiplier); Append(state.HitInvulnerabilityMultiplier);
            signature.Append('|').Append(state.MaxHealthBonus).Append('|').Append(state.WaveHeal)
                .Append('|').Append(state.ProjectileCount).Append('|').Append(state.ProjectileHitLimit);
            Append(state.ProjectileDamageMultiplier); Append(state.ProjectileSpreadDegrees);
            Append(state.ExplosionRadius); Append(state.ExplosionDamageRatio);
            return signature.ToString();

            void Append(float value) => signature.Append('|').Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private sealed class ThrowingHistory : IReadOnlyList<CardDefinition>
        {
            internal const string Message = "Fixture replay failure";
            private readonly CardDefinition _first;
            internal ThrowingHistory(CardDefinition first) => _first = first;
            public int Count => 2;
            public CardDefinition this[int index] => index == 0 ? _first : throw new InvalidOperationException(Message);
            public IEnumerator<CardDefinition> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[CardHistoryReplay] " + message);
        }
    }
}
#endif
