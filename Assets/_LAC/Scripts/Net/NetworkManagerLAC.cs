using Mirror;
using UnityEngine;

namespace LAC.Net
{
    /// <summary>
    /// Quản lý mạng của LẠC. Luôn chạy ở host mode, kể cả khi chỉ có một người chơi.
    /// </summary>
    /// <remarks>
    /// Không tồn tại nhánh mã riêng cho chế độ chơi đơn — xem CLAUDE.md mục 3.1. Chơi đơn
    /// là host mode với một client, chơi đôi là host mode với hai client. Một luồng thực
    /// thi duy nhất, nên lỗi đồng bộ lộ ra ngay từ lần chạy thử đầu tiên thay vì đến tuần
    /// tích hợp mới phát hiện.
    /// </remarks>
    public sealed class NetworkManagerLAC : NetworkManager
    {
        [Header("LẠC")]
        [Tooltip("Tự khởi động host khi vào scene. Tắt khi đã có màn hình sảnh chờ.")]
        [SerializeField] private bool _autoStartHost = true;

        [Tooltip("Truyền tải thật, dùng khi phát hành.")]
        [SerializeField] private Transport _realTransport;

        [Tooltip("Lớp bọc giả lập độ trễ, chỉ dùng trong Editor và bản development.")]
        [SerializeField] private LatencySimulation _latencySimulation;

        public override void Awake()
        {
            SelectTransport();
            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            if (!_autoStartHost) return;
            if (NetworkServer.active || NetworkClient.active) return;

            StartHost();
        }

        /// <summary>
        /// Chọn truyền tải theo loại bản dựng.
        /// </summary>
        /// <remarks>
        /// Giả lập độ trễ 100 ms là cấu hình mặc định khi phát triển — độ trễ 0 ms của
        /// localhost che giấu toàn bộ lỗi đồng bộ, xem CLAUDE.md mục 3.2. Nhưng lớp bọc này
        /// tuyệt đối không được lọt vào bản phát hành, nên việc chọn nằm ở đây thay vì phụ
        /// thuộc vào người sửa tay trong Inspector trước khi build.
        ///
        /// Không tắt component <see cref="LatencySimulation"/>: <c>OnDisable</c> của nó tắt
        /// luôn truyền tải bên dưới. Chỉ cần không gán nó làm truyền tải đang hoạt động.
        /// </remarks>
        private void SelectTransport()
        {
            if (_realTransport == null)
            {
                Debug.LogError("[NetworkManagerLAC] Chưa gán truyền tải thật.", this);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            transport = _latencySimulation != null ? (Transport)_latencySimulation : _realTransport;

            if (_latencySimulation == null)
                Debug.LogWarning("[NetworkManagerLAC] Không có giả lập độ trễ. Lỗi đồng bộ sẽ bị localhost che giấu.", this);
#else
            transport = _realTransport;
#endif
            Transport.active = transport;
        }
    }
}
