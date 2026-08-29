namespace LAC.Core
{
    /// <summary>Các giai đoạn của một ván.</summary>
    public enum RunState
    {
        /// <summary>Chưa bắt đầu — đang ở sảnh chờ hoặc màn chọn nhân vật.</summary>
        Idle = 0,

        /// <summary>Đang trong một đợt quái.</summary>
        WaveActive = 1,

        /// <summary>Đã dọn sạch đợt, người chơi đang chọn thẻ nâng cấp.</summary>
        CardSelection = 2,

        /// <summary>Đã hạ trùm ở đợt cuối.</summary>
        Victory = 3,

        /// <summary>Toàn bộ người chơi đã gục.</summary>
        Defeat = 4
    }
}
