using UnityEngine;
using UnityEngine.UI;

namespace LAC.Cards
{
    /// <summary>Hoa văn đồng/ngọc sau ảnh; chuyển động chỉ trên nét trang trí, không trên chữ.</summary>
    public sealed class CardRitualGraphic : MaskableGraphic
    {
        private bool _animated;
        private float _elapsed;

        public void Configure(bool animated)
        {
            raycastTarget = false;
            _animated = animated;
            Restart();
        }

        public void Restart()
        {
            _elapsed = 0f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!_animated) return;
            _elapsed += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .46f;
            if (radius < 12f) return;
            Vector2 center = rect.center;
            float turn = _animated ? _elapsed * .12f : 0f;
            float reveal = _animated ? Mathf.SmoothStep(.2f, 1f, Mathf.Clamp01(_elapsed / .45f)) : 1f;
            Ring(vh, center, radius * .72f, 64, 0f, 1f, .50f * reveal);
            Ring(vh, center, radius * .82f, 48, -turn, .65f, .70f * reveal);
            Ring(vh, center, radius, 32, turn, .42f, .45f * reveal);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f + turn;
                Vector2 p = center + Polar(angle, radius * .91f);
                Diamond(vh, p, Mathf.Max(2f, radius * .025f), reveal);
                Line(vh, center + Polar(angle, radius * .19f),
                    center + Polar(angle, radius * .43f), .55f * reveal);
            }
            Ring(vh, center, radius * .15f, 16, 0f, 1f, .80f * reveal);
            Diamond(vh, center, radius * .07f, reveal);
        }

        private void Ring(VertexHelper vh, Vector2 center, float radius, int segments,
            float turn, float fraction, float opacity)
        {
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
                Line(vh, center + Polar(i * step + turn, radius),
                    center + Polar((i + fraction) * step + turn, radius), opacity);
        }

        private void Diamond(VertexHelper vh, Vector2 p, float size, float opacity)
        {
            Line(vh, p + Vector2.up * size, p + Vector2.right * size, opacity);
            Line(vh, p + Vector2.right * size, p + Vector2.down * size, opacity);
            Line(vh, p + Vector2.down * size, p + Vector2.left * size, opacity);
            Line(vh, p + Vector2.left * size, p + Vector2.up * size, opacity);
        }

        private void Line(VertexHelper vh, Vector2 a, Vector2 b, float opacity)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * .75f;
            int index = vh.currentVertCount;
            Color tint = color;
            tint.a *= opacity;
            Vertex(vh, a + normal, tint); Vertex(vh, b + normal, tint);
            Vertex(vh, b - normal, tint); Vertex(vh, a - normal, tint);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        private static void Vertex(VertexHelper vh, Vector2 p, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = p;
            vertex.color = tint;
            vh.AddVert(vertex);
        }

        private static Vector2 Polar(float angle, float radius) =>
            new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
