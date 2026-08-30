using System;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Điều phối vòng đời một ván: khởi tạo seed, tiến trình 16 đợt, điều kiện thắng thua.
    /// </summary>
    /// <remarks>
    /// Host là thẩm quyền duy nhất. Client không tự quyết định đợt hiện tại hay kết quả ván;
    /// nó nhận trạng thái qua SyncVar và chỉ dựng phần biểu diễn.
    /// Lớp này không tự sinh quái và không tự đếm quái còn sống. Các hệ thống khác báo cáo
    /// vào đây qua <see cref="ReportWaveCleared"/> và <see cref="ReportPlayerDown"/>. Tách
    /// như vậy để bộ điều phối không phải biết chi tiết của spawner hay hệ thống máu.
    /// </remarks>
    public sealed class RunManager : NetworkBehaviour
    {
        /// <summary>
        /// Điểm truy cập tĩnh. Tự tìm lại nếu tham chiếu bị mất.
        /// </summary>
        /// <remarks>
        /// Không gán một lần trong <c>Awake</c> rồi tin vào nó mãi. Thứ tự <c>Awake</c> giữa
        /// các đối tượng trong scene là không xác định, và Mirror còn tắt rồi bật lại các
        /// đối tượng scene có <c>NetworkIdentity</c> khi host khởi động — đủ để tham chiếu
        /// tĩnh biến mất giữa chừng trong khi đối tượng vẫn sống. Triệu chứng của nó là quái
        /// không hiện ra ở một số lần chạy chứ không phải mọi lần, tức là một lỗi chỉ xuất
        /// hiện trên máy này mà không xuất hiện trên máy kia.
        ///
        /// Lần tìm lại tốn một lời gọi <c>FindFirstObjectByType</c>, nhưng chỉ xảy ra khi
        /// tham chiếu rỗng chứ không phải mỗi khung hình, nên không vi phạm mục 5.
        /// </remarks>
        public static RunManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<RunManager>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        private static RunManager _instance;

        // Số đợt là nội dung game, sẽ chuyển sang tài sản WaveTable ở T-44. Tạm để dạng
        // trường tuần tự hoá để khâu cân bằng chỉnh được mà không phải biên dịch lại.
        [SerializeField, Min(1)] private int _totalWaves = 16;

        [SyncVar(hook = nameof(OnSeedChanged))]
        private int _seed;

        [SyncVar]
        private int _currentWave;

        [SyncVar(hook = nameof(OnStateChanged))]
        private RunState _state = RunState.Idle;

        private int _alivePlayers;

        /// <summary>Đợt hiện tại, đánh số từ 1. Bằng 0 khi ván chưa bắt đầu.</summary>
        public int CurrentWave => _currentWave;

        public int TotalWaves => _totalWaves;
        public RunState State => _state;
        public int Seed => _seed;
        public bool IsFinalWave => _currentWave >= _totalWaves;

        /// <summary>Phát khi một đợt bắt đầu, kèm số thứ tự đợt. Chạy trên cả host và client.</summary>
        public event Action<int> WaveStarted;

        /// <summary>Phát khi một đợt được dọn sạch, kèm số thứ tự đợt vừa xong.</summary>
        public event Action<int> WaveCleared;

        /// <summary>Phát khi ván kết thúc. Tham số true là thắng.</summary>
        public event Action<bool> RunEnded;

        private void Awake()
        {
            // Điểm truy cập tĩnh nhằm loại bỏ FindObjectOfType trong vòng lặp gameplay.
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[RunManager] Đã tồn tại một RunManager trong scene.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── Thẩm quyền host ────────────────────────────────────────────────────────────

        /// <summary>Ghi nhận một người chơi vừa vào ván. Chỉ host được gọi.</summary>
        /// <remarks>
        /// Ván tự khởi động khi người đầu tiên vào. Người thứ hai vào sau chỉ làm tăng số
        /// người còn sống chứ không khởi động lại ván — hai máy không bao giờ nối vào cùng
        /// một thời điểm, nên nếu để việc vào ván khởi động lại thì người vào trước sẽ bị
        /// kéo về đợt 1 ngay giữa lúc đang đánh.
        /// </remarks>
        [Server]
        public void RegisterPlayer()
        {
            _alivePlayers++;
            if (_state == RunState.Idle) StartRun();
        }

        /// <summary>Ghi nhận một người chơi rời ván. Chỉ host được gọi.</summary>
        [Server]
        public void UnregisterPlayer()
        {
            _alivePlayers = Mathf.Max(_alivePlayers - 1, 0);
        }

        /// <summary>Bắt đầu một ván mới. Chỉ host được gọi.</summary>
        /// <param name="seed">Seed dùng chung. Truyền 0 để sinh seed mới.</param>
        [Server]
        public void StartRun(int seed = 0)
        {
            _seed = seed != 0 ? seed : RunRandom.CreateSeed();
            _alivePlayers = Mathf.Max(_alivePlayers, 1);
            _currentWave = 0;

            // Host khởi tạo ngay; client khởi tạo trong hook khi SyncVar tới nơi.
            RunRandom.Initialize(_seed);

            StartWave(1);
        }

        /// <summary>Báo cho bộ điều phối rằng đợt hiện tại đã sạch quái. Chỉ host được gọi.</summary>
        [Server]
        public void ReportWaveCleared()
        {
            if (_state != RunState.WaveActive) return;

            if (_currentWave >= _totalWaves)
            {
                EndRun(true);
                return;
            }

            _state = RunState.CardSelection;
        }

        /// <summary>
        /// Báo rằng mọi người chơi đã chọn xong thẻ và đợt kế tiếp được phép khởi động.
        /// </summary>
        /// <remarks>
        /// Đợt kế tiếp chỉ bắt đầu khi cả hai người chơi đã chọn xong — xem T-23. Điều kiện
        /// đó do hệ thống thẻ kiểm tra; ở đây chỉ nhận kết quả.
        /// </remarks>
        [Server]
        public void ReportCardSelectionComplete()
        {
            if (_state != RunState.CardSelection) return;
            StartWave(_currentWave + 1);
        }

        /// <summary>Báo một người chơi đã gục. Chỉ host được gọi.</summary>
        [Server]
        public void ReportPlayerDown()
        {
            if (_state == RunState.Victory || _state == RunState.Defeat) return;

            _alivePlayers = Mathf.Max(_alivePlayers - 1, 0);
            if (_alivePlayers == 0) EndRun(false);
        }

        /// <summary>Báo một người chơi được hồi sinh. Chỉ host được gọi.</summary>
        [Server]
        public void ReportPlayerRevived() => _alivePlayers++;

        [Server]
        private void StartWave(int waveIndex)
        {
            _currentWave = waveIndex;
            _state = RunState.WaveActive;
        }

        [Server]
        private void EndRun(bool victory)
        {
            _state = victory ? RunState.Victory : RunState.Defeat;
        }

        // ── Hook SyncVar — chạy trên client, và trên host vì host cũng là một client ────
        //
        // Cả ba sự kiện đều phát từ hook của _state, không dùng ClientRpc. RPC trên host đi
        // qua hàng đợi của kết nối cục bộ nên tới chậm một lần cập nhật mạng, trong khi hook
        // SyncVar chạy ngay. Trộn hai kênh sẽ khiến WaveCleared tới sau WaveStarted trên
        // host nhưng đúng thứ tự trên client — sai lệch rất khó truy vết ở hệ thống thẻ.
        //
        // Chỉ _state phát sự kiện WaveStarted, không phải _currentWave. Hai SyncVar cùng
        // thay đổi trong một lần cập nhật; nếu cả hai cùng phát thì sự kiện nổ hai lần và
        // đợt quái được sinh gấp đôi. _currentWave luôn được gán trước _state nên tại thời
        // điểm hook trạng thái chạy, số đợt đã đúng.

        private void OnSeedChanged(int _, int newSeed)
        {
            if (RunRandom.IsInitialized && RunRandom.Seed == newSeed) return;
            RunRandom.Initialize(newSeed);
        }

        private void OnStateChanged(RunState _, RunState newState)
        {
            switch (newState)
            {
                case RunState.WaveActive:
                    if (_currentWave > 0) WaveStarted?.Invoke(_currentWave);
                    break;
                case RunState.CardSelection:
                    WaveCleared?.Invoke(_currentWave);
                    break;
                case RunState.Victory:
                    RunEnded?.Invoke(true);
                    break;
                case RunState.Defeat:
                    RunEnded?.Invoke(false);
                    break;
            }
        }
    }
}
