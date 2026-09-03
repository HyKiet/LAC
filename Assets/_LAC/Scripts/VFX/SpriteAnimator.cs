using UnityEngine;

namespace LAC.VFX
{
    /// <summary>
    /// Chạy một <see cref="SpriteAnimationSet"/> lên một <see cref="SpriteRenderer"/>.
    /// </summary>
    /// <remarks>
    /// <b>Chỉ ghi vào <c>sprite</c>.</b> Không đụng tới <c>color</c> — đó là của
    /// <see cref="SpriteFlash"/> — và không đụng tới <c>flipX</c> — đó là của
    /// <c>PlayerMovement</c>. Ba thành phần cùng ghi lên một renderer, mỗi thành phần
    /// một thuộc tính, nên không cần thứ tự thực thi nào giữa chúng.
    ///
    /// <b>Dùng đồng hồ có tỉ lệ.</b> <c>Time.deltaTime</c> chứ không phải
    /// <c>unscaledDeltaTime</c>: hit-stop của T-15 hạ <c>Time.timeScale</c> để đóng băng
    /// khoảnh khắc chạm, và nhân vật đứng hình trong khoảnh khắc đó chính là tác dụng cần
    /// có. Chớp sáng và rung màn thì ngược lại, chúng phải chạy tiếp nên dùng đồng hồ
    /// không tỉ lệ.
    ///
    /// Không có gì đồng bộ qua mạng ở đây. Hoạt ảnh là biểu diễn thuần tuý, thuộc nhóm
    /// "không đồng bộ" trong bảng ở CLAUDE.md mục 3.2 — mỗi máy tự suy ra từ trạng thái
    /// nó đã có.
    /// </remarks>
    public sealed class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Bộ hoạt ảnh mặc định. Có thể thay lúc chạy qua SetAnimationSet.")]
        [SerializeField] private SpriteAnimationSet _set;

        private SpriteAnimationClip _clip;
        private AnimState _state = AnimState.Idle;
        private AnimState _baseState = AnimState.Idle;

        private int _frame;
        private float _timer;
        private float _speedScale = 1f;
        private bool _oneShot;
        private bool _finished;
        private bool _locked;
        private bool _warnedTooLong;

        /// <summary>Trạng thái đang chạy.</summary>
        public AnimState State => _state;

        /// <summary>Có bộ hoạt ảnh để chạy hay không. Sai thì renderer giữ nguyên sprite tĩnh.</summary>
        public bool HasSet => _set != null;

        /// <summary>Đang chạy một lượt không lặp và chưa xong.</summary>
        public bool IsBusy => _oneShot && !_finished;

        private void Reset() => _renderer = GetComponent<SpriteRenderer>();

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Gán bộ hoạt ảnh. Gọi lại với cùng một bộ không gây tác dụng phụ.
        /// </summary>
        /// <remarks>
        /// Nhận null là hợp lệ: khi gói asset thử nghiệm bị gỡ, tham chiếu trong
        /// <c>CharacterData</c> thành null và thành phần này phải lặng lẽ nhường lại
        /// sprite tĩnh, không được ném lỗi.
        /// </remarks>
        public void SetAnimationSet(SpriteAnimationSet set)
        {
            if (_set == set) return;

            _set = set;
            _locked = false;
            _oneShot = false;
            _warnedTooLong = false;
            _clip = null;

            if (_set != null) Begin(_baseState, force: true);
        }

        /// <summary>Trạng thái nền — Idle hoặc Walk. Bị hoãn nếu đang chạy một lượt dở dang.</summary>
        public void SetBaseState(AnimState state)
        {
            _baseState = state;
            if (_locked || IsBusy) return;
            Begin(state, force: false);
        }

