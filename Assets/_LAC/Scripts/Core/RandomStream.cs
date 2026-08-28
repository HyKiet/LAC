using System.Collections.Generic;

namespace LAC.Core
{
    /// <summary>
    /// Một luồng số ngẫu nhiên độc lập, tất định theo seed.
    /// </summary>
    /// <remarks>
    /// Không dùng <c>System.Random</c> vì thuật toán nội bộ của nó đã thay đổi giữa các
    /// phiên bản .NET và không được bảo đảm giống nhau trên mọi nền tảng. Trong mô hình
    /// host và client cùng mô phỏng, một khác biệt duy nhất ở bit thấp nhất cũng đủ làm
    /// hai máy sinh ra hai đợt quái khác nhau. Xorshift128 có đặc tả cố định nên cùng seed
    /// luôn cho cùng dãy số.
    /// </remarks>
    public sealed class RandomStream
    {
        private uint _x, _y, _z, _w;

        public RandomStream(int seed, string channel)
        {
            // Mỗi kênh được gieo bằng một giá trị dẫn xuất khác nhau để hai kênh
            // không bao giờ trả về cùng một dãy số dù xuất phát từ cùng seed ván đấu.
            uint s = Mix((uint)seed ^ HashChannel(channel));
            _x = s == 0u ? 0x9E3779B9u : s;
            _y = Mix(_x);
            _z = Mix(_y);
            _w = Mix(_z);
        }

        /// <summary>Số nguyên không dấu kế tiếp — hạt nhân của toàn bộ luồng.</summary>
        public uint NextUInt()
        {
            uint t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
            return _w;
        }

        /// <summary>Số thực trong nửa khoảng [0, 1).</summary>
        public float NextFloat()
        {
            // Lấy 24 bit cao vì float chỉ biểu diễn chính xác được 24 bit phần định trị.
            return (NextUInt() >> 8) * (1.0f / 16777216.0f);
        }

        /// <summary>Số nguyên trong nửa khoảng [min, maxExclusive).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            uint span = (uint)(maxExclusive - min);
            return min + (int)(NextUInt() % span);
        }

        /// <summary>Số thực trong nửa khoảng [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Trả về true với xác suất <paramref name="probability"/> trong khoảng [0, 1].</summary>
        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>Chọn ngẫu nhiên một phần tử.</summary>
        public T Pick<T>(IReadOnlyList<T> source)
        {
            return source[Range(0, source.Count)];
        }

        /// <summary>Xáo trộn tại chỗ theo thuật toán Fisher-Yates.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static uint Mix(uint v)
        {
            // SplitMix32 — trải đều các bit của seed đầu vào.
            v += 0x9E3779B9u;
            v = (v ^ (v >> 16)) * 0x21F0AAADu;
            v = (v ^ (v >> 15)) * 0x735A2D97u;
            return v ^ (v >> 15);
        }

        private static uint HashChannel(string channel)
        {
            // FNV-1a 32 bit.
            uint hash = 2166136261u;
            for (int i = 0; i < channel.Length; i++)
            {
                hash ^= channel[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
