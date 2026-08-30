using LAC.Core;
using LAC.VFX;
using Mirror;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Lướt: một cú bật nhanh theo hướng đang nhìn, bất tử trong suốt thời gian lướt.
    /// </summary>
    /// <remarks>
    /// Đây là thao tác duy nhất ngoài di chuyển mà người chơi có — xem CLAUDE.md mục 1.1.
    /// Nó vừa là công cụ né đòn, vừa là cách kích hoạt Trống Đồng ở T-19.
    ///
    /// <b>Vì sao i-frame phải đi qua host.</b> Chuyển động lướt do client tự chạy, giống mọi
    /// chuyển động khác ở T-10. Nhưng sát thương thì host giữ toàn quyền (mục 3.2), nên nếu
    /// chỉ client biết mình đang bất tử thì host vẫn trừ máu như thường. Vì vậy client vừa
    /// lướt cục bộ ngay lập tức, vừa gửi <see cref="CmdDash"/> để host mở cửa sổ bất tử của
    /// riêng nó. Hai cửa sổ không trùng khít nhau — xem chú thích tại <see cref="CmdDash"/>.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerDash : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private SpriteRenderer _renderer;

        [Header("Vệt mờ")]
        [SerializeField] private DashAfterimage _afterimagePrefab;

        [Tooltip("Khoảng thời gian giữa hai ảnh mờ liên tiếp.")]
        [SerializeField, Min(0.01f)] private float _afterimageInterval = 0.03f;

        [Header("Mạng")]
        [Tooltip("Mức bù độ trễ tối đa cho cửa sổ bất tử phía host.")]
        [SerializeField, Min(0f)] private float _maxLatencyCompensation = 0.2f;

        [Tooltip("Chỉ số dùng khi chưa nạp được nhân vật.")]
        [SerializeField, Min(0.02f)] private float _fallbackDuration = 0.15f;
        [SerializeField, Min(0.05f)] private float _fallbackCooldown = 0.4f;
        [SerializeField, Min(0.1f)] private float _fallbackSpeed = 40f;

        private bool _isDashing;
        private float _dashRemaining;
        private float _dashTimeoutAt;
        private float _readyAt;
        private float _nextAfterimageAt;
        private Vector2 _dashDirection;

        private float _serverInvulnerableUntil;
        private float _serverReadyAt;

        private ObjectPool<DashAfterimage> _afterimagePool;

        private float Duration => _character != null && _character.Data != null
            ? _character.Data.DashDuration : _fallbackDuration;

        private float Cooldown => _character != null && _character.Data != null
            ? _character.Data.DashCooldown : _fallbackCooldown;

        private float Speed => _character != null && _character.Data != null
            ? _character.Data.DashSpeed : _fallbackSpeed;

        private float Distance => _character != null && _character.Data != null
            ? _character.Data.DashDistance : _fallbackSpeed * _fallbackDuration;

        /// <summary>Đang trong pha lướt trên máy này.</summary>
        public bool IsDashing => _isDashing;

        /// <summary>Thời gian hồi còn lại, tính bằng giây. Dành cho giao diện ở T-20.</summary>
        public float CooldownRemaining => Mathf.Max(_readyAt - Time.time, 0f);

        /// <summary>Phần thời gian hồi đã trôi qua, từ 0 đến 1. Dành cho giao diện.</summary>
        public float CooldownFraction =>
            Cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / Cooldown);

        /// <summary>
        /// Nhân vật đang bất tử hay không. <see cref="DamageSystem"/> ở T-13 hỏi giá trị này.
        /// </summary>
        /// <remarks>
        /// Trên host trả về cửa sổ do host tự giữ, không phải cửa sổ cục bộ của client — đây
        /// mới là con số có thẩm quyền. Trên client giá trị chỉ dùng cho phần biểu diễn.
        /// </remarks>
        public bool IsInvulnerable => isServer ? Time.time < _serverInvulnerableUntil : IsDashing;

        private void Update()
        {
            if (IsDashing) EmitAfterimage();

            if (!isOwned || _input == null) return;
            if (!_input.DashPressedThisFrame) return;
            if (Time.time < _readyAt || IsDashing) return;

            BeginLocalDash();
            CmdDash();
        }

        /// <summary>
        /// Đẩy nhân vật đi cho tới khi tiêu hết quãng đường lướt.
        /// </summary>
        /// <remarks>
        /// Chạy theo quãng đường còn lại chứ không theo đồng hồ. Chạy theo đồng hồ thì bước
        /// vật lý cuối cùng bị cắt dở và cú lướt ngắn hơn con số ghi trong
        /// <see cref="CharacterData"/> vài phần trăm — đo được 5.46 và 5.54 trên quãng đường
        /// đặt là 6. Sai số đó thay đổi theo tốc độ khung hình, nên cùng một khoảng cách né
        /// lúc qua được lúc không, và người chơi không có cách nào học được tầm lướt của mình.
        /// </remarks>
        private void FixedUpdate()
        {
            if (!_isDashing) return;

            // Nhân vật của người khác lấy vị trí từ NetworkTransform, không tự chạy vật lý.
            if (!isOwned)
            {
                if (Time.time >= _dashTimeoutAt) _isDashing = false;
                return;
            }

            float travel = Mathf.Min(Speed * Time.fixedDeltaTime, _dashRemaining);
            _dashRemaining -= travel;
            _rigidbody.linearVelocity = _dashDirection * (travel / Time.fixedDeltaTime);

            // Hạn giờ là lối thoát khi tường chặn giữa chừng: quãng đường không bao giờ tiêu
            // hết vì nhân vật không đi được, và nếu không có hạn giờ thì pha lướt kẹt vĩnh viễn.
            if (_dashRemaining > 0f && Time.time < _dashTimeoutAt) return;
            _isDashing = false;
        }

        /// <summary>
        /// Chạy pha lướt ngay trên máy người chơi, không chờ host trả lời.
        /// </summary>
        /// <remarks>
        /// Chờ host xác nhận rồi mới bật là chờ trọn một vòng mạng cho một thao tác né đòn.
        /// Ở 100 ms thì cú lướt luôn đến muộn hơn cú đánh mà nó định né.
        /// </remarks>
        private void BeginLocalDash()
        {
            Vector2 direction = _movement != null && _movement.Facing.sqrMagnitude > 0f
                ? _movement.Facing.normalized
                : Vector2.down;

            _dashDirection = direction;
            _dashRemaining = Distance;
            _isDashing = true;
            _dashTimeoutAt = Time.time + Duration * 2f;
            _readyAt = Time.time + Cooldown;
            _nextAfterimageAt = 0f;
        }

        /// <summary>
        /// Xin host mở cửa sổ bất tử tương ứng.
        /// </summary>
        /// <remarks>
        /// Cửa sổ của host bắt đầu muộn hơn của client đúng bằng độ trễ một chiều, vì host
        /// chỉ biết khi gói tin tới nơi. Phần đuôi được bù bằng nửa RTT nên host luôn bất tử
        /// ít nhất bằng khoảng thời gian người chơi nhìn thấy.
        ///
        /// Phần đầu — sát thương rơi vào khoảng vài chục mili giây trước khi host nhận tin —
        /// vẫn trúng. Bù được phần đó cần cơ chế tua ngược trạng thái phía host, nằm ngoài
        /// phạm vi đồ án. Nếu chơi thử thấy rõ hiện tượng "đã né mà vẫn dính", phương án
        /// thay thế là để client tự quyết i-frame của mình, cùng lập luận đã dùng cho vị trí
        /// ở T-10: LẠC hợp tác, không đối kháng.
        /// </remarks>
        [Command]
        private void CmdDash()
        {
            float compensation = connectionToClient != null
                ? Mathf.Min((float)connectionToClient.rtt * 0.5f, _maxLatencyCompensation)
                : 0f;

            // Nới hồi chiêu đúng bằng phần bù, nếu không thì độ trễ tự nó biến thành
            // hình phạt: người chơi mạng kém sẽ lướt thưa hơn người chơi mạng tốt.
            if (Time.time < _serverReadyAt - compensation) return;

            _serverInvulnerableUntil = Time.time + Duration + compensation;
            _serverReadyAt = Time.time + Cooldown;

            RpcDashStarted();
        }

        /// <summary>
        /// Báo cho các máy còn lại rằng nhân vật này vừa lướt, để vẽ vệt mờ.
        /// </summary>
        /// <remarks>
        /// Đây là đồng bộ sự kiện chứ không phải đồng bộ trạng thái: một gói tin cho một lần
        /// lướt, không phải một dòng dữ liệu liên tục. Vị trí vẫn do NetworkTransform lo.
        ///
        /// Cần thiết vì người chơi phải đọc được động tác của đồng đội — biết bạn mình vừa
        /// lướt là biết bạn mình sắp không lướt được trong 0.4 giây nữa, thông tin quyết định
        /// khi hai người dùng chung một Trống Đồng ở T-19.
        /// </remarks>
        [ClientRpc]
        private void RpcDashStarted()
        {
            if (isOwned) return;
            _isDashing = true;
            _dashTimeoutAt = Time.time + Duration;
            _nextAfterimageAt = 0f;
        }

        private void EmitAfterimage()
        {
            if (_afterimagePrefab == null || _renderer == null || _renderer.sprite == null) return;
            if (Time.time < _nextAfterimageAt) return;

            _nextAfterimageAt = Time.time + _afterimageInterval;
            _afterimagePool ??= PoolRegistry.Get(_afterimagePrefab, prewarm: 8, softLimit: 64);

            DashAfterimage ghost = _afterimagePool.Get(transform.position, Quaternion.identity);
            ghost.Play(_afterimagePool, _renderer.sprite, _renderer.color, _renderer.flipX,
                       _renderer.sortingOrder - 1);
        }
    }
}
