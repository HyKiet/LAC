using System.Collections.Generic;
using LAC.Core;
using LAC.Enemies;
using UnityEngine;

namespace LAC.Combat
{
    /// <summary>
    /// Một viên đạn bay thẳng. Sinh cục bộ, <b>không đồng bộ qua mạng</b>.
    /// </summary>
    /// <remarks>
    /// Cuối ván có khoảng 200 viên cùng lúc. Gắn <c>NetworkIdentity</c> lên đạn là lỗi
    /// triển khai số một bị cấm ở CLAUDE.md mục 3.2 — băng thông không chịu nổi. Mỗi máy tự
    /// sinh đạn của mình; đạn trên máy client thuần tuý là hình ảnh, còn sát thương thì chỉ
    /// host quyết qua <see cref="DamageSystem"/>. Hai máy vì thế thấy quỹ đạo hơi lệch nhau,
    /// và đó là đánh đổi có chủ đích: người chơi không nhận ra vài chục mili giây lệch pha
    /// của một viên đạn, nhưng sẽ nhận ra ngay khi game giật vì quá tải đường truyền.
    ///
    /// Va chạm tự kiểm bằng khoảng cách thay vì dùng trigger của vật lý. Với 200 viên và 40
    /// quái là 8000 phép so sánh bình phương khoảng cách mỗi bước — rẻ hơn nhiều so với 200
    /// collider động sinh ra 200 lời gọi ngược mỗi khung hình.
    /// </remarks>
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Bán kính va chạm, cộng vào bán kính của quái.")]
        [SerializeField, Min(0.05f)] private float _radius = 0.2f;

        private ObjectPool<Projectile> _owner;
        private Vector2 _velocity;
        private float _damage;
        private float _diesAt;
        private int _remainingHits;
        private readonly HashSet<int> _hitEnemyIds = new HashSet<int>(8);
        private bool _explodes;
        private bool _hasExploded;
        private float _explosionRadius;
        private float _explosionDamageRatio;

        public void Launch(ObjectPool<Projectile> owner, Vector2 direction, float speed,
                           float damage, float lifetime, int hitLimit, bool explodes = false,
                           float explosionRadius = 0f, float explosionDamageRatio = 0f)
        {
            _owner = owner;
            _velocity = direction.normalized * speed;
            _damage = damage;
            _diesAt = Time.time + lifetime;
            _remainingHits = Mathf.Max(hitLimit, 1);
            _explodes = explodes;
            _explosionRadius = Mathf.Max(explosionRadius, 0f);
            _explosionDamageRatio = Mathf.Clamp01(explosionDamageRatio);
            _hasExploded = false;
            _hitEnemyIds.Clear();

            if (direction.sqrMagnitude > 0f)
                transform.right = direction;
        }

        public void OnSpawned()
        {
            _remainingHits = 1;
            _hitEnemyIds.Clear();
            _explodes = false;
            _hasExploded = false;
            _explosionRadius = 0f;
            _explosionDamageRatio = 0f;
        }

        public void OnDespawned()
        {
            _owner = null;
            _velocity = Vector2.zero;
            _hitEnemyIds.Clear();
            _explodes = false;
            _hasExploded = false;
        }

        private void FixedUpdate()
        {
            transform.position += (Vector3)(_velocity * Time.fixedDeltaTime);

            if (Time.time >= _diesAt || OutsideArena())
            {
                Despawn();
                return;
            }

            CheckHit();
        }

        private bool OutsideArena()
        {
            if (ArenaBounds.Instance == null) return false;
            return !ArenaBounds.Instance.Contains(transform.position);
        }

        private void CheckHit()
        {
            Vector2 position = transform.position;
            var alive = EnemyRegistry.Alive;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;
                if (_hitEnemyIds.Contains(enemy.Id)) continue;

                float reach = _radius + 0.45f;
                if ((enemy.Position - position).sqrMagnitude > reach * reach) continue;

                // Trên client lời gọi này không có tác dụng và trả về false — đúng như thiết
                // kế. Viên đạn vẫn biến mất để hình ảnh hai máy giống nhau.
                _hitEnemyIds.Add(enemy.Id);
                DamageSystem.ApplyToEnemy(enemy, _damage, position);

                if (_explodes && !_hasExploded)
                {
                    _hasExploded = true;
                    Explode(position, enemy.Id);
                }

                if (--_remainingHits > 0)
                {
                    // DamageSystem có thể vừa loại một hay nhiều phần tử khỏi registry.
                    // Kẹp lại chỉ số để lần lặp kế không đọc vượt cuối danh sách.
                    i = Mathf.Min(i, alive.Count);
                    continue;
                }

                Despawn();
                return;
            }
        }

        private void Explode(Vector2 position, int directTargetId)
        {
            if (_explosionRadius <= 0f || _explosionDamageRatio <= 0f) return;

            float splashDamage = _damage * _explosionDamageRatio;
            float radiusSqr = _explosionRadius * _explosionRadius;
            var alive = EnemyRegistry.Alive;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive || enemy.Id == directTargetId) continue;
                if ((enemy.Position - position).sqrMagnitude > radiusSqr) continue;

                // Sát thương vùng đi thẳng vào DamageSystem và không gọi lại Explode.
                DamageSystem.ApplyToEnemy(enemy, splashDamage, position);
            }
        }

        private void Despawn()
        {
            if (_owner != null) _owner.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
