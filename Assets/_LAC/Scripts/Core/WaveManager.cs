using System.Collections.Generic;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Thi hành từng đợt quái theo đặc tả, và báo cho <see cref="RunManager"/> khi đợt đã sạch.
    /// </summary>
    /// <remarks>
    /// Wave tính giờ: host quyết spawn theo lịch, trần dân số và vị trí an toàn rồi phát
    /// sự kiện qua EnemySpawner. Client dựng quái từ sự kiện/snapshot; không quyết theo
    /// số quái sống cục bộ. Boss dùng lịch xác định từ seed trên cả hai máy như trước.
    /// Host giữ thời hạn wave trong RunManager, client chỉ vẽ đồng hồ NetworkTime.
    /// </remarks>
    public sealed class WaveManager : NetworkBehaviour
    {
        /// <summary>Một con quái đã được xếp lịch: đánh con gì, ở đâu, sau bao lâu.</summary>
        private readonly struct ScheduledSpawn
        {
            public readonly EnemyData Enemy;
            public readonly Vector2 Position;

            /// <summary>Giây tính từ lúc đợt bắt đầu.</summary>
            public readonly float Delay;

            public ScheduledSpawn(EnemyData enemy, Vector2 position, float delay)
            {
                Enemy = enemy;
                Position = position;
                Delay = delay;
            }
        }

        [Header("Nội dung đợt")]
        [Tooltip("Bảng 10 đợt. Cũng là nguồn của tổng số đợt trong ván.")]
        [SerializeField] private WaveTable _table;

        [Header("Điểm sinh")]
        [Tooltip("Khoảng cách từ biên sân vào trong, nơi quái hiện ra.")]
        [SerializeField, Min(0f)] private float _spawnInset = 1.2f;

        [Tooltip("Độ tãi quanh mỗi hướng, tính theo tỉ lệ chu vi sân. 0 là quái chồng lên nhau.")]
        [SerializeField, Range(0f, 0.25f)] private float _directionSpread = 0.06f;

        private readonly List<ScheduledSpawn> _schedule = new List<ScheduledSpawn>(64);
        private int _nextSpawnIndex;
        private float _waveStartedAt;

        private int _spawnedWave;
        private int _runSeed;
        private bool _hasRunSeed;
        private bool _timed;
        private RunState _lastState = RunState.Idle;

        private void Update()
        {
            RunManager run = RunManager.Instance;
            if (run == null) return;

            // Defeat và Restart có thể cùng một lần cập nhật mạng: client chỉ thấy wave 1
            // trước/sau như nhau. Seed mới vẫn phải dọn lịch sinh và sequence snapshot cũ.
            if (!_hasRunSeed || _runSeed != run.Seed)
            {
                _hasRunSeed = true;
                _runSeed = run.Seed;
                _spawnedWave = 0;
                _schedule.Clear();
                _nextSpawnIndex = 0;
                if (EnemySpawner.Instance != null) EnemySpawner.Instance.ResetRun();
            }

            if (run.State != _lastState)
            {
                _lastState = run.State;
                if (_lastState == RunState.Victory || _lastState == RunState.Defeat) EndRun();
                if (_lastState == RunState.CardSelection)
                {
                    _schedule.Clear();
                    _nextSpawnIndex = 0;
                    PoolRegistry.ReleaseAllExcept<SoulPickup>();
                    EnemySpawner.Instance?.ClearWaveCombat();
                }
            }

            // Số hiệu đợt là nguồn sự thật. Đợt nào chưa xếp lịch thì xếp, bất kể máy này có
            // mặt từ đầu ván hay vào giữa chừng.
            if (run.State == RunState.WaveActive && run.CurrentWave > 0 && run.CurrentWave != _spawnedWave)
            {
                _spawnedWave = run.CurrentWave;
                BuildSchedule(run.CurrentWave);
            }

            if (run.State != RunState.WaveActive) return;
            if (isServer && run.IsTimedWave && run.WaveSecondsRemaining <= 0f)
            {
                run.ReportWaveCleared();
                return;
            }
            ReleaseDueSpawns();

            if (!isServer) return;

            if (!run.IsTimedWave && run.CurrentWave == _spawnedWave && IsWaveCleared())
            {
                run.ReportWaveCleared();

                return;
            }
        }

        /// <summary>
        /// Đợt đã sạch khi <b>không còn con nào chờ vào sân</b> và không còn con nào sống.
        /// </summary>
        /// <remarks>
        /// Vế đầu là bắt buộc kể từ khi quái vào sân theo nhịp. Trước đó cả đợt hiện ra trong
        /// một khung hình nên đếm số quái sống là đủ; giờ thì giữa hai lượt sinh, sân có thể
        /// sạch trong chốc lát nếu người chơi dọn nhanh hơn nhịp vào. Thiếu vế đầu, host sẽ
        /// tuyên bố đợt xong ngay giữa đợt và chạy hết 15 đợt trong vài giây.
        /// </remarks>
        private bool IsWaveCleared()
        {
            return _nextSpawnIndex >= _schedule.Count && EnemyRegistry.Count == 0;
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
            _schedule.Clear();
            _nextSpawnIndex = 0;
            _spawnedWave = 0;
        }

        /// <summary>
        /// Xếp trước toàn bộ lịch sinh quái của một đợt. <b>Chạy trên cả hai máy.</b>
        /// </summary>
        /// <remarks>
        /// Đợt tính giờ lặp thành phần đến hết thời hạn. Chỉ host tạo lịch này để tránh
        /// client vào muộn tự tái tạo quái đã chết. Boss vẫn dùng một lịch từ seed.
        /// </remarks>
        private void BuildSchedule(int waveIndex)
        {
            _schedule.Clear();
            _nextSpawnIndex = 0;
            _waveStartedAt = Time.time;

            if (EnemySpawner.Instance == null || _table == null)
            {
                Debug.LogError("[WaveManager] Thiếu bộ sinh quái hoặc bảng đợt.", this);
                return;
            }

            // Ván mới bắt đầu: dọn nốt đàn quái của ván trước, thứ được cố ý giữ lại để người
            // chơi nhìn được thứ đã giết mình.
            if (waveIndex == 1) EnemySpawner.Instance.ResetRun();
            EnemySpawner.Instance.BeginWave(waveIndex);

            var stream = new RandomStream(RunRandom.Seed, "wave" + waveIndex);
            WaveSpec spec = _table.BuildSpec(waveIndex, stream);
            if (isServer) spec = RunManager.Instance.GetHostWaveSpec(waveIndex, spec);
            _timed = spec.Duration > 0f;
            if (isServer) RunManager.Instance.Telemetry?.RecordWaveSpec(spec, _table.name);
            if (_timed && !isServer) return;

            if (spec.TotalCount == 0)
            {
                Debug.LogError($"[WaveManager] Đợt {waveIndex} không có quái nào trong bảng.", this);
                return;
            }

            Rect rect = SpawnRect();
            int placed = 0;

            do
            {
                foreach (EnemyData enemy in Interleave(spec))
                {
                    float anchor = spec.Directions[placed % spec.Directions.Length];
                    float offset = stream.Range(-_directionSpread, _directionSpread);
                    // Phép chia số nguyên là có chủ ý: nó gộp quái thành từng lượt Burst con
                    // cùng vào sân, thay vì rải đều từng con một.
                    float delay = placed / spec.Burst * spec.SpawnInterval;
                    if (_timed)
                    {
                        delay = placed / spec.Burst * Mathf.Max(.1f, spec.SpawnInterval);
                        if (delay >= spec.Duration) return;
                    }

                    _schedule.Add(new ScheduledSpawn(enemy, PerimeterPoint(rect, anchor + offset), delay));
                    placed++;
                }
            }
            while (_timed);
        }

        /// <summary>
        /// Trả về các loại quái của đợt, đan xen theo vòng thay vì hết loại này tới loại kia.
        /// </summary>
        /// <remarks>
        /// Đan xen quyết định độ khó thật sự của một đợt hỗn hợp. Đổ hết một loại rồi mới tới
        /// loại sau thì người chơi đối phó với từng loại riêng lẻ, tức là đợt hỗn hợp không
        /// khó hơn hai đợt thuần nối nhau. Đan xen bắt người chơi xử lý nhiều mối đe doạ khác
        /// tốc độ cùng lúc — đúng mục đích của cột thành phần đợt trong bảng.
        ///
        /// Đan xen theo vòng chứ không xáo trộn ngẫu nhiên: kết quả cố định thì nhóm đối chứng
        /// ở T-53 gặp đúng cùng một trình tự trong mọi lượt chơi.
        /// </remarks>
        private static IEnumerable<EnemyData> Interleave(WaveSpec spec)
        {
            var remaining = new int[spec.Mix.Length];
            for (int i = 0; i < spec.Mix.Length; i++) remaining[i] = spec.Mix[i].Count;

            int left = spec.TotalCount;
            while (left > 0)
            {
                for (int i = 0; i < remaining.Length; i++)
                {
                    if (remaining[i] <= 0) continue;

                    remaining[i]--;
                    left--;
                    yield return spec.Mix[i].Enemy;
                }
            }
        }

        /// <summary>Đưa vào sân những con quái đã tới lượt. Chạy trên cả hai máy.</summary>
        private void ReleaseDueSpawns()
        {
            if (_nextSpawnIndex >= _schedule.Count) return;

            float elapsed = _timed ? RunManager.Instance.WaveDuration - RunManager.Instance.WaveSecondsRemaining
                : Time.time - _waveStartedAt;
            while (_nextSpawnIndex < _schedule.Count && _schedule[_nextSpawnIndex].Delay <= elapsed)
            {
                ScheduledSpawn spawn = _schedule[_nextSpawnIndex++];
                if (!_timed) EnemySpawner.Instance.Spawn(spawn.Enemy, spawn.Position);
                else if (isServer && HasPopulationBudget(spawn.Enemy) && TrySafePosition(spawn.Position, out Vector2 safe))
                    EnemySpawner.Instance.SpawnTimed(spawn.Enemy, safe);
            }
        }

        private bool HasPopulationBudget(EnemyData next)
        {
            int reserved = next.PopulationCost;
            foreach (var enemy in EnemyRegistry.Alive)
                if (enemy != null && enemy.IsAlive) reserved += enemy.Data.PopulationCost;
            return reserved <= Mathf.Min(LAC.Director.SafetyConstraints.MaxConcurrent, _table.MaxConcurrent);
        }

        private bool TrySafePosition(Vector2 candidate, out Vector2 safe)
        {
            Rect rect = SpawnRect();
            for (int i = 0; i < 17; i++)
            {
                safe = i == 0 ? candidate : PerimeterPoint(rect, (i - 1) / 16f);
                bool clear = true;
                foreach (var player in PlayerRegistry.All)
                    if (player != null && player.IsAlive && ((Vector2)player.transform.position - safe).sqrMagnitude <
                        Mathf.Max(LAC.Director.SafetyConstraints.MinimumSpawnDistance, _table.SafeSpawnDistance) *
                        Mathf.Max(LAC.Director.SafetyConstraints.MinimumSpawnDistance, _table.SafeSpawnDistance))
                    { clear = false; break; }
                if (clear) return true;
            }
            safe = candidate;
            return false;
        }

        /// <summary>Vùng sinh quái — sân đã thu vào một khoảng lề.</summary>
        private Rect SpawnRect()
        {
            Rect rect = ArenaBounds.Instance != null
                ? ArenaBounds.Instance.Rect
                : new Rect(-18f, -10f, 36f, 20f);

            float inset = Mathf.Min(_spawnInset, Mathf.Min(rect.width, rect.height) * 0.4f);
            return new Rect(rect.xMin + inset, rect.yMin + inset,
                            rect.width - inset * 2f, rect.height - inset * 2f);
        }

        /// <summary>
        /// Một điểm trên chu vi sân, tham số hoá theo <paramref name="u"/> trong [0, 1).
        /// </summary>
        /// <remarks>Host kiểm khoảng cách 3 đơn vị quanh người sống trước khi spawn timed.</remarks>
        private static Vector2 PerimeterPoint(Rect rect, float u)
        {
            u -= Mathf.Floor(u);

            float perimeter = (rect.width + rect.height) * 2f;
            float d = u * perimeter;

            if (d < rect.width) return new Vector2(rect.xMin + d, rect.yMin);
            d -= rect.width;

            if (d < rect.height) return new Vector2(rect.xMax, rect.yMin + d);
            d -= rect.height;

            if (d < rect.width) return new Vector2(rect.xMax - d, rect.yMax);
            d -= rect.width;

            return new Vector2(rect.xMin, rect.yMax - d);
        }
    }
}