        /// <summary>
        /// Chạy một lượt rồi trả về trạng thái nền.
        /// </summary>
        /// <param name="maxDuration">
        /// Trần thời lượng, tính bằng giây. Lớn hơn 0 thì clip bị ép chạy nhanh lên cho vừa.
        /// Dùng cho đòn đánh: nếu hoạt ảnh dài hơn chu kỳ khai hoả thì đòn sau cắt ngang đòn
        /// trước và động tác không bao giờ chạy hết.
        /// </param>
        public void PlayOneShot(AnimState state, float maxDuration = 0f)
        {
            if (_locked || _set == null) return;

            SpriteAnimationClip clip = _set.Get(state);
            if (clip == null) return;

            _speedScale = 1f;
            if (maxDuration > 0f && clip.Duration > maxDuration)
            {
                _speedScale = clip.Duration / maxDuration;
                if (!_warnedTooLong)
                {
                    _warnedTooLong = true;
                    Debug.LogWarning(
                        $"[SpriteAnimator] Hoạt ảnh {state} dài {clip.Duration:0.00}s, " +
                        $"vượt trần {maxDuration:0.00}s. Đang chạy nhanh lên {_speedScale:0.00}×.",
                        this);
                }
            }

            _oneShot = true;
            Begin(state, force: true);
        }

        /// <summary>
        /// Khoá vào một trạng thái cho tới khi <see cref="Unlock"/> được gọi. Dùng cho cái chết.
        /// </summary>
        public void Lock(AnimState state)
        {
            if (_set == null) return;
            if (_locked && _state == state) return;

            _locked = false;
            _oneShot = false;
            Begin(state, force: true);
            _locked = true;
        }

        /// <summary>
        /// Dọn sạch mọi trạng thái và trở về trạng thái nền.
        /// </summary>
        /// <remarks>
        /// Cố ý không kiểm tra <c>_locked</c> trước khi dọn. Quái đi qua object pool: con vừa
        /// chết đang khoá ở khung cuối của hoạt ảnh chết, và cùng đối tượng đó sẽ được dùng
        /// lại cho con tiếp theo. Chỉ gỡ khoá thôi thì một lượt đánh dở dang còn sót lại vẫn
        /// chặn trạng thái nền, và con mới hiện ra giữa chừng một cú vung roi.
        /// </remarks>
        public void Unlock()
        {
            _locked = false;
            _oneShot = false;
            _speedScale = 1f;
            Begin(_baseState, force: true);
        }

        private void Begin(AnimState state, bool force)
        {
            if (_set == null) return;
            if (!force && _state == state && _clip != null) return;

            SpriteAnimationClip clip = _set.Get(state);

            // Không có clip cho trạng thái này thì giữ nguyên những gì đang hiện. Bộ hoạt ảnh
            // chỉ có Idle và Walk vẫn phải dùng được, không được nhấp nháy về sprite rỗng.
            if (clip == null)
            {
                _oneShot = false;
                _finished = true;
                return;
            }

            _clip = clip;
            _state = state;
            _frame = 0;
            _timer = 0f;
            _finished = false;
            if (!_oneShot) _speedScale = 1f;

            ApplyFrame();
        }

        private void Update()
        {
            if (_clip == null || _finished) return;

            float step = _clip.Fps * _speedScale;
            if (step <= 0f) return;

            _timer += Time.deltaTime * step;
            if (_timer < 1f) return;

            int advance = Mathf.FloorToInt(_timer);
            _timer -= advance;
            _frame += advance;

            int count = _clip.FrameCount;
            if (_frame < count)
            {
                ApplyFrame();
                return;
            }

            if (_clip.Loop)
            {
                _frame %= count;
                ApplyFrame();
                return;
            }

            _frame = count - 1;
            ApplyFrame();
            _finished = true;

            if (_clip.HoldLastFrame || _locked) return;

            _oneShot = false;
            Begin(_baseState, force: true);
        }

        private void ApplyFrame()
        {
            if (_renderer == null) return;
            _renderer.sprite = _clip.Frames[_frame];
        }
    }
}
