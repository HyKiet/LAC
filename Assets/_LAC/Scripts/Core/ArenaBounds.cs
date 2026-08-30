using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Kích thước và giới hạn của đấu trường. Nguồn duy nhất cho mọi hệ thống cần biết
    /// "bên trong sân là ở đâu".
    /// </summary>
    /// <remarks>
    /// Camera, bộ sinh quái và đạn nảy tường đều cần cùng một con số. Để mỗi hệ thống tự
    /// giữ một bản sao thì chỉnh kích thước sân một lần phải sửa ba chỗ, và chỗ quên sửa
    /// sẽ biểu hiện thành quái sinh ngoài tường — một lỗi trông như lỗi AI chứ không như
    /// lỗi cấu hình.
    ///
    /// Kích thước là dữ liệu của scene chứ không phải của mạng: hai máy đọc cùng một scene
    /// nên không cần đồng bộ.
    /// </remarks>
    [ExecuteAlways]
    public sealed class ArenaBounds : MonoBehaviour
    {
        public static ArenaBounds Instance { get; private set; }

        [Tooltip("Chiều rộng và chiều cao của vùng chơi, tính bằng đơn vị thế giới.")]
        [SerializeField] private Vector2 _size = new Vector2(36f, 20f);

        /// <summary>Vùng chơi, tính từ tâm của đối tượng này.</summary>
        public Rect Rect => new Rect((Vector2)transform.position - _size * 0.5f, _size);

        public Vector2 Size => _size;

        private void OnEnable() => Instance = this;

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Kéo một điểm về bên trong sân, chừa lại một khoảng lề.</summary>
        public Vector2 Clamp(Vector2 point, Vector2 margin)
        {
            Rect r = Rect;

            // Lề rộng hơn nửa sân thì kẹp về đúng tâm thay vì cho ra khoảng âm.
            float halfX = Mathf.Max(r.width * 0.5f - margin.x, 0f);
            float halfY = Mathf.Max(r.height * 0.5f - margin.y, 0f);
            Vector2 center = r.center;

            return new Vector2(
                Mathf.Clamp(point.x, center.x - halfX, center.x + halfX),
                Mathf.Clamp(point.y, center.y - halfY, center.y + halfY));
        }

        public bool Contains(Vector2 point) => Rect.Contains(point);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, _size);
        }
    }
}
