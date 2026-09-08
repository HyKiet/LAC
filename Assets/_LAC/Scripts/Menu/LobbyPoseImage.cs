using UnityEngine;
using UnityEngine.UI;

namespace LAC.Menu
{
    /// <summary>Căn dáng theo mesh thật, bỏ khoảng trống của frame 100 px trong asset thử nghiệm.</summary>
    public sealed class LobbyPoseImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Sprite current = overrideSprite;
            if (current == null) { base.OnPopulateMesh(vh); return; }
            vh.Clear();
            Vector2[] vertices = current.vertices;
            Vector2[] uv = current.uv;
            ushort[] triangles = current.triangles;
            if (vertices.Length == 0) return;
            Vector2 min = vertices[0], max = vertices[0];
            foreach (Vector2 vertex in vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); }
            Vector2 size = max - min;
            Rect rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width / Mathf.Max(size.x, .001f), rect.height / Mathf.Max(size.y, .001f));
            Vector2 center = (min + max) * .5f;
            for (int i = 0; i < vertices.Length; i++)
                vh.AddVert(rect.center + (vertices[i] - center) * scale, color, uv[i]);
            for (int i = 0; i < triangles.Length; i += 3)
                vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
        }
    }
}
