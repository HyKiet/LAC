using System;
using LAC.Core;
using Mirror;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Máu của một người chơi. <b>Chỉ host được phép thay đổi.</b>
    /// </summary>
    /// <remarks>
    /// Client tự trừ máu của chính mình là một trong ba lỗi triển khai bị cấm ở CLAUDE.md
    /// mục 3.2 — hai máy sẽ phân kỳ chỉ sau vài giây, vì mỗi máy nhìn thấy va chạm ở thời
    /// điểm khác nhau. Ở đây máu là `SyncVar` do host giữ; client chỉ đọc để hiển thị.
    ///
    /// Không có phương thức công khai nào cho phép trừ máu trực tiếp. Mọi sát thương phải đi
    /// qua <see cref="Combat.DamageSystem"/> — một điểm vào duy nhất, nơi các quy tắc bất tử
    /// được kiểm tra một lần thay vì rải rác ở từng nguồn sát thương.
    /// </remarks>
    public sealed class PlayerHealth : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerDash _dash;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Thời gian bất tử sau khi trúng đòn.")]
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.6f;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private int _health;

        [SyncVar]
        private int _maxHealth = 1;

        private float _invulnerableUntil;

        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _health > 0;

        /// <summary>Máu thay đổi. Tham số: máu hiện tại, máu tối đa. Dành cho giao diện và T-15.</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>
        /// Đang miễn nhiễm sát thương hay không.
        /// </summary>
        /// <remarks>
        /// Hai nguồn bất tử: đang lướt (T-11) và vừa trúng đòn. Cửa sổ sau khi trúng đòn là
        /// bắt buộc chứ không phải ưu ái: cuối ván có 40 con vây quanh, mỗi con gây 1 sát
        /// thương, nên không có cửa sổ này thì Thạch Sanh 6 máu chết trong một khung hình mà
        /// không kịp phản ứng.
        /// </remarks>
        public bool IsInvulnerable =>
            Time.time < _invulnerableUntil || (_dash != null && _dash.IsInvulnerable);

        public override void OnStartServer()
        {
            int max = _character != null && _character.Data != null ? _character.Data.MaxHealth : 1;
            _maxHealth = max;
            _health = max;
        }

        public override void OnStartClient() => ApplyAliveState();

        /// <summary>
        /// Trừ máu. Chỉ <see cref="Combat.DamageSystem"/> trên host được gọi.
        /// </summary>
        /// <returns>Đúng nếu sát thương thực sự được áp dụng.</returns>
        [Server]
        internal bool ServerTakeDamage(int amount)
        {
            if (!IsAlive || IsInvulnerable || amount <= 0) return false;

            _health = Mathf.Max(_health - amount, 0);
            _invulnerableUntil = Time.time + _hitInvulnerability;

            if (_health == 0 && RunManager.Instance != null)
                RunManager.Instance.ReportPlayerDown();

            return true;
        }

        /// <summary>Hồi máu. Chỉ host được gọi.</summary>
        [Server]
        internal void ServerHeal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            _health = Mathf.Min(_health + amount, _maxHealth);
        }

        private void OnHealthChanged(int _, int newHealth)
        {
            HealthChanged?.Invoke(newHealth, _maxHealth);
            ApplyAliveState();
        }

        /// <summary>
        /// Cập nhật phần biểu diễn theo trạng thái sống chết. Chạy trên mọi máy.
        /// </summary>
        /// <remarks>
        /// Cái chết do host quyết và lan xuống qua `SyncVar`, nhưng việc tắt điều khiển thì
        /// mỗi máy tự làm cho nhân vật của mình — nếu chờ host gửi lệnh tắt thì trong khoảng
        /// một vòng mạng người chơi vẫn điều khiển được một nhân vật đã chết.
        /// </remarks>
        private void ApplyAliveState()
        {
            bool alive = IsAlive;

            if (_input != null && isOwned) _input.enabled = alive;

            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = alive ? 1f : 0.35f;
                _renderer.color = color;
            }
        }
    }
}
