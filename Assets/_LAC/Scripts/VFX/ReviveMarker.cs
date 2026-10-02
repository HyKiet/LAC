using LAC.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.VFX
{
    public sealed class ReviveMarker : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _label;
        [SerializeField] private LineRenderer _radius;
        private bool _showing;
        private bool _rescuing;
        private void Start()
        {
            if (_health == null || _health.ReviveRules == null) return;
            _radius.positionCount = 65;
            for (int i = 0; i <= 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64;
                _radius.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * _health.ReviveRules.Radius);
            }
        }
        private void LateUpdate()
        {
            bool show = _health != null && _health.IsDowned;
            _group.alpha = show ? 1 : 0;
            _radius.enabled = show;
            if (!show) { _showing = false; return; }
            bool rescuing = _health.IsBeingRevived;
            if (!_showing || _rescuing != rescuing)
                _label.text = rescuing ? "ĐANG HỒI SINH" : $"ĐỨNG GẦN ĐỂ CỨU · {_health.ReviveRules.Duration:0.#} GIÂY";
            _showing = true; _rescuing = rescuing;
            _fill.anchorMax = new Vector2(_health.ReviveProgress, 1);
        }
    }
}
