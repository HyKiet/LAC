using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Nháy sáng sprite trong chốc lát rồi trả về màu cũ.</summary>
    /// <remarks>
    /// Dùng chung cho cả người chơi và quái. Đây là tín hiệu "đã trúng" rẻ nhất và đọc nhanh
    /// nhất: mắt bắt được thay đổi độ sáng trước cả khi nhận ra hình dạng.
    ///
    /// <b>Màu truyền vào phải là hệ số nhân lớn hơn 1.</b> <c>SpriteRenderer.color</c> được
    /// <b>nhân</b> vào màu của điểm ảnh, không thay thế nó. Bản đầu truyền <c>Color.white</c>
    /// — nhân với 1 — nên không có gì thay đổi trên màn hình và hiệu ứng nháy chưa từng
    /// hoạt động kể từ T-15, dù mã vẫn chạy đúng và số sát thương vẫn hiện.
    ///
    /// Nhân quá 1 thì màu tràn trần và bị kẹp về trắng, đúng thứ cần: thân nhân vật loé
    /// trắng còn nét viền tối vẫn giữ được hình. Không cần shader riêng, và giữ được gộp
    /// lệnh vẽ vì vẫn chỉ là màu đỉnh.
    ///
    /// Màu gốc được đọc lại mỗi lần bắt đầu nháy chứ không nhớ một lần lúc khởi tạo — quái
    /// lấy từ pool có thể mang màu của lần dùng trước, và người chơi bị mờ đi khi chết.
    /// </remarks>
    public sealed class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Thời gian nháy. Dưới 0.08 giây thì mắt không kịp bắt khi có 40 quái trên sân.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.11f;

        private Color _original;
        private float _endsAt;
        private bool _flashing;

        public void Flash(Color color)
        {
            if (_renderer == null) return;

            if (!_flashing) _original = _renderer.color;

            // Giữ nguyên độ trong suốt hiện tại: người chơi lúc gục được vẽ mờ đi, nháy sáng
            // không được kéo họ hiện rõ trở lại.
            _renderer.color = new Color(color.r, color.g, color.b, _original.a);
            _endsAt = Time.unscaledTime + _duration;
            _flashing = true;
        }

        private void Update()
        {
            if (!_flashing || _renderer == null) return;

            // Dùng đồng hồ không co giãn: nháy sáng phải kết thúc đúng hạn kể cả trong lúc
            // hit-stop đang làm chậm thời gian, nếu không thì cú đánh mạnh lại nháy lâu hơn.
            if (Time.unscaledTime < _endsAt) return;

            _renderer.color = _original;
            _flashing = false;
        }

        private void OnDisable()
        {
            if (_flashing && _renderer != null) _renderer.color = _original;
            _flashing = false;
        }
    }
}
