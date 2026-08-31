using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using UnityEngine;

namespace LAC.Combat
{
    /// <summary>
    /// Vũ khí tự khai hoả theo chu kỳ, tự chọn mục tiêu gần nhất.
    /// </summary>
    /// <remarks>
    /// Người chơi chỉ có hai thao tác: di chuyển và lướt — xem CLAUDE.md mục 1.1. Họ kiểm
    /// soát vị trí và thời điểm, không kiểm soát hành vi bắn. Vì vậy toàn bộ quyết định ở
    /// đây phải dễ đoán: cùng một khoảng cách luôn cho cùng một kết quả, để người chơi học
    /// được tầm đánh của mình và đứng đúng chỗ.
    ///
    /// Chạy trên mọi máy cho mọi nhân vật, kể cả nhân vật của người khác — nhờ vậy ai cũng
    /// nhìn thấy đồng đội đang đánh. Sát thương thì chỉ có hiệu lực trên host, vì
    /// <see cref="DamageSystem"/> tự bỏ qua khi không phải host. Không cần một dòng lệnh
    /// rẽ nhánh nào cho việc đó.
    ///
    /// Chỉ khai hoả khi có mục tiêu trong tầm. Bắn vào chỗ trống làm mất ý nghĩa của tiếng
    /// động — người chơi phải nghe ra được rằng mình vừa chạm tới đám quái.
    /// </remarks>
    public sealed class WeaponAuto : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerHealth _health;

        [Header("Tài sản dùng chung")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private VFX.SoundWave _wavePrefab;

        [Header("Hình cung")]
        [Tooltip("Nửa góc mở của hình cung, tính bằng độ.")]
        [SerializeField, Range(10f, 180f)] private float _arcHalfAngle = 60f;

        [Header("Biểu diễn")]
        [Tooltip("Màu hiệu ứng của người chơi. KHÔNG được trùng màu dành cho đòn địch — mục 2.1.")]
        [SerializeField] private Color _tint = new Color(0.85f, 0.9f, 1f, 1f);

        private float _nextShotAt;
        private ObjectPool<Projectile> _projectilePool;
        private ObjectPool<VFX.SoundWave> _wavePool;

        private CharacterData Data => _character != null ? _character.Data : null;

        /// <summary>Mục tiêu hiện tại, hoặc null nếu không có quái nào trong tầm.</summary>
        public Enemy CurrentTarget { get; private set; }

        private void Update()
        {
            CharacterData data = Data;
            if (data == null) return;
            if (_health != null && !_health.IsAlive) return;

            CurrentTarget = EnemyRegistry.Nearest(transform.position, data.AttackRange);
            if (CurrentTarget == null) return;

            if (Time.time < _nextShotAt) return;
            _nextShotAt = Time.time + data.AttackInterval;

            Fire(data);
        }

        private void Fire(CharacterData data)
        {
            switch (data.WeaponShape)
            {
                case WeaponShape.Circle: FireCircle(data); break;
                case WeaponShape.Arc: FireArc(data); break;
                default: FireLine(data); break;
            }
        }

        /// <summary>Vòng tròn quanh người chơi — đàn bầu của Thạch Sanh.</summary>
        private void FireCircle(CharacterData data)
        {
            var alive = EnemyRegistry.Alive;
            float rangeSqr = data.AttackRange * data.AttackRange;
            Vector2 origin = transform.position;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;
                if ((enemy.Position - origin).sqrMagnitude > rangeSqr) continue;

                DamageSystem.ApplyToEnemy(enemy, data.BaseDamage, origin);
            }

            SpawnWave(origin, 0.5f, data.AttackRange);
        }

        /// <summary>Hình cung hướng về mục tiêu gần nhất — roi sắt của Gióng.</summary>
        /// <remarks>
        /// Cung nhắm theo mục tiêu chứ không theo hướng di chuyển. Bản đầu dùng hướng di
        /// chuyển, và hệ quả là khi đứng yên đánh thì hướng nhìn kẹt ở giá trị cũ: đo được
        /// Gióng có mục tiêu trong tầm suốt ba giây mà không giết được con nào, vì đám quái
        /// đứng ở sườn còn cung thì vẫn chĩa xuống dưới. Vũ khí khai hoả tự động thì việc
        /// ngắm cũng phải tự động — người chơi chỉ kiểm soát vị trí, xem mục 1.1.
        /// </remarks>
        private void FireArc(CharacterData data)
        {
            Vector2 toTarget = CurrentTarget.Position - (Vector2)transform.position;
            Vector2 facing = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized
                : (_movement != null && _movement.Facing.sqrMagnitude > 0f ? _movement.Facing.normalized : Vector2.down);

            var alive = EnemyRegistry.Alive;
            float rangeSqr = data.AttackRange * data.AttackRange;
            float cosLimit = Mathf.Cos(_arcHalfAngle * Mathf.Deg2Rad);
            Vector2 origin = transform.position;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;

                Vector2 toEnemy = enemy.Position - origin;
                if (toEnemy.sqrMagnitude > rangeSqr) continue;
                if (Vector2.Dot(facing, toEnemy.normalized) < cosLimit) continue;

                DamageSystem.ApplyToEnemy(enemy, data.BaseDamage, origin);
            }

            // Vòng nhỏ đặt lệch về phía trước, đủ để đọc ra hướng vung roi.
            SpawnWave(origin + facing * (data.AttackRange * 0.5f), 0.3f, data.AttackRange * 0.7f);
        }

        /// <summary>Tia thẳng về phía mục tiêu gần nhất — sáo trúc của Tấm.</summary>
        private void FireLine(CharacterData data)
        {
            if (_projectilePrefab == null) return;

            _projectilePool ??= PoolRegistry.Get(_projectilePrefab, prewarm: 32, softLimit: 256);

            Vector2 origin = transform.position;
            Vector2 direction = CurrentTarget.Position - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;

            // Tuổi thọ tính từ tầm đánh, dư một phần ba để đạn không tắt ngay trước mũi mục
            // tiêu khi mục tiêu đang chạy ra xa.
            float lifetime = data.AttackRange / data.ProjectileSpeed * 1.35f;

            Projectile shot = _projectilePool.Get(origin, Quaternion.identity);
            shot.Launch(_projectilePool, direction, data.ProjectileSpeed, data.BaseDamage, lifetime, pierce: 1);
        }

        private void SpawnWave(Vector2 position, float fromRadius, float toRadius)
        {
            if (_wavePrefab == null) return;

            _wavePool ??= PoolRegistry.Get(_wavePrefab, prewarm: 8, softLimit: 64);
            _wavePool.Get(position, Quaternion.identity).Play(_wavePool, fromRadius, toRadius, _tint);
        }
    }
}
