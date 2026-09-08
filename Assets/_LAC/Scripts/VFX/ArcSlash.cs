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

        [Tooltip("Phần góc nửa cung mà vệt quét qua. 1 là quét trọn vẹn từ mép này sang mép kia.")]
        [SerializeField, Range(0f, 1f)] private float _sweepPortion = 0.6f;

        [Tooltip("Bán kính của cung trong sprite, tính bằng đơn vị thế giới ở tỉ lệ 1.")]
        [SerializeField, Min(0.01f)] private float _spriteArcRadius = 0.8125f;

        private ObjectPool<ArcSlash> _owner;
        private float _age;
        private float _baseAngle;
        private float _sweep;
        private Color _tint = Color.white;

        /// <param name="facing">Hướng vung, đã chuẩn hoá.</param>
        /// <param name="range">Tầm đánh. Cung được co giãn để nằm đúng ở mép tầm.</param>
        /// <param name="halfAngle">Nửa góc mở của vùng gây sát thương, tính bằng độ.</param>
        public void Play(ObjectPool<ArcSlash> owner, Vector2 facing, float range, float halfAngle, Color tint)
        {
            _owner = owner;
            _age = 0f;
            _tint = tint;

            _baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            _sweep = halfAngle * _sweepPortion;

            // Co giãn theo tầm đánh thật thay vì để một kích thước cố định: người chơi đọc
            // tầm với của mình bằng chính vệt roi này, nên vẽ ngắn hơn tầm sẽ dạy họ đứng
            // gần hơn mức cần thiết, còn vẽ dài hơn thì họ tưởng với tới mà thật ra không.
            transform.localScale = Vector3.one * (range / Mathf.Max(_spriteArcRadius, 0.01f));

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

            // Quét từ mép trên xuống mép dưới, nhanh ở giữa và chậm ở hai đầu, giống nhịp
            // của một cú vung tay thật.
            float eased = t * t * (3f - 2f * t);
            transform.rotation = Quaternion.Euler(0f, 0f, _baseAngle + Mathf.Lerp(_sweep, -_sweep, eased));

            if (_renderer == null) return;

            // Hiện nhanh, tắt chậm: đầu đòn phải đọc được ngay tại khung hình gây sát thương.
            float alpha = _peakAlpha * (1f - t) * Mathf.Min(1f, t * 6f);
            _renderer.color = new Color(_tint.r, _tint.g, _tint.b, alpha);
        }
    }
}
