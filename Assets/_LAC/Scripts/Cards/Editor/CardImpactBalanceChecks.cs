#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LAC.Combat;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    /// <summary>Đo tác động cấp đầu và giữ trần cũ; không dùng chính công thức runtime làm giá trị mong đợi.</summary>
    public static class CardImpactBalanceChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("LAC/Tests/Validate Frontloaded Card Impact (Edit Mode)")]
        public static void Run()
        {
            Require(!Application.isPlaying, "Thoát Play Mode trước khi kiểm đường cong.");
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Require(cards.Length == 12 && characters.Length == 3, "Thiếu danh mục thẻ/nhân vật thật.");
            var assets = cards.Cast<UnityEngine.Object>().Concat(characters).ToArray();
            var originals = assets.Select(EditorJsonUtility.ToJson).ToArray();
            var go = new GameObject("CardImpactBalanceChecks") { hideFlags = HideFlags.HideAndDontSave };
            CardEvolutionCatalog.EditorSetTestRecipes(Array.Empty<CardEvolutionData>());
            try
            {
                var character = go.AddComponent<PlayerCharacter>();
                var state = go.GetComponent<PlayerUpgradeState>() ?? go.AddComponent<PlayerUpgradeState>();
                var enemy = go.AddComponent<LAC.Enemies.Enemy>();
                Set(state, "_character", character);
                foreach (var data in characters)
                {
                    Set(character, "_data", data);
                    CheckCurve(CardId.CuongCong, s => s.DamageMultiplier, new[] { 1.3f, 1.45f, 1.6f });
                    CheckCurve(CardId.LienKich, s => s.AttackSpeedMultiplier, new[] { 1.225f, 1.3375f, 1.45f });
                    CheckCurve(CardId.KhinhThan, s => s.MoveSpeedMultiplier, new[] { 1.12f, 1.18f, 1.24f });
                    CheckCurve(CardId.AmVang, s => s.AttackRangeMultiplier, new[] { 1.15f, 1.225f, 1.3f });
                    CheckCurve(CardId.ThietBich, s => s.HitInvulnerabilityMultiplier, new[] { 1.225f, 1.3f });
                    CheckCurve(CardId.CuongNo, s => s.DamageMultiplier, new[] { 1.375f, 1.5f });
                    CheckCurve(CardId.CuongNo, s => s.AttackSpeedMultiplier, new[] { .88f, .84f });

                    // Kiểm cả đơn vị thật, không chỉ hệ số của sáu hướng nâng cấp.
                    state.ResetRun(); Grant(CardId.CuongCong, 1);
                    Near(state.DamageFromBase(data.BaseDamage, data.WeaponShape == WeaponShape.Line), data.BaseDamage * 1.3f);
                    int expectedBaseHits = data.Id == CharacterId.Giong ? 4 : data.Id == CharacterId.ThachSanh ? 5 : 10;
                    int expectedUpgradedHits = data.Id == CharacterId.Giong ? 3 : data.Id == CharacterId.ThachSanh ? 4 : 8;
                    Require(HitsToDefeat(10f, data.BaseDamage) == expectedBaseHits, "Mốc quái 10 máu gốc sai: " + data.Id);
                    Require(HitsToDefeat(10f, state.DamageFromBase(data.BaseDamage, false)) == expectedUpgradedHits,
                        "Cường Công cấp đầu không vượt mốc số đòn: " + data.Id);
                    Require(ApplyUntilDefeated(enemy, data.BaseDamage) == expectedBaseHits
                        && ApplyUntilDefeated(enemy, state.DamageFromBase(data.BaseDamage, false)) == expectedUpgradedHits,
                        "Mốc số đòn khác khi trừ máu quái thật: " + data.Id);

                    state.ResetRun(); Grant(CardId.LienKich, 1);
                    Near(state.AttackIntervalFromBase(data.AttackInterval), data.AttackInterval / 1.225f);
                    Require(state.AttackIntervalFromBase(data.AttackInterval) < data.AttackInterval / 1.15f,
                        "Liên Kích cấp đầu không tăng nhịp đánh so với đường cong cũ.");
                    state.ResetRun(); Grant(CardId.KhinhThan, 1);
                    Near(data.MoveSpeed * state.MoveSpeedMultiplier, data.MoveSpeed * 1.12f);
                    state.ResetRun(); Grant(CardId.AmVang, 1);
                    Near(data.AttackRange * state.AttackRangeMultiplier, data.AttackRange * 1.15f);
                    Near(state.AttackRangeMultiplier * state.AttackRangeMultiplier, 1.3225f);
                    state.ResetRun(); Grant(CardId.ThietBich, 1);
                    Near(.6f * state.HitInvulnerabilityMultiplier, .735f);
                    state.ResetRun(); Grant(CardId.CuongNo, 1);
                    Near(state.DamageMultiplier * state.AttackSpeedMultiplier, 1.21f);
                    Grant(CardId.CuongNo, 1); Near(state.DamageMultiplier * state.AttackSpeedMultiplier, 1.26f);

                    // Máu/hồi/lướt không được vô tình ăn đường cong mới.
                    state.ResetRun();
                    int[] expectedHealth = data.Id == CharacterId.Giong ? new[] { 2, 4, 6 }
                        : data.Id == CharacterId.ThachSanh ? new[] { 2, 3, 4 } : new[] { 1, 2, 3 };
                    for (int rank = 0; rank < expectedHealth.Length; rank++)
                    {
                        Grant(CardId.SinhLuc, 1);
                        Require(state.MaxHealthBonus == expectedHealth[rank], "Sinh Lực bị đổi cách làm tròn tổng: " + data.Id);
                        Require(state.WaveHeal == 0, "Sinh Lực vô tình tăng hồi đầu đợt.");
                    }
                    state.ResetRun(); Grant(CardId.HoiXuan, 1);
                    Require(state.WaveHeal == 1 && state.MaxHealthBonus == 0, "Hồi Xuân cấp đầu bị tăng sức mạnh.");
                    Grant(CardId.HoiXuan, 1);
                    Require(state.WaveHeal == 2 && state.MaxHealthBonus == 0, "Hồi Xuân vượt trần cũ.");
                    state.ResetRun(); Grant(CardId.BoPhap, 1);
                    Near(state.DashCooldownMultiplier, .8f); Near(data.DashCooldown * state.DashCooldownMultiplier, .32f);
                    Near(data.DashDuration, .15f);

                    state.ResetRun();
                    foreach (var card in cards)
                        for (int rank = 0; rank < card.MaxStacks; rank++) Require(state.Apply(card, null), "Thiếu cấp: " + card.Id);
                    Near(state.DamageMultiplier, 2.1f); Near(state.AttackSpeedMultiplier, 1.29f);
                    Near(state.DamageMultiplier * state.AttackSpeedMultiplier, 2.709f);
                    Near(state.DamageMultiplier * state.AttackSpeedMultiplier * state.ProjectileCount * state.ProjectileDamageMultiplier, 3.7926f);
                    Near(state.MoveSpeedMultiplier, 1.24f); Near(state.AttackRangeMultiplier, 1.3f);
                    Near(state.HitInvulnerabilityMultiplier, 1.3f); Near(state.DashCooldownMultiplier, .8f);
                    Require(state.MaxHealthBonus == expectedHealth.Last() && state.WaveHeal == 2,
                        "Trần phòng thủ/hồi thay đổi: " + data.Id);
                    Require(state.ProjectileCount == 2 && state.ProjectileHitLimit == 3, "Trần số đạn/xuyên thay đổi.");
                    Near(state.ProjectileDamageMultiplier, .7f); Near(state.ProjectileSpreadDegrees, 7f);
                    Near(state.ExplosionRadius, 1.75f); Near(state.ExplosionDamageRatio, .3f);
                    Require(state.Evolutions.Count == 0, "Fixture thẻ nền giữ tiến hoá thật.");
                    Debug.Log($"[CardImpact] PASS {data.Id}: first-rank/next-rank curves; 10 HP hits {expectedBaseHits}→{expectedUpgradedHits}; cadence; no health/heal/dash buff; all caps unchanged.");

                    void CheckCurve(CardId id, Func<PlayerUpgradeState, float> read, float[] expected)
                    {
                        state.ResetRun();
                        CardDefinition card = cards.Single(c => c.Id == id);
                        Require(card.MaxStacks == expected.Length, "Số cấp đường cong sai: " + id);
                        for (int rank = 1; rank <= expected.Length; rank++)
                        {
                            float expectedScale = rank == 1 ? 1.5f : expected.Length == 3 ? .75f : .5f;
                            Near(card.StatScaleAtStack(rank), expectedScale);
                            Grant(id, 1); Near(read(state), expected[rank - 1]);
                            Require(state.MaxHealthBonus == 0 && state.WaveHeal == 0, "Thẻ chỉ số vô tình cộng máu/hồi: " + id);
                            Near(state.DashCooldownMultiplier, 1f);
                            Require(state.ProjectileCount == 1 && state.ProjectileHitLimit == 1 && !state.Explodes,
                                "Đường cong vô tình đổi đạn: " + id);
                        }
                        Require(!state.Apply(card, null), "Nhận vượt giới hạn: " + id);
                    }

                    void Grant(CardId id, int count)
                    {
                        CardDefinition card = cards.Single(c => c.Id == id);
                        for (int rank = 0; rank < count; rank++) Require(state.Apply(card, null), "Không nhận được cấp: " + id);
                    }
                }
                for (int i = 0; i < assets.Length; i++)
                    Require(originals[i] == EditorJsonUtility.ToJson(assets[i]), "Đã sửa asset khi kiểm: " + assets[i].name);
                Debug.Log("[CardImpact] ALL PASSED: six independent frontloaded curves x three characters, real-unit breakpoints/cadence, unchanged base caps, health rounding, dash/heal/projectile guardrails, immutable assets.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                CardEvolutionCatalog.EditorSetTestRecipes(null);
            }
        }

        private static int HitsToDefeat(float health, float damage) => Mathf.CeilToInt(health / damage - .00001f);
        private static int ApplyUntilDefeated(LAC.Enemies.Enemy enemy, float damage)
        {
            Set(enemy, "_health", 10f);
            for (int count = 1; count <= 20; count++) if (enemy.ApplyDamage(damage)) return count;
            throw new InvalidOperationException("[CardImpact] Không hạ được quái thử sau 20 đòn.");
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Near(float actual, float expected) => Require(Mathf.Abs(actual - expected) < .0001f, $"{actual} != {expected}");
        private static void Require(bool valid, string reason) { if (!valid) throw new InvalidOperationException("[CardImpact] " + reason); }
    }
}
#endif
