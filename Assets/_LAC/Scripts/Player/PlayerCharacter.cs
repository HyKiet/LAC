using Mirror;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>
    /// Danh tính của một người chơi trong ván: nhân vật nào, và chỉ số đi kèm.
    /// </summary>
    /// <remarks>
    /// Chỉ có một prefab người chơi duy nhất cho cả ba nhân vật. Thứ được truyền qua mạng
    /// là <see cref="CharacterId"/> — một số nguyên — còn chỉ số thì mỗi máy tự tra từ
    /// <see cref="CharacterRegistry"/> và tự áp dụng. Đây là mẫu "đồng bộ định danh, không
    /// đồng bộ trạng thái" ở CLAUDE.md mục 3.2.
    ///
    /// Nếu làm ngược lại — ba prefab riêng, hoặc đồng bộ từng chỉ số — thì mỗi lần cân bằng
    /// lại một con số là một lần phải kiểm tra đường truyền. Cách này thì chỉnh
    /// ScriptableObject là xong, vì hai máy đọc cùng một tài sản.
    /// </remarks>
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class PlayerCharacter : NetworkBehaviour
    {
        [SerializeField] private CharacterRegistry _registry;
        [SerializeField] private SpriteRenderer _renderer;

        [SyncVar(hook = nameof(OnCharacterIdChanged))]
        private CharacterId _characterId = CharacterId.ThachSanh;

        private CharacterData _data;

        /// <summary>Chỉ số của nhân vật này. Null cho tới khi định danh được áp dụng.</summary>
        public CharacterData Data => _data;

        public CharacterId CharacterId => _characterId;

        private void OnEnable() => PlayerRegistry.Register(this);

        private void OnDisable() => PlayerRegistry.Unregister(this);

        /// <summary>Host chọn nhân vật. Gọi trước khi đối tượng được sinh ra trên mạng.</summary>
        [Server]
        public void SetCharacter(CharacterId id)
        {
            _characterId = id;
            Apply();
        }

        public override void OnStartClient() => Apply();

        private void OnCharacterIdChanged(CharacterId _, CharacterId newId) => Apply();

        /// <summary>
        /// Dựng lại chỉ số từ định danh. Gọi lại nhiều lần không gây tác dụng phụ.
        /// </summary>
        /// <remarks>
        /// Có ba đường dẫn tới đây — host gán trực tiếp, client nhận trạng thái ban đầu,
        /// client nhận thay đổi qua hook — và thứ tự giữa chúng khác nhau tuỳ máy. Nên hàm
        /// này phải bình thường hoá được mọi thứ tự thay vì giả định một đường dẫn duy nhất.
        /// </remarks>
        private void Apply()
        {
            if (_registry == null)
            {
                Debug.LogError("[PlayerCharacter] Chưa gán bảng tra cứu nhân vật.", this);
                return;
            }

            CharacterData data = _registry.Get(_characterId);
            if (data == null || data == _data) return;

            _data = data;
            gameObject.name = $"Player_{data.DisplayName}";

            if (_renderer != null && data.BodySprite != null)
                _renderer.sprite = data.BodySprite;
        }
    }
}
