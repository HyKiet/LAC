using UnityEngine;
using UnityEngine.UI;

namespace LAC.Cards
{
    /// <summary>Khung đồng vát góc; lóe sáng chỉ nằm trên viền, không phủ ảnh hoặc chữ.</summary>
    public sealed class CardFrameGraphic : MaskableGraphic
    {
        private const float GlintDuration = 0.72f;
        private const float GlintInterval = 5.2f;
        private static readonly Color Bronze = Hex("C08D20");
        private static readonly Color BronzeShadow = Hex("8A5F14");
        private static readonly Color Gold = Hex("EDBB3E");
        private static readonly Color GoldLight = Hex("FBDD82");
        private static readonly Color Jade = Hex("2F7480");
        private static readonly Color JadeLight = Hex("9CCFC0");
        private bool _highlighted;
        private bool _glintEnabled;
        private float _untilGlint;
        private float _glintElapsed = GlintDuration;

        public void Configure(bool glintEnabled, float delay)
        {
            raycastTarget = false;
            _glintEnabled = glintEnabled;
            _untilGlint = delay;
            _glintElapsed = GlintDuration;
            SetVerticesDirty();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            if (highlighted) PlayGlint();
            SetVerticesDirty();
        }

        public void PlayGlint()
        {
            if (!_glintEnabled) return;
            _glintElapsed = 0f;
            _untilGlint = GlintInterval;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!_glintEnabled) return;
            _untilGlint -= Time.unscaledDeltaTime;
            if (_untilGlint <= 0f) PlayGlint();
            if (_glintElapsed >= GlintDuration) return;
            _glintElapsed = Mathf.Min(GlintDuration, _glintElapsed + Time.unscaledDeltaTime);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width < 40f || rect.height < 40f) return;
            DrawRing(vh, rect, 4f, 4f, 18f, 1f);
            DrawRing(vh, rect, 13f, 1.5f, 12f, 0.60f);

            Color trim = _highlighted ? JadeLight : Gold;
            float left = rect.xMin + 29f, right = rect.xMax - 29f;
            float bottom = rect.yMin + 8f, top = rect.yMax - 8f;
            Diamond(vh, new Vector2(rect.center.x, top), 7f, trim);
            Diamond(vh, new Vector2(rect.center.x, bottom), 5f, trim);
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? bottom : top;
                Bar(vh, new Rect(left, y - 1f, 25f, 2f), trim);
                Bar(vh, new Rect(right - 25f, y - 1f, 25f, 2f), trim);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? rect.xMin + 8f : rect.xMax - 8f;
                Bar(vh, new Rect(x - 1f, rect.yMin + 29f, 2f, 25f), trim);
                Bar(vh, new Rect(x - 1f, rect.yMax - 54f, 2f, 25f), trim);
            }
        }

        private void DrawRing(VertexHelper vh, Rect rect, float inset, float width, float cut, float alpha)
        {
            Rect outer = new Rect(rect.xMin + inset, rect.yMin + inset,
                rect.width - inset * 2f, rect.height - inset * 2f);
            Rect inner = new Rect(outer.xMin + width, outer.yMin + width,
                outer.width - width * 2f, outer.height - width * 2f);
            for (int edge = 0; edge < 8; edge++)
            {
                Vector2 a = Corner(outer, edge, cut), b = Corner(outer, (edge + 1) % 8, cut);
                Vector2 c = Corner(inner, edge, Mathf.Max(1f, cut - width));
                Vector2 d = Corner(inner, (edge + 1) % 8, Mathf.Max(1f, cut - width));
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 24f));
                Color shade = _highlighted ? Jade : edge == 4 || edge == 5 ? Gold
                    : edge == 0 || edge == 1 || edge == 2 ? BronzeShadow : Bronze;
                for (int step = 0; step < steps; step++)
                {
                    float start = (float)step / steps, end = (float)(step + 1) / steps;
                    Quad(vh, Vector2.Lerp(a, b, start), Vector2.Lerp(a, b, end),
                        Vector2.Lerp(c, d, end), Vector2.Lerp(c, d, start), shade, alpha, rect);
                }
            }
        }

        private void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color shade, float alpha, Rect rect)
        {
            int index = vh.currentVertCount;
            Vertex(vh, a, Shine(shade, a, rect), alpha);
            Vertex(vh, b, Shine(shade, b, rect), alpha);
            Vertex(vh, c, Shine(shade, c, rect), alpha);
            Vertex(vh, d, Shine(shade, d, rect), alpha);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        private Color Shine(Color shade, Vector2 point, Rect rect)
        {
            if (_glintElapsed >= GlintDuration) return shade;
            float t = _glintElapsed / GlintDuration;
            float location = (point.x - rect.xMin) / rect.width * 0.35f
                + (point.y - rect.yMin) / rect.height * 0.65f;
            float band = Mathf.Clamp01(1f - Mathf.Abs(location - Mathf.Lerp(-0.15f, 1.15f, t)) / 0.13f);
            return Color.Lerp(shade, _highlighted ? JadeLight : GoldLight,
                band * Mathf.Sin(t * Mathf.PI) * 0.48f);
        }

        private void Vertex(VertexHelper vh, Vector2 position, Color shade, float alpha)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            shade.a = alpha * color.a;
            vertex.color = shade;
            vh.AddVert(vertex);
        }

        private void Bar(VertexHelper vh, Rect rect, Color shade)
        {
            Quad(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax),
                shade, 0.80f, GetPixelAdjustedRect());
        }

        private void Diamond(VertexHelper vh, Vector2 center, float size, Color shade)
        {
            Quad(vh, center + Vector2.down * size, center + Vector2.right * size,
                center + Vector2.up * size, center + Vector2.left * size,
                shade, 0.85f, GetPixelAdjustedRect());
        }

        private static Vector2 Corner(Rect r, int index, float cut) => index switch
        {
            0 => new Vector2(r.xMin + cut, r.yMin),
            1 => new Vector2(r.xMax - cut, r.yMin),
            2 => new Vector2(r.xMax, r.yMin + cut),
            3 => new Vector2(r.xMax, r.yMax - cut),
            4 => new Vector2(r.xMax - cut, r.yMax),
            5 => new Vector2(r.xMin + cut, r.yMax),
            6 => new Vector2(r.xMin, r.yMax - cut),
            _ => new Vector2(r.xMin, r.yMin + cut)
        };

        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color result);
            return result;
        }
    }
}
