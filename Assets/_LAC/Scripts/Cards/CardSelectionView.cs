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
        private sealed class Slot
        {
            public GameObject Root;
            public Button Button;
            public Image Icon;
            public Text Placeholder;
            public Text Name;
            public Text Description;
            public Text Stacks;
            public CardDefinition Card;
        }

        private readonly Slot[] _slots = new Slot[3];
        private Font _font;
        private GameObject _overlay;
        private Text _rerollText;
        private Button _rerollButton;
        private Text _ownedText;
        private bool _built;

        private void Awake() => EnsureBuilt();

        public void Show(IReadOnlyList<CardDefinition> cards, PlayerUpgradeState state,
            int rerollsRemaining, Action<CardDefinition> onPick, Action onReroll)
        {
            EnsureBuilt();
            _overlay.SetActive(true);

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
                slot.Stacks.text = $"Đã nhận: {state.GetStacks(card.Id)}/{card.MaxStacks}";
                slot.Icon.sprite = card.Icon;
                slot.Icon.color = card.Icon != null ? Color.white : card.Accent;
                slot.Placeholder.gameObject.SetActive(card.Icon == null);
                slot.Placeholder.text = string.IsNullOrEmpty(card.DisplayName)
                    ? "?" : card.DisplayName.Substring(0, 1).ToUpperInvariant();
                slot.Button.interactable = true;
                ColorBlock colors = slot.Button.colors;
                colors.normalColor = new Color(0.16f, 0.13f, 0.18f, 1f);
                colors.highlightedColor = new Color(0.30f, 0.24f, 0.34f, 1f);
                colors.pressedColor = card.Accent;
                colors.selectedColor = colors.highlightedColor;
                slot.Button.colors = colors;
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => onPick(card));
            }

            _rerollText.text = $"Đổi thẻ  •  {rerollsRemaining} lượt";
            _rerollButton.interactable = rerollsRemaining > 0;
            _rerollButton.onClick.RemoveAllListeners();
            _rerollButton.onClick.AddListener(() => onReroll());
        }

        public void MarkSelected(CardDefinition selected)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (!slot.Root.activeSelf) continue;
                slot.Button.interactable = false;
                if (slot.Card != selected) continue;
                slot.Button.targetGraphic.color = selected.Accent;
            }
            _rerollButton.interactable = false;
        }

        public void Hide()
        {
            EnsureBuilt();
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

            _ownedText = CreateText("OwnedCards", canvasGo.transform, 21, TextAnchor.UpperLeft,
                new Color(0.96f, 0.90f, 0.72f, 1f));
            SetRect(_ownedText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(320f, 260f), new Vector2(0f, 1f));
            _ownedText.raycastTarget = false;

            _overlay = CreateImage("SelectionOverlay", canvasGo.transform,
                new Color(0.025f, 0.02f, 0.035f, 0.88f)).gameObject;
            Stretch((RectTransform)_overlay.transform);

            Image panel = CreateImage("Panel", _overlay.transform, new Color(0.09f, 0.07f, 0.11f, 0.98f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1030f, 650f), new Vector2(0.5f, 0.5f));

            Text title = CreateText("Title", panel.transform, 42, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.78f, 0.30f, 1f));
            title.text = "CHỌN NÂNG CẤP";
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(900f, 70f), new Vector2(0.5f, 0.5f));

            float[] x = { -330f, 0f, 330f };
            for (int i = 0; i < _slots.Length; i++) _slots[i] = CreateSlot(panel.transform, x[i]);

            _rerollButton = CreateButton("Reroll", panel.transform, out _rerollText);
            SetRect((RectTransform)_rerollButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 45f), new Vector2(270f, 58f), new Vector2(0.5f, 0f));
            _rerollText.fontSize = 23;

            _overlay.SetActive(false);
            RefreshOwned(null, null);
        }

        private Slot CreateSlot(Transform parent, float x)
        {
            Button button = CreateButton("Card", parent, out _);
            SetRect((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, 10f), new Vector2(285f, 430f), new Vector2(0.5f, 0.5f));

            Image icon = CreateImage("Icon", button.transform, Color.white);
            SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(104f, 104f), new Vector2(0.5f, 1f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text placeholder = CreateText("Placeholder", icon.transform, 54, TextAnchor.MiddleCenter, Color.white);
            Stretch(placeholder.rectTransform);
            placeholder.raycastTarget = false;

            Text name = CreateText("Name", button.transform, 28, TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.36f, 1f));
            name.fontStyle = FontStyle.Bold;
            SetRect(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -158f), new Vector2(250f, 54f), new Vector2(0.5f, 1f));

            Text description = CreateText("Description", button.transform, 20, TextAnchor.UpperCenter, Color.white);
            SetRect(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -220f), new Vector2(242f, 142f), new Vector2(0.5f, 1f));

            Text stacks = CreateText("Stacks", button.transform, 18, TextAnchor.MiddleCenter,
                new Color(0.72f, 0.72f, 0.76f, 1f));
            SetRect(stacks.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(240f, 38f), new Vector2(0.5f, 0f));

            return new Slot
            {
                Root = button.gameObject,
                Button = button,
                Icon = icon,
                Placeholder = placeholder,
                Name = name,
                Description = description,
                Stacks = stacks
            };
        }

        private Button CreateButton(string name, Transform parent, out Text label)
        {
            Image image = CreateImage(name, parent, new Color(0.16f, 0.13f, 0.18f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.24f, 0.34f, 1f);
            colors.pressedColor = new Color(0.78f, 0.56f, 0.20f, 1f);
            colors.disabledColor = new Color(0.10f, 0.09f, 0.11f, 0.75f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            label = CreateText("Label", image.transform, 22, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
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
            return text;
        }

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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
