using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Dừng hình cực ngắn khi một cú đánh có sức nặng.</summary>
    /// <remarks>
    /// Hit-stop là thủ pháp làm cú đánh có cảm giác chạm vào vật thể thật thay vì xuyên qua
    /// không khí. Nó tác động lên <c>Time.timeScale</c>, tức là ảnh hưởng toàn bộ mô phỏng.
    ///
    /// <b>Vì sao phải giữ rất ngắn.</b> Trên host, mô phỏng chậm lại đồng nghĩa với trạng
    /// thái có thẩm quyền chậm lại, và client sẽ thấy đàn quái khựng theo. Giữ dưới một nhịp
    /// gửi mạng (33 ms ở sendRate 30) thì độ lệch tan trước khi snapshot kế tiếp được gửi,
    /// nên không tích luỹ thành sai lệch vị trí.
    ///
    /// Cũng vì lý do đó, hit-stop chỉ dùng cho các sự kiện thưa — quái chết, người chơi trúng
    /// đòn — chứ không dùng cho mỗi lần đạn chạm.
    /// </remarks>
    public static class HitStop
    {
        private const float MaxDuration = 0.03f;

        private static float _resumeAt;
        private static bool _active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = false;
            _resumeAt = 0f;
        }

        /// <summary>Yêu cầu dừng hình. Lời gọi chồng lên nhau lấy hạn xa nhất, không cộng dồn.</summary>
        public static void Request(float duration)
        {
            float until = Time.unscaledTime + Mathf.Min(duration, MaxDuration);
            if (until <= _resumeAt) return;

            _resumeAt = until;

            if (_active) return;
            _active = true;
            Time.timeScale = 0f;
        }

        /// <summary>Gọi mỗi khung hình từ một thành phần trong scene.</summary>
        public static void Tick()
        {
            if (!_active || Time.unscaledTime < _resumeAt) return;

            _active = false;
            Time.timeScale = 1f;
        }
    }
}
