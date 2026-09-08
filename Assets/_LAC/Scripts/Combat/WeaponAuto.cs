using System;
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
        [SerializeField] private PlayerDash _dash;

        [Header("Tài sản dùng chung")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private VFX.SoundWave _wavePrefab;

        [Header("Hình cung")]
        [SerializeField] private VFX.ArcSlash _arcSlashPrefab;

        [Tooltip("Trong bán kính này thì coi như luôn nằm trong cung, khỏi cần đo góc.")]
        [SerializeField, Min(0f)] private float _pointBlankRadius = 0.35f;

        [Header("Biểu diễn")]
        [Tooltip("Màu hiệu ứng của người chơi. KHÔNG được trùng màu dành cho đòn địch — mục 2.1.")]
        [SerializeField] private Color _tint = new Color(0.612f, 0.812f, 0.753f, 1f); // ChamSang #9CCFC0

        private float _nextShotAt;
        private ObjectPool<Projectile> _projectilePool;
        private ObjectPool<VFX.SoundWave> _wavePool;
        private ObjectPool<VFX.ArcSlash> _slashPool;
        // Phát bắn kế tiếp sau khi lướt được nhân sát thương — xem CharacterData.
        private bool _empoweredShot;

        private CharacterData Data => _character != null ? _character.Data : null;

        /// <summary>Mục tiêu hiện tại, hoặc null nếu không có quái nào trong tầm.</summary>
        public Enemy CurrentTarget { get; private set; }

        /// <summary>
        /// Vừa khai hoả. Tham số là hướng tới mục tiêu, đã chuẩn hoá.
        /// </summary>
        /// <remarks>
        /// Chỉ dành cho phần biểu diễn — hoạt ảnh đánh và hướng nhìn. Sát thương đã do
        /// <see cref="DamageSystem"/> xử lý bên trong các hàm Fire, người nghe sự kiện này
        /// không được động vào máu của bất kỳ ai.
        /// </remarks>
        public event Action<Vector2> Fired;

        private void OnEnable()
        {
            if (_dash != null) _dash.Dashed += OnDashed;
        }

        private void OnDisable()
        {
            if (_dash != null) _dash.Dashed -= OnDashed;
        }

        /// <summary>
        /// Nạp phần thưởng cho phát bắn kế tiếp.
        /// </summary>
        /// <remarks>
        /// Nạp một lần chứ không cộng dồn: lướt hai lần liên tiếp vẫn chỉ được một phát mạnh.
        ///
        /// Host và client có thể áp phần thưởng vào hai phát khác nhau, vì host chỉ biết
        /// client đã lướt khi gói tin tới nơi. Sai lệch đó nằm ở phần biểu diễn và được chấp
        /// nhận theo mục 3.2 — con số sát thương thật vẫn do host quyết, và tổng sát thương
        /// của một lần lướt thì hai bên bằng nhau.
        /// </remarks>
        private void OnDashed() => _empoweredShot = true;

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

        /// <summary>
        /// Sát thương của phát đánh sắp tới, đã tính phần thưởng sau khi lướt.
        /// </summary>
        /// <remarks>
        /// Tiêu phần thưởng ngay tại đây chứ không ở nơi gây sát thương: vũ khí hình tia bắn
        /// ra một viên đạn bay mất vài phần mười giây mới trúng, nếu tiêu lúc trúng thì hai
        /// viên bắn liên tiếp cùng ăn một phần thưởng.
        /// </remarks>
        private int ConsumeDamage(CharacterData data)
        {
            if (!_empoweredShot) return data.BaseDamage;

            _empoweredShot = false;
            return Mathf.Max(Mathf.RoundToInt(data.BaseDamage * data.DashDamageMultiplier),
                             data.BaseDamage);
        }

        private void Fire(CharacterData data)
        {
            int damage = ConsumeDamage(data);

            switch (data.WeaponShape)
            {
                case WeaponShape.Circle: FireCircle(data, damage); break;
                case WeaponShape.Arc: FireArc(data, damage); break;
                default: FireLine(data, damage); break;
            }

            Vector2 toTarget = CurrentTarget != null
                ? (Vector2)(CurrentTarget.transform.position - transform.position)
                : Vector2.zero;

            Fired?.Invoke(toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right);
        }

        /// <summary>Vòng tròn quanh người chơi — đàn bầu của Thạch Sanh.</summary>
        private void FireCircle(CharacterData data, int damage)
        {
            var alive = EnemyRegistry.Alive;
            float rangeSqr = data.AttackRange * data.AttackRange;
            Vector2 origin = transform.position;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;
                if ((enemy.Position - origin).sqrMagnitude > rangeSqr) continue;

                DamageSystem.ApplyToEnemy(enemy, damage, origin);
            }

            if (data.SpawnSoundWave) SpawnWave(origin, 0.5f, data.AttackRange);
        }

        /// <summary>Hình cung hướng về mục tiêu gần nhất — roi sắt của Gióng.</summary>
        /// <remarks>
        /// Cung nhắm theo mục tiêu chứ không theo hướng di chuyển. Bản đầu dùng hướng di
        /// chuyển, và hệ quả là khi đứng yên đánh thì hướng nhìn kẹt ở giá trị cũ: đo được
        /// Gióng có mục tiêu trong tầm suốt ba giây mà không giết được con nào, vì đám quái
        /// đứng ở sườn còn cung thì vẫn chĩa xuống dưới. Vũ khí khai hoả tự động thì việc
        /// ngắm cũng phải tự động — người chơi chỉ kiểm soát vị trí, xem mục 1.1.
        /// </remarks>
        private void FireArc(CharacterData data, int damage)
        {
            Vector2 toTarget = CurrentTarget.Position - (Vector2)transform.position;
            Vector2 facing = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized
                : (_movement != null && _movement.Facing.sqrMagnitude > 0f ? _movement.Facing.normalized : Vector2.down);

            var alive = EnemyRegistry.Alive;
            float rangeSqr = data.AttackRange * data.AttackRange;
            float cosLimit = Mathf.Cos(data.ArcHalfAngle * Mathf.Deg2Rad);
            float pointBlankSqr = _pointBlankRadius * _pointBlankRadius;
            Vector2 origin = transform.position;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = alive[i];
                if (enemy == null || !enemy.IsAlive) continue;

                Vector2 toEnemy = enemy.Position - origin;
                float distSqr = toEnemy.sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                // Quái đứng đè lên người chơi thì không còn hướng nào để đo góc, và
                // Vector2.normalized trả về véc-tơ không khi độ dài quá nhỏ — tích vô hướng
                // bằng 0 nên con quái đang cắm mặt vào Gióng lại là con duy nhất không ăn
                // đòn. Trong bán kính này thì bỏ qua phép đo góc.
                if (distSqr > pointBlankSqr &&
                    Vector2.Dot(facing, toEnemy / Mathf.Sqrt(distSqr)) < cosLimit) continue;

                DamageSystem.ApplyToEnemy(enemy, damage, origin);
            }

            SpawnSlash(origin, facing, data);

            // Vòng nhỏ đặt lệch về phía trước, đủ để đọc ra hướng vung roi — chỉ khi bản
            // thân hoạt ảnh chưa tả được đường roi.
            if (data.SpawnSoundWave)
                SpawnWave(origin + facing * (data.AttackRange * 0.5f), 0.3f, data.AttackRange * 0.7f);
        }

        /// <summary>Tia thẳng về phía mục tiêu gần nhất — sáo trúc của Tấm.</summary>
        private void FireLine(CharacterData data, int damage)
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
            shot.Launch(_projectilePool, direction, data.ProjectileSpeed, damage, lifetime, pierce: 1);
        }

        /// <summary>Vệt roi quét qua trước mặt. Không có prefab thì bỏ qua, không báo lỗi.</summary>
        /// <remarks>
        /// Tách khỏi sóng âm vì hai thứ nói hai điều khác nhau: sóng tròn nghĩa là đòn lan ra
        /// mọi phía, vệt cung nghĩa là đòn chỉ trúng phía trước. Gióng vẽ sóng tròn thì người
        /// chơi học sai tầm đánh và đứng sai chỗ.
        /// </remarks>
        private void SpawnSlash(Vector2 origin, Vector2 facing, CharacterData data)
        {
            if (_arcSlashPrefab == null) return;

            _slashPool ??= PoolRegistry.Get(_arcSlashPrefab, prewarm: 4, softLimit: 32);
            _slashPool.Get(origin, Quaternion.identity)
                      .Play(_slashPool, facing, data.AttackRange, data.ArcHalfAngle, _tint);
        }

        private void SpawnWave(Vector2 position, float fromRadius, float toRadius)
        {
            if (_wavePrefab == null) return;

            _wavePool ??= PoolRegistry.Get(_wavePrefab, prewarm: 8, softLimit: 64);
            _wavePool.Get(position, Quaternion.identity).Play(_wavePool, fromRadius, toRadius, _tint);
        }
    }
}
