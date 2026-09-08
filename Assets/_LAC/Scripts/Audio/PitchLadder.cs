using UnityEngine;

namespace LAC.Audio
{
    /// <summary>
    /// Chuỗi cao độ tăng dần theo số lần kích hoạt liên tiếp.
    /// </summary>
    /// <remarks>
    /// Nhặt Hồn là vòng phản hồi chu kỳ hai giây của cả ván — xem CLAUDE.md mục 2.4. Nếu mỗi
    /// lần nhặt phát cùng một tiếng thì sau đợt thứ ba nó thành tiếng ồn nền và người chơi
    /// thôi để ý. Cao độ dâng lên theo chuỗi biến một hành động lặp lại thành một câu nhạc
    /// ngắn có hướng đi, nên người chơi chủ động chạy vòng để gom cho hết chuỗi.
    ///
    /// Bậc được tính theo nửa cung của thang chia đều, nên chuỗi nghe đúng cao độ chứ không
    /// phải là tiếng bị kéo nhanh dần.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    public sealed class PitchLadder : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;

        [Tooltip("Số bậc tối đa. 11 là vừa đúng một quãng tám, lên nữa thì chói.")]
        [SerializeField, Min(0)] private int _maxSteps = 11;

        [Tooltip("Số nửa cung mỗi bậc.")]
        [SerializeField, Min(0.1f)] private float _semitonesPerStep = 1f;

        [Tooltip("Ngưng quá lâu thì chuỗi về bậc đầu.")]
        [SerializeField, Min(0.05f)] private float _resetAfter = 1.1f;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;

        private AudioSource _source;
        private int _step;
        private float _lastStepAt = float.NegativeInfinity;

        /// <summary>Bậc hiện tại của chuỗi. Dùng cho phần hiển thị nếu cần.</summary>
        public int Step => _step;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        /// <summary>Tiến một bậc và phát tiếng. Gọi mỗi lần nhặt được một Hồn.</summary>
        /// <remarks>
        /// Dùng <c>PlayOneShot</c> để hai lần nhặt sát nhau không cắt tiếng của nhau. Đổi lại,
        /// tiếng đang ngân sẽ bị kéo theo cao độ mới vì cao độ là thuộc tính của nguồn phát
        /// chứ không của từng lần phát. Với tiếng ngắn dưới 0.2 giây thì không nghe ra.
        /// </remarks>
        public void Advance()
        {
            _step = Time.time - _lastStepAt > _resetAfter ? 0 : Mathf.Min(_step + 1, _maxSteps);
            _lastStepAt = Time.time;

            if (_clip == null || _source == null) return;

            _source.pitch = Mathf.Pow(2f, _step * _semitonesPerStep / 12f);
            _source.PlayOneShot(_clip, _volume);
        }

        /// <summary>
        /// Đưa chuỗi về bậc đầu. Gọi khi bắt đầu một đợt hoặc một ván mới.
        /// </summary>
        /// <remarks>
        /// Không đặt tên là <c>Reset</c>: Unity coi đó là hàm gọi lại của Editor và sẽ chạy nó
        /// mỗi lần thêm thành phần hoặc bấm Reset trong Inspector.
        /// </remarks>
        public void ResetLadder()
        {
            _step = 0;
            _lastStepAt = float.NegativeInfinity;
        }
    }
}
