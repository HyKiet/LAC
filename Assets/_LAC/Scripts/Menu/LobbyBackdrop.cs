using UnityEngine;
using UnityEngine.UI;

namespace LAC.Menu
{
    /// <summary>Phông sân đình 2D dùng mesh UI, giữ bảng màu và không cần texture mới.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LobbyBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2 scale = new Vector2(r.width / 1280f, r.height / 720f);
            void Quad(float x, float y, float w, float h, string hex)
            {
                Color c = LobbyView.ColorHex(hex);
                int start = vh.currentVertCount;
                vh.AddVert(r.min + Vector2.Scale(new Vector2(x, y), scale), c, Vector2.zero);
                vh.AddVert(r.min + Vector2.Scale(new Vector2(x, y + h), scale), c, Vector2.zero);
                vh.AddVert(r.min + Vector2.Scale(new Vector2(x + w, y + h), scale), c, Vector2.zero);
                vh.AddVert(r.min + Vector2.Scale(new Vector2(x + w, y), scale), c, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
            void Ring(float cx, float cy, float radius, float thickness, string hex, float squash = 1f)
            {
                Color c = LobbyView.ColorHex(hex);
                for (int i = 0; i < 96; i++)
                {
                    float a = i * Mathf.PI * 2 / 96;
                    float b = (i + 1) * Mathf.PI * 2 / 96;
                    int start = vh.currentVertCount;
                    foreach (Vector2 point in new[] {
                        new Vector2(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius * squash),
                        new Vector2(cx + Mathf.Cos(b) * radius, cy + Mathf.Sin(b) * radius * squash),
                        new Vector2(cx + Mathf.Cos(b) * (radius-thickness), cy + Mathf.Sin(b) * (radius-thickness) * squash),
                        new Vector2(cx + Mathf.Cos(a) * (radius-thickness), cy + Mathf.Sin(a) * (radius-thickness) * squash) })
                        vh.AddVert(r.min + Vector2.Scale(point, scale), c, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start, start + 2, start + 3);
                }
            }
            Quad(0, 0, 1280, 720, "112E3E");
            Ring(640, 405, 176, 2, "1C4B5C");
            Ring(640, 405, 162, 8, "1C4B5C");
            Ring(640, 405, 143, 2, "2F7480");
            Ring(640, 405, 99, 1, "1C4B5C");
            for (int side = 0; side < 2; side++)
            {
                int x = side == 0 ? -50 : 1040;
                Quad(x + 45, 223, 190, 209, "15130F");
                Quad(x, 420, 290, 15, "47403A");
                Quad(x + 20, 435, 250, 15, "2B2724");
                Quad(x + 40, 450, 210, 15, "2B2724");
                Quad(x + 68, 465, 154, 15, "2B2724");
                Quad(x + 120, 480, 50, 12, "47403A");
                Quad(x + 65, 223, 15, 199, "50331E");
                Quad(x + 206, 223, 15, 199, "50331E");
                Quad(x + 25, 394, 10, 30, "C08D20");
                Quad(x + 253, 394, 10, 30, "C08D20");
            }
            Quad(0, 0, 1280, 224, "2B2724");
            Quad(0, 222, 1280, 4, "47403A");
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 13; col++)
                {
                    float x = col * 110 - (row % 2) * 55;
                    Quad(x, row * 44 + 5, 105, 2, "47403A");
                    Quad(x, row * 44 + 5, 2, 40, "47403A");
                }
            Ring(450, 265, 121, 118, "15130F", .20f);
            Ring(830, 265, 121, 118, "15130F", .20f);
            Ring(450, 266, 118, 2, "C08D20", .20f);
            Ring(830, 266, 118, 2, "2F7480", .20f);
            Quad(0, 0, 1280, 110, "15130F");
            Quad(40, 109, 1200, 1, "47403A");
            Quad(40, 681, 1200, 1, "47403A");
        }
    }
}
