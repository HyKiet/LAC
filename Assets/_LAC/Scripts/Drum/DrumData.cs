using UnityEngine;

namespace LAC.Drum
{
    [CreateAssetMenu(menuName = "LAC/Drum")]
    public sealed class DrumData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _soulCost = 20;
        [SerializeField, Min(0.1f)] private float _cooldown = 20f;
        [SerializeField, Min(0.1f)] private float _activationRadius = 1.15f;
        [SerializeField, Min(0f)] private float _stunDuration = 1f;
        [SerializeField, Min(0f)] private float _pushDistance = 2.5f;
        public int SoulCost => _soulCost;
        public float Cooldown => _cooldown;
        public float ActivationRadius => _activationRadius;
        public float StunDuration => _stunDuration;
        public float PushDistance => _pushDistance;
    }
}
