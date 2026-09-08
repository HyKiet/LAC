using System.Collections;
using System.Net;
using System.Net.Sockets;
using LAC.Net;
using LAC.Player;
using LAC.VFX;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LAC.Menu
{
    /// <summary>Sảnh được dựng từ prefab; hai hình nhân vật chỉ là biểu diễn cục bộ.</summary>
    public sealed class LobbyView : MonoBehaviour
    {
        [SerializeField] private CharacterRegistry _characters;
        [SerializeField] private Font _font;
        private NetworkManagerLAC _manager;
        private readonly Image[] _portraits = new Image[2];
        private readonly Text[] _names = new Text[2];
        private readonly Text[] _roles = new Text[2];
        private readonly Text[] _weapons = new Text[2];
        private readonly SpriteAnimationClip[] _clips = new SpriteAnimationClip[2];
        private readonly Button[] _choose = new Button[2];
        private Button _start;
        private Button _invite;
        private Button _leave;
        private Button _ready;
        private Button _host;
        private Button _join;
        private Text _status;
        private Text _room;
        private Text _inviteStatus;
        private Text _readyText;
        private GameObject _dialog;
        private InputField _address;
        private int _previewIndex;
        private bool _starting;
        private static readonly Color Ink = ColorHex("15130F");
        private static readonly Color Paper = ColorHex("F4EADA");
        private static readonly Color Muted = ColorHex("BFA981");
        private static readonly Color Gold = ColorHex("EDBB3E");
        private static readonly Color Jade = ColorHex("9CCFC0");

        private void Start()
        {
            _manager = NetworkManager.singleton as NetworkManagerLAC;
            if (_manager == null)
            {
                Debug.LogError("[LobbyView] Thiếu NetworkManagerLAC trong sảnh.");
                return;
            }
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            _manager.LobbyChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_manager != null) _manager.LobbyChanged -= Refresh;
        }

        private void Build()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            if (EventSystem.current == null)
                new GameObject("LobbyInput", typeof(EventSystem), typeof(InputSystemUIInputModule));

            RectTransform background = Rect("Courtyard", transform, Vector2.zero, Vector2.zero);
            Stretch(background);
            background.gameObject.AddComponent<LobbyBackdrop>().raycastTarget = false;

            RectTransform content = Rect("Content", transform, Vector2.zero, new Vector2(1280, 720));
            Label("LẠC", content, new Vector2(-548, 284), new Vector2(180, 80), 68, Gold, TextAnchor.MiddleLeft);
            Label("CON RỒNG CHÁU TIÊN", content, new Vector2(-453, 231), new Vector2(366, 26), 16, Muted, TextAnchor.MiddleLeft);
            Label("SÂN ĐÌNH", content, new Vector2(0, 274), new Vector2(400, 46), 30, Paper);
            Label("HẸN NHAU TRƯỚC CHUYẾN ĐI", content, new Vector2(0, 233), new Vector2(480, 25), 13, Muted);
            _room = Label("SẢNH CHỜ  /  1–2 NGƯỜI", content, new Vector2(461, 286), new Vector2(286, 28), 15, Jade, TextAnchor.MiddleRight);

            for (int i = 0; i < 2; i++)
            {
                int slot = i;
                float x = i == 0 ? -190 : 190;
                RectTransform pedestal = Rect("Player" + (i + 1), content, new Vector2(x, -10), new Vector2(300, 360));
                _roles[i] = Label("", pedestal, new Vector2(0, 136), new Vector2(300, 30), 15, i == 0 ? Gold : Jade);
                _portraits[i] = Rect("Pose", pedestal, new Vector2(0, 4), new Vector2(175, 175)).gameObject.AddComponent<LobbyPoseImage>();
                _portraits[i].preserveAspect = true;
                _portraits[i].raycastTarget = false;
                _names[i] = Label("", pedestal, new Vector2(0, -116), new Vector2(260, 38), 28, Paper);
                _weapons[i] = Label("", pedestal, new Vector2(0, -149), new Vector2(280, 28), 16, Muted);
                _choose[i] = Button("Chọn nhân vật", pedestal, new Vector2(0, -193), new Vector2(185, 35), () => Cycle(slot), false);
            }

            _ready = Button("Sẵn sàng", content, new Vector2(190, -252), new Vector2(185, 36), ToggleReady, false);
            _readyText = _ready.GetComponentInChildren<Text>();
            _leave = Button("Rời phòng", content, new Vector2(-548, -297), new Vector2(158, 50), () => _manager.LeaveSession(), false);
            _invite = Button("MỜI", content, new Vector2(273, -297), new Vector2(184, 62), () => { _dialog.SetActive(true); Refresh(); }, false);
            _start = Button("BẮT ĐẦU   →", content, new Vector2(488, -297), new Vector2(220, 62), StartJourney, true);
            _status = Label("", content, new Vector2(-148, -297), new Vector2(585, 56), 15, Muted, TextAnchor.MiddleLeft);
            BuildInvite(content);
        }

        private void BuildInvite(Transform parent)
        {
            Image shade = Panel("InviteOverlay", parent, Vector2.zero, new Vector2(1280, 720), new Color(0, 0, 0, .82f));
            _dialog = shade.gameObject;
            Image card = Panel("InvitePanel", shade.transform, Vector2.zero, new Vector2(670, 470), ColorHex("2B2724"));
            Label("MỜI BẠN CÙNG CHƠI", card.transform, new Vector2(0, 181), new Vector2(610, 40), 30, Gold);
            Label("Hai máy dùng cùng bản game và cùng mạng LAN.", card.transform, new Vector2(0, 130), new Vector2(590, 32), 17, Paper);
            _inviteStatus = Label("", card.transform, new Vector2(0, 72), new Vector2(590, 62), 17, Jade);
            _host = Button("Tạo phòng", card.transform, new Vector2(-143, 10), new Vector2(254, 48), () => _manager.HostLobby(), true);
            Button("Sao chép địa chỉ", card.transform, new Vector2(143, 10), new Vector2(254, 48), CopyAddress, false);
            Image input = Panel("JoinAddress", card.transform, new Vector2(-73, -67), new Vector2(394, 48), Ink);
            _address = input.gameObject.AddComponent<InputField>();
            Text value = Label("", input.transform, Vector2.zero, new Vector2(362, 44), 18, Paper, TextAnchor.MiddleLeft);
            Text placeholder = Label("Nhập IP của người mời…", input.transform, Vector2.zero, new Vector2(362, 44), 17, Muted, TextAnchor.MiddleLeft);
            _address.textComponent = value;
            _address.placeholder = placeholder;
            _address.characterLimit = 253;
            _address.lineType = InputField.LineType.SingleLine;
            _join = Button("Tham gia", card.transform, new Vector2(207, -67), new Vector2(127, 48), () => _manager.JoinLobby(_address.text), true);
            Label("Mời qua Steam sẽ được bổ sung sau.\nĐịa chỉ LAN không dùng để kết nối qua Internet.", card.transform, new Vector2(0, -139), new Vector2(590, 52), 15, Muted);
            Button("Đóng", card.transform, new Vector2(0, -198), new Vector2(150, 35), () => _dialog.SetActive(false), false);
            _dialog.SetActive(false);
        }

        private void Refresh()
        {
            if (_status == null) return;
            bool active = NetworkClient.active;
            bool host = NetworkServer.active;
            int count = _manager.Members.Count;
            _room.text = active ? $"SẢNH CHỜ  /  {count} TRÊN 2 NGƯỜI" : "SẢNH CHỜ  /  1–2 NGƯỜI";
            _status.text = _manager.IsLoadingArena ? "Đang vào đấu trường…" : _manager.SessionStatus;
            _invite.interactable = !_manager.IsLoadingArena;
            _start.interactable = !_starting && (!active || _manager.CanStart);
            _leave.gameObject.SetActive(active);
            _host.interactable = !active;
            _join.interactable = !active;
            _address.interactable = !active;
            _inviteStatus.text = host ? "Phòng đã mở • UDP 7777\nĐịa chỉ máy chủ: " + LocalAddress() : _manager.SessionStatus;
            _ready.gameObject.SetActive(active && !host && count > 0);
            _ready.interactable = !_manager.IsLoadingArena;

            for (int i = 0; i < 2; i++)
            {
                bool occupied = i < count;
                LobbyMember member = occupied ? _manager.Members[i] : default;
                CharacterData data = occupied ? _characters.Get(member.Character) : _characters.GetByIndex(i == 0 ? _previewIndex : 1);
                bool local = occupied && member.ConnectionId == _manager.LocalConnectionId;
                _names[i].text = occupied || i == 0 ? data.DisplayName : "Đợi người bạn";
                _weapons[i].text = occupied || i == 0 ? data.WeaponName : "Một chỗ dành cho đồng đội";
                _roles[i].text = occupied ? (member.Host ? "CHỦ PHÒNG" : member.Ready ? "ĐÃ SẴN SÀNG" : "ĐANG CHỌN") + (local ? "  ·  BẠN" : "") : i == 0 ? "BẠN" : "CHƯA THAM GIA";
                _portraits[i].color = occupied || i == 0 ? Color.white : new Color(.61f, .81f, .75f, .27f);
                _portraits[i].sprite = data.BodySprite;
                _clips[i] = data.AnimationSet != null ? data.AnimationSet.Get(AnimState.Idle) : null;
                _choose[i].gameObject.SetActive(local || (!active && i == 0));
                _choose[i].interactable = !_manager.IsLoadingArena;
                if (local) _readyText.text = member.Ready ? "Huỷ sẵn sàng" : "Sẵn sàng";
            }
        }

        private void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                SpriteAnimationClip clip = _clips[i];
                if (clip != null && clip.IsValid && _portraits[i] != null)
                    _portraits[i].sprite = clip.Frames[(int)(Time.unscaledTime * clip.Fps) % clip.FrameCount];
            }
        }

        private void Cycle(int slot)
        {
            if (!NetworkClient.active)
            {
                _previewIndex = (_previewIndex + 1) % _characters.Count;
                Refresh();
                return;
            }
            if (slot >= _manager.Members.Count) return;
            LobbyMember member = _manager.Members[slot];
            if (member.ConnectionId != _manager.LocalConnectionId) return;
            for (int i = 0; i < _characters.Count; i++)
                if (_characters.GetByIndex(i).Id == member.Character)
                {
                    _manager.ChooseCharacter(_characters.GetByIndex((i + 1) % _characters.Count).Id, false);
                    return;
                }
        }

        private void ToggleReady()
        {
            foreach (LobbyMember member in _manager.Members)
                if (member.ConnectionId == _manager.LocalConnectionId)
                    _manager.ChooseCharacter(member.Character, !member.Ready);
        }

        private void StartJourney()
        {
            if (_starting) return;
            if (NetworkClient.active) { _manager.BeginArena(); return; }
            StartCoroutine(HostAndStart());
        }

        private IEnumerator HostAndStart()
        {
            _starting = true;
            _manager.HostLobby();
            float deadline = Time.unscaledTime + 5f;
            while (!_manager.CanStart && Time.unscaledTime < deadline) yield return null;
            if (_manager.CanStart)
            {
                _manager.ChooseCharacter(_characters.GetByIndex(_previewIndex).Id, true);
                // Yêu cầu chọn nhân vật của local client phải được host xử lý trước Start.
                CharacterId chosen = _characters.GetByIndex(_previewIndex).Id;
                while (Time.unscaledTime < deadline)
                {
                    if (_manager.Members.Count > 0 && _manager.Members[0].Character == chosen) break;
                    yield return null;
                }
                if (_manager.Members.Count > 0 && _manager.Members[0].Character == chosen) _manager.BeginArena();
            }
            _starting = false;
            Refresh();
        }

        private void CopyAddress()
        {
            string address = LocalAddress();
            GUIUtility.systemCopyBuffer = address;
            _inviteStatus.text = "Đã sao chép: " + address + "\nGửi địa chỉ này cho bạn cùng mạng LAN.";
        }

        private static string LocalAddress()
        {
            try
            {
                foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                        return address.ToString();
            }
            catch (SocketException) { }
            return "127.0.0.1";
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Text label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = fontSize;
            label.text = text;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private Button Button(string text, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool primary)
        {
            Image image = Panel(text, parent, position, size, primary ? Gold : ColorHex("47403A"));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(.75f, .75f, .75f);
            colors.disabledColor = new Color(.45f, .45f, .45f, .65f);
            button.colors = colors;
            Label(text, image.transform, Vector2.zero, size - new Vector2(12, 4), primary ? 21 : 18, primary ? Ink : Paper);
            button.onClick.AddListener(action);
            return button;
        }

        internal static Color ColorHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
