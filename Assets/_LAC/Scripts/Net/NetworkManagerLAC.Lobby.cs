using System;
using System.Collections.Generic;
using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Net
{
    public sealed partial class NetworkManagerLAC
    {
        [Header("Sảnh chờ")]
        [SerializeField] private bool _useLobby;
        [SerializeField, Scene] private string _arenaScene = "Assets/_LAC/Scenes/Arena.unity";
        [SerializeField, Min(5f)] private float _connectionTimeout = 15f;
        [SerializeField, Min(10f)] private float _loadingTimeout = 45f;

        private readonly Dictionary<int, LobbyMember> _members = new Dictionary<int, LobbyMember>();
        private LobbyMember[] _visibleMembers = Array.Empty<LobbyMember>();
        private bool _loadingArena;
        private bool _runLaunched;
        private float _connectDeadline;
        private float _loadDeadline;
        private string _sessionStatus = "Mời một người bạn hoặc bắt đầu chuyến đi.";

        public event Action LobbyChanged;
        public IReadOnlyList<LobbyMember> Members => _visibleMembers;
        public CharacterRegistry Characters => _characterRegistry;
        public int LocalConnectionId { get; private set; } = -1;
        public bool IsLoadingArena => _loadingArena;
        public string SessionStatus => _sessionStatus;
        public bool UsesLobby => _useLobby;
        public bool CanStart
        {
            get
            {
                if (!NetworkServer.active || _loadingArena || _members.Count == 0) return false;
                foreach (LobbyMember member in _members.Values)
                    if (!member.Ready) return false;
                return true;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _members.Clear();
            _loadingArena = false;
            _runLaunched = false;
            if (_useLobby) NetworkServer.RegisterHandler<LobbyChoiceMessage>(OnLobbyChoice);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_useLobby) NetworkClient.RegisterHandler<LobbyStateMessage>(OnLobbyState);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            if (!_useLobby) return;
            if (_loadingArena || _members.Count >= 2)
            {
                conn.Disconnect();
                return;
            }
            if (_characterRegistry == null || _characterRegistry.Count == 0)
            {
                Debug.LogError("[NetworkManagerLAC] CharacterRegistry của sảnh đang trống.", this);
                conn.Disconnect();
                return;
            }
            CharacterData character = _characterRegistry.GetByIndex(_members.Count);
            if (character == null)
            {
                Debug.LogError("[NetworkManagerLAC] Không thể gán nhân vật mặc định cho thành viên sảnh.", this);
                conn.Disconnect();
                return;
            }
            _members.Add(conn.connectionId, new LobbyMember
            {
                ConnectionId = conn.connectionId,
                Character = character.Id,
                Ready = conn is LocalConnectionToClient,
                Host = conn is LocalConnectionToClient
            });
            BroadcastLobby();
        }

        public override void OnClientConnect()
        {
            _connectDeadline = 0f;
            base.OnClientConnect();
            SetSessionStatus("Đã vào sảnh. Chọn nhân vật và xác nhận sẵn sàng.");
        }

        public void HostLobby()
        {
            if (!_useLobby || NetworkClient.active || NetworkServer.active) return;
            autoCreatePlayer = false;
            StartHost();
        }

        public bool JoinLobby(string address)
        {
            if (!_useLobby || NetworkClient.active || NetworkServer.active) return false;
            address = address?.Trim();
            if (string.IsNullOrWhiteSpace(address) || Uri.CheckHostName(address) == UriHostNameType.Unknown)
            {
                SetSessionStatus("Nhập địa chỉ IP hoặc tên máy hợp lệ.");
                return false;
            }
            networkAddress = address;
            autoCreatePlayer = false;
            _connectDeadline = Time.unscaledTime + _connectionTimeout;
            SetSessionStatus("Đang kết nối tới " + address + "…");
            StartClient();
            return true;
        }

        public void ChooseCharacter(CharacterId character, bool ready)
        {
            if (!NetworkClient.isConnected || _loadingArena) return;
            NetworkClient.Send(new LobbyChoiceMessage { Character = character, Ready = ready });
        }

        private void OnLobbyChoice(NetworkConnectionToClient conn, LobbyChoiceMessage message)
        {
            if (_loadingArena || !_members.TryGetValue(conn.connectionId, out LobbyMember member)) return;
            bool valid = false;
            for (int i = 0; i < _characterRegistry.Count; i++)
                if (_characterRegistry.GetByIndex(i).Id == message.Character) valid = true;
            if (!valid) return;
            // Đổi nhân vật huỷ sẵn sàng của khách, tránh start với lựa chọn chưa xác nhận.
            bool changed = member.Character != message.Character;
            member.Character = message.Character;
            member.Ready = member.Host || (!changed && message.Ready);
            _members[conn.connectionId] = member;
            BroadcastLobby();
        }

        public bool BeginArena()
        {
            if (!CanStart) return false;
            _loadingArena = true;
            _loadDeadline = Time.unscaledTime + _loadingTimeout;
            BroadcastLobby();
            ServerChangeScene(_arenaScene);
            return true;
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            if (!_useLobby || !_loadingArena || SceneManager.GetActiveScene().path != _arenaScene) return;
            if (_members.ContainsKey(conn.connectionId)) OnServerAddPlayer(conn);
            TryLaunchRun();
        }

        private void TryLaunchRun()
        {
            if (!_loadingArena || _runLaunched || _members.Count == 0) return;
            foreach (int id in _members.Keys)
                if (!NetworkServer.connections.TryGetValue(id, out NetworkConnectionToClient conn) ||
                    !conn.isReady || conn.identity == null) return;
            RunManager run = RunManager.Instance;
            if (run == null) return;
            _runLaunched = true;
            _loadDeadline = 0f;
            // Chỉ bắt đầu sau khi CẢ HAI đã tải scene và có player để không mất đợt đầu.
            run.StartRun();
        }

        public override void Update()
        {
            base.Update();
            if (_connectDeadline > 0f && Time.unscaledTime > _connectDeadline)
            {
                LeaveSession();
                SetSessionStatus("Không kết nối được. Kiểm tra địa chỉ, mạng LAN và cổng UDP 7777.");
            }
            if (!NetworkServer.active || _loadDeadline <= 0f || Time.unscaledTime <= _loadDeadline) return;
            // Một máy không tải xong không được giữ cả phòng ở trạng thái loading mãi.
            LeaveSession();
            SetSessionStatus("Một người chơi không tải xong đấu trường. Hãy tạo lại phòng.");
        }

        private void BroadcastLobby()
        {
            var members = new List<LobbyMember>(_members.Values);
            members.Sort((a, b) => a.ConnectionId.CompareTo(b.ConnectionId));
            LobbyMember[] snapshot = members.ToArray();
            foreach (int id in _members.Keys)
                if (NetworkServer.connections.TryGetValue(id, out NetworkConnectionToClient conn))
                    conn.Send(new LobbyStateMessage
                    {
                        Members = snapshot, LocalConnectionId = id, Loading = _loadingArena
                    });
        }

        private void OnLobbyState(LobbyStateMessage message)
        {
            _visibleMembers = message.Members ?? Array.Empty<LobbyMember>();
            LocalConnectionId = message.LocalConnectionId;
            _loadingArena = message.Loading;
            LobbyChanged?.Invoke();
        }

        private void RemoveLobbyMember(int id)
        {
            if (!_useLobby || !_members.Remove(id)) return;
            BroadcastLobby();
            TryLaunchRun();
        }

        public void LeaveSession()
        {
            _connectDeadline = 0f;
            _loadDeadline = 0f;
            if (NetworkServer.active && NetworkClient.active) StopHost();
            else if (NetworkClient.active) StopClient();
            else if (NetworkServer.active) StopServer();
            ClearLobby();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            ClearLobby();
            SetSessionStatus("Đã rời phòng. Nếu mất kết nối, hãy kiểm tra máy chủ rồi thử lại.");
        }

        public override void OnClientError(TransportError error, string reason)
        {
            SetSessionStatus("Kết nối gặp lỗi: " + reason);
        }

        private void ClearLobby()
        {
            _visibleMembers = Array.Empty<LobbyMember>();
            LocalConnectionId = -1;
            _loadingArena = false;
            _runLaunched = false;
            _connectDeadline = 0f;
            _loadDeadline = 0f;
            LobbyChanged?.Invoke();
        }

        private void SetSessionStatus(string value)
        {
            _sessionStatus = value;
            LobbyChanged?.Invoke();
        }
    }
}
