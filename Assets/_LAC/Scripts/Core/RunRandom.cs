using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Nguồn ngẫu nhiên duy nhất của luồng gameplay. Xem CLAUDE.md mục 3.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Host quyết định seed của ván và gửi xuống client một lần khi khởi tạo. Từ đó hai máy
    /// mô phỏng song song mà không cần đồng bộ liên tục — đây là điều kiện tiên quyết để
    /// quái vật và bể thẻ chỉ đồng bộ seed thay vì đồng bộ trạng thái.
    /// </para>
    /// <para>
    /// Ngẫu nhiên được chia thành các kênh độc lập thay vì dùng chung một luồng. Lý do:
    /// nếu mọi hệ thống rút số từ cùng một luồng, chỉ cần một máy rút thêm hoặc thiếu một
    /// lần — chẳng hạn một hiệu ứng hình ảnh chỉ xuất hiện ở phía client — là toàn bộ dãy
    /// số phía sau lệch pha và hai máy phân kỳ. Tách kênh khiến các hệ thống không ảnh
    /// hưởng lẫn nhau.
    /// </para>
    /// <para>
    /// Hiệu ứng hình ảnh và âm thanh thuần tuý trang trí không dùng lớp này; chúng được
    /// phép gọi <c>UnityEngine.Random</c> vì không tác động đến trạng thái ván đấu.
    /// </para>
    /// </remarks>
    public static class RunRandom
    {
        /// <summary>Kênh sinh quái: số lượng, chủng loại, vị trí xuất hiện.</summary>
        public static RandomStream Enemies { get; private set; }

        /// <summary>Kênh bể thẻ: bốc 3 thẻ, đổi thẻ.</summary>
        public static RandomStream Cards { get; private set; }

        /// <summary>Kênh vật phẩm rơi ra: Hồn và các vật phẩm khác.</summary>
        public static RandomStream Loot { get; private set; }

        /// <summary>Kênh AI Đạo Diễn: thăm dò trong thuật toán LinUCB.</summary>
        public static RandomStream Director { get; private set; }

        /// <summary>Seed của ván hiện tại. Host sinh ra, client nhận qua đồng bộ.</summary>
        public static int Seed { get; private set; }

        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Khởi tạo toàn bộ kênh cho một ván. Host gọi khi bắt đầu ván, client gọi lại
        /// với đúng seed nhận được từ host trước khi đợt đầu tiên khởi động.
        /// </summary>
        public static void Initialize(int seed)
        {
            Seed = seed;
            Enemies = new RandomStream(seed, "enemies");
            Cards = new RandomStream(seed, "cards");
            Loot = new RandomStream(seed, "loot");
            Director = new RandomStream(seed, "director");
            IsInitialized = true;
        }

        /// <summary>
        /// Sinh một seed mới cho ván. Chỉ host được gọi.
        /// </summary>
        public static int CreateSeed()
        {
            // Đây là điểm duy nhất trong dự án được phép lấy giá trị ngẫu nhiên ngoài hệ
            // thống, vì thời điểm này chưa tồn tại seed nào và giá trị sẽ được đồng bộ ngay.
            return System.Environment.TickCount ^ (int)(Time.realtimeSinceStartupAsDouble * 1000.0);
        }

        /// <summary>
        /// Xoá trạng thái khi kết thúc ván, để một lời gọi nhầm ở ván sau bị phát hiện ngay
        /// thay vì âm thầm dùng lại seed cũ.
        /// </summary>
        public static void Reset()
        {
            Enemies = null;
            Cards = null;
            Loot = null;
            Director = null;
            Seed = 0;
            IsInitialized = false;
        }
    }
}
