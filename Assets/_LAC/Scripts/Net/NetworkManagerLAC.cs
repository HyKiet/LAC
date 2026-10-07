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
        [SerializeField] private bool _autoStartHost = false;

        [Tooltip("Truyền tải thật, dùng khi phát hành.")]
        [SerializeField] private Transport _realTransport;

        [Tooltip("Lớp bọc giả lập độ trễ, chỉ dùng trong Editor và bản development.")]
        [SerializeField] private LatencySimulation _latencySimulation;

        [Tooltip("Bảng tra cứu ba lớp chơi được. Host thẩm định lựa chọn trước khi sinh player.")]
        [SerializeField] private CharacterRegistry _characterRegistry;

        public CharacterRegistry Characters => _characterRegistry;
        public CharacterId SelectedCharacter { get; set; } = CharacterId.Warrior;

        public struct CharacterChoiceMessage : NetworkMessage
        {
            public CharacterId Character;
        }

        public override void Awake()
        {
            autoCreatePlayer = false;
            SelectTransport();
            base.Awake();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<CharacterChoiceMessage>(OnCharacterChoice);
            RunManager.Instance?.ResetForConnection();
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            SendCharacterChoice();
        }

        public override void OnClientSceneChanged()
        {
            base.OnClientSceneChanged();
            SendCharacterChoice();
        }

        private void SendCharacterChoice()
        {
            if (NetworkClient.ready && NetworkClient.localPlayer == null)
                NetworkClient.Send(new CharacterChoiceMessage { Character = SelectedCharacter });
        }

        private void OnCharacterChoice(NetworkConnectionToClient conn, CharacterChoiceMessage message)
        {
            if (!conn.isReady || conn.identity != null || numPlayers >= maxConnections) return;
            // Từ chối ID ngoài bảng trước khi tạo player; client chỉ được đề nghị một lớp hợp lệ.
            if (message.Character != CharacterId.Warrior && message.Character != CharacterId.Archer
                && message.Character != CharacterId.Lancer) return;
            SpawnPlayer(conn, message.Character);
        }

        public override void Start()
        {
            base.Start();

            if (!_autoStartHost) return;
            if (NetworkServer.active || NetworkClient.active) return;

            StartHost();
        }

        /// <summary>
        /// AddPlayer mặc định không chứa ID lớp, nên không sinh player từ đường này.
        /// </summary>
        /// <remarks>
        /// Client gửi CharacterChoiceMessage khi Ready. Host chỉ chấp nhận ID hợp lệ và
        /// một player mỗi kết nối; cho phép hai người chọn cùng lớp.
        /// </remarks>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // AddPlayer mặc định không chứa lựa chọn. Chỉ message đã thẩm định được sinh player.
        }

        private void SpawnPlayer(NetworkConnectionToClient conn, CharacterId selected)
        {
            // Gán dữ liệu trước AddPlayer để gói spawn đầu tiên có đúng lớp và máu.
            Transform start = GetStartPosition();
            Vector3 position = start != null ? start.position : Vector3.zero;

            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);

            if (player.TryGetComponent(out PlayerCharacter character) && _characterRegistry != null)
            {
                CharacterData assigned = _characterRegistry.Get(selected);
                if (assigned != null) character.SetCharacter(assigned.Id);
            }

            NetworkServer.AddPlayerForConnection(conn, player);

            if (RunManager.Instance != null) RunManager.Instance.RegisterPlayer();
            RunManager.Instance?.Telemetry?.Join(character);
        }

        public override void OnStopHost()
        {
            RunManager.Instance?.Telemetry?.Finish("aborted");
            base.OnStopHost();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn.identity != null && RunManager.Instance != null)
            {
                RunManager.Instance.Telemetry?.Leave(conn.identity.GetComponent<PlayerCharacter>());
                RunManager.Instance.UnregisterPlayer(conn.identity.GetComponent<PlayerCharacter>().IsAlive);
            }

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
