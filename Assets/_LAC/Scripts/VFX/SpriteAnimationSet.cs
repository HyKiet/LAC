using System;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Các trạng thái hoạt ảnh mà một nhân vật hoặc quái có thể ở trong.</summary>
    public enum AnimState
    {
        Idle = 0,
        Walk = 1,
        Attack = 2,
        Hurt = 3,
        Death = 4
    }

    /// <summary>Một chuỗi khung hình cùng nhịp phát.</summary>
    [Serializable]
    public sealed class SpriteAnimationClip
    {
        [SerializeField] private AnimState _state = AnimState.Idle;
        [SerializeField] private Sprite[] _frames;

        [Tooltip("Số khung hình mỗi giây.")]
        [SerializeField, Range(1f, 60f)] private float _fps = 12f;

        [SerializeField] private bool _loop = true;

        [Tooltip("Khi hết chuỗi thì đứng lại ở khung cuối thay vì trả về trạng thái nền. Dùng cho lúc chết.")]
        [SerializeField] private bool _holdLastFrame;

        public AnimState State => _state;
        public Sprite[] Frames => _frames;
        public float Fps => _fps;
        public bool Loop => _loop;
        public bool HoldLastFrame => _holdLastFrame;

        public int FrameCount => _frames != null ? _frames.Length : 0;
        public bool IsValid => FrameCount > 0;

        /// <summary>Thời lượng một lượt chạy, tính bằng giây.</summary>
        public float Duration => _fps <= 0f ? 0f : FrameCount / _fps;
    }

    /// <summary>
    /// Toàn bộ hoạt ảnh của một nhân vật, gom thành một tài sản.
    /// </summary>
    /// <remarks>
    /// Cố ý <b>không</b> dùng <c>AnimatorController</c> của Unity. Ba lý do:
    ///
    /// Một, <c>.controller</c> và <c>.anim</c> là tệp YAML — đúng loại tệp mà CLAUDE.md
    /// mục 6.2 gọi là nguồn xung đột nghiêm trọng nhất của dự án, và chỉ sửa được trong
    /// cửa sổ Animator nên không ai đọc được diff. ScriptableObject cũng là YAML nhưng
    /// phẳng, một trường một dòng, merge được bằng mắt.
    ///
    /// Hai, mục 5 bắt buộc mọi nội dung game phải là ScriptableObject trong <c>Data/</c>.
    /// <c>.controller</c> không phải ScriptableObject.
    ///
    /// Ba, ngân sách là 40 quái ở 60 FPS. Mỗi <c>Animator</c> dựng một playable graph
    /// riêng; ở đây nhu cầu chỉ là năm chuỗi khung, không blend, không transition có điều
    /// kiện — một phép cộng float và một phép gán <c>sprite</c> là đủ.
    /// </remarks>
    [CreateAssetMenu(fileName = "SpriteAnimationSet", menuName = "LAC/Sprite Animation Set")]
    public sealed class SpriteAnimationSet : ScriptableObject
    {
        [SerializeField] private SpriteAnimationClip[] _clips;

        [Header("Tuỳ chọn")]
        [Tooltip("Chạy hoạt ảnh trúng đòn. Tắt khi đối tượng đã có SpriteFlash — hai thứ " +
                 "cùng nhuộm đỏ sẽ chớp hai lần lệch nhịp.")]
        [SerializeField] private bool _playHurtClip;

        public bool PlayHurtClip => _playHurtClip;

        /// <summary>
        /// Clip của một trạng thái, hoặc null nếu tài sản không định nghĩa trạng thái đó.
        /// </summary>
        /// <remarks>
        /// Trả null là kết quả hợp lệ, không phải lỗi. Bộ hoạt ảnh có thể chỉ có Idle và
        /// Walk; phần còn lại do <see cref="SpriteAnimator"/> tự lùi về trạng thái nền.
        /// </remarks>
        public SpriteAnimationClip Get(AnimState state)
        {
            if (_clips == null) return null;

            for (int i = 0; i < _clips.Length; i++)
            {
                SpriteAnimationClip clip = _clips[i];
                if (clip != null && clip.State == state && clip.IsValid) return clip;
            }

            return null;
        }
    }
}
