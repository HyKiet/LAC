using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using LAC.Utils;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Gom toàn bộ phản hồi khi đánh trúng: nháy sáng, đẩy lùi, số sát thương, dừng hình, rung màn.
    /// </summary>
    /// <remarks>
    /// Toàn bộ những thứ ở đây là <b>biểu diễn thuần tuý và cục bộ</b>, không đồng bộ qua
    /// mạng — xem bảng ở CLAUDE.md mục 3.2. Chúng được kích hoạt bởi sự kiện sát thương, mà
    /// sự kiện đó chỉ phát trên host. Client sẽ nhận phản hồi của riêng nó khi các hệ thống
    /// biểu diễn phía client được nối vào ở giai đoạn sau.
    ///
    /// Gom về một chỗ thay vì rải vào từng hệ thống vì phản hồi cần được <b>điều tiết theo
    /// mức độ</b>: một cú đánh thường chỉ nháy sáng, còn quái chết mới được dừng hình và rung
    /// màn. Nếu mỗi hệ thống tự quyết thì cuối ván mọi thứ cùng kêu to và không còn gì nổi bật.
    /// </remarks>
    public sealed class HitFeedback : MonoBehaviour
    {
        [Header("Số sát thương")]
        [SerializeField] private DamageNumber _damageNumberPrefab;
        [SerializeField] private Color _enemyHitColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color _playerHitColor = new Color(1f, 0.45f, 0.4f);

        [Header("Nháy sáng")]
        [Tooltip("Hệ số nhân, KHÔNG phải màu hiển thị. Phải lớn hơn 1 mới thấy được — " +
                 "xem chú thích trong SpriteFlash.")]
        [SerializeField] private Color _flashColor = new Color(6f, 6f, 6f, 1f);

        [Header("Sức nặng")]
        [Tooltip("Dừng hình khi quái chết. Không dùng cho mỗi lần trúng đòn thường.")]
        [SerializeField, Range(0f, 0.03f)] private float _killHitStop = 0.02f;

        [Tooltip("Dừng hình khi chính người chơi trúng đòn.")]
        [SerializeField, Range(0f, 0.03f)] private float _playerHitStop = 0.03f;

        [SerializeField, Min(0f)] private float _killShake = 0.06f;
        [SerializeField, Min(0f)] private float _playerHitShake = 0.22f;

        private ObjectPool<DamageNumber> _numberPool;
        private CameraFollow _camera;

        private void OnEnable()
        {
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyDied += OnEnemyDied;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyDied -= OnEnemyDied;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
        }

        private void Update() => HitStop.Tick();

        private void OnEnemyDamaged(Enemy enemy, int amount, Vector2 source)
        {
            if (enemy == null) return;

            if (enemy.TryGetComponent(out SpriteFlash flash)) flash.Flash(_flashColor);
            enemy.ApplyKnockback(source);
            ShowNumber(amount, enemy.Position + Vector2.up * 0.5f, _enemyHitColor);
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null) return;

            HitStop.Request(_killHitStop);
            Shake(_killShake);
        }

        private void OnPlayerDamaged(PlayerCharacter player, int amount, Vector2 source)
        {
            if (player == null) return;

            if (player.TryGetComponent(out SpriteFlash flash)) flash.Flash(_flashColor);
            ShowNumber(amount, (Vector2)player.transform.position + Vector2.up * 0.7f, _playerHitColor);

            // Người chơi trúng đòn là sự kiện nặng nhất trong game: rung mạnh hơn và dừng lâu
            // hơn quái chết. Đây là thứ duy nhất người chơi tuyệt đối không được bỏ lỡ.
            HitStop.Request(_playerHitStop);
            Shake(_playerHitShake);
        }

        private void ShowNumber(int amount, Vector2 position, Color color)
        {
            if (_damageNumberPrefab == null || amount <= 0) return;

            _numberPool ??= PoolRegistry.Get(_damageNumberPrefab, prewarm: 16, softLimit: 128);
            _numberPool.Get(position, Quaternion.identity).Play(_numberPool, amount, position, color);
        }

        private void Shake(float amount)
        {
            if (_camera == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                _camera = main.GetComponent<CameraFollow>();
                if (_camera == null) return;
            }

            _camera.Shake(amount);
        }
    }
}
