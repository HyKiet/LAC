using UnityEngine;

namespace LAC.Enemies
{
    [CreateAssetMenu(menuName = "LAC/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _healthPerExtraPlayer = 1f;
        [SerializeField, Range(0.1f, 0.9f)] private float _phaseThreshold = 0.5f;
        [SerializeField, Min(0.1f)] private float _warning = 0.9f;
        [SerializeField, Min(0.1f)] private float _recovery = 1.2f;
        [SerializeField, Min(0.1f)] private float _chargeSpeed = 10f;
        [SerializeField, Min(0.1f)] private float _chargeDuration = 0.8f;
        [SerializeField, Min(1)] private int _fanCount = 5;
        [SerializeField, Range(0f, 180f)] private float _fanAngle = 60f;
        [SerializeField, Min(0.1f)] private float _sweepRadius = 3.5f;
        [SerializeField, Min(0.1f)] private float _summonInterval = 8f;
        [SerializeField, Min(1)] private int _summonCount = 4;
        [SerializeField, Min(1)] private int _populationLimit = 40;
        [SerializeField, Min(0.1f)] private float _summonRadius = 4f;
        [SerializeField] private EnemyData _summon;
        public float HealthMultiplier(int players) => 1f + Mathf.Max(0, players - 1) * _healthPerExtraPlayer;
        public float PhaseThreshold => _phaseThreshold;
        public float Warning => _warning;
        public float Recovery => _recovery;
        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDuration => _chargeDuration;
        public int FanCount => _fanCount;
        public float FanAngle => _fanAngle;
        public float SweepRadius => _sweepRadius;
        public float SummonInterval => _summonInterval;
        public int SummonCount => _summonCount;
        public int PopulationLimit => _populationLimit;
        public float SummonRadius => _summonRadius;
        public EnemyData Summon => _summon;
    }
}
