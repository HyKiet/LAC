using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Nháy sáng sprite trong chốc lát rồi trả về màu cũ.</summary>
    /// <remarks>
    /// Dùng chung cho cả người chơi và quái. Đây là tín hiệu "đã trúng" rẻ nhất và đọc nhanh
    /// nhất: mắt bắt được thay đổi độ sáng trước cả khi nhận ra hình dạng.
    ///
    /// Màu gốc được đọc lại mỗi lần bắt đầu nháy chứ không nhớ một lần lúc khởi tạo — quái
    /// lấy từ pool có thể mang màu của lần dùng trước, và người chơi bị mờ đi khi chết.
    /// </remarks>
    public sealed class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Min(0.01f)] private float _duration = 0.07f;

        private Color _original;
        private float _endsAt;
        private bool _flashing;

        public void Flash(Color color)
        {
            if (_renderer == null) return;

            if (!_flashing) _original = _renderer.color;

            _renderer.color = color;
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
