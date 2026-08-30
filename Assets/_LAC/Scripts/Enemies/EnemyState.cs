namespace LAC.Enemies
{
    /// <summary>Ba trạng thái của quái, theo docs/ARCHITECTURE.md mục 2.4.</summary>
    public enum EnemyState
    {
        /// <summary>Vừa hiện ra, chưa di chuyển và chưa gây sát thương.</summary>
        Spawning = 0,

        /// <summary>Đang truy đuổi người chơi gần nhất.</summary>
        Chasing = 1,

        /// <summary>Đã áp sát và đang gây sát thương chạm.</summary>
        Attacking = 2,

        /// <summary>Đã chết, đang chờ trả về pool.</summary>
        Dead = 3
    }
}
