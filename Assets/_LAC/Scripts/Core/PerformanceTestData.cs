using LAC.Combat;
using LAC.VFX;
using UnityEngine;
namespace LAC.Core
{
    public sealed class PerformanceTestData : ScriptableObject
    {
        [SerializeField] private Projectile _projectile;
        [SerializeField] private SoundWave _wave;
        public Projectile Projectile => _projectile;
        public SoundWave Wave => _wave;
    }
}
