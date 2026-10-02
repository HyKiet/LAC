using System.Collections.Generic;
using LAC.Core;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    // Chỉ manager có NetworkIdentity. Quái, đạn và đường báo lao đều dùng pool cục bộ.
    public sealed partial class EnemySpawner : NetworkBehaviour
    {
        private static EnemySpawner _instance;
        public static EnemySpawner Instance
        {
            get { if (_instance == null) _instance = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include); return _instance; }
        }
        [SerializeField] private Enemy _enemyPrefab;
        [SerializeField, Min(1)] private int _prewarm = 40;
        [SerializeField, Min(1)] private int _softLimit = 96;
        [SerializeField, Min(0.05f)] private float _snapshotInterval = 0.5f;
        private ObjectPool<Enemy> _pool;
        private ObjectPool<EnemyProjectile> _projectilePool;
        private EnemyCatalogData _catalog;
        private RunManager _run;
        private int _nextId;
        private int _spawnedCount;
        private int _wave;
        private float _nextSnapshotAt;
        private uint _snapshotSequence;
        private uint _receivedSequence;
        private int _receivedHighWater = -1;
        private readonly HashSet<int> _lastRoster = new HashSet<int>();
        private readonly HashSet<int> _deadIds = new HashSet<int>();
        private readonly List<Enemy> _removeBuffer = new List<Enemy>(64);
        public int SpawnedCount => _spawnedCount;
        public uint SentSnapshots => _snapshotSequence;
        public uint ReceivedSnapshots => _receivedSequence;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
            _catalog = Resources.Load<EnemyCatalogData>("Enemies/EnemyCatalog");
        }
        private void Start()
        {
            _run = RunManager.Instance;
            var prefab = Resources.Load<EnemyProjectile>("Enemies/EnemyProjectile");
            if (prefab != null) _projectilePool = PoolRegistry.Get(prefab, 32, 200);
        }
        private void OnDestroy() { if (_instance == this) _instance = null; }
        private RunManager Run => _run != null ? _run : (_run = RunManager.Instance);
        private bool Matches(int seed, int wave) => Run != null && Run.Seed == seed && Run.CurrentWave == wave && !Run.IsOver;

        public void BeginWave(int wave)
        {
            if (_wave == wave) return;
            _wave = wave;
            // Định danh của quái theo đợt không phụ thuộc lịch sử chơi hay thời điểm gia nhập.
            _nextId = wave * 1000;
            _receivedHighWater = -1;
            _lastRoster.Clear();
            _deadIds.Clear();
        }
        public Enemy Spawn(EnemyData data, Vector2 position)
        {
            _spawnedCount++;
            int id = _nextId++;
            if (!isServer && (_deadIds.Contains(id) || (id <= _receivedHighWater && !_lastRoster.Contains(id)))) return null;
            return SpawnWithId(id, data, position);
        }
        private Enemy SpawnWithId(int id, EnemyData data, Vector2 position)
        {
            if (EnemyRegistry.TryGet(id, out var existing)) return existing;
            if (_enemyPrefab == null || data == null) return null;
            _pool ??= PoolRegistry.Get(_enemyPrefab, _prewarm, _softLimit);
            var enemy = _pool.Get(position, Quaternion.identity);
            enemy.Initialize(id, data, position);
            if (enemy.IsBoss && isServer) RegisterBoss(enemy);
            return enemy;
        }
        public void ResetRun()
        {
            _pool?.ReleaseAll();
            _projectilePool?.ReleaseAll();
            EnemyRegistry.Clear();
            _nextId = 0;
            _spawnedCount = 0;
            _wave = 0;
            _receivedSequence = _snapshotSequence = 0;
            _nextSnapshotAt = 0f;
            _receivedHighWater = -1;
            _deadIds.Clear();
            _lastRoster.Clear();
            ResetBossEncounter();
        }
        [Server]
        public void DamageEnemy(Enemy enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive) return;
            bool killed = enemy.ApplyDamage(amount);
            if (enemy.IsBoss) UpdateBossHealth(enemy);
            if (killed) KillEnemy(enemy);
        }
        [Server]
        public void KillEnemy(Enemy enemy)
        {
            if (enemy == null || !enemy.IsAlive) return;
            int id = enemy.Id;
            Vector2 position = enemy.Position;
            var child = enemy.Data.Behaviour == EnemyBehaviour.Splitter ? enemy.Data.SplitChild : null;
            float offset = enemy.Data.SplitOffset;
            bool boss = enemy.IsBoss;
            enemy.Kill();
            int childIndex = child != null ? _catalog.IndexOf(child) : -1;
            ApplySplit(id, childIndex, position, offset);
            // Không tái dùng chính object cha làm con ngay trong callback sát thương của cha.
            _pool?.Release(enemy);
            RpcEnemyDied(id, childIndex, position, offset, Run.Seed, Run.CurrentWave);
            if (boss) EndBossEncounter();
        }
        private void ApplySplit(int parent, int childIndex, Vector2 position, float offset)
        {
            if (childIndex < 0 || parent < 0) return;
            for (int i = 0; i < 2; i++)
            {
                int id = -(parent * 2 + i + 1);
                if (_deadIds.Contains(id)) continue;
                Vector2 point = position + Vector2.right * (i == 0 ? -offset : offset);
                if (ArenaBounds.Instance != null) point = ArenaBounds.Instance.Clamp(point, Vector2.one * 0.4f);
                SpawnWithId(id, _catalog.Get(childIndex), point);
            }
        }
        [ClientRpc]
        private void RpcEnemyDied(int id, int childIndex, Vector2 position, float offset, int seed, int wave)
        {
            if (isServer || !Matches(seed, wave)) return;
            BeginWave(wave);
            if (!_deadIds.Add(id)) return;
            if (EnemyRegistry.TryGet(id, out var enemy)) { enemy.Kill(); _pool.Release(enemy); }
            ApplySplit(id, childIndex, position, offset);
        }
        [Server]
        public void FireProjectile(Enemy enemy, Vector2 direction)
        {
            int kind = _catalog.IndexOf(enemy.Data);
            PlayProjectile(kind, enemy.Position, direction, 0f);
            RpcProjectile(kind, enemy.Position, direction, NetworkTime.time, Run.Seed, Run.CurrentWave);
        }
        private void PlayProjectile(int kind, Vector2 position, Vector2 direction, float elapsed)
        {
            var data = _catalog.Get(kind);
            if (_projectilePool == null || data == null || elapsed >= data.ProjectileLifetime) return;
            var shot = _projectilePool.Get(position, Quaternion.identity);
            shot.Launch(_projectilePool, data, direction, elapsed);
        }
        [ClientRpc]
        private void RpcProjectile(int kind, Vector2 position, Vector2 direction, double time, int seed, int wave)
        {
            if (!isServer && Matches(seed, wave)) PlayProjectile(kind, position, direction, Mathf.Max(0f, (float)(NetworkTime.time - time)));
        }
        [Server]
        public void StartCharge(Enemy enemy, Vector2 direction)
        {
            enemy.BeginCharge(direction, 0f);
            RpcCharge(enemy.Id, direction, NetworkTime.time, Run.Seed, Run.CurrentWave);
        }
        [ClientRpc]
        private void RpcCharge(int id, Vector2 direction, double time, int seed, int wave)
        {
            if (!isServer && Matches(seed, wave) && EnemyRegistry.TryGet(id, out var enemy))
                enemy.BeginCharge(direction, Mathf.Max(0f, (float)(NetworkTime.time - time)));
        }
        private void Update()
        {
            if (!isServer) { ApplyBossStatus(); return; }
            if (Run == null || Run.State != RunState.WaveActive || Time.unscaledTime < _nextSnapshotAt) return;
            _nextSnapshotAt = Time.unscaledTime + _snapshotInterval;
            BroadcastSnapshot();
        }
        private void BroadcastSnapshot()
        {
            var alive = EnemyRegistry.Alive;
            int count = alive.Count;
            var ids = new int[count];
            var kinds = new int[count];
            var positions = new Vector2[count];
            var velocities = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = alive[i].Id;
                kinds[i] = _catalog.IndexOf(alive[i].Data);
                positions[i] = alive[i].Position;
                velocities[i] = alive[i].Velocity;
            }
            // Gửi cả roster rỗng. Không cắt ở 64 con; transport reliable tự phân mảnh gói lớn.
            RpcSnapshot(ids, kinds, positions, velocities, _nextId - 1, ++_snapshotSequence, NetworkTime.time, Run.Seed, Run.CurrentWave);
        }
        [ClientRpc]
        private void RpcSnapshot(int[] ids, int[] kinds, Vector2[] positions, Vector2[] velocities,
            int highWater, uint sequence, double sentAt, int seed, int wave)
        {
            if (isServer) return;
            ApplySnapshotRoster(ids, kinds, positions, velocities, highWater, sequence, sentAt, seed, wave);
        }
        // Tách phần nhận để kiểm thử gói cũ, reset và quái sinh trễ mà không giả lập Mirror RPC.
        public void ApplySnapshotRoster(int[] ids, int[] kinds, Vector2[] positions, Vector2[] velocities,
            int highWater, uint sequence, double sentAt, int seed, int wave)
        {
            if (isServer || !Matches(seed, wave) || sequence <= _receivedSequence || ids.Length != kinds.Length ||
                ids.Length != positions.Length || ids.Length != velocities.Length) return;
            BeginWave(wave);
            _receivedSequence = sequence;
            _receivedHighWater = highWater;
            _lastRoster.Clear();
            float age = Mathf.Clamp((float)(NetworkTime.time - sentAt), 0f, _snapshotInterval);
            for (int i = 0; i < ids.Length; i++)
            {
                _lastRoster.Add(ids[i]);
                if (_deadIds.Contains(ids[i])) continue;
                var data = _catalog.Get(kinds[i]);
                var enemy = SpawnWithId(ids[i], data, positions[i]);
                if (enemy != null && data != null && enemy.Data != data)
                    enemy.Initialize(ids[i], data, positions[i]);
                if (enemy != null) enemy.ApplySnapshot(positions[i] + velocities[i] * age);
            }
            _removeBuffer.Clear();
            foreach (var enemy in EnemyRegistry.Alive)
                if (enemy.Id <= highWater && !_lastRoster.Contains(enemy.Id)) _removeBuffer.Add(enemy);
            foreach (var enemy in _removeBuffer)
            {
                // Hiệu chỉnh roster không phát rơi Hồn hay tăng kill count lần thứ hai.
                _deadIds.Add(enemy.Id);
                _pool.Release(enemy);
            }
        }
    }
}
