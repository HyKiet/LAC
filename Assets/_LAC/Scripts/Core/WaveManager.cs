using LAC.Enemies;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Sinh quái cho từng đợt và báo cho <see cref="RunManager"/> khi đợt đã sạch.
    /// </summary>
    /// <remarks>
    /// <b>Thành phần đợt do cả hai máy tự tính, không ai gửi cho ai.</b> `RunManager` đã đồng
    /// bộ seed và số đợt; từ hai con số đó mỗi máy rút ra cùng một đặc tả đợt và gọi
    /// <see cref="EnemySpawner.Spawn"/> theo cùng thứ tự. Gửi danh sách quái qua mạng sẽ tốn
    /// băng thông cho một thứ hai bên đều tự suy ra được.
    ///
    /// Mỗi đợt dùng một luồng ngẫu nhiên riêng, gieo từ seed của ván cộng số hiệu đợt, chứ
    /// không rút tiếp từ luồng `RunRandom.Enemies`. Lý do: người chơi thứ hai vào giữa ván
    /// sẽ không có lịch sử rút của các đợt trước, nên nếu dùng luồng nối tiếp thì họ tính ra
    /// một đợt hoàn toàn khác. Luồng theo đợt làm kết quả chỉ phụ thuộc số hiệu đợt.
    ///
    /// Chỉ host quyết định thời điểm đợt kết thúc, vì chỉ host biết chắc con quái nào đã chết.
    ///
    /// <b>Đọc trạng thái chứ không nghe sự kiện.</b> Bản đầu đăng ký vào `RunManager.WaveStarted`
    /// trong `Start`, nhưng `NetworkManagerLAC.Start` khởi động host và phát sự kiện đó cũng
    /// trong `Start`, mà Unity không bảo đảm thứ tự `Start` giữa các đối tượng trong scene.
    /// Kết quả là một cuộc đua: máy nào để `WaveManager` chạy trước thì có quái, máy nào chạy
    /// sau thì sân trống. So sánh số hiệu đợt trong `Update` không có cửa sổ nào để lỡ, và
    /// đồng thời xử lý được người chơi vào giữa ván.
    /// </remarks>
    public sealed class WaveManager : NetworkBehaviour
    {
        [Header("Nội dung đợt — tạm thời")]
        [Tooltip("Loại quái duy nhất cho tới khi có bảng đợt ở T-44.")]
        [SerializeField] private EnemyData _enemy;

        [SerializeField, Min(1)] private int _baseCount = 6;

        [Tooltip("Số quái cộng thêm mỗi đợt.")]
        [SerializeField, Min(0f)] private float _countPerWave = 2f;

        [Tooltip("Trần số quái cùng lúc. Ngân sách hiệu năng là 40.")]
        [SerializeField, Min(1)] private int _maxCount = 40;

        [Tooltip("Khoảng cách từ biên sân vào trong, nơi quái hiện ra.")]
        [SerializeField, Min(0f)] private float _spawnInset = 1.2f;

        [Header("Tạm thời — gỡ khi có hệ thống thẻ")]
        [Tooltip("Tự chuyển sang đợt kế tiếp mà không cần chọn thẻ. Tắt khi T-22 và T-23 xong.")]
        [SerializeField] private bool _autoAdvanceCardSelection = true;

        [SerializeField, Min(0f)] private float _autoAdvanceDelay = 1.5f;

        private int _spawnedWave;
        private RunState _lastState = RunState.Idle;
        private float _advanceAt;

        private void Update()
        {
            RunManager run = RunManager.Instance;
            if (run == null) return;

            if (run.State != _lastState)
            {
                _lastState = run.State;
                if (_lastState == RunState.Victory || _lastState == RunState.Defeat) EndRun();
            }

            // Số hiệu đợt là nguồn sự thật. Đợt nào chưa sinh quái thì sinh, bất kể máy này
            // có mặt từ đầu ván hay vào giữa chừng.
            if (run.State == RunState.WaveActive && run.CurrentWave > 0 && run.CurrentWave != _spawnedWave)
            {
                _spawnedWave = run.CurrentWave;
                SpawnWave(run.CurrentWave);
            }

            if (!isServer) return;

            if (run.State == RunState.WaveActive && run.CurrentWave == _spawnedWave && EnemyRegistry.Count == 0)
            {
                run.ReportWaveCleared();

                // Chỗ giữ tạm cho màn hình chọn thẻ. Khi T-22 và T-23 xong thì lớp thẻ mới
                // là thứ gọi ReportCardSelectionComplete, và cờ ở trên được tắt.
                if (_autoAdvanceCardSelection) _advanceAt = Time.time + _autoAdvanceDelay;
                return;
            }

            if (_advanceAt <= 0f || Time.time < _advanceAt) return;

            _advanceAt = 0f;
            run.ReportCardSelectionComplete();
        }

        /// <summary>
        /// Ván kết thúc: ngừng sinh quái nhưng <b>giữ nguyên đàn quái trên sân</b>.
        /// </summary>
        /// <remarks>
        /// Bản đầu thu hồi toàn bộ quái ngay khi thua. Hệ quả là màn hình sạch trơn đúng lúc
        /// người chơi muốn biết vì sao mình chết — và trong lúc chưa có màn hình kết thúc ván
        /// ở T-20, nó trông y hệt như quái chưa từng được sinh ra. Sân được dọn khi ván mới
        /// bắt đầu, không phải khi ván cũ kết thúc.
        /// </remarks>
        private void EndRun()
        {
            _spawnedWave = 0;
            _advanceAt = 0f;
        }

        /// <summary>Chạy trên cả hai máy: mỗi máy tự sinh đúng đàn quái của mình.</summary>
        private void SpawnWave(int waveIndex)
        {
            if (EnemySpawner.Instance == null || _enemy == null)
            {
                Debug.LogError("[WaveManager] Thiếu bộ sinh quái hoặc chỉ số quái.", this);
                return;
            }

            // Ván mới bắt đầu: dọn nốt đàn quái của ván trước, thứ được cố ý giữ lại để người
            // chơi nhìn được thứ đã giết mình.
            if (waveIndex == 1) EnemySpawner.Instance.ResetRun();

            var stream = new RandomStream(RunRandom.Seed, "wave" + waveIndex);
            int count = Mathf.Min(_baseCount + Mathf.RoundToInt(_countPerWave * (waveIndex - 1)), _maxCount);

            for (int i = 0; i < count; i++)
                EnemySpawner.Instance.Spawn(_enemy, EdgePoint(stream));
        }

        /// <summary>Một điểm ngẫu nhiên sát biên sân — quái đi vào từ ngoài rìa.</summary>
        /// <remarks>
        /// Sinh sát biên chứ không sinh quanh người chơi: quái hiện ra ngay cạnh người chơi
        /// là đòn không né được, và người chơi không có cách nào phòng bị.
        /// </remarks>
        private Vector2 EdgePoint(RandomStream stream)
        {
            Rect rect = ArenaBounds.Instance != null
                ? ArenaBounds.Instance.Rect
                : new Rect(-18f, -10f, 36f, 20f);

            float minX = rect.xMin + _spawnInset;
            float maxX = rect.xMax - _spawnInset;
            float minY = rect.yMin + _spawnInset;
            float maxY = rect.yMax - _spawnInset;

            switch (stream.Range(0, 4))
            {
                case 0: return new Vector2(stream.Range(minX, maxX), maxY);
                case 1: return new Vector2(stream.Range(minX, maxX), minY);
                case 2: return new Vector2(minX, stream.Range(minY, maxY));
                default: return new Vector2(maxX, stream.Range(minY, maxY));
            }
        }
    }
}
