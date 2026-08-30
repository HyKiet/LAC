using LAC.Combat;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>Định danh ba nhân vật. Dùng để gắn hiệu ứng riêng của từng nhân vật.</summary>
    public enum CharacterId
    {
        ThachSanh = 0,
        Giong = 1,
        Tam = 2
    }

    /// <summary>
    /// Toàn bộ chỉ số của một nhân vật chơi được.
    /// </summary>
    /// <remarks>
    /// Vũ khí gắn cố định với nhân vật và không thay thế được trong ván, nên chỉ số vũ khí
    /// nằm ngay trong tệp này thay vì tách thành một tài sản riêng — đổi nhân vật tương
    /// đương đổi lối chơi.
    /// Không hard-code giá trị nào trong C#: mọi con số dưới đây thuộc về tài sản trong
    /// thư mục Data/Characters, để khâu cân bằng không phải biên dịch lại mã nguồn.
    /// </remarks>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "LAC/Character Data")]
    public sealed class CharacterData : ScriptableObject
    {
        [Header("Định danh")]
        [SerializeField] private CharacterId _id = CharacterId.ThachSanh;
        [SerializeField] private string _displayName = "Thạch Sanh";
        [SerializeField, TextArea(2, 4)] private string _description = "";

        [Header("Chỉ số cơ bản")]
        [SerializeField, Min(1)] private int _maxHealth = 6;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 5f;

        [Header("Vũ khí")]
        [SerializeField] private string _weaponName = "Đàn bầu";
        [SerializeField] private WeaponShape _weaponShape = WeaponShape.Circle;
        [SerializeField, Min(0.1f)] private float _attackRange = 4f;
        [SerializeField, Min(0.02f)] private float _attackInterval = 0.9f;
        [SerializeField, Min(1)] private int _baseDamage = 1;

        [Tooltip("Tốc độ đạn. Chỉ dùng cho vũ khí hình tia.")]
        [SerializeField, Min(1f)] private float _projectileSpeed = 12f;

        [Header("Lướt")]
        [SerializeField, Min(0.1f)] private float _dashDistance = 6f;
        [SerializeField, Min(0.02f)] private float _dashDuration = 0.15f;
        [SerializeField, Min(0.05f)] private float _dashCooldown = 0.4f;

        [Header("Hiển thị")]
        [SerializeField] private Sprite _bodySprite;
        [SerializeField] private Sprite _portrait;

        public CharacterId Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;

        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;

        public string WeaponName => _weaponName;
        public WeaponShape WeaponShape => _weaponShape;
        public float AttackRange => _attackRange;
        public float AttackInterval => _attackInterval;
        public int BaseDamage => _baseDamage;
        public float ProjectileSpeed => _projectileSpeed;

        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;

        public Sprite BodySprite => _bodySprite;
        public Sprite Portrait => _portrait;

        /// <summary>Tốc độ di chuyển trong lúc lướt, suy ra từ quãng đường và thời lượng.</summary>
        public float DashSpeed => _dashDistance / _dashDuration;
    }
}
