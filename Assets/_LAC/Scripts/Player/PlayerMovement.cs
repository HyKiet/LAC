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

        [Tooltip("Ngưỡng tốc độ để coi nhân vật của người khác là đang đi, tính bằng đơn vị mỗi giây.")]
        [SerializeField, Min(0f)] private float _remoteMoveThreshold = 0.35f;

        private Vector2 _lastPosition;
        private Vector2 _facingOverride;
        private float _facingOverrideUntil;

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

            _lastPosition = transform.position;
        }

        /// <summary>
        /// Đưa nhân vật về điểm sinh khi ván mới bắt đầu. Chỉ host được gọi.
        /// </summary>
        /// <remarks>
        /// Phải đi qua RPC chứ không đặt vị trí thẳng trên host. `NetworkTransform` của người
        /// chơi chạy theo chiều client → server, nên vị trí do host ghi sẽ bị chính chủ sở
        /// hữu ghi đè lại ngay gói tin kế tiếp: người chơi trên máy host thấy mình về điểm
        /// sinh còn máy kia thì không, rồi cả hai lệch nhau.
        /// </remarks>
        [Server]
        public void ServerRespawn(Vector3 position) => RpcRespawn(position);

        [ClientRpc]
        private void RpcRespawn(Vector3 position)
        {
            // Mỗi máy chỉ dịch chuyển nhân vật của chính nó; nhân vật của người kia sẽ tự
            // tới nơi qua NetworkTransform.
            if (!isOwned) { _lastPosition = position; return; }

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.position = position;
            transform.position = position;

            _lastPosition = position;
            IsMoving = false;
            Facing = Vector2.down;
            _facingOverrideUntil = 0f;
        }

        /// <summary>
        /// Quay mặt về một hướng trong một khoảng thời gian, bất kể đang đi hướng nào.
        /// </summary>
        /// <remarks>
        /// Vũ khí gọi hàm này khi khai hoả. Vừa chạy sang trái vừa bắn sang phải là tình
        /// huống thường xuyên trong thể loại này — quay mặt về phía mục tiêu thì người chơi
        /// đọc được mình đang đánh ai, còn quay theo hướng chạy thì không.
        /// </remarks>
        public void FaceTowards(Vector2 direction, float holdSeconds)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            _facingOverride = direction;
            _facingOverrideUntil = Time.time + holdSeconds;
        }

        private void Update()
        {
            if (isOwned) ReadOwnedIntent();
            else ReadRemoteIntent();

            ApplyFlip();
        }

        private void ReadOwnedIntent()
        {
            if (_input == null) return;

            Vector2 move = _input.Move;
            IsMoving = move.sqrMagnitude > 0f;
            if (IsMoving) Facing = move;
        }

        /// <summary>
        /// Suy ra ý định di chuyển của nhân vật người khác từ độ dời vị trí.
        /// </summary>
        /// <remarks>
        /// Máy này không có thao tác bàn phím của họ — thứ duy nhất nó nhận được là vị trí
        /// do <c>NetworkTransform</c> bơm vào. Không suy ra ở đây thì nhân vật đồng đội
        /// trượt ngang màn hình trong tư thế đứng yên, luôn quay mặt một phía.
        ///
        /// Có ngưỡng chết vì vị trí đến theo từng gói tin rồi được nội suy, nên luôn có
        /// dao động nhỏ ngay cả khi họ đứng im; không lọc thì nhân vật rung qua rung lại
        /// giữa hai hoạt ảnh.
        /// </remarks>
        private void ReadRemoteIntent()
        {
            Vector2 current = transform.position;
            Vector2 delta = current - _lastPosition;
            _lastPosition = current;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float speed = delta.magnitude / dt;
            IsMoving = speed > _remoteMoveThreshold;
            if (IsMoving) Facing = delta.normalized;
        }

        /// <summary>Lật hình là phần biểu diễn thuần tuý, xử lý cục bộ và không đồng bộ.</summary>
        private void ApplyFlip()
        {
            if (_renderer == null) return;

            Vector2 look = Time.time < _facingOverrideUntil ? _facingOverride : Facing;
            if (!IsMoving && Time.time >= _facingOverrideUntil) return;

            if (Mathf.Abs(look.x) > 0.01f) _renderer.flipX = look.x < 0f;
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
