using LAC.Enemies;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Đặc tả một đợt quái: đánh những con nào, bao nhiêu con, vào sân theo nhịp nào và từ
    /// những hướng nào.
    /// </summary>
    /// <remarks>
    /// <b>Đây là ranh giới giữa nội dung đợt và việc thi hành đợt.</b> `WaveManager` chỉ biết
    /// kiểu này, không biết ai đã sinh ra nó. Hiện chỉ có một nguồn là bảng đợt cố định
    /// <see cref="WaveTable"/>; ở T-45 AI Đạo Diễn trở thành nguồn thứ hai, sinh cùng kiểu này
    /// từ véc-tơ ngữ cảnh — xem docs/GDD.md mục 7.3, nơi đầu ra của đạo diễn được đặc tả đúng
    /// bằng bốn trường dưới đây. Nhờ vậy T-45 không phải sửa một dòng nào trong `WaveManager`,
    /// và T-53 đổi qua lại giữa hai nguồn chỉ bằng cách đổi nguồn cấp đặc tả.
    ///
    /// Đặc tả là dữ liệu thuần, không mang trạng thái thi hành. Hai máy cùng dựng ra một đặc
    /// tả giống hệt nhau từ seed và số hiệu đợt, rồi mỗi máy tự thi hành nó.
    /// </remarks>
    public sealed class WaveSpec
    {
        /// <summary>Một nhóm quái cùng loại trong đợt.</summary>
        public readonly struct Group
        {
            public readonly EnemyData Enemy;
            public readonly int Count;

            public Group(EnemyData enemy, int count)
            {
                Enemy = enemy;
                Count = count;
            }
        }

        /// <summary>Thành phần đợt. Có thể rỗng nếu bảng đợt điền thiếu.</summary>
        public Group[] Mix { get; }

        /// <summary>Tổng số quái của cả đợt, đã cộng qua mọi nhóm.</summary>
        public int TotalCount { get; }

        /// <summary>
        /// Các hướng quái đi vào sân, mỗi giá trị là một vị trí trên chu vi sân trong [0, 1).
        /// </summary>
        /// <remarks>
        /// Dùng toạ độ chu vi chứ không dùng góc: sân là hình chữ nhật 36×20 nên chia đều theo
        /// góc sẽ dồn quái về hai cạnh ngắn. Chia đều theo chu vi cho mật độ đồng đều trên mọi
        /// cạnh, và ánh xạ về điểm sinh chỉ là một phép cộng dồn theo bốn cạnh.
        /// </remarks>
        public float[] Directions { get; }

        /// <summary>Giây giữa hai lượt quái hiện ra.</summary>
        public float SpawnInterval { get; }

        /// <summary>Số quái hiện ra trong một lượt.</summary>
        public int Burst { get; }

        public WaveSpec(Group[] mix, float[] directions, float spawnInterval, int burst)
        {
            Mix = mix ?? System.Array.Empty<Group>();
            Directions = directions != null && directions.Length > 0 ? directions : new[] { 0f };
            SpawnInterval = Mathf.Max(spawnInterval, 0f);
            Burst = Mathf.Max(burst, 1);

            int total = 0;
            for (int i = 0; i < Mix.Length; i++)
            {
                if (Mix[i].Enemy == null) continue;
                total += Mathf.Max(Mix[i].Count, 0);
            }
            TotalCount = total;
        }
    }
}
