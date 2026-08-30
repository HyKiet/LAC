using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Bảng tra cứu từ định danh nhân vật sang tài sản chỉ số.
    /// </summary>
    /// <remarks>
    /// Mạng chỉ truyền định danh nhân vật, không truyền chỉ số — xem CLAUDE.md mục 3.2.
    /// Máy nhận cần một điểm tra cứu để dựng lại <see cref="CharacterData"/> từ định danh
    /// đó. Dùng tài sản này thay vì thư mục Resources vì tham chiếu trực tiếp được kiểm
    /// tra lúc biên dịch tài sản, còn Resources chỉ báo lỗi khi chạy.
    /// </remarks>
    [CreateAssetMenu(fileName = "CharacterRegistry", menuName = "LAC/Character Registry")]
    public sealed class CharacterRegistry : ScriptableObject
    {
        [SerializeField] private CharacterData[] _characters = new CharacterData[0];

        public int Count => _characters.Length;

        /// <summary>Trả về chỉ số của nhân vật, hoặc null nếu bảng chưa có định danh này.</summary>
        public CharacterData Get(CharacterId id)
        {
            for (int i = 0; i < _characters.Length; i++)
            {
                if (_characters[i] != null && _characters[i].Id == id)
                    return _characters[i];
            }

            Debug.LogError($"[CharacterRegistry] Thiếu nhân vật '{id}' trong bảng tra cứu.", this);
            return null;
        }

        /// <summary>Nhân vật theo thứ tự khai báo. Dùng để phân nhân vật cho người vào ván.</summary>
        public CharacterData GetByIndex(int index)
        {
            if (_characters.Length == 0) return null;
            return _characters[Mathf.Clamp(index, 0, _characters.Length - 1)];
        }
    }
}
