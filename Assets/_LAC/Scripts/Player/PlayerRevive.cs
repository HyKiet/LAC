using LAC.Core;
using Mirror;
using UnityEngine;

namespace LAC.Player
{
    public sealed partial class PlayerHealth
    {
        [SerializeField] private ReviveData _reviveRules;
        [SyncVar] private uint _rescuer;
        [SyncVar] private double _reviveStartedAt;
        private RunManager _reviveRun;
        public ReviveData ReviveRules => _reviveRules;
        public bool IsDowned => !IsAlive && _reviveRun != null && !_reviveRun.IsOver && _reviveRun.State != RunState.Idle;
        public float ReviveProgress => _rescuer == 0 || !IsDowned || _reviveRules == null ? 0 :
            Mathf.Clamp01((float)(NetworkTime.time - _reviveStartedAt) / _reviveRules.Duration);
        public bool IsBeingRevived => _rescuer != 0 && IsDowned;

        private void Start() => _reviveRun = RunManager.Instance;
        private void Update()
        {
            if (!isServer) return;
            if (!IsDowned || _reviveRules == null || _reviveRun.State != RunState.WaveActive)
            { ResetRevive(); return; }
            PlayerCharacter rescuer = null;
            foreach (var candidate in PlayerRegistry.All)
            {
                if (candidate == null || candidate == _character || !candidate.IsAlive) continue;
                if (((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude > _reviveRules.Radius * _reviveRules.Radius) continue;
                rescuer = candidate; break;
            }
            if (rescuer == null) { ResetRevive(); return; }
            if (_rescuer != rescuer.netId)
            {
                _rescuer = rescuer.netId;
                _reviveStartedAt = NetworkTime.time;
            }
            if (NetworkTime.time - _reviveStartedAt < _reviveRules.Duration) return;
            // Chỉ host hoàn tất. Client nội suy thanh tiến độ từ sự kiện bắt đầu/hủy.
            ResetRevive();
            _health = Mathf.Max(1, Mathf.CeilToInt(_maxHealth * _reviveRules.HealthFraction));
            _invulnerableUntil = Time.time + _reviveRules.Invulnerability;
            _reviveRun.Telemetry?.Revive(_character);
            _reviveRun.ReportPlayerRevived();
        }
        private void ResetRevive() { _rescuer = 0; _reviveStartedAt = 0; }
    }
}
