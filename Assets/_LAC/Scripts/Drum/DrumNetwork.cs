using LAC.Drum;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    // Dùng NetworkIdentity sẵn có của RunManager; trống và sóng chỉ là biểu diễn cục bộ.
    public sealed partial class RunManager
    {
        [SyncVar] private int _drumEnergy;
        [SyncVar] private double _drumReadyAt;
        [SyncVar] private bool _drumReady;
        [SyncVar] private uint _drumActivations;
        private DrumData _drumData;
        private DongSonDrum _drumView;
        public DrumData DrumRules => _drumData;
        public int DrumEnergy => _drumEnergy;
        public bool DrumReady => _drumReady && State == RunState.WaveActive;
        public uint DrumActivations => _drumActivations;
        public float DrumCooldownRemaining => (float)System.Math.Max(0d, _drumReadyAt - NetworkTime.time);
        public Vector2 DrumPosition => ArenaBounds.Instance != null ? ArenaBounds.Instance.Rect.center : Vector2.zero;

        private void InitializeDrum()
        {
            _drumData = Resources.Load<DrumData>("Drum/DrumRules");
            var prefab = Resources.Load<DongSonDrum>("Drum/DongSonDrum");
            if (prefab != null)
            {
                _drumView = Instantiate(prefab, transform);
                _drumView.Bind(this);
            }
        }

        private void FixedUpdate()
        {
            if (!isServer || _drumData == null) return;
            _drumReady = _drumEnergy >= _drumData.SoulCost && DrumCooldownRemaining <= 0f;
        }

        [Server]
        private void ResetDrum()
        {
            _drumEnergy = 0;
            _drumReadyAt = 0;
            _drumReady = false;
            _drumActivations = 0;
        }

        [Server]
        public bool TryActivateDrum(PlayerDash dash, Vector2 start, Vector2 end)
        {
            if (_drumData == null || State != RunState.WaveActive || dash == null ||
                !dash.ServerCanActivateDrum(start, end) || _drumEnergy < _drumData.SoulCost ||
                DrumCooldownRemaining > 0f || !DrumShockwave.Intersects(start, end, DrumPosition, _drumData.ActivationRadius)) return false;

            // Tiêu năng lượng và khóa hồi chiêu trước hiệu ứng: hai yêu cầu cùng tick chỉ thắng một.
            _drumEnergy = 0;
            _drumReadyAt = NetworkTime.time + _drumData.Cooldown;
            _drumReady = false;
            _drumActivations++;
            var alive = EnemyRegistry.Alive;
            int[] ids = new int[alive.Count];
            for (int i = 0; i < alive.Count; i++)
            {
                ids[i] = alive[i].Id;
                alive[i].ApplyDrumShockwave(DrumPosition, _drumData.PushDistance, _drumData.StunDuration);
            }
            EnemyProjectileClearable.ClearAll();
            RpcDrumShockwave(ids, Seed);
            return true;
        }

        [ClientRpc]
        private void RpcDrumShockwave(int[] ids, int seed)
        {
            if (seed != Seed || IsOver) return;
            if (!isServer)
            {
                foreach (int id in ids)
                    if (EnemyRegistry.TryGet(id, out var enemy))
                        enemy.ApplyDrumShockwave(DrumPosition, _drumData.PushDistance, _drumData.StunDuration);
                EnemyProjectileClearable.ClearAll();
            }
            if (_drumView != null) _drumView.PlayShockwave();
        }
    }
}
