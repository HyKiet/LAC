using LAC.Core;
using UnityEngine;

namespace LAC.UI
{
    public sealed class DrumHud : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image _energy;
        [SerializeField] private UnityEngine.UI.Image _cooldown;
        [SerializeField] private UnityEngine.UI.Text _value;
        [SerializeField] private UnityEngine.UI.Text _status;
        private RunManager _run;
        private string _lastStatus;
        private int _lastEnergy = -1;
        private int _lastSeconds = -1;
        private RunState _lastState;
        private bool _lastReady;
        private void Start() => _run = RunManager.Instance;
        private void Update()
        {
            if (_run == null || _run.DrumRules == null) return;
            var rules = _run.DrumRules;
            _energy.fillAmount = (float)_run.DrumEnergy / rules.SoulCost;
            _cooldown.fillAmount = Mathf.Clamp01(_run.DrumCooldownRemaining / rules.Cooldown);
            if (_lastEnergy != _run.DrumEnergy)
            {
                _lastEnergy = _run.DrumEnergy;
                _value.text = $"{_lastEnergy}/{rules.SoulCost}";
            }
            int seconds = Mathf.CeilToInt(_run.DrumCooldownRemaining);
            if (_lastStatus != null && seconds == _lastSeconds && _run.State == _lastState && _run.DrumReady == _lastReady) return;
            _lastSeconds = seconds;
            _lastState = _run.State;
            _lastReady = _run.DrumReady;
            string status = _run.IsOver ? "VÁN ĐÃ KẾT THÚC" :
                _run.State == RunState.CardSelection ? "ĐANG CHỌN THẺ" :
                _run.DrumReady ? "SẴN SÀNG · LƯỚT VÀO TRỐNG" :
                seconds > 0 ? $"HỒI {seconds} GIÂY · NHẶT HỒN" : "NHẶT HỒN ĐỂ NẠP TRỐNG";
            if (status != _lastStatus) { _status.text = status; _lastStatus = status; }
            _status.color = _run.DrumReady ? new Color32(251, 221, 130, 255) : new Color32(156, 207, 192, 255);
        }
    }
}
