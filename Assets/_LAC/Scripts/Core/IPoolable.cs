namespace LAC.Core
{
    /// <summary>
    /// Đối tượng cần đặt lại trạng thái mỗi lần được lấy ra hoặc trả về pool.
    /// </summary>
    /// <remarks>
    /// Đối tượng lấy từ pool không phải là đối tượng mới: nó giữ nguyên mọi giá trị của lần
    /// sử dụng trước. Đạn còn nhớ số lần xuyên thấu đã dùng, quái còn nhớ máu bằng không.
    /// Đây là nguồn lỗi phổ biến nhất khi chuyển từ Instantiate sang pool, nên mọi trạng
    /// thái riêng của một lần sử dụng phải được đặt lại trong <see cref="OnSpawned"/>.
    /// </remarks>
    public interface IPoolable
    {
        /// <summary>Gọi ngay sau khi đối tượng được lấy khỏi pool và bật hoạt động.</summary>
        void OnSpawned();

        /// <summary>Gọi ngay trước khi đối tượng được trả về pool và tắt hoạt động.</summary>
        void OnDespawned();
    }
}
