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
        public const float EvolutionSeconds = 2.4f;
        private GameObject _evolutionPanel;
        private Image _evolutionIcon;
        private Text _evolutionName;
        private Text _evolutionDescription;
        private Text _evolutionIngredients;
        private Text _evolvedText;
        private Sprite _evolutionEmblem;
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
        private Text _title;
        private Text _subtitle;
        private Text _selectionHint;
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
            _evolutionPanel.SetActive(false);
            _title.text = "CHỌN NÂNG CẤP";
            _subtitle.text = "Chọn một thẻ để tăng sức mạnh";
            _rerollButton.gameObject.SetActive(true);
            _selectionHint.gameObject.SetActive(true);
            SetRect(_statusText.rectTransform, BottomCenter, BottomCenter,
                new Vector2(-310f, 77f), new Vector2(520f, 48f), Center);

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
            _evolutionPanel.SetActive(false);
            foreach (Slot slot in _slots) slot.Root.SetActive(false);
            _rerollButton.interactable = false;
            _rerollButton.gameObject.SetActive(false);
            _selectionHint.gameObject.SetActive(false);
            _title.text = "ĐANG CHỜ ĐỒNG ĐỘI";
            _subtitle.text = "Đợt mới bắt đầu khi mọi người chọn xong";
            SetRect(_statusText.rectTransform, Center, Center,
                Vector2.zero, new Vector2(900f, 60f), Center);
            SetStatus("ĐANG CHỜ NGƯỜI CHƠI CÒN LẠI…");
        }

        public void ShowEvolution(CardEvolutionData recipe)
        {
            ShowWaiting();
            _evolutionPanel.SetActive(true);
            _evolutionIcon.sprite = recipe.Icon != null ? recipe.Icon : _evolutionEmblem;
            _evolutionName.text = recipe.DisplayName;
            _evolutionDescription.text = recipe.Description;
            var parts = new List<string>();
            foreach (var ingredient in recipe.Ingredients)
                parts.Add($"{ingredient.Card.DisplayName} ×{ingredient.Stacks}");
            _evolutionIngredients.text = string.Join("  +  ", parts);
            SetStatus("");
        }

        public void Hide()
        {
            EnsureBuilt();
            SetHighlightedSlot(null);
            for (int i = 0; i < _slots.Length; i++)
                _slots[i]?.Hover?.ResetPresentation();
            _overlay.SetActive(false);
            _evolutionPanel.SetActive(false);
        }

        public void RefreshOwned(IReadOnlyList<CardDefinition> definitions, PlayerUpgradeState state)
        {
            EnsureBuilt();
            if (state == null || definitions == null)
            {
                _ownedText.text = "NÂNG CẤP: Chưa có";
                _evolvedText.text = "";
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
            lines.Clear();
            foreach (var recipe in state.Evolutions) lines.Add(recipe.DisplayName);
            _evolvedText.text = lines.Count == 0 ? "" : "TIẾN HOÁ\n" + string.Join("\n", lines);
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
                new Vector2(24f, -24f), new Vector2(320f, 340f), new Vector2(0f, 1f));
            _ownedText.raycastTarget = false;

            _evolvedText = CreateText("EvolvedCards", canvasGo.transform, 20, TextAnchor.UpperRight, Hex("FBDD82"));
            SetRect(_evolvedText.rectTransform, Vector2.one, Vector2.one,
                new Vector2(-24f, -24f), new Vector2(340f, 270f), Vector2.one);

            _overlay = CreateImage("SelectionOverlay", canvasGo.transform, Hex("15130F", 0.93f)).gameObject;
            Stretch((RectTransform)_overlay.transform);

            Image panelShadow = CreateImage("PanelShadow", _overlay.transform, Hex("15130F", 0.65f));
            SetRect(panelShadow.rectTransform, Center, Center, new Vector2(0f, -12f),
                new Vector2(1400f, 892f), Center);
            Image panel = CreateImage("Panel", _overlay.transform, Hex("15130F"));
            SetRect(panel.rectTransform, Center, Center, Vector2.zero,
                new Vector2(1360f, 860f), Center);
            Image panelInset = CreateImage("PanelInset", panel.transform, Hex("2B2724", 0.30f));
            Inset(panelInset.rectTransform, 22f);
            panelInset.raycastTarget = false;
            CardFrameGraphic panelFrame = CreateFrame("PanelFrame", panel.transform, false, 0f);
            Stretch(panelFrame.rectTransform);
            panelFrame.color = new Color(1f, 1f, 1f, 0.65f);

            Image titleRule = CreateImage("TitleRule", panel.transform, Hex("C08D20", 0.38f));
            SetRect(titleRule.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -146f), new Vector2(1190f, 2f), Center);
            Text title = CreateText("Title", panel.transform, 44, TextAnchor.MiddleCenter, Hex("FBDD82"));
            _title = title;
            title.text = "CHỌN NÂNG CẤP";
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -66f), new Vector2(1000f, 64f), Center);
            Text subtitle = CreateText("Subtitle", panel.transform, 23, TextAnchor.MiddleCenter, Hex("BFA981"));
            _subtitle = subtitle;
            subtitle.text = "Chọn một thẻ để tăng sức mạnh";
            SetRect(subtitle.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -111f), new Vector2(1000f, 36f), Center);

            _statusText = CreateText("SelectionStatus", panel.transform, 23, TextAnchor.MiddleCenter, Hex("9CCFC0"));
            SetRect(_statusText.rectTransform, BottomCenter, BottomCenter,
                new Vector2(-310f, 77f), new Vector2(520f, 48f), Center);

            float[] x = { -390f, 0f, 390f };
            for (int i = 0; i < _slots.Length; i++) _slots[i] = CreateSlot(panel.transform, x[i], i);

            Image footerRule = CreateImage("PanelFooterRule", panel.transform, Hex("C08D20", 0.30f));
            SetRect(footerRule.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 122f), new Vector2(1190f, 1f), Center);

            _rerollButton = CreateButton("Reroll", panel.transform, out _rerollText);
            SetRect((RectTransform)_rerollButton.transform, BottomCenter, BottomCenter,
                new Vector2(310f, 77f), new Vector2(330f, 58f), Center);
            _rerollText.fontSize = 23;
            _rerollText.fontStyle = FontStyle.Bold;
            CardFrameGraphic rerollFrame = CreateFrame("RerollFrame", _rerollButton.transform, false, 0f);
            Stretch(rerollFrame.rectTransform);
            Text hint = CreateText("SelectionHint", panel.transform, 18, TextAnchor.MiddleCenter, Hex("94805C"));
            _selectionHint = hint;
            hint.text = "Chọn 1 trong 3  •  Tối đa 2 lượt đổi mỗi ván";
            SetRect(hint.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 29f), new Vector2(1050f, 30f), Center);

            _evolutionEmblem = Resources.Load<Sprite>("EvolutionEmblem");
            _evolutionPanel = CreateImage("EvolutionPanel", _overlay.transform, Hex("15130F")).gameObject;
            SetRect((RectTransform)_evolutionPanel.transform, Center, Center, Vector2.zero,
                new Vector2(1340f, 840f), Center);
            CardFrameGraphic evolutionFrame = CreateFrame("EvolutionFrame", _evolutionPanel.transform, false, 0f);
            Stretch(evolutionFrame.rectTransform);
            Text evolutionTitle = CreateText("EvolutionTitle", _evolutionPanel.transform, 34,
                TextAnchor.MiddleCenter, Hex("9CCFC0"));
            evolutionTitle.text = "TIẾN HOÁ THẺ";
            SetRect(evolutionTitle.rectTransform, Center, Center, new Vector2(0, 282), new Vector2(950, 60), Center);
            _evolutionIcon = CreateImage("EvolutionEmblem", _evolutionPanel.transform, Color.white);
            _evolutionIcon.preserveAspect = true;
            _evolutionIcon.raycastTarget = false;
            SetRect(_evolutionIcon.rectTransform, Center, Center, new Vector2(0, 105), new Vector2(250, 250), Center);
            _evolutionName = CreateText("EvolutionName", _evolutionPanel.transform, 38,
                TextAnchor.MiddleCenter, Hex("FBDD82"));
            _evolutionName.fontStyle = FontStyle.Bold;
            SetRect(_evolutionName.rectTransform, Center, Center, new Vector2(0, -65), new Vector2(950, 64), Center);
            _evolutionIngredients = CreateText("EvolutionIngredients", _evolutionPanel.transform, 23,
                TextAnchor.MiddleCenter, Hex("9CCFC0"));
            SetRect(_evolutionIngredients.rectTransform, Center, Center, new Vector2(0, -133), new Vector2(950, 66), Center);
            _evolutionDescription = CreateText("EvolutionDescription", _evolutionPanel.transform, 24,
                TextAnchor.MiddleCenter, Hex("F4EADA"));
            SetRect(_evolutionDescription.rectTransform, Center, Center, new Vector2(0, -215), new Vector2(900, 95), Center);
            _evolutionPanel.SetActive(false);

            _overlay.SetActive(false);
            RefreshOwned(null, null);
        }

        private Slot CreateSlot(Transform parent, float x, int index)
        {
            Image shadow = CreateImage("CardShadow", parent, Hex("15130F", 0.75f));
            SetRect(shadow.rectTransform, Center, Center,
                new Vector2(x + 7f, -23f), new Vector2(346f, 562f), Center);
            shadow.raycastTarget = false;
            Image root = CreateImage("Card", parent, Color.clear);
            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            SetRect((RectTransform)button.transform, Center, Center,
                new Vector2(x, -15f), new Vector2(340f, 560f), Center);
            shadow.transform.SetParent(button.transform, false);
            SetRect(shadow.rectTransform, Center, Center,
                new Vector2(7f, -8f), new Vector2(346f, 562f), Center);

            Image surface = CreateImage("CardSurface", button.transform, Hex("15130F"));
            Inset(surface.rectTransform, 16f);

            Image artWell = CreateImage("ArtWell", surface.transform, Hex("15130F"));
            SetRect(artWell.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -22f), new Vector2(280f, 252f), TopCenter);
            Image artLine = CreateImage("ArtLine", artWell.transform, Hex("C08D20", 0.48f));
            SetRect(artLine.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 2f), new Vector2(0.5f, 0f));

            Image icon = CreateImage("Icon", artWell.transform, Color.white);
            SetRect(icon.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -4f), new Vector2(244f, 244f), TopCenter);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text placeholder = CreateText("Placeholder", icon.transform, 54, TextAnchor.MiddleCenter, Color.white);
            Stretch(placeholder.rectTransform);

            Image namePlate = CreateImage("NamePlate", surface.transform, Hex("2B2724", 0.75f));
            SetRect(namePlate.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -289f), new Vector2(280f, 58f), TopCenter);
            Text name = CreateText("Name", namePlate.transform, 30, TextAnchor.MiddleCenter, Hex("FBDD82"));
            name.fontStyle = FontStyle.Bold;
            Stretch(name.rectTransform);

            Text description = CreateText("Description", surface.transform, 22, TextAnchor.UpperCenter, Hex("F4EADA"));
            SetRect(description.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -361f), new Vector2(274f, 110f), TopCenter);

            Image footerRule = CreateImage("FooterRule", surface.transform, Hex("C08D20", 0.38f));
            SetRect(footerRule.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 47f), new Vector2(254f, 1f), BottomCenter);
            Text stacks = CreateText("Stacks", surface.transform, 18, TextAnchor.MiddleCenter, Hex("BFA981"));
            SetRect(stacks.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 16f), new Vector2(272f, 32f), BottomCenter);

            CardFrameGraphic frame = CreateFrame("BronzeFrame", button.transform, true, 1.2f + index * 1.1f);
            Stretch(frame.rectTransform);
            CardHoverVisual hover = button.gameObject.AddComponent<CardHoverVisual>();
            hover.Configure(frame, surface, new[] { artLine, footerRule }, SetHighlightedSlot);
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

        private static CardFrameGraphic CreateFrame(string name, Transform parent, bool glint, float delay)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(CardFrameGraphic));
            go.transform.SetParent(parent, false);
            var frame = go.GetComponent<CardFrameGraphic>();
            frame.Configure(glint, delay);
            return frame;
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
