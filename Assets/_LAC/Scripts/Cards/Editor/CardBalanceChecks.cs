#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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
                Near(state.AttackSpeedMultiplier, 1.25f);
                Near(state.DamageMultiplier * state.AttackSpeedMultiplier * state.ProjectileCount * state.ProjectileDamageMultiplier, 3.675f);
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
            finally { UnityEngine.Object.DestroyImmediate(go); }
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
