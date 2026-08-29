namespace LAC.Combat
{
    /// <summary>
    /// Hình dạng vùng tác động của một đòn đánh.
    /// </summary>
    /// <remarks>
    /// Ba nhân vật khác nhau ở hình dạng đòn đánh chứ không chỉ ở chỉ số. Đây là yếu tố
    /// quyết định lối chơi: Thạch Sanh kiểm soát vòng quanh mình, Gióng phải xoay mặt về
    /// phía địch, Tấm phải đứng thẳng hàng với mục tiêu.
    /// </remarks>
    public enum WeaponShape
    {
        /// <summary>Vòng tròn quanh người chơi — đàn bầu của Thạch Sanh.</summary>
        Circle = 0,

        /// <summary>Hình cung phía trước — roi sắt của Gióng.</summary>
        Arc = 1,

        /// <summary>Tia thẳng theo một hướng — sáo trúc của Tấm.</summary>
        Line = 2
    }
}
