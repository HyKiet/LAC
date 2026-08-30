using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Một ảnh mờ đọng lại phía sau nhân vật khi lướt, tự nhạt dần rồi trả về pool.
    /// </summary>
    /// <remarks>
    /// Thuần tuý biểu diễn, xử lý hoàn toàn cục bộ và không đồng bộ qua mạng — xem bảng ở
    /// CLAUDE.md mục 3.2.
    ///
    /// Vẽ ở alpha thấp và ở sorting order thấp hơn nhân vật, theo ràng buộc đọc hiểu thị
    /// giác tại mục 2.1: hiệu ứng của người chơi không được che khuất thứ gì. Một lần lướt
    /// sinh khoảng năm ảnh mờ; đến cuối ván với hai người chơi lướt liên tục thì con số này
    /// nhân lên, nên tất cả đều đi qua pool.
    /// </remarks>
    public sealed class DashAfterimage : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Thời gian ảnh mờ tan hết.")]
        [SerializeField, Min(0.02f)] private float _lifetime = 0.22f;

        [Tooltip("Độ mờ lúc vừa xuất hiện.")]
        [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.45f;

        private ObjectPool<DashAfterimage> _owner;
        private float _age;
        private Color _baseColor;

        /// <summary>Bắt đầu một ảnh mờ. Pool truyền vào để ảnh tự trả mình về khi tan.</summary>
        public void Play(ObjectPool<DashAfterimage> owner, Sprite sprite, Color tint, bool flipX, int sortingOrder)
        {
            _owner = owner;
            _renderer.sprite = sprite;
            _renderer.flipX = flipX;
            _renderer.sortingOrder = sortingOrder;
            _baseColor = tint;
            ApplyAlpha(_startAlpha);
        }

        public void OnSpawned()
        {
            _age = 0f;
        }

        public void OnDespawned()
        {
            _owner = null;
            _renderer.sprite = null;
        }

        private void Update()
        {
            // Khi thoát play mode, Unity huỷ các thành phần theo thứ tự không xác định và
            // Update vẫn chạy thêm một nhịp. Không chặn ở đây thì mỗi ảnh mờ còn sống sinh
            // một MissingReferenceException lúc dừng — chín dòng đỏ cho một lỗi vô hại,
            // đủ để che mất lỗi thật.
            if (_renderer == null)
            {
                enabled = false;
                return;
            }

            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }

            ApplyAlpha(Mathf.Lerp(_startAlpha, 0f, _age / _lifetime));
        }

        private void ApplyAlpha(float alpha) =>
            _renderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
    }
}
