using System.Collections.Generic;
using UnityEngine;

namespace LAC.Enemies
{
    /// <summary>
    /// Danh sách quái đang sống trên máy hiện tại, tra cứu được theo định danh.
    /// </summary>
    /// <remarks>
    /// Hai nhu cầu khác nhau nên có hai cấu trúc. Vũ khí ở T-12 cần duyệt toàn bộ quái để
    /// tìm mục tiêu gần nhất, mỗi khung hình, với tối đa 40 con — cần một danh sách liền kề.
    /// Còn snapshot vị trí và sự kiện chết từ host tới nơi kèm định danh và phải tìm ra đúng
    /// con quái đó ngay — cần một bảng băm.
    ///
    /// Danh sách là cục bộ của từng máy. Hai máy có cùng tập định danh vì cùng sinh quái từ
    /// một seed, chứ không phải vì có ai đồng bộ danh sách này.
    /// </remarks>
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> _alive = new List<Enemy>(64);
        private static readonly Dictionary<int, Enemy> _byId = new Dictionary<int, Enemy>(64);

        /// <summary>Xoá trạng thái tĩnh mỗi lần vào play mode. Xem `PoolRegistry.ResetStatics`.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        public static IReadOnlyList<Enemy> Alive => _alive;

        public static int Count => _alive.Count;

        public static void Register(Enemy enemy)
        {
            if (enemy == null || _byId.ContainsKey(enemy.Id)) return;

            _alive.Add(enemy);
            _byId[enemy.Id] = enemy;
        }

        public static void Unregister(Enemy enemy)
        {
            if (enemy == null) return;

            _alive.Remove(enemy);
            _byId.Remove(enemy.Id);
        }

        public static bool TryGet(int id, out Enemy enemy) => _byId.TryGetValue(id, out enemy);

        /// <summary>Quái gần vị trí cho trước nhất, hoặc null nếu sân đã sạch.</summary>
        /// <param name="maxDistance">Bỏ qua quái xa hơn khoảng này. Truyền 0 để không giới hạn.</param>
        public static Enemy Nearest(Vector2 position, float maxDistance = 0f)
        {
            Enemy nearest = null;
            float nearestSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;

            for (int i = 0; i < _alive.Count; i++)
            {
                Enemy candidate = _alive[i];
                if (candidate == null || !candidate.IsAlive) continue;

                float sqr = (candidate.Position - position).sqrMagnitude;
                if (sqr >= nearestSqr) continue;

                nearestSqr = sqr;
                nearest = candidate;
            }

            return nearest;
        }

        /// <summary>Xoá sạch danh sách. Gọi khi kết thúc ván.</summary>
        public static void Clear()
        {
            _alive.Clear();
            _byId.Clear();
        }
    }
}
