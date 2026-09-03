using LAC.Core;
using LAC.Player;
using LAC.VFX;
using UnityEngine;

namespace LAC.Enemies
{
    /// <summary>
    /// Một con quái. Máy trạng thái ba bước: xuất hiện, truy đuổi, áp sát đánh.
    /// </summary>
    /// <remarks>
    /// <b>Quái không mang <c>NetworkIdentity</c>.</b> Cuối ván có tới 40 con cùng lúc; đồng
    /// bộ từng con như một đối tượng mạng sẽ ngốn hết băng thông. Thay vào đó hai máy cùng
    /// sinh quái từ một seed và mô phỏng song song, host gửi snapshot vị trí hai lần mỗi giây
    /// để kéo lại sai lệch tích luỹ — xem docs/ARCHITECTURE.md mục 2.4 và CLAUDE.md mục 3.2.
    /// Thẩm quyền nằm ở <see cref="EnemySpawner"/>, không ở đây.
    ///
    /// Di chuyển bằng <c>Rigidbody2D</c> kiểu Kinematic chứ không phải Dynamic: quái không
    /// xô đẩy nhau. Cho chúng va chạm với nhau trông thì hợp lý hơn, nhưng kết quả xô đẩy
    /// giữa 40 vật thể phụ thuộc vào thứ tự giải va chạm của engine và phân kỳ rất nhanh
    /// giữa hai máy — snapshot 2 Hz sẽ không kéo lại kịp và hình ảnh giật liên tục.
    /// </remarks>
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Trình chạy hoạt ảnh. Bỏ trống thì quái dùng sprite tĩnh trong EnemyData.")]
        [SerializeField] private SpriteAnimator _animator;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Collider2D _collider;

        [Tooltip("Lề giữ quái bên trong sân, tính từ tâm quái.")]
        [SerializeField, Min(0f)] private float _arenaMargin = 0.4f;

        [Tooltip("Tốc độ kéo vị trí về snapshot của host. Càng lớn càng nhanh nhưng càng giật.")]
        [SerializeField, Range(0f, 1f)] private float _snapshotBlend = 0.35f;

        [Header("Giãn cách")]
        [Tooltip("Bán kính bắt đầu đẩy nhau ra.")]
        [SerializeField, Min(0f)] private float _separationRadius = 0.85f;

        [Tooltip("Sức đẩy so với sức hút về phía người chơi. Trên 1 thì chen chúc thắng đuổi bắt.")]
        [SerializeField, Range(0f, 4f)] private float _separationWeight = 1.4f;

        [Tooltip("Trần của lực đẩy, tránh việc một con bị bắn vọt ra khỏi đám.")]
        [SerializeField, Min(1f)] private float _separationMax = 4f;

        [Tooltip("Phần tốc độ dùng để giãn ra khi đang đứng đánh.")]
        [SerializeField, Range(0f, 1f)] private float _crowdSpeedScale = 0.6f;

        [Header("Phản hồi khi trúng đòn")]
        [Tooltip("Quãng đường bị đẩy lùi mỗi lần trúng đòn.")]
        [SerializeField, Min(0f)] private float _knockbackDistance = 0.35f;

