using System;
using UnityEngine;

namespace LAC.Utils
{
    /// <summary>Năm nhóm màu gốc của tranh Đông Hồ, cộng nhóm nâu pha.</summary>
    public enum PigmentFamily
    {
        /// <summary>Vỏ điệp nghiền — nền giấy.</summary>
        Diep = 0,
        /// <summary>Than lá tre — nét và bóng.</summary>
        Than = 1,
        /// <summary>Hoa hoè — vàng.</summary>
        Hoe = 2,
        /// <summary>Lá chàm và gỉ đồng — lam lục.</summary>
        Cham = 3,
        /// <summary>Đất pha — nâu.</summary>
        Nau = 4,
        /// <summary>Sỏi son — đỏ. Dành riêng cho đòn tấn công của địch.</summary>
        Son = 5
    }

    [Serializable]
    public sealed class PaletteEntry
    {
        [SerializeField] private string _name = "";
        [SerializeField] private PigmentFamily _family = PigmentFamily.Diep;
        [SerializeField] private Color _color = Color.white;

        public string Name => _name;
        public PigmentFamily Family => _family;
        public Color Color => _color;
    }

    /// <summary>
    /// Bảng 24 màu Đông Hồ đã chốt cho LẠC.
    /// </summary>
    /// <remarks>
    /// <b>Vì sao bảng màu là tài sản chứ không phải hằng số trong mã.</b> Ràng buộc đọc hiểu
    /// ở CLAUDE.md mục 2.1 không phải là một lời khuyên thẩm mỹ mà là một quy tắc kiểm tra
    /// được: một màu dành riêng cho đòn địch, và không thứ gì khác được dùng màu đó. Quy tắc
    /// chỉ kiểm tra được nếu bảng màu tồn tại dưới dạng dữ liệu mà mã đọc được.
    ///
    /// <b>Vì sao dành riêng cả nhóm son chứ không chỉ một màu.</b> Đòn địch cũng cần sáng tối
    /// để đọc ra hình khối, nên một màu đơn lẻ là không đủ. Đổi lại, nhân vật và hiệu ứng
    /// người chơi mất quyền dùng đỏ — vẫn còn 20 màu, thừa cho pixel art ở mật độ này.
    /// </remarks>
    [CreateAssetMenu(fileName = "PaletteData", menuName = "LAC/Palette Data")]
    public sealed class PaletteData : ScriptableObject
    {
        [SerializeField] private PaletteEntry[] _entries = new PaletteEntry[0];

        [Tooltip("Màu chuẩn của đòn tấn công địch. Phải thuộc nhóm Son.")]
        [SerializeField] private Color _enemyAttack = new Color(0.886f, 0.231f, 0.149f);

        public int Count => _entries.Length;
        public PaletteEntry this[int index] => _entries[index];

        /// <summary>Màu dành riêng cho đòn tấn công của địch — mục 2.1.</summary>
        public Color EnemyAttack => _enemyAttack;

        /// <summary>Màu theo tên, hoặc magenta nếu không có — sai màu thì phải nhìn thấy ngay.</summary>
        public Color Get(string entryName)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i] != null && _entries[i].Name == entryName) return _entries[i].Color;

            Debug.LogError($"[PaletteData] Không có màu '{entryName}' trong bảng.", this);
            return Color.magenta;
        }

        /// <summary>
        /// Màu này có thuộc nhóm dành riêng cho địch hay không.
        /// </summary>
        /// <remarks>
        /// Dùng để tự kiểm: mọi hiệu ứng của người chơi phải trả về false. So sánh có dung
        /// sai vì màu đi qua Inspector và qua chuyển đổi sRGB thì không còn khớp từng bit.
        /// </remarks>
        public bool IsReservedForEnemy(Color color, float tolerance = 0.02f)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                PaletteEntry e = _entries[i];
                if (e == null || e.Family != PigmentFamily.Son) continue;

                if (Mathf.Abs(e.Color.r - color.r) < tolerance &&
                    Mathf.Abs(e.Color.g - color.g) < tolerance &&
                    Mathf.Abs(e.Color.b - color.b) < tolerance) return true;
            }

            return false;
        }
    }
}
