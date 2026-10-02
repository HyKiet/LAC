using System.Globalization;
using System.Text;
using LAC.Combat;
using LAC.Player;
using UnityEngine;

namespace LAC.Cards
{
    /// <summary>Văn bản của lần nhận kế tiếp; tính một lần khi mở/đổi đề nghị, không trong Update.</summary>
    public static class CardImpactPreview
    {
        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        public static string Description(CardDefinition card, PlayerUpgradeState state)
        {
            if (card == null || state == null) return "";
            float scale = card.StatScaleAtStack(state.GetStacks(card.Id) + 1);
            switch (card.Id)
            {
                case CardId.CuongCong:
                    return $"{Percent(card.DamageBonus * scale)} sát thương gốc.\nCộng theo chỉ số gốc.";
                case CardId.LienKich:
                    return $"{Percent(card.AttackSpeedBonus * scale)} tốc đánh gốc.\nRút ngắn nhịp khai hoả.";
                case CardId.SinhLuc:
                    return $"{Percent(card.HealthBonusRatio * scale)} máu gốc, làm tròn.\nHồi phần máu vừa tăng.";
                case CardId.BoPhap:
                    return $"{Percent(-card.DashCooldownReduction * scale)} hồi lướt gốc.\nKhông đổi tốc độ lướt.";
                case CardId.SongTien:
                    return $"Bắn {1 + card.ExtraProjectiles} đạn, góc mở {Number(card.ProjectileSpread)}°.\nMỗi đạn {Number(card.ProjectileDamageRatio * 100f)}% sát thương.";
                case CardId.XuyenTam:
                    return $"Xuyên thêm {card.ExtraPierces} địch/đạn.\nKhông đổi sát thương.";
                case CardId.BocPha:
                    return $"Nổ rộng {Number(card.ExplosionRadius)} đơn vị.\n{Number(card.ExplosionDamageRatio * 100f)}% sát thương lên địch khác.";
                case CardId.KhinhThan:
                    return $"{Percent(card.MoveSpeedBonus * scale)} tốc độ di chuyển.\nKhông tăng tốc độ lướt.";
                case CardId.AmVang:
                    return $"{Percent(card.AttackRangeBonus * scale)} tầm đánh gốc.\nKhông tăng bán kính nổ.";
                case CardId.HoiXuan:
                    return $"Hồi {card.WaveHeal} máu đầu mỗi đợt.\nKhông hồi sinh.";
                case CardId.ThietBich:
                    return $"{Percent(card.HitInvulnerabilityBonus * scale)} bảo vệ sau trúng.\nKhông tăng bảo vệ lướt.";
                case CardId.CuongNo:
                    return $"{Percent(card.DamageBonus * scale)} sát thương gốc.\n{Percent(card.AttackSpeedBonus * scale)} tốc đánh gốc.";
                default: return card.Description;
            }
        }

