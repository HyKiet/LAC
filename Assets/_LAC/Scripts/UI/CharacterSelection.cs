using LAC.Core;
using LAC.Director;
using LAC.Net;
using LAC.Player;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LAC.UI
{
    // Sảnh thử nghiệm đặt ngay trong Arena; lựa chọn được gửi trước khi sinh player.
    public sealed class CharacterSelection : MonoBehaviour
    {
        [SerializeField] private CharacterRegistry _characters;
        private NetworkManagerLAC _manager;
        private RunManager _run;
        private GameObject _overlay;
        private UnityEngine.UI.Text _status;
        private UnityEngine.UI.InputField _address;
        private UnityEngine.UI.Button _host, _join, _cancel;
        private readonly UnityEngine.UI.Button[] _choices = new UnityEngine.UI.Button[3];
        private readonly UnityEngine.UI.Text[] _choiceLabels = new UnityEngine.UI.Text[3];
        private readonly UnityEngine.UI.Button[] _directorChoices = new UnityEngine.UI.Button[2];
        private readonly TextMeshProUGUI[] _directorLabels = new TextMeshProUGUI[2];
        private TextMeshProUGUI _directorDescription;
        private TMP_FontAsset _directorFont;
        private DirectorMode _displayedDirectorMode;
        private Font _font;
        private int _selected;
        private bool _wasActive;
        private const string DefaultStatus = "Chọn lớp nhân vật rồi bắt đầu. Cả hai người có thể chọn cùng một lớp.";

        public CharacterId Selected => _characters.GetByIndex(_selected).Id;
        public bool IsVisible => _overlay != null && _overlay.activeSelf;
        public DirectorMode SelectedDirectorMode => _run != null ? _run.ConfiguredDirectorMode : DirectorMode.Fixed;

        private void Start()
        {
            _manager = NetworkManager.singleton as NetworkManagerLAC;
            if (_manager == null) _manager = FindAnyObjectByType<NetworkManagerLAC>();
            _run = RunManager.Instance;
            if (_characters == null || _characters.Count != 3 || _manager == null || _run == null)
            {
                Debug.LogError("[CharacterSelection] Thiếu manager, RunManager hoặc bộ ba nhân vật.");
                enabled = false;
                return;
            }
            Build();
            if (_directorFont == null) return;
            Select(0);
            RefreshDirectorSelection();
        }

        private void OnDestroy() => DirectorHud.ReleaseFontAsset(_directorFont);

        public void SelectDirectorMode(DirectorMode mode)
        {
            if (_run == null || !_run.TrySetDirectorMode(mode)) return;
            RefreshDirectorSelection();
        }

        private void RefreshDirectorSelection()
        {
            _displayedDirectorMode = _run.ConfiguredDirectorMode;
            for (int i = 0; i < _directorChoices.Length; i++)
            {
                bool selected = (DirectorMode)i == _displayedDirectorMode;
                _directorChoices[i].targetGraphic.color = Hex(selected ? "1C4B5C" : "2B2724");
                _directorLabels[i].color = Hex(selected ? "FBDD82" : "F4EADA");
                _directorLabels[i].text = (i == 0 ? "CỐ ĐỊNH" : "THÍCH ỨNG") + (selected ? " · ĐÃ CHỌN" : "");
            }
            var persona = Resources.Load<DungeonMasterData>("Director/DungeonMaster");
            _directorDescription.text = persona != null ? persona.ModeDescription(_displayedDirectorMode) : "";
        }

        public void Select(int index)
        {
            if (NetworkClient.active || NetworkServer.active || index < 0 || index >= _characters.Count) return;
            _selected = index;
            _manager.SelectedCharacter = Selected;
            for (int i = 0; i < _choices.Length; i++)
            {
                _choices[i].GetComponent<UnityEngine.UI.Image>().color = Hex(i == index ? "1C4B5C" : "2B2724");
                _choiceLabels[i].text = i == index ? "ĐÃ CHỌN" : "CHỌN LỚP";
            }
        }

        public void BeginHost()
        {
            if (NetworkClient.active || NetworkServer.active) return;
            _manager.SelectedCharacter = Selected;
            _status.text = "Đang tạo trận…";
            _manager.StartHost();
        }

        public void BeginJoin()
        {
            if (NetworkClient.active || NetworkServer.active) return;
            string address = _address.text.Trim();
            if (string.IsNullOrEmpty(address)) { _status.text = "Nhập địa chỉ host trước khi kết nối."; return; }
            _manager.SelectedCharacter = Selected;
            _manager.networkAddress = address;
            _status.text = "Đang kết nối tới " + address + "…";
            _manager.StartClient();
        }

        private void Update()
        {
            if (_overlay == null) return;
            bool active = NetworkClient.active || NetworkServer.active;
            bool inGame = NetworkClient.localPlayer != null;
            _overlay.SetActive(!inGame);
            if (!inGame)
            {
                foreach (var choice in _choices) choice.interactable = !active;
                foreach (var choice in _directorChoices) choice.interactable = !active;
                _host.interactable = _join.interactable = _address.interactable = !active;
                _cancel.gameObject.SetActive(active);
                if (_wasActive && !active) _status.text = "Kết nối đã đóng. Kiểm tra địa chỉ host rồi thử lại.";
                if (_run != null && _displayedDirectorMode != _run.ConfiguredDirectorMode) RefreshDirectorSelection();
            }
            _wasActive = active;
        }

        private void Cancel()
        {
            if (NetworkServer.active) _manager.StopHost();
            else _manager.StopClient();
        }

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _directorFont = DirectorHud.CreateFontAsset(_font);
            if (_directorFont == null) { enabled = false; return; }
            if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("CharacterCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 400;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1f;
            var shade = Image("Selection", canvas.transform, Hex("15130F"));
            _overlay = shade.gameObject;
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            Text("Title", _overlay.transform, "CHỌN NHÂN VẬT", 38, new Vector2(0, 330), new Vector2(1200, 65), Hex("FBDD82"));
            Text("Subtitle", _overlay.transform, "10 ĐỢT  •  SINH TỒN TÍNH GIỜ  •  CO-OP 2 NGƯỜI", 20, new Vector2(0, 272), new Vector2(1200, 40), Hex("9CCFC0"));
            for (int i = 0; i < 3; i++) BuildCard(i);
            BuildDirectorChoice();
            _status = Text("Status", _overlay.transform, DefaultStatus, 19, new Vector2(0, -308), new Vector2(1270, 34), Hex("E0CFAF"));
            _host = Button("StartHost", _overlay.transform, "VÀO TRẬN / TẠO HOST", new Vector2(-345, -364), new Vector2(360, 52), BeginHost);
            var input = Image("HostAddress", _overlay.transform, Hex("2B2724"));
            Rect(input.rectTransform, new Vector2(35, -364), new Vector2(310, 52));
            _address = input.gameObject.AddComponent<UnityEngine.UI.InputField>();
            var value = Text("Address", input.transform, "", 22, Vector2.zero, new Vector2(280, 50), Hex("F4EADA"));
            value.alignment = TextAnchor.MiddleLeft;
            _address.textComponent = value;
            _address.text = "localhost";
            _address.characterLimit = 128;
            _join = Button("JoinHost", _overlay.transform, "VÀO HOST", new Vector2(365, -364), new Vector2(270, 52), BeginJoin);
            _cancel = Button("Cancel", _overlay.transform, "HUỶ KẾT NỐI", new Vector2(0, -408), new Vector2(270, 28), Cancel);
            _cancel.gameObject.SetActive(false);
            Text("Controls", _overlay.transform, "WASD: di chuyển   •   Space: lướt   •   Co-op: nhập IP của máy host", 18,
                new Vector2(0, -430), new Vector2(1240, 30), Hex("BFA981"));
        }

        private void BuildDirectorChoice()
        {
            DirectorText("ModeTitle", "NHỊP TRẬN", 20, new Vector2(-420, -221), new Vector2(250, 38), Hex("9CCFC0"));
            DirectorText("ModeHelp", "Host chọn trước trận", 18, new Vector2(425, -221), new Vector2(270, 38), Hex("BFA981"));
            for (int i = 0; i < _directorChoices.Length; i++)
            {
                int index = i;
                var image = Image(i == 0 ? "FixedMode" : "AdaptiveMode", _overlay.transform, Hex("2B2724"));
                Rect(image.rectTransform, new Vector2(i == 0 ? -136 : 142, -221), new Vector2(258, 38));
                var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SelectDirectorMode((DirectorMode)index));
                var colors = button.colors;
                colors.highlightedColor = Hex("9CCFC0");
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(.35f, .35f, .35f, .7f);
                button.colors = colors;
                _directorChoices[i] = button;
                _directorLabels[i] = DirectorText(i == 0 ? "FixedLabel" : "AdaptiveLabel", "", 18,
                    Vector2.zero, new Vector2(250, 36), Hex("F4EADA"), image.transform);
            }
            _directorDescription = DirectorText("ModeDescription", "", 19, new Vector2(0, -267),
                new Vector2(1260, 36), Hex("E0CFAF"));
        }

        private TextMeshProUGUI DirectorText(string name, string value, int fontSize, Vector2 position,
            Vector2 bounds, Color color, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent != null ? parent : _overlay.transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = _directorFont;
            text.fontSharedMaterial = _directorFont.material;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            Rect(text.rectTransform, position, bounds);
            go.SetActive(true);
            return text;
        }

        private void BuildCard(int index)
        {
            CharacterData data = _characters.GetByIndex(index);
            var panel = Image("Class_" + data.DisplayName, _overlay.transform, Hex("2B2724"));
            Rect(panel.rectTransform, new Vector2((index - 1) * 380, 35), new Vector2(345, 430));
            Text("Name", panel.transform, data.DisplayName.ToUpperInvariant(), 30, new Vector2(0, 163), new Vector2(300, 50), Hex("FBDD82"));
            var portrait = Image("Portrait", panel.transform, Color.white);
            portrait.sprite = data.Portrait != null ? data.Portrait : data.BodySprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            // Lancer có frame 320px, hai lớp khác 192px; giữ cùng cỡ thân thay vì cỡ sheet.
            float portraitSize = 190 * data.BodySprite.rect.width / 192f;
            Rect(portrait.rectTransform, new Vector2(0, 69), Vector2.one * portraitSize);
            Text("Weapon", panel.transform, data.WeaponName, 23, new Vector2(0, -12), new Vector2(310, 40), Hex("9CCFC0"));
            Text("Stats", panel.transform, $"Máu {data.MaxHealth}  •  Tốc độ {data.MoveSpeed:0.#}\nSát thương {data.BaseDamage}  •  Tầm {data.AttackRange:0.#}\nMỗi {data.AttackInterval:0.##} giây", 18,
                new Vector2(0, -73), new Vector2(310, 85), Hex("E0CFAF"));
            Text("Description", panel.transform, data.Description, 17, new Vector2(0, -134), new Vector2(310, 55), Hex("BFA981"));
            _choices[index] = Button("Choose", panel.transform, "CHỌN LỚP", new Vector2(0, -187), new Vector2(280, 40), () => Select(index));
            _choiceLabels[index] = _choices[index].GetComponentInChildren<UnityEngine.UI.Text>();
        }

        private UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var image = Image(name, parent, Hex("2F7480"));
            Rect(image.rectTransform, position, size);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = Hex("9CCFC0");
            colors.selectedColor = Hex("9CCFC0");
            colors.disabledColor = new Color(.35f, .35f, .35f, .7f);
            button.colors = colors;
            Text("Label", image.transform, label, 19, Vector2.zero, size, Hex("F4EADA"));
            return button;
        }

        private UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color;
            return image;
        }

        private UnityEngine.UI.Text Text(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = _font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Rect(text.rectTransform, position, bounds);
            return text;
        }

        private static void Rect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static Color Hex(string value, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            color.a = alpha; return color;
        }
    }
}
