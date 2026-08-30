using UnityEngine;
using UnityEngine.InputSystem;

namespace LAC.Player
{
    /// <summary>
    /// Đọc thao tác của người chơi ngồi trước máy này. Chỉ bật trên nhân vật của chính họ.
    /// </summary>
    /// <remarks>
    /// Tách việc đọc thiết bị ra khỏi việc di chuyển vì hai thứ này thay đổi vì lý do khác
    /// nhau: sơ đồ phím thay đổi khi thêm thiết bị, còn cách di chuyển thay đổi khi cân bằng
    /// lối chơi. Gộp lại thì mỗi lần thêm một nút là một lần đụng vào mã vật lý.
    ///
    /// Thành phần này không biết gì về mạng. Việc quyết định nhân vật nào được nhận thao tác
    /// thuộc về <see cref="PlayerMovement"/> — nó bật thành phần này khi có quyền điều khiển.
    /// </remarks>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _actionMapName = "Gameplay";

        [Tooltip("Vùng chết của cần analog. Cần gạt nghỉ không bao giờ về đúng 0.")]
        [SerializeField, Range(0f, 0.5f)] private float _stickDeadZone = 0.2f;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _dash;

        /// <summary>Hướng di chuyển mong muốn, độ dài từ 0 đến 1.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>Người chơi vừa bấm lướt trong khung hình này. Dùng ở T-11.</summary>
        public bool DashPressedThisFrame => _dash != null && _dash.WasPressedThisFrame();

        private void Awake()
        {
            if (_actions == null)
            {
                Debug.LogError("[PlayerInputReader] Chưa gán tài sản thao tác.", this);
                enabled = false;
                return;
            }

            _map = _actions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (_map == null)
            {
                Debug.LogError($"[PlayerInputReader] Không có bản đồ thao tác '{_actionMapName}'.", this);
                enabled = false;
                return;
            }

            _move = _map.FindAction("Move", throwIfNotFound: false);
            _dash = _map.FindAction("Dash", throwIfNotFound: false);
        }

        private void OnEnable() => _map?.Enable();

        private void OnDisable()
        {
            _map?.Disable();
            Move = Vector2.zero;
        }

        private void Update()
        {
            if (_move == null) return;

            Vector2 raw = _move.ReadValue<Vector2>();

            // Vùng chết áp theo độ dài véc-tơ chứ không theo từng trục. Áp theo trục sẽ cắt
            // vuông góc và khiến hướng chéo bị lệch khi cần gạt nghiêng nhẹ.
            float magnitude = raw.magnitude;
            if (magnitude < _stickDeadZone)
            {
                Move = Vector2.zero;
                return;
            }

            Move = magnitude > 1f ? raw / magnitude : raw;
        }
    }
}
