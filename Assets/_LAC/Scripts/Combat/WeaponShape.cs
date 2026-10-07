namespace LAC.Combat
{
    /// <summary>
    /// Hình dạng vùng tác động của một đòn đánh.
    /// </summary>
    /// <remarks>
    /// Warrior quét cung, Archer bắn đạn thẳng, Lancer đâm xuyên hàng. Circle được giữ
    /// để hỗ trợ dữ liệu/hiệu ứng cũ, không còn dùng trong registry người chơi hiện hành.
    /// </remarks>
    public enum WeaponShape
    {
        /// <summary>Vòng tròn quanh người chơi — đàn bầu của Thạch Sanh.</summary>
        Circle = 0,

        /// <summary>Hình cung phía trước — roi sắt của Gióng.</summary>
        Arc = 1,

        /// <summary>Tia thẳng theo một hướng — sáo trúc của Tấm.</summary>
        Line = 2,

        /// <summary>Đâm theo đoạn thẳng, xuyên tất cả mục tiêu trong dải hẹp.</summary>
        Thrust = 3
    }
}
