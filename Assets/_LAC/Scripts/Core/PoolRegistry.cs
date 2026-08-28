using System.Collections.Generic;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Sổ đăng ký pool theo prefab. Cho phép mọi hệ thống lấy đúng một pool dùng chung
    /// cho cùng một prefab mà không cần tham chiếu chéo giữa các module.
    /// </summary>
    /// <remarks>
    /// Tồn tại để loại bỏ nhu cầu gọi <c>FindObjectOfType</c> trong vòng lặp gameplay —
    /// xem CLAUDE.md mục 5. Tra cứu qua bảng băm có chi phí không đổi.
    /// </remarks>
    public static class PoolRegistry
    {
        private static readonly Dictionary<Component, IPool> Pools = new Dictionary<Component, IPool>();
        private static Transform _root;

        /// <summary>
        /// Lấy pool của một prefab, tạo mới nếu chưa tồn tại.
        /// </summary>
        /// <param name="prewarm">Chỉ có tác dụng ở lần gọi đầu tiên với prefab này.</param>
        public static ObjectPool<T> Get<T>(T prefab, int prewarm = 0, int softLimit = 256) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolRegistry] Prefab null — kiểm tra tham chiếu trong ScriptableObject dữ liệu.");
                return null;
            }

            if (Pools.TryGetValue(prefab, out IPool existing)) return (ObjectPool<T>)existing;

            var pool = new ObjectPool<T>(prefab, EnsureRoot(), prewarm, softLimit);
            Pools.Add(prefab, pool);
            return pool;
        }

        /// <summary>Thu hồi mọi đối tượng đang hoạt động ở tất cả pool. Gọi khi kết thúc một ván.</summary>
        public static void ReleaseAll()
        {
            foreach (IPool pool in Pools.Values) pool.ReleaseAll();
        }

        /// <summary>Huỷ toàn bộ pool. Gọi khi rời scene đấu trường.</summary>
        public static void ClearAll()
        {
            foreach (IPool pool in Pools.Values) pool.Clear();
            Pools.Clear();

            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
        }

        private static Transform EnsureRoot()
        {
            if (_root != null) return _root;

            var go = new GameObject("[Pools]");
            _root = go.transform;
            return _root;
        }
    }
}
