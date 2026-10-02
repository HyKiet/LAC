using LAC.Combat;
using LAC.VFX;
using UnityEngine;
namespace LAC.Core
{
    public sealed class PerformanceTestData : ScriptableObject
    {
        [SerializeField] private Projectile _projectile;
        [SerializeField] private SoundWave _wave;
        [SerializeField] private InstrumentWaveData[] _instruments;
        public Projectile Projectile => _projectile;
        public SoundWave Wave => _wave;
        public InstrumentWaveData[] Instruments => _instruments;
    }
}
