using UnityEngine;

namespace LAC.Enemies
{
    /// <summary>Chỉ số và hình dạng của một loại quái.</summary>
    /// <remarks>
    /// Độ thử thách của LẠC đến từ thành phần đợt chứ không từ trí thông minh của từng con
    /// quái — xem docs/GDD.md mục 6.1. Nên tài sản này chỉ mô tả chỉ số và một hành vi đơn
    /// giản; phần khó nằm ở việc AI Đạo Diễn trộn các loại quái với nhau.
    /// </remarks>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "LAC/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string _displayName = "Cô Hồn";

        [Header("Chỉ số")]
        [SerializeField, Min(1)] private int _maxHealth = 10;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 3f;
        [SerializeField, Min(1)] private int _contactDamage = 1;

        [Tooltip("Khoảng cách tính từ tâm để coi là đã áp sát.")]
        [SerializeField, Min(0.1f)] private float _attackRange = 0.7f;

        [Tooltip("Giây giữa hai lần gây sát thương chạm.")]
        [SerializeField, Min(0.1f)] private float _attackInterval = 0.8f;

        [Header("Xuất hiện")]
        [Tooltip("Thời gian báo trước trước khi quái bắt đầu đuổi.")]
        [SerializeField, Min(0f)] private float _spawnDelay = 0.35f;

        [Header("Hiển thị")]
        [SerializeField] private Sprite _bodySprite;
        [SerializeField] private Color _tint = Color.white;

        public string DisplayName => _displayName;
        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public int ContactDamage => _contactDamage;
        public float AttackRange => _attackRange;
        public float AttackInterval => _attackInterval;
        public float SpawnDelay => _spawnDelay;
        public Sprite BodySprite => _bodySprite;
        public Color Tint => _tint;
    }
}
