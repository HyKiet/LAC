using LAC.Core;
using LAC.Player;
using UnityEngine;

namespace LAC.Utils
{
    /// <summary>
    /// Camera bám theo nhân vật của người chơi ngồi trước máy này.
    /// </summary>
    /// <remarks>
    /// Mỗi máy có camera riêng bám nhân vật riêng — co-op của LẠC là hai màn hình, không
    /// phải màn hình chung. Camera chung sẽ buộc hai người chơi luôn ở gần nhau, mâu thuẫn
    /// với lối chơi phải chạy vòng để tránh đám quái.
    ///
    /// Mục tiêu được tìm qua <see cref="PlayerRegistry"/> thay vì <c>FindObjectOfType</c>,
    /// và chỉ tìm cho tới khi thấy — nhân vật cục bộ xuất hiện sau camera vài khung hình vì
    /// còn chờ mạng sinh ra.
    /// </remarks>
    public sealed class CameraFollow : MonoBehaviour
    {
        [Tooltip("Thời gian camera đuổi kịp nhân vật. Càng lớn càng mượt nhưng càng trễ.")]
        [SerializeField, Range(0f, 0.5f)] private float _smoothTime = 0.12f;

        [Tooltip("Giữ khung hình nằm trong đấu trường. Tắt nếu đấu trường nhỏ hơn màn hình.")]
        [SerializeField] private bool _clampToArena = true;

        [Tooltip("Số đơn vị được phép nhìn ra ngoài vùng chơi, đủ để thấy vòng tường.")]
        [SerializeField, Min(0f)] private float _revealBeyondArena = 1.5f;

        [SerializeField] private Camera _camera;

        [Tooltip("Thời gian một cú rung màn tắt hẳn.")]
        [SerializeField, Min(0.01f)] private float _shakeDecay = 0.25f;

        [Tooltip("Trần biên độ rung, tính bằng đơn vị thế giới.")]
        [SerializeField, Min(0f)] private float _maxShake = 0.35f;

        private Transform _target;
        private Vector3 _velocity;
        private float _shake;

        /// <summary>
        /// Rung màn một cú. Các lời gọi chồng nhau lấy giá trị lớn nhất, không cộng dồn.
        /// </summary>
        /// <remarks>
        /// Cộng dồn sẽ khiến cuối ván — lúc sự kiện dày đặc nhất — màn hình rung đến mức
        /// không nhìn được gì, đúng vào lúc người chơi cần nhìn rõ nhất.
        /// </remarks>
        public void Shake(float amount) => _shake = Mathf.Min(Mathf.Max(_shake, amount), _maxShake);

        private void Awake()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                if (_target == null) return;

                // Lần đầu bám thì nhảy thẳng tới nơi. Trượt từ gốc toạ độ về nhân vật là một
                // cú lia camera mà người chơi không yêu cầu.
                transform.position = Framed(_target.position);
                return;
            }

            Vector3 wanted = Vector3.SmoothDamp(
                transform.position, Framed(_target.position), ref _velocity, _smoothTime);

            if (_shake > 0.0001f)
            {
                // Đồng hồ không co giãn: rung màn phải tiếp tục chạy trong lúc hit-stop đang
                // dừng thời gian, nếu không thì màn hình đứng cứng đúng lúc cần có sức nặng.
                _shake = Mathf.Max(_shake - _maxShake / _shakeDecay * Time.unscaledDeltaTime, 0f);
                wanted += (Vector3)(Random.insideUnitCircle * _shake);
            }

            transform.position = wanted;
        }

        /// <summary>
        /// Vị trí camera cần tới để nhìn vào mục tiêu mà không lộ ra ngoài đấu trường.
        /// </summary>
        /// <remarks>
        /// Kẹp theo nửa khung hình chứ không theo một con số cố định: tỉ lệ màn hình của
        /// người chơi quyết định khung rộng bao nhiêu, và màn hình siêu rộng sẽ nhìn xuyên
        /// qua tường nếu dùng lề cứng.
        ///
        /// Lề được nới ra một chút để vòng tường lọt vào khung hình. Kẹp khít vào vùng chơi
        /// thì mép màn hình rơi đúng lên biên, người chơi bị chặn bởi một bức tường mà họ
        /// không nhìn thấy.
        /// </remarks>
        private Vector3 Framed(Vector3 targetPosition)
        {
            Vector2 wanted = targetPosition;

            if (_clampToArena && ArenaBounds.Instance != null && _camera != null && _camera.orthographic)
            {
                float halfHeight = _camera.orthographicSize;
                float halfWidth = halfHeight * _camera.aspect;
                wanted = ArenaBounds.Instance.Clamp(
                    wanted, new Vector2(halfWidth - _revealBeyondArena, halfHeight - _revealBeyondArena));
            }

            return new Vector3(wanted.x, wanted.y, transform.position.z);
        }

        private void AcquireTarget()
        {
            for (int i = 0; i < PlayerRegistry.Count; i++)
            {
                PlayerCharacter player = PlayerRegistry.All[i];
                if (player == null || !player.isLocalPlayer) continue;

                _target = player.transform;
                return;
            }
        }

    }
}
