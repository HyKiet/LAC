using LAC.Combat;
using LAC.VFX;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Dịch trạng thái của nhân vật người chơi thành trạng thái hoạt ảnh.
    /// </summary>
    /// <remarks>
    /// Thuần cục bộ, không có một dòng mạng nào. Hoạt ảnh nằm ở hàng "không đồng bộ" trong
    /// bảng ở CLAUDE.md mục 3.2: mỗi máy đã có sẵn mọi thứ cần để tự suy ra — vị trí đến
    /// qua <c>NetworkTransform</c>, máu đến qua <c>SyncVar</c>, đòn đánh thì vũ khí chạy
    /// trên mọi máy. Gửi thêm trạng thái hoạt ảnh qua đường truyền là trả tiền băng thông
    /// cho thứ hai bên đều tự tính được.
    ///
    /// Chạy cho cả nhân vật của mình lẫn của người khác, không phân biệt.
    /// </remarks>
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private SpriteAnimator _animator;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private WeaponAuto _weapon;

        [Tooltip("Thời gian giữ hướng nhìn về phía mục tiêu sau mỗi đòn đánh.")]
        [SerializeField, Min(0f)] private float _aimHold = 0.25f;

        [Tooltip("Phần chu kỳ khai hoả mà hoạt ảnh đánh được phép chiếm.")]
        [SerializeField, Range(0.3f, 1f)] private float _attackClipBudget = 0.8f;

        private SpriteAnimationSet _appliedSet;
        private bool _wasAlive = true;
        private int _lastHealth = int.MaxValue;

        private void OnEnable()
        {
            if (_weapon != null) _weapon.Fired += OnFired;
            if (_health != null) _health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (_weapon != null) _weapon.Fired -= OnFired;
            if (_health != null) _health.HealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            SyncAnimationSet();

            if (_animator == null || !_animator.HasSet) return;
            if (_health != null && !_health.IsAlive) return;

            bool moving = _movement != null && _movement.IsMoving;
            _animator.SetBaseState(moving ? AnimState.Walk : AnimState.Idle);
        }

        /// <summary>
        /// Nạp bộ hoạt ảnh từ nhân vật khi định danh đã được áp dụng.
        /// </summary>
        /// <remarks>
        /// Không làm một lần trong Awake được: <see cref="PlayerCharacter.Data"/> còn null cho
        /// tới khi <c>SyncVar</c> định danh về tới máy này, và thời điểm đó khác nhau giữa
        /// host và client. Kiểm tra mỗi khung là vài phép so sánh tham chiếu, rẻ hơn nhiều so
        /// với việc bố trí thứ tự khởi tạo giữa hai đường dẫn khác nhau.
        /// </remarks>
        private void SyncAnimationSet()
        {
            if (_animator == null || _character == null) return;

            CharacterData data = _character.Data;
            SpriteAnimationSet set = data != null ? data.AnimationSet : null;
            if (set == _appliedSet) return;

            _appliedSet = set;
            _animator.SetAnimationSet(set);
        }

        private void OnFired(Vector2 direction)
        {
            if (_movement != null) _movement.FaceTowards(direction, _aimHold);

            if (_animator == null || !_animator.HasSet) return;
            if (_health != null && !_health.IsAlive) return;

            // Hoạt ảnh đánh phải chạy xong trước đòn kế tiếp. Không chặn trần thì ở nhân vật
            // có chu kỳ ngắn — Tấm bắn 0.12 giây một phát — mỗi đòn cắt ngang đòn trước và
            // động tác đứng nguyên ở khung đầu tiên.
            float budget = 0f;
            CharacterData data = _character != null ? _character.Data : null;
            if (data != null) budget = data.AttackInterval * _attackClipBudget;

            _animator.PlayOneShot(AnimState.Attack, budget);
        }

        private void OnHealthChanged(int health, int max)
        {
            bool damaged = health < _lastHealth;
            _lastHealth = health;

            if (_animator == null || !_animator.HasSet) return;

            bool alive = health > 0;

            if (!alive)
            {
                _animator.Lock(AnimState.Death);
                _wasAlive = false;
                return;
            }

            if (!_wasAlive)
            {
                _animator.Unlock();
                _wasAlive = true;
                return;
            }

            // Chớp đỏ của SpriteFlash và hoạt ảnh trúng đòn cùng nhuộm đỏ nhân vật. Bật cả
            // hai thì chớp hai lần lệch nhịp, nên bộ hoạt ảnh tự khai báo mình có dùng
            // hoạt ảnh trúng đòn hay không.
            if (damaged && _appliedSet != null && _appliedSet.PlayHurtClip)
                _animator.PlayOneShot(AnimState.Hurt);
        }
    }
}
