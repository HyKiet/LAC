using LAC.Player;
using Mirror;

namespace LAC.Net
{
    // DTO trên dây cần trường public để Mirror Weaver sinh bộ tuần tự hoá.
    // Chỉ truyền định danh và sự kiện sảnh, không truyền chỉ số gameplay.
    public struct LobbyMember
    {
        public int ConnectionId;
        public CharacterId Character;
        public bool Ready;
        public bool Host;
    }

    public struct LobbyStateMessage : NetworkMessage
    {
        public LobbyMember[] Members;
        public int LocalConnectionId;
        public bool Loading;
    }

    public struct LobbyChoiceMessage : NetworkMessage
    {
        public CharacterId Character;
        public bool Ready;
    }
}
