using LAC.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace LAC.UI
{
    /// <summary>Thanh máu liên tục của người chơi cục bộ.</summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        [FormerlySerializedAs("_pipRow")]
        [SerializeField] private RectTransform _barRoot;
        [SerializeField] private Vector2 _barSize = new Vector2(300f, 28f);
        [SerializeField] private Color _fillColor = new Color32(79, 166, 148, 255);
        [SerializeField] private Color _trackColor = new Color32(21, 19, 15, 255);
        [SerializeField] private Color _borderColor = new Color32(191, 169, 129, 255);

        private RectTransform _fill;
        private UnityEngine.UI.Text _label;
        private PlayerHealth _tracked;
        private int _current = -1;
        private int _maximum = -1;

        private void Awake()
        {
            // Prefab riêng để giao diện đợt không cần sửa scene đấu trường.
            var waveHud = Resources.Load<WaveHud>("UI/WaveHud");
            if (waveHud != null) Instantiate(waveHud, transform, false);
            if (_barRoot == null) return;
            var border = CreateImage("HealthBar", _barRoot, _borderColor);
            border.anchorMin = border.anchorMax = new Vector2(0f, 0.5f);
            border.pivot = new Vector2(0f, 0.5f);
            border.sizeDelta = _barSize;
            var track = CreateImage("Track", border, _trackColor);
            Stretch(track, 2f);
            _fill = CreateImage("Fill", track, _fillColor);
            Stretch(_fill, 0f);

            // Dùng cùng phông dựng sẵn với giao diện thẻ, không cần tài nguyên TMP bổ sung.
            var go = new GameObject("HealthValue", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(track, false);
            _label = go.GetComponent<UnityEngine.UI.Text>();
            Stretch(_label.rectTransform, 0f);
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 18;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color32(244, 234, 218, 255);
            _label.raycastTarget = false;
            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = _trackColor;
            shadow.effectDistance = new Vector2(1f, -1f);
            Refresh(0, 0);
        }

        private void Update()
        {
            if (_tracked == null)
            {
                for (int i = 0; i < PlayerRegistry.Count; i++)
                {
                    PlayerCharacter player = PlayerRegistry.All[i];
                    if (player == null || !player.isLocalPlayer || player.Data == null) continue;
                    if (!player.TryGetComponent(out PlayerHealth health)) continue;
                    _tracked = health;
                    break;
                }
            }

            // Hook máu có thể tới trước máu tối đa; đọc sau khi cả gói SyncVar đã áp xong.
            int current = _tracked != null ? _tracked.Health : 0;
            int maximum = _tracked != null ? _tracked.MaxHealth : 0;
            if (current != _current || maximum != _maximum) Refresh(current, maximum);
        }

        private void Refresh(int current, int maximum)
        {
            _current = current;
            _maximum = maximum;
            if (_fill == null) return;
            _fill.anchorMax = new Vector2(maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f, 1f);
            _label.text = maximum > 0 ? $"{current} / {maximum}" : "— / —";
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }
    }
}
