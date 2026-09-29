using LAC.Core;
using UnityEngine;

namespace LAC.UI
{
    /// <summary>Biểu diễn tiến trình đợt từ trạng thái đã đồng bộ của host.</summary>
    public sealed class WaveHud : MonoBehaviour
    {
        [SerializeField] private RectTransform _counterRoot;
        [SerializeField] private UnityEngine.UI.Text _counter;
        [SerializeField] private UnityEngine.UI.Text _status;
        [SerializeField] private CanvasGroup _bannerGroup;
        [SerializeField] private RectTransform _banner;
        [SerializeField] private UnityEngine.UI.Text _bannerTitle;
        [SerializeField] private UnityEngine.UI.Text _bannerSubtitle;
        [SerializeField, Min(0.01f)] private float _enterDuration = 0.28f;
        [SerializeField, Min(0f)] private float _holdDuration = 1.15f;
        [SerializeField, Min(0.01f)] private float _exitDuration = 0.32f;
        [SerializeField] private float _slideDistance = 140f;

        private RunManager _run;
        private Vector2 _bannerPosition;
        private int _wave = -1;
        private int _total = -1;
        private int _seed;
        private RunState _state = RunState.Idle;
        private float _announcedAt;
        private bool _animating;

        private void Awake()
        {
            _bannerPosition = _banner.anchoredPosition;
            _bannerGroup.interactable = false;
            _bannerGroup.blocksRaycasts = false;
            HideBanner();
            _counterRoot.gameObject.SetActive(false);
        }

        private void Start() => _run = RunManager.Instance;

        private void OnEnable() => _wave = -1;

        private void OnDisable() => HideBanner();

        private void Update()
        {
            if (_run == null) return;

            // Đọc sau SyncVar để không bỏ lỡ đợt đầu hoặc người vào giữa ván.
            if (_wave != _run.CurrentWave || _state != _run.State ||
                _seed != _run.Seed || _total != _run.TotalWaves)
                Refresh();

            if (_animating) Animate(Time.unscaledTime - _announcedAt);
        }

        private void Refresh()
        {
            bool announce = _run.State == RunState.WaveActive && _run.CurrentWave > 0 &&
                (_wave != _run.CurrentWave || _seed != _run.Seed || _state != RunState.WaveActive);
            _wave = _run.CurrentWave;
            _total = _run.TotalWaves;
            _seed = _run.Seed;
            _state = _run.State;

            _counterRoot.gameObject.SetActive(_wave > 0 && _state != RunState.Idle);
            _counter.text = $"ĐỢT {_wave:00} / {_total:00}";
            switch (_state)
            {
                case RunState.CardSelection: _status.text = "ĐÃ VƯỢT QUA · CHỌN THẺ"; break;
                case RunState.Victory: _status.text = "HOÀN THÀNH"; break;
                case RunState.Defeat: _status.text = "KẾT THÚC VÁN"; break;
                default: _status.text = _wave == _total ? "ĐỢT CUỐI" : "ĐANG CHIẾN ĐẤU"; break;
            }

            if (announce)
            {
                _bannerTitle.text = $"ĐỢT {_wave:00}";
                _bannerSubtitle.text = _wave == _total ? "ĐỢT CUỐI · TIẾN LÊN!" : "SẴN SÀNG CHIẾN ĐẤU";
                _announcedAt = Time.unscaledTime;
                _animating = true;
                _banner.gameObject.SetActive(true);
                Animate(0f);
            }
            else if (_state != RunState.WaveActive) HideBanner();
        }

        private void Animate(float elapsed)
        {
            float enter = Mathf.Clamp01(elapsed / _enterDuration);
            float exit = Mathf.Clamp01((elapsed - _enterDuration - _holdDuration) / _exitDuration);
            float easedEnter = 1f - Mathf.Pow(1f - enter, 3f);
            _bannerGroup.alpha = enter * (1f - exit);
            _banner.anchoredPosition = _bannerPosition + Vector2.right *
                (-_slideDistance * (1f - easedEnter) + _slideDistance * exit * exit);
            _banner.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, easedEnter);
            _counterRoot.localScale = Vector3.one *
                (1f + 0.1f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.5f) * Mathf.PI));
            if (exit >= 1f) HideBanner();
        }

        private void HideBanner()
        {
            _animating = false;
            _bannerGroup.alpha = 0f;
            _banner.gameObject.SetActive(false);
            _counterRoot.localScale = Vector3.one;
        }
    }
}
