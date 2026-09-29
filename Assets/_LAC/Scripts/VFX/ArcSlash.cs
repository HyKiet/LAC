using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Vệt roi hình cung quét qua trước mặt — đòn đánh của Gióng.
    /// </summary>
    /// <remarks>
    /// Vũ khí hình cung không dùng sóng âm đồng tâm được: sóng tròn nói rằng đòn đánh lan ra
    /// mọi phía, còn cái mà người chơi cần đọc ở Gióng là <b>đòn chỉ trúng phía trước</b>.
    /// Vẽ sai hình dạng thì người chơi học sai tầm đánh và đứng sai chỗ. Đây là lý do
    /// <c>CharacterData.SpawnSoundWave</c> tắt cho Gióng — xem chú thích ở trường đó.
    ///
    /// Vẫn chịu đúng ba ràng buộc đọc hiểu ở CLAUDE.md mục 2.1: vật liệu additive, sorting
    /// order thấp hơn nhân vật, và không dùng màu dành riêng cho đòn địch.
    ///
    /// <b>Trần độ mờ khoá ở 0.22</b> — đúng mức mà docs/PALETTE.md đo được là còn giữ tỉ lệ
    /// tương phản 3:1 với đòn địch cho <i>một</i> lớp. Vệt roi không bao giờ chồng lên chính
    /// nó vì tồn tại 0.16 giây trong chu kỳ đánh 0.6 giây, nên một lớp là trường hợp xấu
    /// nhất và trần này là đủ.
    /// </remarks>
    public sealed class ArcSlash : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;

        [SerializeField, Min(0.02f)] private float _duration = 0.16f;

        [Tooltip("Độ mờ lúc mạnh nhất. Giữ thấp — xem ràng buộc ở mục 2.1.")]
        [SerializeField, Range(0f, 0.22f)] private float _peakAlpha = 0.2f;

        [SerializeField, Min(0.01f)] private float _lineWidth = 0.06f;

        private ObjectPool<ArcSlash> _owner;
        private float _age;
        private LineRenderer _arc;
        private const int Segments = 48;
        private Color _tint = Color.white;

        private void Awake()
        {
            _arc = gameObject.AddComponent<LineRenderer>();
            _arc.useWorldSpace = false;
            _arc.positionCount = Segments + 1;
            _arc.widthMultiplier = _lineWidth;
            _arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arc.receiveShadows = false;
            if (_renderer == null) return;
            _arc.sharedMaterial = _renderer.sharedMaterial;
            _arc.sortingLayerID = _renderer.sortingLayerID;
            _arc.sortingOrder = _renderer.sortingOrder;
            _renderer.enabled = false;
        }

        /// <param name="facing">Hướng vung, đã chuẩn hoá.</param>
        /// <param name="range">Tầm đánh. Cung được co giãn để nằm đúng ở mép tầm.</param>
        /// <param name="halfAngle">Nửa góc mở của vùng gây sát thương, tính bằng độ.</param>
        public void Play(ObjectPool<ArcSlash> owner, Vector2 facing, float range, float halfAngle, Color tint)
        {
            _owner = owner;
            _age = 0f;
            _tint = tint;

            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            transform.localScale = Vector3.one;
            // Biên ngoài nằm đúng tầm đánh; góc lấy trực tiếp từ vùng sát thương.
            float radius = Mathf.Max(0f, range - _lineWidth * 0.5f);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)Segments) * Mathf.Deg2Rad;
                _arc.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            Apply();
        }

        public void OnSpawned() => _age = 0f;

        public void OnDespawned() => _owner = null;

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _duration)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }

            Apply();
        }

        private void Apply()
        {
            float t = Mathf.Clamp01(_age / _duration);

            if (_arc == null) return;
            // Sát thương tức thời nên hiện cung ngay, không đợi đỉnh hoạt ảnh.
            float alpha = _peakAlpha * (1f - t);
            _arc.startColor = _arc.endColor = new Color(_tint.r, _tint.g, _tint.b, alpha);
        }
    }
}
