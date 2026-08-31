using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Combat
{
    /// <summary>
    /// Điểm vào duy nhất cho mọi sát thương trong game. Thẩm quyền thuộc host.
    /// </summary>
    /// <remarks>
    /// Sát thương đến từ nhiều nguồn — quái chạm người, đạn trúng quái, sóng xung kích của
    /// Trống Đồng, vệt cháy của thẻ tiến hoá. Nếu mỗi nguồn tự trừ máu thì mỗi quy tắc bất
    /// tử phải được nhớ ở từng nơi, và chỉ cần một nguồn quên kiểm tra i-frame là cú lướt né
    /// đòn mất tác dụng trong đúng tình huống đó. Gom về một chỗ thì quy tắc được viết một
    /// lần và áp cho mọi nguồn.
    ///
    /// <b>Toàn bộ lớp này chỉ chạy trên host.</b> Lời gọi trên client bị bỏ qua lặng lẽ chứ
    /// không báo lỗi: client vẫn chạy cùng mã gameplay với host — đạn vẫn bay, quái vẫn đuổi
    /// — nên nó sẽ gọi vào đây rất thường xuyên. Đó là hành vi đúng, không phải sai sót.
    /// Client mô phỏng phần biểu diễn, host mô phỏng trạng thái thật.
    /// </remarks>
    public sealed class DamageSystem : MonoBehaviour
    {
        /// <summary>Điểm truy cập tĩnh, tự tìm lại nếu mất. Xem chú thích ở `RunManager.Instance`.</summary>
        public static DamageSystem Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<DamageSystem>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        private static DamageSystem _instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[DamageSystem] Đã có một hệ thống sát thương trong scene.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable() => GameEvents.EnemyTouchedPlayer += OnEnemyTouchedPlayer;

        private void OnDisable() => GameEvents.EnemyTouchedPlayer -= OnEnemyTouchedPlayer;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnEnemyTouchedPlayer(Enemy enemy, PlayerCharacter player)
        {
            if (enemy == null || enemy.Data == null) return;
            ApplyToPlayer(player, enemy.Data.ContactDamage, enemy.Position);
        }

        /// <summary>
        /// Gây sát thương lên một người chơi. Bỏ qua nếu không phải host.
        /// </summary>
        /// <returns>Đúng nếu sát thương thực sự được áp dụng.</returns>
        public static bool ApplyToPlayer(PlayerCharacter player, int amount, Vector2 source)
        {
            if (!NetworkServer.active || player == null) return false;
            if (!player.TryGetComponent(out PlayerHealth health)) return false;

            if (!health.ServerTakeDamage(amount)) return false;

            GameEvents.RaisePlayerDamaged(player, amount, source);
            return true;
        }

        /// <summary>
        /// Gây sát thương lên một con quái. Bỏ qua nếu không phải host.
        /// </summary>
        /// <remarks>
        /// Chuyển tiếp cho <see cref="EnemySpawner"/> chứ không tự trừ máu quái: cái chết của
        /// quái phải được phát xuống client theo định danh, và chỉ bộ sinh quái mới biết cách
        /// làm việc đó — xem T-14.
        /// </remarks>
        /// <returns>Đúng nếu sát thương thực sự được áp dụng.</returns>
        public static bool ApplyToEnemy(Enemy enemy, int amount, Vector2 source)
        {
            if (!NetworkServer.active || enemy == null || !enemy.IsAlive || amount <= 0) return false;
            if (EnemySpawner.Instance == null) return false;

            EnemySpawner.Instance.DamageEnemy(enemy, amount);
            GameEvents.RaiseEnemyDamaged(enemy, amount, source);
            return true;
        }

        /// <summary>Hồi máu cho một người chơi. Bỏ qua nếu không phải host.</summary>
        public static void HealPlayer(PlayerCharacter player, int amount)
        {
            if (!NetworkServer.active || player == null) return;
            if (!player.TryGetComponent(out PlayerHealth health)) return;

            health.ServerHeal(amount);
        }
    }
}
