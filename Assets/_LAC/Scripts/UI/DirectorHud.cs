using LAC.Core;
using LAC.Director;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LAC.UI
{
    /// <summary>Giải thích nhịp trận từ quyết định của host; F3 mở dữ liệu dùng khi báo cáo.</summary>
    public sealed class DirectorHud : MonoBehaviour
    {
        private RunManager _run;
        private GameObject _panel;
        private GameObject _detailsPanel;
        private RectTransform _detailsRect;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _summary;
        private TextMeshProUGUI _details;
        private UnityEngine.UI.Image _accent;
        private TMP_FontAsset _font;
        private bool _expanded;
        private bool _hasSnapshot;
        private DirectorMode _mode;
        private DirectorState _directorState;
        private int _wave;
        private string _reason;
        private string _source;
        private string _fallback;
        private float _healthMin;
        private float _damageRatio;
        private int _downs;
        private bool _contextValid;
        private static TMP_Settings _runtimeSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFontSettings() => _runtimeSettings = null;

        public bool IsDebugExpanded => _expanded;
        public bool IsVisible => _panel != null && _panel.activeSelf;
        public string Headline => _title != null ? _title.text : "";
        public string Explanation => _summary != null ? _summary.text : "";
        public string DebugText => _details != null ? _details.text : "";

        private void Start()
        {
            _run = RunManager.Instance;
            if (_run == null)
            {
                Debug.LogError("[DirectorHud] Thiếu RunManager.", this);
                enabled = false;
                return;
            }
            Build();
            _run.RunStarted += OnRunStarted;
        }

        private void OnDisable()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_run != null) _run.RunStarted -= OnRunStarted;
            ReleaseFontAsset(_font);
        }

        private void OnRunStarted()
        {
            SetDebugExpanded(false);
            _hasSnapshot = false;
        }

        private void Update()
        {
            if (_run == null || _panel == null) return;
            bool visible = NetworkClient.localPlayer != null && _run.State == RunState.WaveActive;
            if (_panel.activeSelf != visible) _panel.SetActive(visible);
            if (!visible) return;

            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                SetDebugExpanded(!_expanded);

            // Đọc sau cả gói SyncVar, tránh diễn giải trước khi các trường quyết định tới đủ.
            if (!_hasSnapshot || _mode != _run.DirectorMode || _directorState != _run.DirectorState ||
                _wave != _run.CurrentWave || _reason != _run.DirectorReason || _source != _run.DirectorSource ||
                _fallback != _run.DirectorFallbackReason || _healthMin != _run.DirectorHealthMin ||
                _damageRatio != _run.DirectorDamageRatioMax || _downs != _run.DirectorDowns ||
                _contextValid != _run.DirectorContextValid)
                Refresh();
        }

        public void SetDebugExpanded(bool expanded)
        {
            _expanded = expanded;
            if (_detailsPanel != null) _detailsPanel.SetActive(expanded);
        }

        private void Refresh()
        {
            _hasSnapshot = true;
            _mode = _run.DirectorMode;
            _directorState = _run.DirectorState;
            _wave = _run.CurrentWave;
            _reason = _run.DirectorReason;
            _source = _run.DirectorSource;
            _fallback = _run.DirectorFallbackReason;
            _healthMin = _run.DirectorHealthMin;
            _damageRatio = _run.DirectorDamageRatioMax;
            _downs = _run.DirectorDowns;
            _contextValid = _run.DirectorContextValid;

            _accent.color = Palette("9CCFC0");
            if (_source == "invalid_baseline")
            {
                _title.text = "NHỊP TRẬN · TẠM DỪNG";
                _summary.text = "Nhịp sinh quái tạm dừng cho đợt này.";
            }
            else if (_mode == DirectorMode.Fixed)
            {
                _title.text = "NHỊP TRẬN · CỐ ĐỊNH";
                _summary.text = "Giữ nhịp sinh theo bảng đợt.";
            }
            else if (!string.IsNullOrEmpty(_fallback))
            {
                _title.text = "ĐẠO DIỄN · NHỊP CHUẨN";
                _summary.text = "Dùng nhịp tiêu chuẩn cho đợt này.";
            }
            else if (_wave == 1 || _run.IsFinalWave)
            {
                _title.text = _wave == 1 ? "ĐẠO DIỄN · KHỞI ĐỘNG" : "ĐẠO DIỄN · TRÙM";
                _summary.text = _wave == 1 ? "Đợt mở đầu giữ nhịp quen thuộc." : "Trùm giữ sức mạnh và nhịp trận cố định.";
            }
            else
            {
                switch (_directorState)
                {
                    case DirectorState.Relief:
                        _title.text = "ĐẠO DIỄN · HỖ TRỢ";
                        _summary.text = "Giãn nhịp sinh để hỗ trợ đội đang yếu.";
                        _accent.color = Palette("FBDD82");
                        break;
                    case DirectorState.Variety:
                        _title.text = "ĐẠO DIỄN · ĐỔI CHIẾN THUẬT";
                        _summary.text = "Đổi hướng và thành phần quái; giữ ngân sách.";
                        break;
                    default:
                        _title.text = "ĐẠO DIỄN · BÌNH THƯỜNG";
                        _summary.text = "Giữ nhịp sinh tiêu chuẩn của đợt.";
                        break;
                }
            }
            _title.color = _accent.color;
            if (_mode == DirectorMode.Adaptive && _source != "invalid_baseline" && string.IsNullOrEmpty(_fallback) &&
                _wave > 1 && !_run.IsFinalWave && !string.IsNullOrEmpty(_reason))
                _summary.text = _reason;

            string context = !_contextValid
                ? "Ngữ cảnh: chưa có dữ liệu cuối đợt."
                : $"Cuối đợt {_wave - 1}: HP thấp nhất {_healthMin:P0}\nMất máu cao nhất {_damageRatio:P0} · Gục {_downs}";
            _details.text = $"{(_mode == DirectorMode.Adaptive ? "THÍCH ỨNG" : "CỐ ĐỊNH")} · ĐỢT {_wave:00}\n" +
                context + $"\nNguồn: {_source}\nLý do: {_reason}\n" +
                $"Dự phòng: {(string.IsNullOrEmpty(_fallback) ? "không" : _fallback)}";
            float height = Mathf.Clamp(_details.GetPreferredValues(_details.text, 408, 0).y + 20, 220, 410);
            _detailsRect.sizeDelta = new Vector2(440, height);
            _details.rectTransform.sizeDelta = new Vector2(408, height - 20);
        }

        private void Build()
        {
            _font = CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            if (_font == null) { enabled = false; return; }
            var canvas = new GameObject("DirectorCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().overrideSorting = true;
            canvas.GetComponent<Canvas>().sortingOrder = 20;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1f;

            var panel = Image("DirectorPanel", canvas.transform, Palette("15130F", .96f));
            _panel = panel.gameObject;
            Corner(panel.rectTransform, new Vector2(24, 100), new Vector2(440, 80));
            var group = _panel.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            _accent = Image("Accent", panel.transform, Palette("9CCFC0"));
            TopLeft(_accent.rectTransform, Vector2.zero, new Vector2(4, 80));
            _title = Text("Title", panel.transform, 21, new Vector2(16, -5), new Vector2(365, 27));
            _summary = Text("Explanation", panel.transform, 19, new Vector2(16, -31), new Vector2(408, 46));
            var hint = Text("DebugHint", panel.transform, 16, new Vector2(386, -8), new Vector2(42, 29));
            hint.text = "F3";
            hint.alignment = TextAlignmentOptions.Right;
            hint.color = Palette("BFA981");

            var details = Image("DirectorDetails", panel.transform, Palette("15130F", .98f));
            _detailsPanel = details.gameObject;
            _detailsRect = details.rectTransform;
            _detailsRect.anchorMin = _detailsRect.anchorMax = new Vector2(0, 1);
            _detailsRect.pivot = Vector2.zero;
            _detailsRect.anchoredPosition = new Vector2(0, 10);
            _detailsRect.sizeDelta = new Vector2(440, 220);
            _details = Text("DecisionData", details.transform, 19, new Vector2(16, -10), new Vector2(408, 200));
            _details.textWrappingMode = TextWrappingModes.Normal;
            SetDebugExpanded(false);
            _panel.SetActive(false);
        }

        private TextMeshProUGUI Text(string name, Transform parent, int size, Vector2 position, Vector2 bounds)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSharedMaterial = _font.material;
            text.fontSize = size;
            text.color = Palette("F4EADA");
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            TopLeft(text.rectTransform, position, bounds);
            go.SetActive(true);
            return text;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Corner(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static TMP_FontAsset CreateFontAsset(Font source)
        {
            source = Resources.Load<Font>("UI/DirectorFont");
            if (source == null)
            {
                Debug.LogError("[DirectorHud] Thiếu font UI/DirectorFont.");
                return null;
            }
            if (_runtimeSettings == null)
            {
                var settings = Resources.Load<TMP_Settings>("DirectorUI/TMP Settings");
                var singleton = typeof(TMP_Settings).GetField("s_Instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (settings == null || singleton == null)
                {
                    Debug.LogError("[DirectorHud] Thiếu cấu hình TMP tương thích.");
                    return null;
                }
                // TMP không có API đặt cấu hình riêng. Bản sao tránh sửa asset, đường dẫn riêng
                // tránh trùng Resources khi máy nhóm đã nhập TMP Essentials mặc định.
                _runtimeSettings = Instantiate(settings);
                _runtimeSettings.hideFlags = HideFlags.DontSave;
                singleton.SetValue(null, _runtimeSettings);
            }
            // Shader trong Resources được bàn giao cùng mã, không phụ thuộc TMP Essentials cục bộ.
            Shader shader = Resources.Load<Shader>("UI/DirectorSDF");
            if (shader == null)
            {
                Debug.LogError("[DirectorHud] Thiếu shader UI/DirectorSDF.");
                return null;
            }
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null) return null;
            font.material.shader = shader;
            font.name = "LAC_RuntimeVietnamese";
            font.hideFlags = HideFlags.DontSave;
            font.isMultiAtlasTexturesEnabled = true;
            return font;
        }

        internal static void ReleaseFontAsset(TMP_FontAsset font)
        {
            if (font == null) return;
            Material material = font.material;
            Texture2D[] atlases = font.atlasTextures;
            if (material != null) Destroy(material);
            if (atlases != null)
                foreach (Texture2D atlas in atlases)
                    if (atlas != null) Destroy(atlas);
            Destroy(font);
        }

        private static Color Palette(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            color.a = alpha;
            return color;
        }
    }
}
