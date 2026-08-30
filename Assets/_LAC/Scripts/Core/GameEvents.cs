using System;

namespace LAC.Core
{
    /// <summary>
    /// Kênh sự kiện tĩnh dùng chung, cắt phụ thuộc chéo giữa các module.
    /// </summary>
    /// <remarks>
    /// Cái chết của một con quái là mối quan tâm của ít nhất bốn hệ thống: bộ đếm đợt, cơ chế
    /// Hồn, telemetry cho khoá luận, và AI Đạo Diễn. Để quái tự gọi thẳng từng hệ thống thì
    /// lớp quái phải biết cả bốn, và thêm hệ thống thứ năm lại phải sửa lớp quái.
    ///
    /// Sự kiện ở đây là **cục bộ trên một máy**, không đi qua mạng. Máy nào phát sự kiện là
    /// quyết định của hệ thống sinh ra nó — cái chết chẳng hạn do host quyết rồi báo xuống
    /// client, và cả hai máy đều phát sự kiện này khi cái chết được thi hành tại chỗ.
    ///
    /// Danh sách sẽ dài thêm khi các hệ thống khác hoàn thành: `WaveCleared`, `CardPicked`,
    /// `PlayerHit` — xem docs/ARCHITECTURE.md mục 2.1.
    /// </remarks>
    public static class GameEvents
    {
        /// <summary>Một con quái vừa chết. Tham số là quái đó, vẫn còn đọc được chỉ số.</summary>
        public static event Action<Enemies.Enemy> EnemyDied;

        /// <summary>Một con quái vừa chạm và gây sát thương lên một người chơi.</summary>
        /// <remarks>
        /// Quái không tự trừ máu. `DamageSystem` ở T-13 lắng nghe sự kiện này và là nơi duy
        /// nhất được phép thay đổi máu — xem CLAUDE.md mục 3.2.
        /// </remarks>
        public static event Action<Enemies.Enemy, Player.PlayerCharacter> EnemyTouchedPlayer;

        public static void RaiseEnemyDied(Enemies.Enemy enemy) => EnemyDied?.Invoke(enemy);

        public static void RaiseEnemyTouchedPlayer(Enemies.Enemy enemy, Player.PlayerCharacter player) =>
            EnemyTouchedPlayer?.Invoke(enemy, player);

        /// <summary>
        /// Gỡ toàn bộ người nghe. Gọi khi kết thúc ván.
        /// </summary>
        /// <remarks>
        /// Sự kiện tĩnh sống qua cả lần đổi scene. Không gỡ thì đối tượng của ván trước vẫn
        /// bị gọi ở ván sau — một trong những nguồn rò rỉ khó truy nhất khi dùng event bus.
        /// </remarks>
        public static void Clear()
        {
            EnemyDied = null;
            EnemyTouchedPlayer = null;
        }
    }
}
