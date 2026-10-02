using LAC.Combat;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    public enum BossAction { Rest, Charge, Fan, Sweep, Summon, Transition }

    public sealed partial class Enemy
    {
        private BossAction _bossAction;
        private double _bossStartedAt;
        private Vector2 _bossDirection;
        private bool _bossResolved;
        private int _bossStep;
        private double _nextSummonAt;
        private float _baseColliderRadius = -1f;
        public bool IsBoss => _data != null && _data.Behaviour == EnemyBehaviour.Boss && _data.Boss != null;
        public int BossPhase { get; private set; }
        public BossAction CurrentBossAction => _bossAction;

        private void ResetBoss()
        {
            BossPhase = 1;
            _bossAction = BossAction.Rest;
            _bossStartedAt = NetworkTime.time;
            _bossResolved = false;
            _bossStep = 0;
            _nextSummonAt = 0;
            if (_chargeLine != null) { _chargeLine.loop = false; _chargeLine.positionCount = 2; }
            if (_collider is CircleCollider2D circle)
            {
                if (_baseColliderRadius < 0) _baseColliderRadius = circle.radius;
                circle.radius = IsBoss ? _data.AttackRange : _baseColliderRadius;
            }
        }

        public void SetBossHealth(float health, float maximum, int phase)
        {
            if (!IsBoss) return;
            _health = Mathf.Clamp(health, 0, maximum);
            MaxHealth = maximum;
            BossPhase = phase;
        }

        public void BeginBossAction(BossAction action, Vector2 direction, double startedAt)
        {
            if (!IsBoss) return;
            _bossAction = action;
            _bossDirection = direction.sqrMagnitude > 0 ? direction.normalized : Vector2.down;
            _bossStartedAt = startedAt;
            _bossResolved = false;
            if (_chargeLine == null) return;
            _chargeLine.enabled = false;
            _chargeLine.loop = false;
            if (action == BossAction.Sweep || action == BossAction.Summon || action == BossAction.Transition)
            {
                _chargeLine.loop = true;
                _chargeLine.positionCount = 64;
                float radius = action == BossAction.Sweep ? _data.Boss.SweepRadius : _data.Boss.SummonRadius;
                for (int i = 0; i < 64; i++)
                {
                    float angle = i * Mathf.PI * 2f / 64;
                    _chargeLine.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }
            else if (action == BossAction.Fan)
            {
                _chargeLine.positionCount = _data.Boss.FanCount * 3;
                for (int i = 0; i < _data.Boss.FanCount; i++)
                {
                    Vector3 ray = FanDirection(i) * _data.ProjectileSpeed;
                    _chargeLine.SetPosition(i * 3, Vector3.zero);
                    _chargeLine.SetPosition(i * 3 + 1, ray);
                    _chargeLine.SetPosition(i * 3 + 2, Vector3.zero);
                }
            }
            else
            {
                _chargeLine.positionCount = 2;
                _chargeLine.SetPosition(0, Vector3.zero);
                _chargeLine.SetPosition(1, _bossDirection * (_data.Boss.ChargeSpeed * _data.Boss.ChargeDuration));
            }
        }

        private Vector2 FanDirection(int i)
        {
            var rules = _data.Boss;
            float angle = rules.FanCount <= 1 ? 0 : -rules.FanAngle * 0.5f + rules.FanAngle * i / (rules.FanCount - 1);
            return Quaternion.Euler(0, 0, angle) * _bossDirection;
        }

        private void TickBoss()
        {
            var rules = _data.Boss;
            double now = NetworkTime.time;
            double age = now - _bossStartedAt;
            bool warning = _bossAction != BossAction.Rest && age < rules.Warning;
            if (_chargeLine != null) _chargeLine.enabled = warning ||
                (_bossAction == BossAction.Sweep && age < rules.Warning + 0.2f);
            if (warning) { Step(Vector2.zero, 0f); return; }

            bool charging = _bossAction == BossAction.Charge && age < rules.Warning + rules.ChargeDuration;
            Vector2 before = Position;
            Step(charging ? _bossDirection : Vector2.zero, charging ? rules.ChargeSpeed : 0);
            if (!NetworkServer.active) return;

            if (charging && Time.time >= _nextAttackAt)
            {
                foreach (var player in PlayerRegistry.All)
                    if (player != null && player.IsAlive &&
                        LAC.Drum.DrumShockwave.Intersects(before, before + Velocity * Time.fixedDeltaTime,
                            player.transform.position, _data.AttackRange) &&
                        DamageSystem.ApplyToPlayer(player, _data.ContactDamage, before))
                        _nextAttackAt = Time.time + _data.AttackInterval;
            }
            if (!_bossResolved && _bossAction != BossAction.Rest)
            {
                _bossResolved = true;
                if (_bossAction == BossAction.Fan)
                    for (int i = 0; i < rules.FanCount; i++) EnemySpawner.Instance.FireProjectile(this, FanDirection(i));
                if (_bossAction == BossAction.Sweep)
                    foreach (var player in PlayerRegistry.All)
                        if (player != null && player.IsAlive && Vector2.Distance(Position, player.transform.position) <= rules.SweepRadius)
                            DamageSystem.ApplyToPlayer(player, _data.ContactDamage, Position);
                if (_bossAction == BossAction.Summon) EnemySpawner.Instance.SummonBossAdds(this);
            }
            double duration = _bossAction == BossAction.Rest ? rules.Recovery :
                rules.Warning + rules.Recovery + (_bossAction == BossAction.Charge ? rules.ChargeDuration : 0);
            if (age < duration) return;
            var target = PlayerRegistry.Nearest(Position);
            if (target == null) return;
            BossAction next;
            if (BossPhase == 2 && now >= _nextSummonAt)
            { next = BossAction.Summon; _nextSummonAt = now + rules.SummonInterval; }
            else if (BossPhase == 2) next = BossAction.Sweep;
            else next = _bossStep++ % 2 == 0 ? BossAction.Charge : BossAction.Fan;
            EnemySpawner.Instance.StartBossAction(this, next, (Vector2)target.transform.position - Position);
        }
    }
}
