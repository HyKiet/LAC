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

        private Transform _target;
        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                if (_target == null) return;

                // Lần đầu bám thì nhảy thẳng tới nơi. Trượt từ gốc toạ độ về nhân vật là một
                // cú lia camera mà người chơi không yêu cầu.
                transform.position = WithDepth(_target.position);
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position, WithDepth(_target.position), ref _velocity, _smoothTime);
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

        private Vector3 WithDepth(Vector3 position) =>
            new Vector3(position.x, position.y, transform.position.z);
    }
}
