using System.Collections.Generic;
using LAC.Enemies;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Thi hành từng đợt quái theo đặc tả, và báo cho <see cref="RunManager"/> khi đợt đã sạch.
    /// </summary>
    /// <remarks>
    /// <b>Thành phần đợt do cả hai máy tự tính, không ai gửi cho ai.</b> `RunManager` đã đồng
    /// bộ seed và số đợt; từ hai con số đó mỗi máy dựng ra cùng một <see cref="WaveSpec"/> và
    /// gọi <see cref="EnemySpawner.Spawn"/> theo cùng thứ tự. Gửi danh sách quái qua mạng sẽ
    /// tốn băng thông cho một thứ hai bên đều tự suy ra được.
    ///
    /// Lớp này <b>không biết nội dung đợt đến từ đâu</b> — nó chỉ nhận một đặc tả. Hiện nguồn
    /// là bảng cố định <see cref="WaveTable"/> ở T-44; ở T-45 AI Đạo Diễn thay vào chỗ đó mà
    /// không phải sửa gì trong tệp này.
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
        [Tooltip("Bảng 16 đợt. Cũng là nguồn của tổng số đợt trong ván.")]
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
        private RunState _lastState = RunState.Idle;

        private void Update()
        {
            RunManager run = RunManager.Instance;
            if (run == null) return;

            if (run.State != _lastState)
            {
                _lastState = run.State;
                if (_lastState == RunState.Victory || _lastState == RunState.Defeat) EndRun();
            }

            // Số hiệu đợt là nguồn sự thật. Đợt nào chưa xếp lịch thì xếp, bất kể máy này có
            // mặt từ đầu ván hay vào giữa chừng.
            if (run.State == RunState.WaveActive && run.CurrentWave > 0 && run.CurrentWave != _spawnedWave)
            {
                _spawnedWave = run.CurrentWave;
                BuildSchedule(run.CurrentWave);
            }

            ReleaseDueSpawns();

            if (!isServer) return;

            if (run.State == RunState.WaveActive && run.CurrentWave == _spawnedWave && IsWaveCleared())
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
        /// tuyên bố đợt xong ngay giữa đợt và chạy hết 16 đợt trong vài giây.
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
        /// <b>Xếp trước cả đợt chứ không quyết từng lượt.</b> Định danh quái là một bộ đếm
        /// tăng dần trong `EnemySpawner`, nên hai máy chỉ đặt cùng định danh cho cùng con quái
        /// khi thứ tự sinh giống hệt nhau. Xếp sẵn một danh sách rồi chạy theo chỉ số làm thứ
        /// tự đó cố định, kể cả khi hai máy sinh quái lệch nhau về thời điểm do độ trễ. Nếu
        /// quyết từng lượt tại thời điểm sinh thì mọi thứ ảnh hưởng tới thời điểm — tốc độ
        /// khung hình chẳng hạn — đều có thể đảo thứ tự và làm lệch định danh.
        ///
        /// Thời điểm bắt đầu đợt ở hai máy vẫn lệch nhau đúng bằng độ trễ đường truyền, vì
        /// client biết đợt mới khi SyncVar tới nơi. Đó là sai lệch về biểu diễn, đã nằm trong
        /// mô hình ở CLAUDE.md mục 3.2; snapshot vị trí ở T-35 kéo lại phần lệch tích luỹ.
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

            var stream = new RandomStream(RunRandom.Seed, "wave" + waveIndex);
            WaveSpec spec = _table.BuildSpec(waveIndex, stream);

            if (spec.TotalCount == 0)
            {
                Debug.LogError($"[WaveManager] Đợt {waveIndex} không có quái nào trong bảng.", this);
                return;
            }

            Rect rect = SpawnRect();
            int placed = 0;

            foreach (EnemyData enemy in Interleave(spec))
            {
                float anchor = spec.Directions[placed % spec.Directions.Length];
                float offset = stream.Range(-_directionSpread, _directionSpread);
                // Phép chia số nguyên là có chủ ý: nó gộp quái thành từng lượt Burst con
                // cùng vào sân, thay vì rải đều từng con một.
                float delay = placed / spec.Burst * spec.SpawnInterval;

                _schedule.Add(new ScheduledSpawn(enemy, PerimeterPoint(rect, anchor + offset), delay));
                placed++;
            }
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

            float elapsed = Time.time - _waveStartedAt;
            while (_nextSpawnIndex < _schedule.Count && _schedule[_nextSpawnIndex].Delay <= elapsed)
            {
                ScheduledSpawn spawn = _schedule[_nextSpawnIndex++];
                EnemySpawner.Instance.Spawn(spawn.Enemy, spawn.Position);
            }
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
        /// <remarks>
        /// Sinh sát biên chứ không sinh quanh người chơi: quái hiện ra ngay cạnh người chơi là
        /// đòn không né được, và người chơi không có cách nào phòng bị.
        ///
        /// docs/GDD.md mục 7.6 còn cấm sinh trong bán kính 3 quanh người chơi, và luật đó
        /// <b>chưa được cài ở đây</b> — cố ý. Nó cần đọc vị trí người chơi, mà vị trí người
        /// chơi ở hai máy không bao giờ trùng khít do dự đoán cục bộ và độ trễ; đưa nó vào một
        /// phép tính phải cho ra kết quả giống nhau ở hai máy là tự tạo ra sai lệch. Luật này
        /// thuộc về tầng an toàn của đạo diễn ở T-45, nơi host là bên duy nhất quyết định.
        /// Trong lúc chờ, việc sinh sát biên đã chặn phần lớn trường hợp; chỗ hở còn lại là
        /// người chơi đứng dí vào tường.
        /// </remarks>
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