        [Tooltip("Thời gian tiêu hết quãng đẩy lùi.")]
        [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.12f;

        private Vector2 _knockbackVelocity;
        private float _knockbackEndsAt;

        private EnemyData _data;
        private int _id;
        private int _health;
        private EnemyState _state;
        private float _stateEndsAt;
        private float _nextAttackAt;
        private PlayerCharacter _target;

        public int Id => _id;
        public EnemyData Data => _data;
        public EnemyState State => _state;
        public int Health => _health;
        public bool IsAlive => _state != EnemyState.Dead;
        public Vector2 Position => _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;

        /// <summary>Dựng một con quái vừa lấy từ pool. Hai máy gọi với cùng tham số.</summary>
        public void Initialize(int id, EnemyData data, Vector2 position)
        {
            _id = id;
            _data = data;
            _health = data.MaxHealth;
            _target = null;
            _nextAttackAt = 0f;
            _knockbackEndsAt = 0f;
            _knockbackVelocity = Vector2.zero;

            _rigidbody.position = position;
            transform.position = position;

            _renderer.sprite = data.BodySprite;
            _renderer.color = data.Tint;

            // Đối tượng này vừa được dùng cho một con đã chết, và nó đang khoá ở khung cuối
            // của hoạt ảnh chết. Phải dọn trước khi đặt trạng thái mới.
            if (_animator != null)
            {
                _animator.SetAnimationSet(data.AnimationSet);
                _animator.Unlock();
            }

            EnterState(data.SpawnDelay > 0f ? EnemyState.Spawning : EnemyState.Chasing);
            EnemyRegistry.Register(this);
        }

        public void OnSpawned()
        {
            _state = EnemyState.Spawning;
            if (_collider != null) _collider.enabled = true;
        }

        public void OnDespawned()
        {
            EnemyRegistry.Unregister(this);
            _target = null;
            _data = null;
        }

        /// <summary>
        /// Trừ máu. Chỉ host được gọi, qua <see cref="EnemySpawner"/>.
        /// </summary>
        /// <returns>Đúng nếu cú này giết được quái.</returns>
        public bool ApplyDamage(int amount)
        {
            if (!IsAlive) return false;

            _health -= Mathf.Max(amount, 0);
            return _health <= 0;
        }

        /// <summary>Thi hành cái chết trên máy này. Host quyết định, cả hai máy cùng gọi.</summary>
        public void Kill()
        {
            if (!IsAlive) return;

            _state = EnemyState.Dead;
            if (_collider != null) _collider.enabled = false;
            if (_animator != null) _animator.Lock(AnimState.Death);

            GameEvents.RaiseEnemyDied(this);
            EnemyRegistry.Unregister(this);
        }

        /// <summary>
        /// Bị đẩy lùi khỏi nguồn sát thương.
        /// </summary>
        /// <remarks>
        /// Đẩy lùi là phần biểu diễn, chạy trên mọi máy và không đồng bộ — xem bảng ở mục 3.2.
        /// Sai lệch vị trí nó gây ra được snapshot của host kéo lại trong vòng nửa giây.
        ///
        /// Đẩy lùi cũng có tác dụng chơi được chứ không chỉ để nhìn: nó tạo ra khoảng hở giữa
        /// người chơi và đám quái, nên một cú đánh mạnh vừa là sát thương vừa là không gian
        /// thở. Không có nó, người chơi bị vây rồi thì không còn cách nào thoát ngoài lướt.
        /// </remarks>
        public void ApplyKnockback(Vector2 fromPosition)
        {
            if (!IsAlive || _knockbackDistance <= 0f) return;

            Vector2 away = Position - fromPosition;
            if (away.sqrMagnitude < 0.0001f) return;

            _knockbackVelocity = away.normalized * (_knockbackDistance / _knockbackDuration);
            _knockbackEndsAt = Time.time + _knockbackDuration;
        }

        /// <summary>Kéo vị trí về đúng chỗ host báo. Chỉ client gọi.</summary>
        /// <remarks>
        /// Nội suy chứ không gán thẳng. Gán thẳng thì mỗi nửa giây quái nhảy một cái, và với
        /// 40 con thì cả màn hình giật cùng lúc. Nội suy làm sai lệch tan dần trong khoảng
        /// giữa hai snapshot, nên người chơi không nhìn ra.
        /// </remarks>
        public void ApplySnapshot(Vector2 position)
        {
            if (!IsAlive) return;
            _rigidbody.position = Vector2.Lerp(_rigidbody.position, position, _snapshotBlend);
        }

        private void EnterState(EnemyState next)
        {
            _state = next;
            if (next == EnemyState.Spawning && _data != null)
                _stateEndsAt = Time.time + _data.SpawnDelay;

            // Đứng đánh thì nền vẫn là Idle: mỗi nhịp chạm sẽ chồng một lượt Attack lên trên,
            // còn giữa hai nhịp thì con quái phải đứng thở chứ không lặp mãi động tác vung.
            if (_animator == null) return;
            switch (next)
            {
                case EnemyState.Chasing: _animator.SetBaseState(AnimState.Walk); break;
                case EnemyState.Spawning:
                case EnemyState.Attacking: _animator.SetBaseState(AnimState.Idle); break;
            }
        }

        private void FixedUpdate()
        {
            if (_data == null || !IsAlive) return;

            switch (_state)
            {
                case EnemyState.Spawning:
                    // Đứng yên và không gây sát thương trong pha báo trước. Quái hiện ra rồi
                    // lao vào ngay là không đọc kịp — xem ràng buộc đọc hiểu ở CLAUDE.md 2.1.
                    if (Time.time >= _stateEndsAt) EnterState(EnemyState.Chasing);
                    return;

                case EnemyState.Chasing:
                    Chase();
                    return;

                case EnemyState.Attacking:
                    Attack();
                    return;
            }
        }

        private void Chase()
        {
            _target = PlayerRegistry.Nearest(Position);
            if (_target == null) return;

            Vector2 toTarget = (Vector2)_target.transform.position - Position;
            if (toTarget.sqrMagnitude <= _data.AttackRange * _data.AttackRange)
            {
                EnterState(EnemyState.Attacking);
                return;
            }

            Vector2 direction = toTarget.normalized;

            // Cộng cả độ lớn của lực đẩy, không chỉ hướng. Nếu chuẩn hoá lực đẩy thì dù
            // chen chúc đến mấy nó vẫn chỉ bằng một hằng số, và lực hút về phía người chơi
            // luôn thắng — cả đàn co về đúng một điểm bất kể đông bao nhiêu.
            Vector2 push = Separation();
            if (push != Vector2.zero)
                direction = (direction + Vector2.ClampMagnitude(push * _separationWeight, _separationMax)).normalized;

            Step(direction, _data.MoveSpeed);
        }

        private void Step(Vector2 direction, float speed)
        {
            // Lật hình là biểu diễn cục bộ, không đồng bộ — xem bảng ở CLAUDE.md mục 3.2.
            if (_renderer != null && Mathf.Abs(direction.x) > 0.01f)
                _renderer.flipX = direction.x < 0f;

            Vector2 wanted = Position + direction * (speed * Time.fixedDeltaTime);

            if (Time.time < _knockbackEndsAt)
                wanted += _knockbackVelocity * Time.fixedDeltaTime;

            if (ArenaBounds.Instance != null)
                wanted = ArenaBounds.Instance.Clamp(wanted, Vector2.one * _arenaMargin);

            _rigidbody.MovePosition(wanted);
        }

        /// <summary>
        /// Lực đẩy khỏi những con quái đứng quá gần.
        /// </summary>
        /// <remarks>
        /// Không có lực này thì 40 con hội tụ về đúng một điểm và trông như một con duy nhất
        /// — người chơi mất khả năng ước lượng mối nguy, đúng thứ mà ràng buộc đọc hiểu thị
        /// giác ở CLAUDE.md mục 2.1 cấm.
        ///
        /// Tự tính thay vì để engine giải va chạm. Kết quả ở đây là một hàm thuần tuý của vị
        /// trí các con quái, nên hai máy tính ra cùng một số; còn thứ tự giải va chạm của
        /// engine thì không bảo đảm giống nhau và sẽ làm hai máy phân kỳ.
        ///
        /// Vòng lặp là O(n²) — 40 con cho 1600 phép so sánh mỗi bước vật lý. Đo được 2.67 ms
        /// mỗi khung hình với 40 con, còn xa ngân sách 16.6 ms của 60 FPS. Khi thêm loại quái
        /// và số lượng tăng thì mới cần chia ô không gian.
        /// </remarks>
        private Vector2 Separation()
        {
            if (_separationRadius <= 0f || _separationWeight <= 0f) return Vector2.zero;

            Vector2 push = Vector2.zero;
            float radiusSqr = _separationRadius * _separationRadius;
            var all = EnemyRegistry.Alive;

            for (int i = 0; i < all.Count; i++)
            {
                Enemy other = all[i];
                if (other == null || other == this || !other.IsAlive) continue;

                Vector2 away = Position - other.Position;
                float sqr = away.sqrMagnitude;
                if (sqr > radiusSqr) continue;

                // Hai con nằm chồng khít lên nhau thì không có hướng để đẩy. Lấy hướng từ
                // chính định danh: mỗi con một góc cố định, giống nhau trên cả hai máy.
                if (sqr < 0.0001f)
                {
                    push += new Vector2(Mathf.Cos(_id), Mathf.Sin(_id));
                    continue;
                }

                // Mỗi con hàng xóm góp tối đa 1 đơn vị, giảm tuyến tính theo khoảng cách.
                // Dùng 1/d² thì một con ở sát rạt sinh ra lực lớn tuỳ ý và bắn cả hai văng đi.
                float distance = Mathf.Sqrt(sqr);
                push += (away / distance) * (1f - distance / _separationRadius);
            }

            return push;
        }

        private void Attack()
        {
            if (_target == null || !_target.isActiveAndEnabled)
            {
                EnterState(EnemyState.Chasing);
                return;
            }

            Vector2 toTarget = (Vector2)_target.transform.position - Position;
            if (toTarget.sqrMagnitude > _data.AttackRange * _data.AttackRange)
            {
                EnterState(EnemyState.Chasing);
                return;
            }

            // Vẫn tiếp tục giãn ra trong lúc đứng đánh. Không làm việc này thì cả đàn dồn
            // vào đúng một điểm ngay khi chạm tầm đánh và trông như một con duy nhất — đo
            // được 40 con nằm trong bán kính 0.076 ở bản chỉ giãn cách lúc truy đuổi.
            Vector2 push = Separation();
            if (push != Vector2.zero)
                Step(push.normalized, _data.MoveSpeed * _crowdSpeedScale);

            if (Time.time < _nextAttackAt) return;
            _nextAttackAt = Time.time + _data.AttackInterval;

            // Chặn trần theo nhịp đánh: clip dài hơn thì nhịp sau cắt ngang nhịp trước và
            // động tác đứng nguyên ở khung đầu.
            if (_animator != null)
                _animator.PlayOneShot(AnimState.Attack, _data.AttackInterval * 0.8f);

            // Quái không tự trừ máu người chơi. Nó chỉ báo "hai bên đang chạm nhau" — một
            // sự thật cục bộ. Việc trừ máu là thẩm quyền của host và do DamageSystem ở T-13
            // thi hành, xem CLAUDE.md mục 3.2.
            GameEvents.RaiseEnemyTouchedPlayer(this, _target);
        }
    }
}
