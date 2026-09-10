using System.Collections.Generic;
using LAC.Enemies;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Bảng đợt cố định — nội dung của cả 16 đợt, biên soạn bằng tay.
    /// </summary>
    /// <remarks>
    /// Hai vai trò, đều nằm trong docs/TASKS.md T-44. Thứ nhất, đây là <b>nhóm đối chứng</b>
    /// của thực nghiệm khoá luận ở T-53: 15 người chơi bảng cố định này, 15 người chơi bản do
    /// AI Đạo Diễn sinh, rồi so sánh. Đối chứng phải bất biến giữa các lượt chơi nên nó là dữ
    /// liệu biên soạn tay chứ không phải công thức. Thứ hai, đây là <b>phương án dự phòng</b>
    /// nếu đạo diễn ở T-45 không kịp hoàn thành.
    ///
    /// <b>Cả 16 đợt nằm trong một tài sản duy nhất, không phải 16 tệp.</b> Tài sản Unity lưu
    /// dạng YAML và không hợp nhất tự động được — CLAUDE.md mục 6.2. Mười sáu tệp nghĩa là
    /// mười sáu nguồn xung đột, trong khi khâu cân bằng ở T-51 gần như luôn sửa nhiều đợt cùng
    /// một lúc. Một tệp thì mỗi lần cân bằng là một xung đột, và nhìn được cả đường cong độ
    /// khó trên một màn hình Inspector.
    /// </remarks>
    [CreateAssetMenu(fileName = "WaveTable", menuName = "LAC/Wave Table")]
    public sealed class WaveTable : ScriptableObject
    {
        /// <summary>Một nhóm quái cùng loại trong một đợt.</summary>
        [System.Serializable]
        private sealed class Group
        {
            [SerializeField] private EnemyData _enemy;
            [SerializeField, Min(0)] private int _count = 1;

            public EnemyData Enemy => _enemy;
            public int Count => _count;
        }

        /// <summary>Nội dung của một đợt.</summary>
        [System.Serializable]
        private sealed class Entry
        {
            [Tooltip("Chỉ để đọc trong Inspector, không dùng lúc chạy.")]
            [SerializeField] private string _label = "Đợt";

            [SerializeField] private Group[] _groups = new Group[0];

            [Tooltip("Giây giữa hai lượt quái hiện ra. 0 nghĩa là cả đợt đổ ra cùng lúc.")]
            [SerializeField, Min(0f)] private float _spawnInterval = 1.6f;

            [Tooltip("Số quái hiện ra trong một lượt.")]
            [SerializeField, Min(1)] private int _burst = 2;

            [Tooltip("Số hướng quái đi vào sân. Càng nhiều hướng càng khó xoay trở.")]
            [SerializeField, Range(1, 8)] private int _directions = 2;

            public string Label => _label;
            public Group[] Groups => _groups;
            public float SpawnInterval => _spawnInterval;
            public int Burst => _burst;
            public int Directions => _directions;
        }

        [SerializeField] private Entry[] _waves = new Entry[0];

        [Tooltip("Trần số quái cùng lúc trên sân. Ngân sách hiệu năng ở CLAUDE.md mục 5 là 40.")]
        [SerializeField, Min(1)] private int _maxConcurrent = 40;

        /// <summary>Số đợt của một ván. Nguồn duy nhất — `RunManager` đọc từ đây.</summary>
        public int TotalWaves => _waves.Length;

        public int MaxConcurrent => _maxConcurrent;

        /// <summary>
        /// Dựng đặc tả cho một đợt. <b>Cả hai máy cùng gọi và phải ra cùng kết quả.</b>
        /// </summary>
        /// <param name="waveIndex">Số hiệu đợt, đánh số từ 1.</param>
        /// <param name="stream">Luồng riêng của đợt này, gieo từ seed ván cộng số hiệu đợt.</param>
        /// <remarks>
        /// Chỉ hướng vào sân là ngẫu nhiên; thành phần và số lượng lấy nguyên từ bảng. Ngẫu
        /// nhiên hoá số lượng sẽ phá mất chính thứ mà nhóm đối chứng cần: hai người chơi cùng
        /// bảng cố định phải gặp cùng một lượng áp lực, nếu không thì không so sánh được với
        /// nhóm dùng đạo diễn.
        /// </remarks>
        public WaveSpec BuildSpec(int waveIndex, RandomStream stream)
        {
            Entry entry = GetEntry(waveIndex);
            if (entry == null) return new WaveSpec(null, null, 0f, 1);

            return new WaveSpec(
                BuildMix(entry),
                BuildDirections(entry.Directions, stream),
                entry.SpawnInterval,
                entry.Burst);
        }

        private Entry GetEntry(int waveIndex)
        {
            if (_waves.Length == 0) return null;

            // Ván dài hơn bảng thì lặp lại đợt cuối, không dừng sinh quái. Chuyện này chỉ xảy
            // ra khi ai đó chỉnh số đợt mà quên thêm dòng vào bảng; sân trống trơn là triệu
            // chứng khó truy hơn nhiều so với một đợt lặp lại.
            int index = Mathf.Clamp(waveIndex - 1, 0, _waves.Length - 1);
            return _waves[index];
        }

        /// <summary>
        /// Thành phần đợt, đã kẹp về trần số quái cùng lúc.
        /// </summary>
        /// <remarks>
        /// <b>Trần được áp lúc dựng đặc tả chứ không phải lúc sinh quái.</b> Chặn theo số quái
        /// đang sống trên sân thì mỗi máy đọc một con số khác nhau — sự kiện chết đi từ host
        /// xuống client mất một quãng độ trễ, nên trong quãng đó host thấy sân đã vơi còn
        /// client thì chưa. Hai máy sẽ sinh quái ở hai thời điểm khác nhau và lệch dần. Kẹp
        /// theo dữ liệu bảng là một hàm thuần của bảng, hai máy luôn ra cùng kết quả, và trần
        /// 40 con ở docs/GDD.md mục 7.6 vẫn được giữ đúng vì cả đợt có thể sống cùng lúc.
        /// </remarks>
        private WaveSpec.Group[] BuildMix(Entry entry)
        {
            var mix = new List<WaveSpec.Group>(entry.Groups.Length);
            int budget = _maxConcurrent;

            for (int i = 0; i < entry.Groups.Length && budget > 0; i++)
            {
                Group group = entry.Groups[i];
                if (group == null || group.Enemy == null || group.Count <= 0) continue;

                int count = Mathf.Min(group.Count, budget);
                budget -= count;
                mix.Add(new WaveSpec.Group(group.Enemy, count));
            }

            return mix.ToArray();
        }

        /// <summary>Các hướng vào sân, trải đều trên chu vi rồi xoay đi một góc ngẫu nhiên.</summary>
        /// <remarks>
        /// Trải đều rồi xoay chứ không rút từng hướng độc lập: rút độc lập thì hai hướng dễ
        /// rơi sát nhau và đợt bốn hướng trông y hệt đợt một hướng, tức là đúng thứ mà bảng
        /// đợt đang cố phân biệt. Phép xoay giữ lại tính bất ngờ giữa các lượt chơi.
        /// </remarks>
        private static float[] BuildDirections(int count, RandomStream stream)
        {
            count = Mathf.Max(count, 1);
            float rotation = stream != null ? stream.NextFloat() : 0f;

            var directions = new float[count];
            for (int i = 0; i < count; i++)
            {
                float u = rotation + (float)i / count;
                directions[i] = u - Mathf.Floor(u);
            }
            return directions;
        }

        private void OnValidate()
        {
            for (int i = 0; i < _waves.Length; i++)
            {
                Entry entry = _waves[i];
                if (entry == null) continue;

                int total = 0;
                for (int g = 0; g < entry.Groups.Length; g++)
                {
                    Group group = entry.Groups[g];
                    if (group != null && group.Enemy != null) total += group.Count;
                }

                if (total > _maxConcurrent)
                {
                    Debug.LogWarning(
                        $"[WaveTable] Đợt {i + 1} \"{entry.Label}\" có {total} quái, " +
                        $"vượt trần {_maxConcurrent}. " +
                        "Phần vượt sẽ bị cắt lúc chạy.", this);
                }
            }
        }
    }
}
