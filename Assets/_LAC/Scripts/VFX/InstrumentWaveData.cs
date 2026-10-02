using LAC.Combat;
using UnityEngine;

namespace LAC.VFX
{
    [CreateAssetMenu(menuName = "LAC/Instrument Wave")]
    public sealed class InstrumentWaveData : ScriptableObject
    {
        [SerializeField] private InstrumentWave _prefab;
        [SerializeField] private WeaponShape _shape;
        [SerializeField] private Color _tint = new Color32(156, 207, 192, 255);
        [SerializeField, Min(.02f)] private float _duration = .45f;
        [SerializeField, Range(0, .22f)] private float _alpha = .09f;
        [SerializeField, Min(.01f)] private float _width = .045f;
        [SerializeField, Min(.01f)] private float _packetRadius = .22f;
        public InstrumentWave Prefab => _prefab;
        public WeaponShape Shape => _shape;
        public Color Tint => _tint;
        public float Duration => _duration;
        public float Alpha => _alpha;
        public float Width => _width;
        public float PacketRadius => _packetRadius;
    }
}
