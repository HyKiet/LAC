using UnityEngine;

namespace LAC.Player
{
    [CreateAssetMenu(menuName = "LAC/Revive Rules")]
    public sealed class ReviveData : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float _radius = 1.5f;
        [SerializeField, Min(.1f)] private float _duration = 3f;
        [SerializeField, Range(.01f, 1f)] private float _healthFraction = .5f;
        [SerializeField, Min(0)] private float _invulnerability = 1f;
        public float Radius => _radius;
        public float Duration => _duration;
        public float HealthFraction => _healthFraction;
        public float Invulnerability => _invulnerability;
    }
}
