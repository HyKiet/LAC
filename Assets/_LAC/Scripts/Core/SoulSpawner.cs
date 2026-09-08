using LAC.Audio;
using LAC.Enemies;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Nghe cái chết của quái và thả Hồn ra tại chỗ.
    /// </summary>
    /// <remarks>
    /// Đặt thành một thành phần riêng trong scene thay vì gọi thẳng từ <c>Enemy</c>: quái
    /// không cần biết trên đời có cơ chế Hồn, và T-38 sau này chỉ phải đọc bộ đếm ở
    /// <c>RunManager</c> chứ không phải sửa gì ở đây — xem chú thích của <c>GameEvents</c>.
    /// </remarks>
    public sealed class SoulSpawner : MonoBehaviour
    {
        [SerializeField] private SoulPickup _soulPrefab;

        [Tooltip("Chuỗi cao độ dùng chung cho mọi lần nhặt. Bỏ trống thì nhặt không có tiếng.")]
        [SerializeField] private PitchLadder _pickupSound;

        [Tooltip("Số Hồn tạo sẵn. Cuối ván có thể vài chục Hồn cùng tồn tại.")]
        [SerializeField, Min(0)] private int _prewarm = 64;

        [SerializeField, Min(1)] private int _softLimit = 256;

        private ObjectPool<SoulPickup> _pool;

        private void Awake()
        {
            if (_soulPrefab == null)
            {
                Debug.LogError("[SoulSpawner] Chưa gán prefab Hồn.", this);
                return;
            }

            _pool = PoolRegistry.Get(_soulPrefab, _prewarm, _softLimit);
        }

        private void OnEnable() => GameEvents.EnemyDied += OnEnemyDied;

        private void OnDisable() => GameEvents.EnemyDied -= OnEnemyDied;

        private void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null || _pool == null) return;

            int count = RollCount(enemy);
            for (int i = 0; i < count; i++)
            {
                SoulPickup soul = _pool.Get(enemy.Position, Quaternion.identity);
                soul.Launch(_pool, _pickupSound, ScatterDirection());
            }
        }

        /// <summary>
        /// Số Hồn rơi ra, tra theo định danh quái nên hai máy luôn ra cùng kết quả.
        /// </summary>
        /// <remarks>
        /// Dùng <see cref="RandomStream.Hash01"/> chứ không rút tuần tự khỏi luồng. Cái chết
        /// của quái đến với host ngay lập tức nhưng đến với client qua RPC, và người vào giữa
        /// ván thì bỏ lỡ hẳn những cái chết trước đó — rút tuần tự thì kể từ đó hai máy lệch
        /// nhau vĩnh viễn. Tra theo định danh thì số lần gọi và thứ tự gọi đều không ảnh hưởng.
        ///
        /// Luồng <c>Loot</c> có thể chưa tồn tại nếu quái chết trước khi ván khởi tạo seed;
        /// khi đó rơi ở mức tối thiểu thay vì ném lỗi.
        /// </remarks>
        private static int RollCount(Enemy enemy)
        {
            EnemyData data = enemy.Data;
            if (data == null) return 1;

            int min = data.SoulDropMin;
            int span = data.SoulDropMax - min;
            if (span <= 0 || RunRandom.Loot == null) return min;

            float t = RunRandom.Loot.Hash01(enemy.Id);
            return min + Mathf.Min((int)(t * (span + 1)), span);
        }

        /// <summary>
        /// Hướng văng ra khi rơi.
        /// </summary>
        /// <remarks>
        /// Dùng ngẫu nhiên của Unity chứ không phải <c>RunRandom</c>: đường bay là trang trí
        /// thuần tuý, đúng ngoại lệ duy nhất nêu ở CLAUDE.md mục 3.3. Hai máy văng Hồn theo
        /// hai hướng khác nhau không gây sai lệch trạng thái vì chỉ host đếm số nhặt được.
        /// </remarks>
        private static Vector2 ScatterDirection()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
