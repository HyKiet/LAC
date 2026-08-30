using LAC.Core;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    /// <summary>
    /// Sinh quái và giữ thẩm quyền về cái chết cùng vị trí của chúng.
    /// </summary>
    /// <remarks>
    /// Quái không phải đối tượng mạng, nên phải có một chỗ duy nhất chịu trách nhiệm về
    /// phần mạng của chúng — chính là lớp này. Mô hình gồm ba phần:
    ///
    /// 1. <b>Sinh:</b> hai máy cùng gọi <see cref="Spawn"/> theo cùng thứ tự, với cùng tham
    ///    số rút từ <see cref="RunRandom"/>. Định danh là một bộ đếm tăng dần nên hai máy
    ///    tự khắc đặt cùng một số cho cùng một con quái, không cần ai gửi cho ai.
    /// 2. <b>Vị trí:</b> mỗi máy tự mô phỏng. Host gửi snapshot hai lần mỗi giây để kéo lại
    ///    sai lệch tích luỹ.
    /// 3. <b>Cái chết:</b> chỉ host quyết, rồi phát xuống bằng RPC theo định danh.
    ///
    /// Chi phí băng thông của snapshot: 40 con × 12 byte × 2 lần/giây ≈ 1 KB/s. Đồng bộ từng
    /// con như đối tượng mạng thông thường sẽ tốn gấp vài chục lần và vẫn không chính xác hơn.
    /// </remarks>
    public sealed class EnemySpawner : NetworkBehaviour
    {
        /// <summary>Điểm truy cập tĩnh, tự tìm lại nếu mất. Xem chú thích ở `RunManager.Instance`.</summary>
        public static EnemySpawner Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        private static EnemySpawner _instance;

        [SerializeField] private Enemy _enemyPrefab;

        [Tooltip("Số quái cấp phát sẵn. Ngân sách một đợt cuối là 40 con cùng lúc.")]
        [SerializeField, Min(1)] private int _prewarm = 40;

        [SerializeField, Min(1)] private int _softLimit = 96;

        [Tooltip("Giây giữa hai snapshot vị trí do host gửi.")]
        [SerializeField, Min(0.05f)] private float _snapshotInterval = 0.5f;

        [Tooltip("Số quái tối đa trong một gói snapshot, tránh vượt giới hạn gói tin.")]
        [SerializeField, Min(8)] private int _snapshotBatch = 64;

        private ObjectPool<Enemy> _pool;
        private int _nextId;
        private float _nextSnapshotAt;

        private int[] _idBuffer;
        private Vector2[] _positionBuffer;

        /// <summary>Số quái đã sinh từ đầu ván. Hai máy phải luôn khớp nhau.</summary>
        public int SpawnedCount => _nextId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[EnemySpawner] Đã có một bộ sinh quái trong scene.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            _idBuffer = new int[_snapshotBatch];
            _positionBuffer = new Vector2[_snapshotBatch];
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Sinh một con quái. <b>Cả hai máy đều gọi</b>, cùng thứ tự và cùng tham số.
        /// </summary>
        public Enemy Spawn(EnemyData data, Vector2 position)
        {
            if (_enemyPrefab == null || data == null)
            {
                Debug.LogError("[EnemySpawner] Thiếu prefab quái hoặc chỉ số quái.", this);
                return null;
            }

            _pool ??= PoolRegistry.Get(_enemyPrefab, _prewarm, _softLimit);

            Enemy enemy = _pool.Get(position, Quaternion.identity);
            enemy.Initialize(_nextId++, data, position);
            return enemy;
        }

        /// <summary>Đặt lại bộ đếm định danh và thu hồi toàn bộ quái. Hai máy cùng gọi.</summary>
        public void ResetRun()
        {
            _pool?.ReleaseAll();
            EnemyRegistry.Clear();
            _nextId = 0;
        }

        // ── Thẩm quyền host ────────────────────────────────────────────────────────────

        /// <summary>Gây sát thương lên quái. Chỉ host được gọi, qua `DamageSystem` ở T-13.</summary>
        [Server]
        public void DamageEnemy(Enemy enemy, int amount)
        {
            if (enemy == null || !enemy.IsAlive) return;
            if (!enemy.ApplyDamage(amount)) return;

            KillEnemy(enemy);
        }

        /// <summary>Giết một con quái và báo xuống các máy còn lại. Chỉ host được gọi.</summary>
        /// <remarks>
        /// Host thi hành cái chết ngay tại chỗ chứ không chờ RPC của chính mình quay về.
        /// RPC gửi từ host vẫn phải đi qua hàng đợi của kết nối cục bộ nên tới muộn một lần
        /// cập nhật mạng, và trong khoảng đó host có thể gây sát thương thêm một lần nữa lên
        /// một con quái đã chết. Đây đúng là lỗi đã gặp ở `RunManager` tại T-05.
        /// </remarks>
        [Server]
        public void KillEnemy(Enemy enemy)
        {
            if (enemy == null || !enemy.IsAlive) return;

            int id = enemy.Id;
            enemy.Kill();
            _pool?.Release(enemy);

            RpcEnemyDied(id);
        }

        [ServerCallback]
        private void Update()
        {
            if (Time.time < _nextSnapshotAt) return;
            _nextSnapshotAt = Time.time + _snapshotInterval;

            BroadcastSnapshot();
        }

        private void BroadcastSnapshot()
        {
            var alive = EnemyRegistry.Alive;
            if (alive.Count == 0) return;

            int count = 0;
            for (int i = 0; i < alive.Count && count < _idBuffer.Length; i++)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;

                _idBuffer[count] = enemy.Id;
                _positionBuffer[count] = enemy.Position;
                count++;
            }

            if (count == 0) return;

            var ids = new int[count];
            var positions = new Vector2[count];
            System.Array.Copy(_idBuffer, ids, count);
            System.Array.Copy(_positionBuffer, positions, count);

            RpcSnapshot(ids, positions);
        }

        // ── Nhận trên client ───────────────────────────────────────────────────────────

        [ClientRpc]
        private void RpcEnemyDied(int id)
        {
            if (isServer) return;
            if (!EnemyRegistry.TryGet(id, out Enemy enemy)) return;

            enemy.Kill();
            _pool?.Release(enemy);
        }

        [ClientRpc]
        private void RpcSnapshot(int[] ids, Vector2[] positions)
        {
            if (isServer) return;

            for (int i = 0; i < ids.Length; i++)
            {
                if (EnemyRegistry.TryGet(ids[i], out Enemy enemy))
                    enemy.ApplySnapshot(positions[i]);
            }
        }
    }
}
