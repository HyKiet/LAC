using UnityEngine;
using LAC.Combat;

namespace LAC.Cards
{
    [CreateAssetMenu(fileName = "Card", menuName = "LAC/Cards/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private CardId _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 5)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        [SerializeField, Min(0.001f)] private float _weight = 1f;
        [SerializeField] private Color _accent = new Color(0.82f, 0.62f, 0.28f, 1f);
        [Header("Hiệu ứng mỗi cấp — cộng theo chỉ số gốc")]
        [SerializeField] private float _damageBonus;
        [SerializeField] private float _attackSpeedBonus;
        [SerializeField] private float _healthBonusRatio;
        [SerializeField] private float _dashCooldownReduction;
        [SerializeField] private float _moveSpeedBonus;
        [SerializeField] private float _attackRangeBonus;
        [SerializeField] private float _hitInvulnerabilityBonus;
        [Tooltip("Hệ số phần trăm theo cấp; để trống dùng 1. Không nhân số đạn/hồi máu/tiến hoá.")]
        [SerializeField] private float[] _statScales = System.Array.Empty<float>();
        [SerializeField] private int _waveHeal;
        [Header("Cải biến đạn")]
        [SerializeField] private bool _requiresProjectile;
        [SerializeField] private int _extraProjectiles;
        [SerializeField] private float _projectileDamageRatio = 1f;
        [SerializeField] private float _projectileSpread;
        [SerializeField] private int _extraPierces;
        [SerializeField] private float _explosionRadius;
        [SerializeField] private float _explosionDamageRatio;

        public CardId Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxStacks => _maxStacks;
        public float Weight => _weight;
        public Color Accent => _accent;
        public float DamageBonus => _damageBonus;
        public float AttackSpeedBonus => _attackSpeedBonus;
        public float HealthBonusRatio => _healthBonusRatio;
        public float DashCooldownReduction => _dashCooldownReduction;
        public float MoveSpeedBonus => _moveSpeedBonus;
        public float AttackRangeBonus => _attackRangeBonus;
        public float HitInvulnerabilityBonus => _hitInvulnerabilityBonus;
        public int WaveHeal => _waveHeal;
        public bool RequiresProjectile => _requiresProjectile;
        public int ExtraProjectiles => _extraProjectiles;
        public float ProjectileDamageRatio => _projectileDamageRatio;
        public float ProjectileSpread => _projectileSpread;
        public int ExtraPierces => _extraPierces;
        public float ExplosionRadius => _explosionRadius;
        public float ExplosionDamageRatio => _explosionDamageRatio;
        public bool Supports(WeaponShape shape) => !_requiresProjectile || shape == WeaponShape.Line;

        public float StatScaleAtStack(int stack)
        {
            if (_statScales == null || stack < 1 || stack > _statScales.Length) return 1f;
            float scale = _statScales[stack - 1];
            return float.IsNaN(scale) || float.IsInfinity(scale) ? 1f : Mathf.Max(0f, scale);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void EditorConfigureStatScales(params float[] scales) =>
            _statScales = scales == null ? System.Array.Empty<float>() : (float[])scales.Clone();

        public void EditorConfigureEffects(float damage = 0f, float attackSpeed = 0f,
            float health = 0f, float dash = 0f, float move = 0f, float range = 0f,
            float protection = 0f, int waveHeal = 0, bool projectileOnly = false,
            int extraProjectiles = 0, float projectileDamage = 1f, float spread = 0f,
            int pierces = 0, float explosionRadius = 0f, float explosionDamage = 0f)
        {
            _damageBonus = damage; _attackSpeedBonus = attackSpeed; _healthBonusRatio = health;
            _dashCooldownReduction = dash; _moveSpeedBonus = move; _attackRangeBonus = range;
            _hitInvulnerabilityBonus = protection; _waveHeal = waveHeal;
            _requiresProjectile = projectileOnly; _extraProjectiles = extraProjectiles;
            _projectileDamageRatio = projectileDamage; _projectileSpread = spread;
            _extraPierces = pierces; _explosionRadius = explosionRadius;
            _explosionDamageRatio = explosionDamage;
        }

        public void EditorConfigure(CardId id, string displayName, string description,
            int maxStacks, float weight, Color accent)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _maxStacks = Mathf.Max(1, maxStacks);
            _weight = Mathf.Max(0.001f, weight);
            _accent = accent;
        }
#endif
    }
}
