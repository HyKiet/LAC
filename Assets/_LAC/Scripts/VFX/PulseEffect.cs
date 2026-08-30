using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Vòng tròn lan toả rồi tan. Hiệu ứng tạm cho đòn đánh diện rộng.
    /// </summary>
    /// <remarks>
    /// Đây là chỗ giữ chỗ cho <b>sóng âm Đông Sơn</b> ở T-16 — hình ảnh chủ đạo của sản phẩm
    /// sẽ là các vòng đồng tâm mang hoa văn trống đồng. Ở đây chỉ cần một vòng trơn để nhìn
    /// ra nhịp khai hoả.
    ///
    /// Vẽ ở alpha thấp và sorting order dưới nhân vật, theo ràng buộc đọc hiểu ở mục 2.1:
    /// hiệu ứng của người chơi không bao giờ được che khuất đòn của địch.
    /// </remarks>
    public sealed class PulseEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;

        [SerializeField, Min(0.05f)] private float _duration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _startAlpha = 0.5f;

        private ObjectPool<PulseEffect> _owner;
        private float _age;
        private float _fromScale;
        private float _toScale;
        private Color _tint = Color.white;

        public void Play(ObjectPool<PulseEffect> owner, float fromRadius, float toRadius, Color tint)
        {
            _owner = owner;
            _fromScale = fromRadius * 2f;
            _toScale = toRadius * 2f;
            _tint = tint;
            _age = 0f;

            transform.localScale = Vector3.one * _fromScale;
            SetAlpha(_startAlpha);
        }

        public void OnSpawned() => _age = 0f;

        public void OnDespawned() => _owner = null;

        private void Update()
        {
            if (_renderer == null)
            {
                enabled = false;
                return;
            }

            _age += Time.deltaTime;
            float t = _age / _duration;

            if (t >= 1f)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(_fromScale, _toScale, t);
            SetAlpha(Mathf.Lerp(_startAlpha, 0f, t));
        }

        private void SetAlpha(float alpha) =>
            _renderer.color = new Color(_tint.r, _tint.g, _tint.b, alpha);
    }
}
