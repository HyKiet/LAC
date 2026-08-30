using Mirror;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Di chuyển nhân vật theo thao tác của người sở hữu.
    /// </summary>
    /// <remarks>
    /// Client tự chạy vật lý cho nhân vật của mình và đẩy vị trí lên qua
    /// <c>NetworkTransformReliable</c> đặt ở chiều <c>ClientToServer</c> — xem bảng đồng bộ
    /// ở CLAUDE.md mục 3.2. Nếu bắt client hỏi host rồi mới được đi thì mỗi bước chân phải
    /// chờ trọn một vòng mạng, ở 100 ms là đủ để cảm giác điều khiển hỏng hoàn toàn.
    ///
    /// Đổi lại, host không thẩm định vị trí. Đây là đánh đổi có chủ đích: LẠC là game hợp
    /// tác, không có đối kháng giữa người chơi, nên gian lận vị trí chỉ ảnh hưởng đến chính
    /// ván của họ. Máu và sát thương thì ngược lại — host giữ toàn quyền, xem T-13.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Lướt giành quyền điều khiển vận tốc trong pha lướt.")]
        [SerializeField] private PlayerDash _dash;

        [Tooltip("Tốc độ dùng khi chưa nạp được chỉ số nhân vật.")]
        [SerializeField, Min(0.1f)] private float _fallbackSpeed = 5f;

        /// <summary>Hướng nhìn hiện tại. Giữ nguyên khi đứng yên, dùng cho lướt và ngắm bắn.</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>Nhân vật có đang di chuyển hay không. Dùng cho hoạt ảnh.</summary>
        public bool IsMoving { get; private set; }

        private float MoveSpeed =>
            _character != null && _character.Data != null ? _character.Data.MoveSpeed : _fallbackSpeed;

        public override void OnStartClient()
        {
            // Nhân vật của người khác do mạng điều khiển vị trí. Để vật lý động chạy trên nó
            // thì hai bên tranh nhau ghi transform, kết quả là hình ảnh giật liên tục.
            _rigidbody.bodyType = isOwned ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

            if (_input != null) _input.enabled = isOwned;
        }

        private void Update()
        {
            if (!isOwned || _input == null) return;

            Vector2 move = _input.Move;
            IsMoving = move.sqrMagnitude > 0f;
            if (IsMoving) Facing = move;

            // Lật hình là phần biểu diễn thuần tuý, xử lý cục bộ và không đồng bộ.
            if (_renderer != null && Mathf.Abs(move.x) > 0.01f)
                _renderer.flipX = move.x < 0f;
        }

        private void FixedUpdate()
        {
            if (!isOwned) return;

            // Trong pha lướt, PlayerDash mới là thứ đặt vận tốc. Không nhường thì hai thành
            // phần cùng ghi vào một Rigidbody trong cùng một bước vật lý, và cú lướt bị kéo
            // ngược lại thành tốc độ đi bộ.
            if (_dash != null && _dash.IsDashing) return;

            _rigidbody.linearVelocity = _input != null ? _input.Move * MoveSpeed : Vector2.zero;
        }
    }
}
