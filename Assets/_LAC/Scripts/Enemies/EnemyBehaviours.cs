using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    public sealed partial class Enemy
    {
        private RunManager _run;
        private Vector2 _snapshotError;
        private Vector2 _chargeDirection;
        private float _chargeStartsAt;
        private float _chargeEndsAt;
        private float _nextChargeAt;
        private LineRenderer _chargeLine;
        public Vector2 Velocity { get; private set; }
        public bool IsCharging => Time.time >= _chargeStartsAt && Time.time < _chargeEndsAt;
        public bool IsWindingUp => Time.time < _chargeStartsAt && Time.time < _chargeEndsAt;

        private void ResetBehaviour()
        {
            _run = RunManager.Instance;
            _snapshotError = Vector2.zero;
            Velocity = Vector2.zero;
            _chargeStartsAt = _chargeEndsAt = 0f;
            _nextChargeAt = Time.time + _data.SpawnDelay + _data.ChargeInterval;
            if (_chargeLine == null) _chargeLine = GetComponentInChildren<LineRenderer>(true);
            if (_chargeLine != null) _chargeLine.enabled = false;
        }

        public void BeginCharge(Vector2 direction, float elapsed)
        {
            if (!IsAlive || IsStunned || _data.Behaviour != EnemyBehaviour.Charger) return;
            _chargeDirection = direction.normalized;
            _chargeStartsAt = Time.time + Mathf.Max(0f, _data.ChargeWarning - elapsed);
            _chargeEndsAt = Time.time + Mathf.Max(0f, _data.ChargeWarning + _data.ChargeDuration - elapsed);
            _nextChargeAt = Time.time + _data.ChargeInterval;
            if (_chargeLine != null)
            {
                _chargeLine.enabled = true;
                _chargeLine.SetPosition(0, Vector3.zero);
                _chargeLine.SetPosition(1, _chargeDirection * (_data.MoveSpeed * _data.ChargeDuration));
            }
        }

        private bool TickSpecialBehaviour()
        {
            if (_data.Behaviour != EnemyBehaviour.Ranged && _data.Behaviour != EnemyBehaviour.Charger) return false;
            _target = PlayerRegistry.Nearest(Position);
            if (_target == null) { Step(Vector2.zero, 0f); return true; }
            Vector2 toward = (Vector2)_target.transform.position - Position;
            if (_data.Behaviour == EnemyBehaviour.Ranged)
            {
                float distance = toward.magnitude;
                Vector2 direction = distance < _data.KeepDistance - 0.5f ? -toward.normalized :
                    distance > _data.KeepDistance + 0.5f ? toward.normalized : Vector2.zero;
                Step(Vector2.ClampMagnitude(direction + Separation(), 1f), _data.MoveSpeed);
                if (NetworkServer.active && distance <= _data.AttackRange && Time.time >= _nextAttackAt)
                {
                    _nextAttackAt = Time.time + _data.AttackInterval;
                    EnemySpawner.Instance.FireProjectile(this, toward.normalized);
                }
                return true;
            }

            if (NetworkServer.active && Time.time >= _nextChargeAt)
                EnemySpawner.Instance.StartCharge(this, toward.normalized);
            if (IsWindingUp) { Step(Vector2.zero, 0f); return true; }
            if (_chargeLine != null) _chargeLine.enabled = false;
            Vector2 before = Position;
            Step(IsCharging ? _chargeDirection : Vector2.zero, IsCharging ? _data.MoveSpeed : 0f);
            // Kiểm tra cả đoạn lao, không để cú lao nhanh xuyên qua người chơi giữa hai bước vật lý.
            if (IsCharging && NetworkServer.active && Time.time >= _nextAttackAt)
            {
                foreach (var player in PlayerRegistry.All)
                {
                    if (player == null || !player.IsAlive) continue;
                    if (!LAC.Drum.DrumShockwave.Intersects(before, before + Velocity * Time.fixedDeltaTime,
                        player.transform.position, _data.AttackRange)) continue;
                    if (LAC.Combat.DamageSystem.ApplyToPlayer(player, _data.ContactDamage, before))
                        _nextAttackAt = Time.time + _data.AttackInterval;
                    break;
                }
            }
            return true;
        }
    }
}
