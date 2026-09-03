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
        /// <summary>Gỡ mọi người nghe mỗi lần vào play mode. Xem `PoolRegistry.ResetStatics`.</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        /// <summary>Một con quái vừa chết. Tham số là quái đó, vẫn còn đọc được chỉ số.</summary>
        public static event Action<Enemies.Enemy> EnemyDied;

        /// <summary>Một con quái vừa chạm và gây sát thương lên một người chơi.</summary>
        /// <remarks>
        /// Quái không tự trừ máu. `DamageSystem` ở T-13 lắng nghe sự kiện này và là nơi duy
        /// nhất được phép thay đổi máu — xem CLAUDE.md mục 3.2.
        /// </remarks>
        public static event Action<Enemies.Enemy, Player.PlayerCharacter> EnemyTouchedPlayer;

        /// <summary>
        /// Một người chơi vừa trúng đòn. Tham số: người chơi, lượng máu đã mất, vị trí nguồn.
        /// </summary>
        /// <remarks>Chỉ phát trên host, vì chỉ host quyết được sát thương có hiệu lực hay không.</remarks>
        public static event Action<Player.PlayerCharacter, int, UnityEngine.Vector2> PlayerDamaged;

        /// <summary>
        /// Một con quái vừa trúng đòn. Tham số: quái, lượng máu đã mất, vị trí nguồn sát thương.
        /// </summary>
        /// <remarks>
        /// Vị trí nguồn đi kèm chứ không để bên nhận tự đoán: hướng đẩy lùi phải là hướng ra
        /// xa thứ đã đánh trúng. Đoán bằng "ra xa người chơi gần nhất" sẽ sai ngay khi có đạn
        /// nảy tường hoặc hai người chơi đứng hai phía.
        /// </remarks>
        public static event Action<Enemies.Enemy, int, UnityEngine.Vector2> EnemyDamaged;

        public static void RaiseEnemyDied(Enemies.Enemy enemy) => EnemyDied?.Invoke(enemy);

        public static void RaisePlayerDamaged(Player.PlayerCharacter player, int amount, UnityEngine.Vector2 source) =>
            PlayerDamaged?.Invoke(player, amount, source);

        public static void RaiseEnemyDamaged(Enemies.Enemy enemy, int amount, UnityEngine.Vector2 source) =>
            EnemyDamaged?.Invoke(enemy, amount, source);

        public static void RaiseEnemyTouchedPlayer(Enemies.Enemy enemy, Player.PlayerCharacter player) =>
            EnemyTouchedPlayer?.Invoke(enemy, player);

        /// <summary>
        /// Gỡ toàn bộ người nghe. Gọi khi <b>rời scene đấu trường</b>, không phải khi kết
        /// thúc một ván.
        /// </summary>
        /// <remarks>
        /// Sự kiện tĩnh sống qua cả lần đổi scene. Không gỡ thì đối tượng của ván trước vẫn
        /// bị gọi ở ván sau — một trong những nguồn rò rỉ khó truy nhất khi dùng event bus.
        ///
        /// <b>Không gọi hàm này giữa hai ván.</b> Chơi lại không nạp lại scene, nên
        /// <c>HitFeedback</c> và <c>PlayerHud</c> vẫn đang sống và đã đăng ký từ
        /// <c>OnEnable</c> — gọi <c>Clear</c> ở đó sẽ gỡ luôn chúng và ván sau mất sạch phản
        /// hồi khi đánh trúng, trong khi mã vẫn chạy đúng nên rất khó truy. Người nghe của
        /// một ván đều tự gỡ trong <c>OnDisable</c> của chính mình, nên không có rò rỉ nào
        /// cần dọn ở ranh giới giữa hai ván.
        /// </remarks>
        public static void Clear()
        {
            EnemyDied = null;
            EnemyTouchedPlayer = null;
            PlayerDamaged = null;
            EnemyDamaged = null;
        }
    }
}
