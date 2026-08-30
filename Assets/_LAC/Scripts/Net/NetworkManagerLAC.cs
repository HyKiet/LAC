using LAC.Core;
using LAC.Player;
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

        [Tooltip("Bảng nhân vật. Người vào ván được phân nhân vật theo thứ tự khai báo.")]
        [SerializeField] private CharacterRegistry _characterRegistry;

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
        /// Sinh nhân vật cho một người vừa nối vào và ghi họ vào ván đang chạy.
        /// </summary>
        /// <remarks>
        /// Nhân vật được phân theo thứ tự nối vào cho tới khi có màn hình chọn nhân vật ở
        /// T-20. Chỉ host quyết định ai cầm nhân vật nào — nếu để client tự chọn rồi báo lên
        /// thì hai người cùng bấm một lúc sẽ cùng nhận một nhân vật.
        ///
        /// Định danh nhân vật được gán trước <see cref="NetworkServer.AddPlayerForConnection"/>
        /// nên nó nằm sẵn trong gói trạng thái ban đầu. Gán sau sẽ tạo ra một khoảng thời gian
        /// người chơi đã hiện trên màn hình nhưng chưa có chỉ số.
        /// </remarks>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform start = GetStartPosition();
            Vector3 position = start != null ? start.position : Vector3.zero;

            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);

            if (player.TryGetComponent(out PlayerCharacter character) && _characterRegistry != null)
            {
                CharacterData assigned = _characterRegistry.GetByIndex(numPlayers);
                if (assigned != null) character.SetCharacter(assigned.Id);
            }

            NetworkServer.AddPlayerForConnection(conn, player);

            if (RunManager.Instance != null) RunManager.Instance.RegisterPlayer();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn.identity != null && RunManager.Instance != null)
                RunManager.Instance.UnregisterPlayer();

            base.OnServerDisconnect(conn);
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
