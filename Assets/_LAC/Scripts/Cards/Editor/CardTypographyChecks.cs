#if UNITY_EDITOR
using System;
using LAC.Core;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    /// <summary>Kiểm chữ ở độ phân giải Game View hiện tại, trên dữ liệu thật và hierarchy tạm.</summary>
    public static class CardTypographyChecks
    {
        private static GameObject _preview;
        private static float _previousScale;

        [MenuItem("LAC/Tests/Preview Readable Cards (Play Mode)")]
        public static void Preview()
        {
            Require(Application.isPlaying && NetworkServer.active, "Cần host trong Play mode.");
            Require(RunManager.Instance.State == RunState.WaveActive,
                "Không mở preview khi đang chọn thẻ hoặc kết thúc ván.");
            if (_preview == null)
            {
                _previousScale = Time.timeScale;
                _preview = new GameObject("CardTypographyPreview");
                _preview.AddComponent<PlayerUpgradeState>();
                _preview.AddComponent<CardSelectionView>();
            }
            Time.timeScale = 0f;
            ShowSample(_preview.GetComponent<CardSelectionView>(),
                _preview.GetComponent<PlayerUpgradeState>());
        }

        [MenuItem("LAC/Tests/Validate Card Typography (Play Mode)")]
        public static void Validate()
        {
            Preview();
            var view = _preview.GetComponent<CardSelectionView>();
            var state = _preview.GetComponent<PlayerUpgradeState>();
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var recipes = Resources.LoadAll<CardEvolutionData>("Evolutions");
            Require(cards.Length == 12 && recipes.Length == 8, "Danh mục thiếu thẻ.");
            Canvas canvas = _preview.GetComponentInChildren<Canvas>();
            Require(canvas.pixelPerfect, "Canvas chưa căn pixel.");
            Require(canvas.scaleFactor >= 1f - .001f,
                "Đặt Game View tối thiểu 1280×720 trước khi kiểm chữ.");
            try
            {
                foreach (var card in cards)
                {
                    view.Show(new[] { card }, state, 2, _ => { }, () => { });
                    view.SetStatus("TỰ CHỌN SAU 10 GIÂY");
                    ValidateVisibleText();
                    Text description = FindText("Description");
                    Require(description.fontSize * canvas.scaleFactor >= 21.9f,
                        "Mô tả nhỏ hơn 22 pixel tại 720p.");
                    Require(description.alignment == TextAnchor.UpperLeft, "Mô tả chưa căn trái.");
                    CheckContrast(description.color, ColorOf("15130F"));
                    CheckContrast(description.color, ColorOf("112E3E"));
                    Text name = FindText("Name");
                    CheckContrast(name.color, ColorOf("2B2724"));
                    foreach (var hover in _preview.GetComponentsInChildren<CardHoverVisual>())
                    {
                        Vector3 before = hover.transform.localPosition;
                        hover.SetHighlighted(true);
                        Require(hover.transform.localPosition == before
                            && hover.transform.localScale == Vector3.one
                            && hover.transform.localRotation == Quaternion.identity,
                            "Hover đổi transform, gây chữ dao động.");
                    }
                }
                foreach (var recipe in recipes)
                {
                    view.ShowEvolution(recipe);
                    ValidateVisibleText();
                }
                view.ShowWaiting();
                ValidateVisibleText();
                Require(!FindInactive("Reroll").activeSelf, "Màn chờ còn nút đổi.");
                Debug.Log($"[CardTypography] PASS: 12 cards, 8 evolutions, waiting, Vietnamese glyphs, no clipping; "
                    + $"screen={Screen.width}x{Screen.height}, scale={canvas.scaleFactor:F3}, "
                    + $"description={22f * canvas.scaleFactor:F1}px, pixelPerfect, no best-fit/blur effects; contrast >= 7:1.");
            }
            finally { ShowSample(view, state); }
        }

        [MenuItem("LAC/Tests/Close Card Typography Preview")]
        public static void Close()
        {
            if (_preview == null) return;
            UnityEngine.Object.DestroyImmediate(_preview);
            _preview = null;
            Time.timeScale = _previousScale;
        }

        private static void ShowSample(CardSelectionView view, PlayerUpgradeState state)
        {
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            view.Show(new[] { Find(cards, CardId.CuongNo), Find(cards, CardId.SongTien),
                Find(cards, CardId.ThietBich) }, state, 2, _ => { }, () => { });
            view.SetStatus("TỰ CHỌN SAU 10 GIÂY");
            Canvas.ForceUpdateCanvases();
        }

        private static CardDefinition Find(CardDefinition[] cards, CardId id) =>
            Array.Find(cards, card => card.Id == id);

        private static void ValidateVisibleText()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in _preview.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(text.text)) continue;
                Require(!text.resizeTextForBestFit && !text.supportRichText,
                    "Chữ bị tự thu nhỏ hoặc diễn giải markup.");
                Require(text.GetComponents<BaseMeshEffect>().Length == 0,
                    "Không dùng viền/bóng nhân mesh cho chữ.");
                Require(text.preferredHeight <= text.rectTransform.rect.height + .5f,
                    $"Tràn chữ {text.name}: {text.preferredHeight:F1}/{text.rectTransform.rect.height:F1}: {text.text}");
                text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
                foreach (char character in text.text)
                    if (!char.IsWhiteSpace(character))
                        Require(text.font.HasCharacter(character), $"Thiếu ký tự {character} trong {text.name}.");
            }
        }

        private static Text FindText(string name)
        {
            foreach (var text in _preview.GetComponentsInChildren<Text>())
                if (text.name == name) return text;
            throw new InvalidOperationException("Không tìm thấy " + name);
        }

        private static GameObject FindInactive(string name)
        {
            foreach (var rect in _preview.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name) return rect.gameObject;
            throw new InvalidOperationException("Không tìm thấy " + name);
        }

        private static void CheckContrast(Color foreground, Color background) =>
            Require((Luminance(foreground) + .05f) / (Luminance(background) + .05f) >= 7f,
                "Độ tương phản chữ dưới 7:1.");

        private static float Luminance(Color color) => .2126f * Linear(color.r)
            + .7152f * Linear(color.g) + .0722f * Linear(color.b);
        private static float Linear(float value) => value <= .04045f ? value / 12.92f
            : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        private static Color ColorOf(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[CardTypography] " + message);
        }
    }
}
#endif
