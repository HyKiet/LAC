using System.Collections.Generic;
using LAC.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LAC.UI
{
    /// <summary>
    /// Màn hình hiện ra khi ván kết thúc, và là lối vào của ván tiếp theo.
    /// </summary>
    /// <remarks>
    /// <b>Đây là phần đóng vòng lặp, không phải màn thống kê sau ván.</b> Màn thống kê đầy
    /// đủ là T-49. Ở đây chỉ có ba thứ người chơi cần ngay: thắng hay thua, đi được tới đợt
    /// nào, và làm sao chơi lại. Không có nó thì ván thua là ngõ cụt — cách duy nhất chơi
    /// lại là tắt ứng dụng, và bài thực nghiệm ở T-53 cần ba mươi lượt chơi liên tiếp.
    ///
    /// Tiến trình hiển thị bằng <b>một hàng ô, mỗi ô một đợt</b>, cùng cách với thanh máu ở
    /// T-15B và vì cùng một lý do: người chơi đếm được "tôi qua được 7 trên 16" nhanh hơn
    /// đọc một con số, và thấy luôn còn bao xa nữa.
    ///
    /// Màu ở đây <b>không được dùng nhóm son</b> dù đây là giao diện — nhóm son dành riêng
    /// cho đòn tấn công của địch, xem docs/PALETTE.md. Thắng dùng nhóm hoè, thua dùng nhóm
    /// than.
    /// </remarks>
    public sealed class RunEndScreen : MonoBehaviour
    {
        [Header("Khung")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _dim;
        [SerializeField] private Image _title;
        [SerializeField] private Image _prompt;

        [Header("Tiến trình")]
        [SerializeField] private RectTransform _pipRow;
        [SerializeField] private Sprite _pipFull;
        [SerializeField] private Sprite _pipEmpty;
        [SerializeField, Min(4f)] private float _pipSize = 14f;
        [SerializeField, Min(0f)] private float _pipSpacing = 3f;

        [Header("Chữ")]
        [SerializeField] private Sprite _victorySprite;
        [SerializeField] private Sprite _defeatSprite;
        [SerializeField] private Sprite _promptSprite;

        [Header("Màu — theo bảng Đông Hồ, không dùng nhóm son")]
        [SerializeField] private Color _victoryColor = new Color(0.929f, 0.733f, 0.243f);  // Hoe
        [SerializeField] private Color _defeatColor = new Color(0.431f, 0.396f, 0.333f);   // ThanNhat
        [SerializeField] private Color _pipEmptyColor = new Color(0.169f, 0.153f, 0.141f); // ThanDam
        [SerializeField] private Color _dimColor = new Color(0.082f, 0.075f, 0.059f, 0.82f);

        [Tooltip("Khoá phím một lúc sau khi ván kết thúc, tránh bấm nhầm ngay khi vừa chết.")]
        [SerializeField, Min(0f)] private float _inputLockout = 0.8f;

        private readonly List<Image> _pips = new List<Image>(16);
        private RunManager _run;
        private InputAction _restart;
        private float _armedAt;
        private bool _shown;

        /// <remarks>
        /// Hành động bấm phím dựng bằng mã chứ không lấy từ <c>LACControls</c>. Hai lý do:
        /// tài sản đó bị nhân bản cho từng người chơi nên không có bản nào là "của giao
        /// diện"; và <c>PlayerInputReader</c> bị tắt khi người chơi gục — đúng lúc cần bấm
        /// nhất thì nó không còn nghe.
        /// </remarks>
        private void Awake()
        {
            _restart = new InputAction("RestartRun", InputActionType.Button);
            _restart.AddBinding("<Keyboard>/r");
            _restart.AddBinding("<Keyboard>/enter");
            _restart.AddBinding("<Gamepad>/start");

            if (_dim != null) _dim.color = _dimColor;
            if (_prompt != null && _promptSprite != null) _prompt.sprite = _promptSprite;

            Hide();
        }

        private void OnEnable() => _restart.Enable();

        private void OnDisable()
        {
            _restart.Disable();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _restart?.Dispose();
        }

        private void Update()
        {
            // Bám muộn: RunManager là đối tượng mạng, Mirror tắt rồi bật lại nó khi host khởi
            // động nên tham chiếu lấy trong Awake có thể trỏ vào đối tượng đã chết.
            if (_run == null)
            {
                RunManager found = RunManager.Instance;
                if (found == null) return;

                _run = found;
                _run.RunEnded += OnRunEnded;
                _run.RunStarted += Hide;

                // Vào giữa chừng một ván đã kết thúc thì vẫn phải thấy màn hình.
                if (_run.IsOver) OnRunEnded(_run.State == RunState.Victory);
            }

            if (!_shown || Time.unscaledTime < _armedAt) return;
            if (!_restart.WasPressedThisFrame()) return;

            // Client xin, host thi hành — mục 3.2. Host cũng đi qua đúng con đường này, nên
            // không có nhánh riêng cho chế độ chơi đơn.
            _run.CmdRequestRestart();
        }

        private void Unsubscribe()
        {
            if (_run == null) return;
            _run.RunEnded -= OnRunEnded;
            _run.RunStarted -= Hide;
            _run = null;
        }

        private void OnRunEnded(bool victory)
        {
            _shown = true;
            _armedAt = Time.unscaledTime + _inputLockout;

            if (_root != null) _root.SetActive(true);
            if (_title != null)
            {
                _title.sprite = victory ? _victorySprite : _defeatSprite;
                _title.color = victory ? _victoryColor : Color.white;
                _title.SetNativeSize();
            }
            if (_prompt != null) _prompt.SetNativeSize();

            BuildPips(victory ? _victoryColor : _defeatColor);
        }

        private void Hide()
        {
            _shown = false;
            if (_root != null) _root.SetActive(false);
        }

        private void BuildPips(Color fill)
        {
            if (_pipRow == null || _run == null) return;

            int total = Mathf.Max(_run.TotalWaves, 1);
            int cleared = Mathf.Clamp(_run.WavesCleared, 0, total);

            while (_pips.Count < total)
            {
                var go = new GameObject("Pip" + _pips.Count, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_pipRow, false);
                _pips.Add(go.GetComponent<Image>());
            }

            float width = total * _pipSize + (total - 1) * _pipSpacing;

            for (int i = 0; i < _pips.Count; i++)
            {
                Image pip = _pips[i];
                bool used = i < total;
                pip.gameObject.SetActive(used);
                if (!used) continue;

                bool done = i < cleared;
                pip.sprite = done ? _pipFull : _pipEmpty;
                pip.color = done ? fill : _pipEmptyColor;

                var rect = pip.rectTransform;
                rect.sizeDelta = new Vector2(_pipSize, _pipSize);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(
                    -width * 0.5f + _pipSize * 0.5f + i * (_pipSize + _pipSpacing), 0f);
            }
        }
    }
}
