using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Hiển thị một số nguyên bằng các sprite chữ số ghép lại.
    /// </summary>
    /// <remarks>
    /// Không dùng TextMeshPro. Phông vector bị làm mờ khi thu về cỡ pixel art và phá vỡ lưới
    /// điểm ảnh; ngoài ra TMP đòi nhập bộ tài sản riêng, thêm một bước cài đặt cho ba máy.
    /// Chữ số vẽ tay 3×5 điểm ảnh nằm đúng trên lưới và dùng lại được cho cả số sát thương
    /// bay lên lẫn HUD.
    ///
    /// Dùng chung một mảng sprite cho mười chữ số; con số được dựng lại mỗi lần đặt giá trị,
    /// không cấp phát thêm đối tượng vì các ô chữ số đã được tạo sẵn.
    /// </remarks>
    public sealed class PixelNumber : MonoBehaviour
    {
        [Tooltip("Sprite của các chữ số, theo thứ tự 0 đến 9.")]
        [SerializeField] private Sprite[] _digits = new Sprite[10];

        [Tooltip("Các ô chữ số đã tạo sẵn, xếp từ trái sang phải.")]
        [SerializeField] private SpriteRenderer[] _slots = new SpriteRenderer[0];

        [Tooltip("Khoảng cách giữa hai chữ số, tính bằng đơn vị thế giới.")]
        [SerializeField] private float _spacing = 0.22f;

        private static readonly int[] _scratch = new int[8];

        /// <summary>Đặt giá trị hiển thị. Số âm được vẽ như giá trị tuyệt đối.</summary>
        public void SetValue(int value)
        {
            value = Mathf.Abs(value);

            int count = 0;
            do
            {
                _scratch[count++] = value % 10;
                value /= 10;
            }
            while (value > 0 && count < _scratch.Length);

            int usable = Mathf.Min(count, _slots.Length);
            float width = (usable - 1) * _spacing;

            for (int i = 0; i < _slots.Length; i++)
            {
                SpriteRenderer slot = _slots[i];
                if (slot == null) continue;

                if (i >= usable)
                {
                    slot.enabled = false;
                    continue;
                }

                // _scratch giữ chữ số hàng đơn vị ở vị trí 0, nên phải đọc ngược lại.
                int digit = _scratch[usable - 1 - i];
                slot.sprite = _digits[digit];
                slot.enabled = true;
                slot.transform.localPosition = new Vector3(-width * 0.5f + i * _spacing, 0f, 0f);
            }
        }

        /// <summary>Đổi màu toàn bộ chữ số.</summary>
        public void SetColor(Color color)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null) _slots[i].color = color;
        }

        /// <summary>Đổi sorting order của toàn bộ chữ số.</summary>
        public void SetSortingOrder(int order)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null) _slots[i].sortingOrder = order;
        }
    }
}
