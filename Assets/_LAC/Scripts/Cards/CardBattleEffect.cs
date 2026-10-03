using LAC.Core;
using LAC.Player;
using LAC.VFX;
using UnityEngine;

namespace LAC.Cards
{
    /// <summary>Nét đồng/ngọc mảnh; dùng lại mesh và pool, không tham gia mô phỏng chiến đấu.</summary>
    public sealed class CardBattleEffect : MonoBehaviour, IPoolable
    {
        public enum Kind { Upgrade, Heal, Wind, Shield }
        [SerializeField] private MeshFilter _filter;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private PixelNumber _number;
        private static readonly int Tint = Shader.PropertyToID("_Color");
        private readonly Vector3[] _vertices = new Vector3[256];
        private readonly int[] _indices = new int[384];
        private Mesh _mesh;
        private MaterialPropertyBlock _properties;
        private ObjectPool<CardBattleEffect> _owner;
        private PlayerCharacter _target;
        private Kind _kind;
        private Color _color;
        private float _age, _duration, _width, _anchorY;
        private int _count;
        public Kind EffectKind => _kind;

        private void Awake()
        {
            _mesh = new Mesh { name = "Card feedback strokes" };
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;
            _properties = new MaterialPropertyBlock();
            var colors = new Color32[_vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color32(255, 255, 255, 255);
            for (int i = 0; i < 64; i++)
            {
                int v = i * 4, t = i * 6;
                _indices[t] = v; _indices[t + 1] = v + 1; _indices[t + 2] = v + 2;
                _indices[t + 3] = v; _indices[t + 4] = v + 2; _indices[t + 5] = v + 3;
            }
            _mesh.vertices = _vertices;
            _mesh.colors32 = colors;
            _mesh.triangles = _indices;
        }

        public void Play(ObjectPool<CardBattleEffect> owner, CardBattleFeedbackData data,
            Kind kind, PlayerCharacter target, Vector2 direction, int healAmount = 0)
        {
            _owner = owner; _target = target; _kind = kind; _age = 0f;
            _duration = data.SecondsFor(kind); _width = data.StrokeWidth;
            _color = data.ColorFor(kind);
            _color.a = Mathf.Clamp(_color.a, 0f, .22f);
            _anchorY = -.05f;
            if (kind == Kind.Heal && target != null)
            {
                var body = target.GetComponent<SpriteRenderer>();
                float top = body != null ? body.bounds.max.y - target.transform.position.y : 1.25f;
                // Sprite thử nghiệm có nhiều viền trong suốt; không để số bay xa khỏi người.
                _anchorY = Mathf.Clamp(top, .9f, 1.65f) - .5f;
            }
            transform.rotation = kind == Kind.Wind
                ? Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
                : Quaternion.identity;
            System.Array.Clear(_vertices, 0, _vertices.Length);
            _count = 0;
            BuildStrokes();
            _mesh.vertices = _vertices;
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(4, 4, 1));
            _number.gameObject.SetActive(kind == Kind.Heal);
            if (kind == Kind.Heal) _number.SetValue(healAmount);
            Present(0f);
        }

        public void OnSpawned() => _age = 0f;
        public void OnDespawned()
        {
            _owner = null; _target = null;
            _number.gameObject.SetActive(false);
            transform.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            if (_owner == null) return;
            if (_target == null || !_target.IsAlive || RunManager.Instance == null || RunManager.Instance.IsOver)
            { _owner.Release(this); return; }
            _age += Time.deltaTime;
            if (_age >= _duration) { _owner.Release(this); return; }
            Present(_age / _duration);
        }

        private void Present(float t)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, 1f, t));
            bool visible = RunManager.Instance != null && RunManager.Instance.State == RunState.WaveActive;
            _renderer.enabled = visible;
            _number.gameObject.SetActive(visible && _kind == Kind.Heal);
            if (_kind != Kind.Wind && _target != null)
                transform.position = _target.transform.position + Vector3.up * _anchorY
                    + (_kind == Kind.Heal ? Vector3.up * t * .65f : Vector3.zero);
            float scale = _kind == Kind.Upgrade ? Mathf.Lerp(.65f, 1.15f, t) : 1f;
            transform.localScale = _kind == Kind.Shield ? new Vector3(1.4f, 1.8f, 1f) : Vector3.one * scale;
            Color tint = _color; tint.a *= fade;
            _properties.SetColor(Tint, tint);
            _renderer.SetPropertyBlock(_properties);
            if (_kind == Kind.Heal)
                _number.SetColor(new Color(_color.r, _color.g, _color.b, 1f - t * t));
        }

        private void BuildStrokes()
        {
            switch (_kind)
            {
                case Kind.Upgrade:
                    for (int i = 0; i < 24; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 24;
                        Line(Polar(angle, .78f), Polar(angle + .19f, .78f));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 p = Polar(i * Mathf.PI * .5f, .95f);
                        Line(p + Vector2.up * .12f, p + Vector2.right * .09f);
                        Line(p + Vector2.right * .09f, p + Vector2.down * .12f);
                        Line(p + Vector2.down * .12f, p + Vector2.left * .09f);
                        Line(p + Vector2.left * .09f, p + Vector2.up * .12f);
                    }
                    break;
                case Kind.Heal:
                    Cross(new Vector2(-.28f, .75f), .09f);
                    Cross(new Vector2(-.48f, .28f), .075f);
                    Cross(new Vector2(.48f, .42f), .06f);
                    break;
                case Kind.Shield:
                    Line(new Vector2(-.42f, .65f), new Vector2(0, .76f));
                    Line(new Vector2(0, .76f), new Vector2(.42f, .65f));
                    Line(new Vector2(.42f, .65f), new Vector2(.36f, .15f));
                    Line(new Vector2(.36f, .15f), new Vector2(0, -.15f));
                    Line(new Vector2(0, -.15f), new Vector2(-.36f, .15f));
                    Line(new Vector2(-.36f, .15f), new Vector2(-.42f, .65f));
                    break;
                case Kind.Wind:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = (i - 1) * .16f;
                        Line(new Vector2(-.72f, y), new Vector2(-.4f, y - .09f));
                        Line(new Vector2(-.4f, y - .09f), new Vector2(-.1f, y - .035f));
                    }
                    break;
            }
        }

        private void Cross(Vector2 p, float radius)
        { Line(p - Vector2.right * radius, p + Vector2.right * radius); Line(p - Vector2.up * radius, p + Vector2.up * radius); }
        private static Vector2 Polar(float angle, float radius) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        private void Line(Vector2 a, Vector2 b)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * (_width * .5f);
            _vertices[_count++] = a + n; _vertices[_count++] = b + n;
            _vertices[_count++] = b - n; _vertices[_count++] = a - n;
        }
        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}
