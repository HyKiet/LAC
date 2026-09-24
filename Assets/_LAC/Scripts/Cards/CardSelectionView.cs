using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LAC.Cards
{
    public sealed class CardSelectionView : MonoBehaviour
    {
        private static EventSystem _fallbackEventSystem;

        private sealed class Slot
        {
            public GameObject Root;
            public Button Button;
            public Image Icon;
            public Text Placeholder;
            public Text Name;
            public Text Description;
            public Text Stacks;
            public CardHoverVisual Hover;
            public CardDefinition Card;
        }

        private readonly Slot[] _slots = new Slot[3];
        private Font _font;
        private GameObject _overlay;
        private Text _rerollText;
        private Button _rerollButton;
        private Text _ownedText;
        private Text _statusText;
        private bool _built;

        private void Awake()
        {
            if (Application.isPlaying) EnsureBuilt();
        }

        public void Show(IReadOnlyList<CardDefinition> cards, PlayerUpgradeState state,
            int rerollsRemaining, Action<CardDefinition> onPick, Action onReroll)
        {
            EnsureBuilt();
            _overlay.SetActive(true);
            SetRect(_statusText.rectTransform, BottomCenter, BottomCenter,
                new Vector2(-325f, 52f), new Vector2(380f, 48f), Center);

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                bool active = i < cards.Count;
                slot.Root.SetActive(active);
                if (!active) continue;

                CardDefinition card = cards[i];
                slot.Card = card;
                slot.Name.text = card.DisplayName;
                slot.Description.text = card.Description;
                slot.Stacks.text = $"ĐÃ NHẬN  {state.GetStacks(card.Id)}/{card.MaxStacks}";
                slot.Icon.sprite = card.Icon;
                slot.Icon.color = card.Icon != null ? Color.white : card.Accent;
                slot.Placeholder.gameObject.SetActive(card.Icon == null);
                slot.Placeholder.text = string.IsNullOrEmpty(card.DisplayName)
                    ? "?" : card.DisplayName.Substring(0, 1).ToUpperInvariant();
                slot.Button.interactable = true;
                slot.Hover.ResetPresentation();
                slot.Hover.SetInteractable(true);
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => onPick(card));
            }

            _rerollText.text = $"ĐỔI THẺ  •  {rerollsRemaining} LƯỢT";
            _rerollButton.interactable = rerollsRemaining > 0;
            _rerollButton.onClick.RemoveAllListeners();
            _rerollButton.onClick.AddListener(() => onReroll());

            SetHighlightedSlot(null);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void MarkSelected(CardDefinition selected, Transform playerTarget)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (!slot.Root.activeSelf) continue;
                slot.Button.interactable = false;
                slot.Hover.SetInteractable(false);
                if (slot.Card == selected) slot.Hover.PlayConsume(playerTarget);
                else slot.Hover.SetDimmed(true);
            }
            _rerollButton.interactable = false;
        }

        public void SetStatus(string message) => _statusText.text = message;

        public void SetButtonsEnabled(bool enabled)
        {
            foreach (Slot slot in _slots)
            {
                slot.Button.interactable = enabled;
                slot.Hover.SetInteractable(enabled);
            }
            _rerollButton.interactable = enabled;
        }

        public void ShowWaiting()
        {
            EnsureBuilt();
            _overlay.SetActive(true);
            foreach (Slot slot in _slots) slot.Root.SetActive(false);
            _rerollButton.interactable = false;
            SetRect(_statusText.rectTransform, Center, Center,
                Vector2.zero, new Vector2(900f, 60f), Center);
            SetStatus("ĐANG CHỜ NGƯỜI CHƠI CÒN LẠI…");
        }

        public void Hide()
        {
            EnsureBuilt();
            SetHighlightedSlot(null);
            for (int i = 0; i < _slots.Length; i++)
                _slots[i]?.Hover?.ResetPresentation();
            _overlay.SetActive(false);
        }

        public void RefreshOwned(IReadOnlyList<CardDefinition> definitions, PlayerUpgradeState state)
        {
            EnsureBuilt();
            if (state == null || definitions == null)
            {
                _ownedText.text = "NÂNG CẤP: Chưa có";
                return;
            }

            var lines = new List<string>(7) { "NÂNG CẤP ĐÃ NHẬN" };
            for (int i = 0; i < definitions.Count; i++)
            {
                CardDefinition card = definitions[i];
                if (card == null) continue;
                int stacks = state.GetStacks(card.Id);
                if (stacks > 0) lines.Add($"{card.DisplayName}  {stacks}/{card.MaxStacks}");
            }
            if (lines.Count == 1) lines.Add("Chưa có");
            _ownedText.text = string.Join("\n", lines);
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();

            var canvasGo = new GameObject("CardCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _ownedText = CreateText("OwnedCards", canvasGo.transform, 20, TextAnchor.UpperLeft, Hex("E0CFAF"));
            SetRect(_ownedText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(320f, 260f), new Vector2(0f, 1f));
            _ownedText.raycastTarget = false;

            _overlay = CreateImage("SelectionOverlay", canvasGo.transform, Hex("15130F", 0.93f)).gameObject;
            Stretch((RectTransform)_overlay.transform);

            Image panelGlow = CreateImage("PanelGlow", _overlay.transform, Hex("C08D20", 0.30f));
            SetRect(panelGlow.rectTransform, Center, Center, Vector2.zero,
                new Vector2(1090f, 720f), Center);
            Image panel = CreateImage("Panel", panelGlow.transform, Hex("15130F"));
            Inset(panel.rectTransform, 3f);

            Image titleRule = CreateImage("TitleRule", panel.transform, Hex("C08D20", 0.75f));
            SetRect(titleRule.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -92f), new Vector2(880f, 2f), Center);
            Text title = CreateText("Title", panel.transform, 39, TextAnchor.MiddleCenter, Hex("FBDD82"));
            title.text = "CHỌN NÂNG CẤP";
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -48f), new Vector2(900f, 62f), Center);

            _statusText = CreateText("SelectionStatus", panel.transform, 20, TextAnchor.MiddleCenter, Hex("9CCFC0"));
            SetRect(_statusText.rectTransform, BottomCenter, BottomCenter,
                new Vector2(-325f, 52f), new Vector2(380f, 48f), Center);

            float[] x = { -330f, 0f, 330f };
            for (int i = 0; i < _slots.Length; i++) _slots[i] = CreateSlot(panel.transform, x[i]);

            _rerollButton = CreateButton("Reroll", panel.transform, out _rerollText);
            SetRect((RectTransform)_rerollButton.transform, BottomCenter, BottomCenter,
                new Vector2(0f, 28f), new Vector2(270f, 52f), BottomCenter);
            _rerollText.fontSize = 20;
            _rerollText.fontStyle = FontStyle.Bold;

            _overlay.SetActive(false);
            RefreshOwned(null, null);
        }

        private Slot CreateSlot(Transform parent, float x)
        {
            Image glow = CreateImage("CardGlow", parent, Hex("C08D20", 0.16f));
            Button button = glow.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            SetRect((RectTransform)button.transform, Center, Center,
                new Vector2(x, 6f), new Vector2(300f, 486f), Center);

            Image border = CreateImage("BronzeBorder", button.transform, Hex("C08D20", 0.74f));
            Inset(border.rectTransform, 5f);
            Image surface = CreateImage("CardSurface", border.transform, Hex("15130F"));
            Inset(surface.rectTransform, 4f);

            Image artWell = CreateImage("ArtWell", surface.transform, Hex("112E3E"));
            SetRect(artWell.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -18f), new Vector2(260f, 230f), TopCenter);
            Image artLine = CreateImage("ArtLine", artWell.transform, Hex("4FA694", 0.48f));
            SetRect(artLine.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 3f), new Vector2(0.5f, 0f));

            Image icon = CreateImage("Icon", artWell.transform, Color.white);
            SetRect(icon.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -4f), new Vector2(220f, 220f), TopCenter);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text placeholder = CreateText("Placeholder", icon.transform, 54, TextAnchor.MiddleCenter, Color.white);
            Stretch(placeholder.rectTransform);

            Image namePlate = CreateImage("NamePlate", surface.transform, Hex("2B2724"));
            SetRect(namePlate.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -256f), new Vector2(260f, 58f), TopCenter);
            Text name = CreateText("Name", namePlate.transform, 28, TextAnchor.MiddleCenter, Hex("FBDD82"));
            name.fontStyle = FontStyle.Bold;
            Stretch(name.rectTransform);

            Text description = CreateText("Description", surface.transform, 19, TextAnchor.UpperCenter, Hex("F4EADA"));
            SetRect(description.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -326f), new Vector2(244f, 104f), TopCenter);

            Image footerRule = CreateImage("FooterRule", surface.transform, Hex("C08D20", 0.38f));
            SetRect(footerRule.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 46f), new Vector2(226f, 1f), BottomCenter);
            Text stacks = CreateText("Stacks", surface.transform, 16, TextAnchor.MiddleCenter, Hex("BFA981"));
            SetRect(stacks.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 22f), new Vector2(240f, 32f), BottomCenter);

            CardHoverVisual hover = button.gameObject.AddComponent<CardHoverVisual>();
            hover.Configure(glow, border, surface, new[] { artLine, footerRule },
                x / 990f, SetHighlightedSlot);
            return new Slot
            {
                Root = button.gameObject,
                Button = button,
                Icon = icon,
                Placeholder = placeholder,
                Name = name,
                Description = description,
                Stacks = stacks,
                Hover = hover
            };
        }

        private Button CreateButton(string name, Transform parent, out Text label)
        {
            Image image = CreateImage(name, parent, Hex("2B2724"));
            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Hex("2B2724");
            colors.highlightedColor = Hex("1C4B5C");
            colors.pressedColor = Hex("C08D20");
            colors.selectedColor = Hex("1C4B5C");
            colors.disabledColor = Hex("15130F", 0.75f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            label = CreateText("Label", image.transform, 22, TextAnchor.MiddleCenter, Hex("F4EADA"));
            Stretch(label.rectTransform);
            return button;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Color Hex(string rgb, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
            color.a = alpha;
            return color;
        }

        private static Vector2 Center => new Vector2(0.5f, 0.5f);
        private static Vector2 TopCenter => new Vector2(0.5f, 1f);
        private static Vector2 BottomCenter => new Vector2(0.5f, 0f);

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || _fallbackEventSystem != null
                || FindAnyObjectByType<EventSystem>() != null) return;
            _fallbackEventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
        }

        private void SetHighlightedSlot(CardHoverVisual highlighted)
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i]?.Hover?.SetHighlighted(_slots[i].Hover == highlighted);
        }
    }
}
