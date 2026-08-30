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
        private int _damage;
        private float _diesAt;
        private int _remainingHits;

        public void Launch(ObjectPool<Projectile> owner, Vector2 direction, float speed,
                           int damage, float lifetime, int pierce)
        {
            _owner = owner;
            _velocity = direction.normalized * speed;
            _damage = damage;
            _diesAt = Time.time + lifetime;
            _remainingHits = Mathf.Max(pierce, 1);

            if (direction.sqrMagnitude > 0f)
                transform.right = direction;
        }

        public void OnSpawned()
        {
            _remainingHits = 1;
        }

        public void OnDespawned()
        {
            _owner = null;
            _velocity = Vector2.zero;
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

                float reach = _radius + 0.45f;
                if ((enemy.Position - position).sqrMagnitude > reach * reach) continue;

                // Trên client lời gọi này không có tác dụng và trả về false — đúng như thiết
                // kế. Viên đạn vẫn biến mất để hình ảnh hai máy giống nhau.
                DamageSystem.ApplyToEnemy(enemy, _damage);

                if (--_remainingHits > 0) continue;

                Despawn();
                return;
            }
        }

        private void Despawn()
        {
            if (_owner != null) _owner.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
