using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Enemies
{
    public sealed partial class EnemySpawner
    {
        // Chỉ đổi khi có sự kiện; người vào giữa trận cũng nhận được đòn đang báo trước.
        [SyncVar] private int _bossId;
        [SyncVar] private int _bossSeed;
        [SyncVar] private int _bossWave;
        [SyncVar] private float _bossHealth;
        [SyncVar] private float _bossMaximum;
        [SyncVar] private int _bossPhase;
        [SyncVar] private BossAction _bossAction;
        [SyncVar] private Vector2 _bossDirection;
        [SyncVar] private double _bossActionAt;
        [SyncVar] private uint _bossRevision;
        private uint _appliedBossRevision;
        private Enemy _appliedBoss;
        private int _nextSummonId = -1000000;
        public float BossHealth => _bossHealth;
        public float BossMaximum => _bossMaximum;
        public int BossPhase => _bossPhase;
        public bool BossActive => _bossHealth > 0 && Matches(_bossSeed, _bossWave);

        private void ResetBossEncounter()
        {
            _appliedBoss = null;
            _appliedBossRevision = 0;
            _nextSummonId = -1000000;
            if (!isServer) return;
            _bossHealth = _bossMaximum = 0;
            _bossId = _bossSeed = _bossWave = _bossPhase = 0;
        }

        [Server]
        private void RegisterBoss(Enemy enemy)
        {
            _bossId = enemy.Id;
            _bossSeed = Run.Seed;
            _bossWave = Run.CurrentWave;
            _bossMaximum = enemy.Data.MaxHealth * enemy.Data.Boss.HealthMultiplier(PlayerRegistry.Count);
            _bossHealth = _bossMaximum;
            _bossPhase = 1;
            enemy.SetBossHealth(_bossHealth, _bossMaximum, _bossPhase);
            StartBossAction(enemy, BossAction.Rest, Vector2.down);
        }

        [Server]
        private void UpdateBossHealth(Enemy enemy)
        {
            _bossHealth = Mathf.Max(0, enemy.Health);
            if (_bossPhase == 1 && _bossHealth > 0 && _bossHealth <= _bossMaximum * enemy.Data.Boss.PhaseThreshold)
            {
                _bossPhase = 2;
                enemy.SetBossHealth(_bossHealth, _bossMaximum, _bossPhase);
                StartBossAction(enemy, BossAction.Transition, Vector2.down);
            }
        }

        [Server]
        public void StartBossAction(Enemy enemy, BossAction action, Vector2 direction)
        {
            if (enemy == null || !enemy.IsBoss || !enemy.IsAlive || !BossActive) return;
            _bossAction = action;
            _bossDirection = direction;
            _bossActionAt = NetworkTime.time;
            _bossRevision++;
            enemy.BeginBossAction(action, direction, _bossActionAt);
        }

        [Server]
        public void InterruptBoss(Enemy enemy, float duration)
        {
            StartBossAction(enemy, BossAction.Rest, Vector2.down);
            _bossActionAt += duration;
            enemy.BeginBossAction(BossAction.Rest, Vector2.down, _bossActionAt);
        }

        private void ApplyBossStatus()
        {
            if (!BossActive || !EnemyRegistry.TryGet(_bossId, out var boss) || !boss.IsBoss) return;
            boss.SetBossHealth(_bossHealth, _bossMaximum, _bossPhase);
            if (boss == _appliedBoss && _bossRevision == _appliedBossRevision) return;
            _appliedBoss = boss;
            _appliedBossRevision = _bossRevision;
            boss.BeginBossAction(_bossAction, _bossDirection, _bossActionAt);
        }

        [Server]
        public void SummonBossAdds(Enemy boss)
        {
            if (boss == null || !boss.IsAlive || !boss.IsBoss || boss.BossPhase != 2) return;
            var rules = boss.Data.Boss;
            int count = Mathf.Min(rules.SummonCount, rules.PopulationLimit - EnemyRegistry.Count);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / rules.SummonCount;
                Vector2 position = boss.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rules.SummonRadius;
                if (ArenaBounds.Instance != null) position = ArenaBounds.Instance.Clamp(position, Vector2.one);
                int id = _nextSummonId--;
                SpawnWithId(id, rules.Summon, position);
                RpcBossSummon(id, _catalog.IndexOf(rules.Summon), position, Run.Seed, Run.CurrentWave);
            }
        }

        [ClientRpc]
        private void RpcBossSummon(int id, int kind, Vector2 position, int seed, int wave)
        {
            if (isServer || !Matches(seed, wave) || _deadIds.Contains(id)) return;
            SpawnWithId(id, _catalog.Get(kind), position);
        }

        [Server]
        private void EndBossEncounter()
        {
            _bossHealth = 0;
            // Quái triệu hồi không giữ wave cuối mở sau khi trùm bị hạ.
            while (EnemyRegistry.Count > 0) KillEnemy(EnemyRegistry.Alive[EnemyRegistry.Count - 1]);
            _projectilePool?.ReleaseAll();
        }
    }
}
