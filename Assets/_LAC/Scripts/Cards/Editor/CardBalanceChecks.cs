#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LAC.Combat;
using LAC.Core;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardBalanceChecks
    {
        [MenuItem("LAC/Tests/Validate 12 Card Balance (Edit Mode)")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi chạy mô phỏng seed.");
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            Require(cards.Length == 12 && cards.Select(c => c.Id).Distinct().Count() == 12, "Danh mục không đủ 12 ID duy nhất.");
            var originals = cards.Select(EditorJsonUtility.ToJson).ToArray();
            var go = new GameObject("CardBalanceChecks") { hideFlags = HideFlags.HideAndDontSave };
            // Bài hồi quy T-24 đo riêng thẻ nền; T-26 kiểm cả danh mục tiến hoá.
            CardEvolutionCatalog.EditorSetTestRecipes(Array.Empty<CardEvolutionData>());
            try
            {
                var state = go.AddComponent<PlayerUpgradeState>();
                foreach (WeaponShape shape in Enum.GetValues(typeof(WeaponShape)))
                {
                    var supported = cards.Where(c => c.Supports(shape)).ToArray();
                    // Sau 14 lựa chọn vẫn phải còn >=3 ID, ngay cả khi cố vét thẻ ít cấp nhất.
                    Require(supported.OrderBy(c => c.MaxStacks).Take(supported.Length - 2).Sum(c => c.MaxStacks) > 14,
                        "Có thể cạn còn dưới 3 lựa chọn trước lượt 15: " + shape);
                    var seen = new HashSet<CardId>();
                    for (int seed = 1; seed <= 1000; seed++)
                    {
                        state.ResetRun();
                        RunRandom.Initialize(seed);
                        for (int round = 0; round < 15; round++)
                        {
                            var offer = CardDeck.Draw(cards, state, 3, shape: shape);
                            CheckOffer(offer, state, shape);
                            if (round < 2)
                            {
                                var previous = new HashSet<CardId>(offer.Select(c => c.Id));
                                int alternatives = supported.Count(c => state.GetStacks(c.Id) < c.MaxStacks && !previous.Contains(c.Id));
                                offer = CardDeck.Draw(cards, state, 3, previous, shape);
                                CheckOffer(offer, state, shape);
                                if (alternatives >= 3) Require(offer.All(c => !previous.Contains(c.Id)), "Đổi thẻ lặp dù có đủ thay thế.");
                            }
                            var chosen = offer[(seed + round) % offer.Count];
                            seen.Add(chosen.Id);
                            Require(state.Apply(chosen, null), "Không áp dụng được thẻ hợp lệ.");
                        }
                    }
                    Require(seen.Count == supported.Length, "Có thẻ không bao giờ xuất hiện.");
                    Debug.Log($"[CardBalance] PASS {shape}: 1000 seeds x 15 picks, pool={supported.Length}, capacity={supported.Sum(c => c.MaxStacks)}, coverage={seen.Count}.");
                }

                state.ResetRun();
                foreach (var card in cards)
                {
                    for (int i = 0; i < card.MaxStacks; i++) Require(state.Apply(card, null), "Thiếu cấp.");
                    Require(!state.Apply(card, null), "Vượt giới hạn cộng dồn.");
                }
                Near(state.DamageMultiplier, 2.1f);
                Near(state.AttackSpeedMultiplier, 1.29f);
                Near(state.DamageMultiplier * state.AttackSpeedMultiplier * state.ProjectileCount * state.ProjectileDamageMultiplier, 3.7926f);
                Near(state.MoveSpeedMultiplier, 1.24f);
                Near(state.AttackRangeMultiplier, 1.3f);
                Near(state.HitInvulnerabilityMultiplier, 1.3f);
                Near(state.DashCooldownMultiplier, .8f);
                Require(state.WaveHeal == 2 && state.ProjectileCount == 2 && state.ProjectileHitLimit == 3, "Sai trần hiệu ứng.");
                Require(state.MaxHealthBonusFromBase(4) == 3 && state.MaxHealthBonusFromBase(6) == 4
                    && state.MaxHealthBonusFromBase(10) == 6, "Máu không theo chỉ số gốc.");
                state.ResetRun();
                state.Apply(cards.Single(c => c.Id == CardId.SongTien), null);
                Near(state.DamageFromBase(1, true), .7f);
                state.Apply(cards.Single(c => c.Id == CardId.BocPha), null);
                Near(state.DamageFromBase(1, true) * state.ExplosionDamageRatio, .21f);
                // Sát thương phần lẻ phải thực sự được giữ tới nơi trừ máu.
                var enemy = go.AddComponent<LAC.Enemies.Enemy>();
                typeof(LAC.Enemies.Enemy).GetField("_health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(enemy, 10f);
                enemy.ApplyDamage(.7f);
                Near(enemy.Health, 9.3f);
                state.ResetRun();
                Require(state.WaveHeal == 0 && !state.Explodes && state.ProjectileCount == 1, "Reset còn hiệu ứng.");
                Near(state.MoveSpeedMultiplier, 1f); Near(state.AttackRangeMultiplier, 1f);
                for (int i = 0; i < cards.Length; i++) Require(originals[i] == EditorJsonUtility.ToJson(cards[i]), "Đã ghi đè asset.");
                Debug.Log("[CardBalance] ALL PASSED: 45,000 picks, applicability, caps, fractional damage, health scaling, reset, immutable assets.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); CardEvolutionCatalog.EditorSetTestRecipes(null); }
            CardImpactBalanceChecks.Run();
            ValidateCuongNoMarginals(cards);
        }

        // Các thẻ khác không đổi damage/tốc đánh; thử mọi cấp của các nguyên liệu
        // liên quan và cả tiến hoá thật, không chỉ một build điển hình.
        private static void ValidateCuongNoMarginals(CardDefinition[] cards)
        {
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var go = new GameObject("CuongNoMarginalChecks") { hideFlags = HideFlags.HideAndDontSave };
            var character = go.AddComponent<PlayerCharacter>();
            var state = go.GetComponent<PlayerUpgradeState>() ?? go.AddComponent<PlayerUpgradeState>();
            typeof(PlayerUpgradeState).GetField("_character", fields).SetValue(state, character);
            var characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            int checkedCount = 0;
            float minimum = float.PositiveInfinity;
            try
            {
                Require(characters.Length == 3, "Phải kiểm cả ba nhân vật.");
                Require(CardEvolutionCatalog.Recipes.Count == 8, "Phải dùng danh mục tiến hoá thật.");
                foreach (var data in characters)
                {
                    typeof(PlayerCharacter).GetField("_data", fields).SetValue(character, data);
                    for (int damage = 0; damage <= 3; damage++)
                    for (int protection = 0; protection <= 2; protection++)
                    for (int range = 0; range <= 3; range++)
                    for (int speed = 0; speed <= 3; speed++)
                    for (int rage = 0; rage < 2; rage++)
                    {
                        Require(damage + protection + range + speed + rage + 1 <= 15, "Build vượt 15 lượt.");
                        state.ResetRun();
                        Grant(CardId.CuongCong, damage); Grant(CardId.ThietBich, protection);
                        Grant(CardId.AmVang, range); Grant(CardId.LienKich, speed); Grant(CardId.CuongNo, rage);
                        float before = state.DamageMultiplier * state.AttackSpeedMultiplier;
                        Grant(CardId.CuongNo, 1);
                        float delta = state.DamageMultiplier * state.AttackSpeedMultiplier - before;
                        Require(delta > .0001f, $"Cuồng Nộ giảm DPS: {data.name}, C{damage}/T{protection}/A{range}/L{speed}/N{rage}, delta={delta}.");
                        minimum = Mathf.Min(minimum, delta);
                        checkedCount++;
                    }
                    // Cấp đầu nặng hơn nhưng cấp hai vẫn phải tăng DPS, không thành bẫy.
                    state.ResetRun(); Grant(CardId.CuongCong, 3); Grant(CardId.ThietBich, 2);
                    Require(state.HasEvolution("ThanhGiong"), "Thiếu tiến hoá trong build hồi quy.");
                    Grant(CardId.CuongNo, 1); Near(state.DamageMultiplier * state.AttackSpeedMultiplier, 2.09f);
                    Grant(CardId.CuongNo, 1); Near(state.DamageMultiplier * state.AttackSpeedMultiplier, 2.1f);
                }
                Require(checkedCount == 1152, "Thiếu tổ hợp cấp cần kiểm tra.");
                Near(minimum, .01f);
                Debug.Log($"[CardBalance] CuongNo marginal PASS: {checkedCount} real-state cases / 3 characters / 8 recipes; min gain={minimum:F4} baseline DPS; evolved trap 2.090→2.100.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); CardEvolutionCatalog.EditorSetTestRecipes(null); }

            void Grant(CardId id, int count)
            {
                CardDefinition card = cards.Single(c => c.Id == id);
                for (int i = 0; i < count; i++) Require(state.Apply(card, null), "Cấp không hợp lệ: " + id);
            }
        }

        private static void CheckOffer(List<CardDefinition> offer, PlayerUpgradeState state, WeaponShape shape)
        {
            Require(offer.Count == 3 && offer.Select(c => c.Id).Distinct().Count() == 3, "Thiếu hoặc trùng lựa chọn.");
            Require(offer.All(c => c.Supports(shape) && state.GetStacks(c.Id) < c.MaxStacks), "Đề nghị thẻ không dùng được.");
        }
        private static void Near(float actual, float expected) => Require(Mathf.Abs(actual - expected) < .0001f, $"{actual} != {expected}");
        private static void Require(bool valid, string reason) { if (!valid) throw new InvalidOperationException("[CardBalance] " + reason); }
    }
}
#endif