        public static string Metrics(CardDefinition card, PlayerUpgradeState state, CardUpgradePreview projected)
        {
            if (card == null || state == null || projected == null) return "";
            CharacterData data = state.CharacterData;
            bool projectile = data != null && data.WeaponShape == WeaponShape.Line;
            string first;
            string second = "";
            switch (card.Id)
            {
                case CardId.CuongCong:
                case CardId.CuongNo:
                    first = Damage(state, projected, data, projectile);
                    if (card.Id == CardId.CuongNo)
                        second = Interval(state, projected, data);
                    else if (!Mathf.Approximately(state.AttackSpeedMultiplier, projected.AttackSpeedMultiplier))
                        second = Interval(state, projected, data);
                    break;
                case CardId.LienKich:
                    first = Interval(state, projected, data);
                    if (state.ProjectileCount != projected.ProjectileCount)
                        second = Pair("Đạn", state.ProjectileCount, projected.ProjectileCount);
                    else if (!Mathf.Approximately(state.DamageMultiplier, projected.DamageMultiplier))
                        second = Damage(state, projected, data, projectile);
                    break;
                case CardId.SinhLuc:
                    first = data != null
                        ? Pair("Máu tối đa", data.MaxHealth + state.MaxHealthBonus, data.MaxHealth + projected.MaxHealthBonus)
                        : "Máu tăng theo máu gốc";
                    if (state.WaveHeal != projected.WaveHeal)
                        second = Pair("Hồi/đợt", state.WaveHeal, projected.WaveHeal);
                    else if (!Mathf.Approximately(state.HitInvulnerabilityMultiplier, projected.HitInvulnerabilityMultiplier))
                        second = Protection(state, projected);
                    break;
                case CardId.BoPhap:
                    first = data != null
                        ? Pair("Hồi lướt", data.DashCooldown * state.DashCooldownMultiplier,
                            data.DashCooldown * projected.DashCooldownMultiplier, " s")
                        : Pair("Hệ số hồi", state.DashCooldownMultiplier, projected.DashCooldownMultiplier, "×");
                    if (!Mathf.Approximately(state.MoveSpeedMultiplier, projected.MoveSpeedMultiplier))
                        second = Move(state, projected, data);
                    break;
                case CardId.SongTien:
                    first = Pair("Đạn", state.ProjectileCount, projected.ProjectileCount);
                    second = Damage(state, projected, data, true);
                    break;
                case CardId.XuyenTam:
                    first = Pair("Mục tiêu/đạn", state.ProjectileHitLimit, projected.ProjectileHitLimit);
                    break;
                case CardId.BocPha:
                    first = Pair("Bán kính nổ", state.ExplosionRadius, projected.ExplosionRadius);
                    second = data != null
                        ? Pair("ST nổ", state.DamageFromBase(data.BaseDamage, true) * state.ExplosionDamageRatio,
                            data.BaseDamage * projected.DamageMultiplier * projected.ProjectileDamageMultiplier * projected.ExplosionDamageRatio)
                        : Pair("Tỉ lệ nổ", state.ExplosionDamageRatio * 100f, projected.ExplosionDamageRatio * 100f, "%");
                    break;
                case CardId.KhinhThan:
                    first = Move(state, projected, data);
                    break;
                case CardId.AmVang:
                    first = data != null
                        ? Pair("Tầm đánh", data.AttackRange * state.AttackRangeMultiplier,
                            data.AttackRange * projected.AttackRangeMultiplier)
                        : Pair("Hệ số tầm", state.AttackRangeMultiplier, projected.AttackRangeMultiplier, "×");
                    if (state.ProjectileHitLimit != projected.ProjectileHitLimit)
                        second = Pair("Mục tiêu/đạn", state.ProjectileHitLimit, projected.ProjectileHitLimit);
                    else if (!Mathf.Approximately(state.DamageMultiplier, projected.DamageMultiplier))
                        second = Damage(state, projected, data, projectile);
                    break;
                case CardId.HoiXuan:
                    first = Pair("Hồi/đợt", state.WaveHeal, projected.WaveHeal);
                    break;
                case CardId.ThietBich:
                    first = Protection(state, projected);
                    break;
                default: first = "Theo chỉ số trong ván"; break;
            }
            if (projected.NewEvolutions.Count > 0)
            {
                var names = new StringBuilder("Mở ");
                for (int i = 0; i < projected.NewEvolutions.Count; i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(projected.NewEvolutions[i].DisplayName);
                }
                second = names.ToString();
            }
            return string.IsNullOrEmpty(second) ? first : first + "\n" + second;
        }

        private static string Damage(PlayerUpgradeState before, CardUpgradePreview after, CharacterData data, bool projectile) =>
            data != null ? Pair(projectile ? "ST/đạn" : "Sát thương", before.DamageFromBase(data.BaseDamage, projectile),
                data.BaseDamage * after.DamageMultiplier * (projectile ? after.ProjectileDamageMultiplier : 1f))
            : Pair("Hệ số ST", before.DamageMultiplier * (projectile ? before.ProjectileDamageMultiplier : 1f),
                after.DamageMultiplier * (projectile ? after.ProjectileDamageMultiplier : 1f), "×");

        private static string Interval(PlayerUpgradeState before, CardUpgradePreview after, CharacterData data) =>
            data != null ? Pair("Nhịp đánh", before.AttackIntervalFromBase(data.AttackInterval),
                data.AttackInterval / Mathf.Max(.1f, after.AttackSpeedMultiplier), " s")
            : Pair("Hệ số tốc đánh", before.AttackSpeedMultiplier, after.AttackSpeedMultiplier, "×");

        private static string Move(PlayerUpgradeState before, CardUpgradePreview after, CharacterData data) =>
            data != null ? Pair("Tốc độ", data.MoveSpeed * before.MoveSpeedMultiplier, data.MoveSpeed * after.MoveSpeedMultiplier)
            : Pair("Hệ số tốc độ", before.MoveSpeedMultiplier, after.MoveSpeedMultiplier, "×");

        // Chỉ số gốc này nằm ở PlayerHealth, không ở CharacterData: không giả định mọi prefab đều 0,6 giây.
        private static string Protection(PlayerUpgradeState before, CardUpgradePreview after) =>
            Pair("Hệ số bảo vệ", before.HitInvulnerabilityMultiplier, after.HitInvulnerabilityMultiplier, "×");

        private static string Pair(string label, float before, float after, string unit = "") =>
            $"{label}  {Number(before)} → {Number(after)}{unit}";
        private static string Percent(float value) => (value >= 0 ? "+" : "−") + Number(Mathf.Abs(value) * 100f) + "%";
        private static string Number(float value) => value.ToString("0.###", Vietnamese);
    }
}
