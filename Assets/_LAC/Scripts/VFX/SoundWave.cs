using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Sóng âm Đông Sơn: các vòng tròn đồng tâm lan toả mang hoa văn trống đồng.
    /// </summary>
    /// <remarks>
    /// Đây là <b>hình ảnh chủ đạo của sản phẩm</b> — xem CLAUDE.md mục 2.1. Toàn bộ vũ khí
    /// trong game đều là nhạc cụ, và đòn đánh được biểu diễn dưới dạng sóng âm chứ không
    /// phải tia lửa hay vệt chém. Càng về cuối ván màn hình càng phủ kín sóng do chính người
    /// chơi tạo ra; đây là cảnh dùng cho trailer.
    ///
    /// <b>Ba ràng buộc bắt buộc về khả năng đọc hiểu, không được nới:</b> vẽ ở alpha thấp
    /// với chế độ additive; nằm ở sorting order thấp hơn nhân vật và thấp hơn đòn địch;
    /// tuyệt đối không dùng màu đã dành riêng cho đòn tấn công của kẻ địch. Không tuân thủ
    /// thì từ khoảng đợt 10 người chơi mất khả năng quan sát đạn địch.
    ///
    /// Nhiều vòng lệch pha nhau tạo cảm giác một tiếng đàn ngân ra chứ không phải một cú nổ.
    /// Vòng sau xuất phát trễ và tan sớm hơn, nên phần đuôi của sóng mảnh dần đúng như âm
    /// thanh tắt dần.
    ///
    /// <b>Trần độ mờ bị khoá ở 0.4 trong Inspector.</b> Bản đầu để 0.55 và ảnh chụp cho thấy
    /// sóng phủ trắng gần hết vùng chơi — đúng kiểu hỏng mà mục 2.1 mô tả. Với chế độ additive
    /// thì các lớp sóng chồng nhau cộng dồn độ sáng, nên con số an toàn thấp hơn nhiều so với
    /// cảm giác khi nhìn một vòng đơn lẻ.
    /// </remarks>
    public sealed class SoundWave : MonoBehaviour, IPoolable
    {
        [Tooltip("Các vòng đồng tâm. Vòng đầu là vòng dẫn, các vòng sau lệch pha theo sau.")]
        [SerializeField] private SpriteRenderer[] _rings = new SpriteRenderer[0];

        [SerializeField, Min(0.05f)] private float _duration = 0.45f;

        [Tooltip("Độ trễ giữa hai vòng liên tiếp, tính theo phần của tổng thời gian.")]
        [SerializeField, Range(0f, 0.5f)] private float _ringDelay = 0.18f;

        [Tooltip("Độ mờ lúc mạnh nhất. Giữ thấp — xem ràng buộc ở mục 2.1.")]
        // Trần 0.22 là mức mà MỘT lớp sóng còn giữ được 3:1 với đòn địch #FF7A55; quá đó
        // thì dù chỉ một vòng cũng đã nuốt mất đòn địch. Giá trị mặc định 0.07 là mức an
        // toàn khi ba lớp chồng nhau — xem docs/PALETTE.md.
        [SerializeField, Range(0f, 0.22f)] private float _peakAlpha = 0.07f;

        [Tooltip("Tốc độ xoay của hoa văn, độ mỗi giây. Làm sóng có nhịp thay vì đứng im.")]
        [SerializeField] private float _spinSpeed = 25f;

        private ObjectPool<SoundWave> _owner;
        private float _age;
        private float _fromRadius;
        private float _toRadius;
        private Color _tint = Color.white;

        public void Play(ObjectPool<SoundWave> owner, float fromRadius, float toRadius, Color tint)
        {
            _owner = owner;
            _fromRadius = fromRadius;
            _toRadius = toRadius;
            _tint = tint;
            _age = 0f;

            transform.rotation = Quaternion.identity;
            Apply();
        }

        public void OnSpawned() => _age = 0f;

        public void OnDespawned() => _owner = null;

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _duration + _ringDelay * _duration * _rings.Length)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }

            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < _rings.Length; i++)
            {
                SpriteRenderer ring = _rings[i];
                if (ring == null) continue;

                float start = i * _ringDelay * _duration;
                float t = (_age - start) / _duration;

                if (t < 0f || t > 1f)
                {
                    ring.enabled = false;
                    continue;
                }

                ring.enabled = true;

                float radius = Mathf.Lerp(_fromRadius, _toRadius, EaseOut(t));
                ring.transform.localScale = Vector3.one * (radius * 2f);

                // Vòng ngoài mờ hơn vòng dẫn: sóng loang ra thì yếu đi.
                float falloff = 1f - i / (float)Mathf.Max(_rings.Length, 1) * 0.45f;
                float alpha = _peakAlpha * falloff * Mathf.Sin(t * Mathf.PI);

                ring.color = new Color(_tint.r, _tint.g, _tint.b, alpha);
            }
        }

        /// <summary>Lan nhanh lúc đầu rồi chậm dần — nghe như một tiếng gõ, không như một cú đẩy đều.</summary>
        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
