using LAC.Core;
using LAC.Combat;
using LAC.Drum;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    [RequireComponent(typeof(EnemyProjectileClearable))]
    public sealed class EnemyProjectile : MonoBehaviour, IPoolable
    {
        private ObjectPool<EnemyProjectile> _pool;
        private EnemyData _data;
        private Vector2 _velocity;
        private float _remaining;
        private RunManager _run;
        private EnemyProjectileClearable _clearable;
        private System.Action _release;
        private void Awake() { _clearable = GetComponent<EnemyProjectileClearable>(); _release = Release; }

        public void Launch(ObjectPool<EnemyProjectile> pool, EnemyData data, Vector2 direction, float elapsed)
        {
            _pool = pool;
            _data = data;
            _run = RunManager.Instance;
            _velocity = direction.normalized * data.ProjectileSpeed;
            _remaining = data.ProjectileLifetime - elapsed;
            transform.position += (Vector3)(_velocity * elapsed);
            _clearable.BindRelease(_release);
        }
        public void OnSpawned() { }
        public void OnDespawned() { _pool = null; _data = null; }
        private void Release() { if (_pool != null) _pool.Release(this); }
        private void FixedUpdate()
        {
            if (_data == null) return;
            if (_run == null || _run.State != RunState.WaveActive) { Release(); return; }
            _remaining -= Time.fixedDeltaTime;
            if (_remaining <= 0f) { Release(); return; }
            Vector2 start = transform.position;
            Vector2 end = start + _velocity * Time.fixedDeltaTime;
            if (ArenaBounds.Instance != null && !ArenaBounds.Instance.Rect.Contains(end)) { Release(); return; }
            // Máy client chỉ vẽ; vị trí người chơi được dự đoán không được quyết định sát thương.
            if (NetworkServer.active)
            {
                foreach (var player in PlayerRegistry.All)
                {
                    if (player == null || !player.IsAlive) continue;
                    var collider = player.GetComponent<Collider2D>();
                    float radius = _data.ProjectileRadius + (collider != null ? Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.y) : 0.3f);
                    if (!DrumShockwave.Intersects(start, end, player.transform.position, radius)) continue;
                    DamageSystem.ApplyToPlayer(player, _data.ContactDamage, start);
                    Release();
                    return;
                }
            }
            transform.position = end;
        }
    }
}
