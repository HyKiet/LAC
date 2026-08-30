using System.Collections.Generic;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Danh sách người chơi đang hoạt động trên máy hiện tại.
    /// </summary>
    /// <remarks>
    /// Quái vật và vũ khí cần tìm người chơi ở mỗi khung hình. <c>FindObjectOfType</c> quét
    /// toàn bộ scene nên bị cấm trong vòng lặp gameplay — xem CLAUDE.md mục 5. Danh sách này
    /// được cập nhật khi người chơi sinh ra và biến mất, nên tra cứu chỉ tốn một vòng lặp
    /// trên tối đa hai phần tử.
    ///
    /// Danh sách là cục bộ của từng máy, không đồng bộ qua mạng. Host và client đều có đủ
    /// người chơi vì người chơi là đối tượng có <c>NetworkIdentity</c>.
    /// </remarks>
    public static class PlayerRegistry
    {
        private static readonly List<PlayerCharacter> _players = new List<PlayerCharacter>(2);

        /// <summary>Xoá trạng thái tĩnh mỗi lần vào play mode. Xem `PoolRegistry.ResetStatics`.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _players.Clear();

        public static IReadOnlyList<PlayerCharacter> All => _players;

        public static int Count => _players.Count;

        public static void Register(PlayerCharacter player)
        {
            if (player == null || _players.Contains(player)) return;
            _players.Add(player);
        }

        public static void Unregister(PlayerCharacter player) => _players.Remove(player);

        /// <summary>Người chơi gần vị trí cho trước nhất, hoặc null nếu không còn ai.</summary>
        public static PlayerCharacter Nearest(Vector2 position)
        {
            PlayerCharacter nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < _players.Count; i++)
            {
                PlayerCharacter candidate = _players[i];
                if (candidate == null) continue;

                float sqr = ((Vector2)candidate.transform.position - position).sqrMagnitude;
                if (sqr >= nearestSqr) continue;

                nearestSqr = sqr;
                nearest = candidate;
            }

            return nearest;
        }
    }
}
