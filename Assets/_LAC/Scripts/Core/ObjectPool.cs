using System.Collections.Generic;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>Giao diện không phụ thuộc kiểu, dùng cho <see cref="PoolRegistry"/>.</summary>
    public interface IPool
    {
        void ReleaseAll();
        void Clear();
    }

    /// <summary>
    /// Pool đối tượng dùng chung cho đạn, quái, hiệu ứng và số sát thương.
    /// </summary>
    /// <remarks>
    /// Bắt buộc thay thế cho <c>Instantiate</c> và <c>Destroy</c> trong vòng lặp gameplay.
    /// Ở giai đoạn cuối ván có khoảng 200 viên đạn hoạt động đồng thời với vòng đời rất
    /// ngắn; cấp phát và thu hồi liên tục sẽ kích hoạt bộ dọn rác và tạo ra hiện tượng
    /// khựng hình ngay tại thời điểm màn hình đông đúc nhất.
    /// </remarks>
    public sealed class ObjectPool<T> : IPool where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly Stack<T> _idle;
        private readonly HashSet<T> _active;
        private readonly int _softLimit;
        private bool _limitWarningShown;

        public int CountActive => _active.Count;
        public int CountIdle => _idle.Count;
        public int CountTotal => _active.Count + _idle.Count;

        /// <param name="prefab">Prefab gốc. Không được là null.</param>
        /// <param name="root">Transform chứa các đối tượng nhàn rỗi.</param>
        /// <param name="prewarm">Số đối tượng tạo sẵn khi khởi tạo pool.</param>
        /// <param name="softLimit">Ngưỡng cảnh báo khi pool phải cấp phát thêm lúc đang chạy.</param>
        public ObjectPool(T prefab, Transform root, int prewarm = 0, int softLimit = 256)
        {
            _prefab = prefab;
            _root = root;
            _softLimit = softLimit;
            _idle = new Stack<T>(Mathf.Max(prewarm, 8));
            _active = new HashSet<T>();

            for (int i = 0; i < prewarm; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _idle.Push(instance);
            }
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = _idle.Count > 0 ? _idle.Pop() : GrowAndCreate();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            _active.Add(instance);

            if (instance is IPoolable poolable) poolable.OnSpawned();
            return instance;
        }

        public T Get() => Get(Vector3.zero, Quaternion.identity);

        public void Release(T instance)
        {
            if (instance == null) return;

            // Trả về hai lần sẽ khiến cùng một đối tượng được cấp phát cho hai nơi khác
            // nhau. Lỗi này biểu hiện thành đạn nhấp nháy hoặc quái xuất hiện sai vị trí
            // và rất khó truy vết ngược, nên chặn ngay tại đây.
            if (!_active.Remove(instance))
            {
                Debug.LogWarning("[ObjectPool] Đối tượng được trả về pool hai lần hoặc không thuộc pool này: " + instance.name, instance);
                return;
            }

            if (instance is IPoolable poolable) poolable.OnDespawned();

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_root, false);
            _idle.Push(instance);
        }

        /// <summary>Thu hồi toàn bộ đối tượng đang hoạt động. Dùng khi kết thúc một đợt hoặc một ván.</summary>
        public void ReleaseAll()
        {
            if (_active.Count == 0) return;

            var buffer = new List<T>(_active);
            for (int i = 0; i < buffer.Count; i++) Release(buffer[i]);
        }

        /// <summary>Huỷ toàn bộ đối tượng. Chỉ gọi khi rời scene, không gọi trong gameplay.</summary>
        public void Clear()
        {
            ReleaseAll();
            while (_idle.Count > 0)
            {
                T instance = _idle.Pop();
                if (instance != null) Object.Destroy(instance.gameObject);
            }
        }

        private T GrowAndCreate()
        {
            if (CountTotal >= _softLimit && !_limitWarningShown)
            {
                // Không chặn việc cấp phát: thiếu đạn giữa trận là lỗi nghiêm trọng hơn một
                // lần khựng hình. Nhưng phải báo để nâng giá trị prewarm cho đúng.
                _limitWarningShown = true;
                Debug.LogWarning("[ObjectPool] Vượt ngưỡng " + _softLimit + " đối tượng: " + _prefab.name + ". Tăng giá trị prewarm để tránh cấp phát giữa trận.", _prefab);
            }
            return CreateInstance();
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _root);
            instance.name = _prefab.name;
            return instance;
        }
    }
}
