using LAC.Combat;
using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    public sealed class InstrumentWave : MonoBehaviour, IPoolable
    {
        [SerializeField] private LineRenderer[] _rings;
        private ObjectPool<InstrumentWave> _owner;
        private InstrumentWaveData _data;
        private float _age, _range, _halfAngle, _speed;
        private const int Segments = 64;
        public WeaponShape Shape => _data.Shape;

        public void Play(ObjectPool<InstrumentWave> owner, InstrumentWaveData data,
            Vector2 direction, float range, float halfAngle, float speed)
        {
            _owner = owner; _data = data; _range = range; _halfAngle = halfAngle; _speed = speed; _age = 0;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            transform.localScale = Vector3.one;
            foreach (var ring in _rings)
            {
                ring.positionCount = Segments + 1;
                ring.widthMultiplier = data.Width;
                ring.loop = false;
            }
            Apply();
        }
        public void OnSpawned() => _age = 0;
        public void OnDespawned() { _owner = null; _data = null; }
        private void Update()
        {
            if (_data == null) return;
            _age += Time.deltaTime;
            if (_age >= _data.Duration) { _owner.Release(this); return; }
            Apply();
        }
        private void Apply()
        {
            float t = Mathf.Clamp01(_age / _data.Duration);
            for (int r = 0; r < _rings.Length; r++)
            {
                var ring = _rings[r];
                float alpha = _data.Alpha * (1 - t) * (1 - r * .2f);
                var tint = _data.Tint; tint.a = alpha;
                ring.startColor = ring.endColor = tint;
                float outer = Mathf.Max(0, _range - _data.Width * .5f);
                for (int i = 0; i <= Segments; i++)
                {
                    float f = i / (float)Segments;
                    Vector3 point;
                    if (_data.Shape == WeaponShape.Line)
                    {
                        // Cụm sóng theo hướng đạn, không vẽ tia tức thời tới cuối tầm.
                        float angle = Mathf.Lerp(-80, 80, f) * Mathf.Deg2Rad;
                        float radius = _data.PacketRadius * (1 + r * .3f);
                        float x = Mathf.Max(0, _age * _speed - r * _data.PacketRadius);
                        point = new Vector3(Mathf.Min(outer, x + Mathf.Cos(angle) * radius), Mathf.Sin(angle) * radius);
                    }
                    else
                    {
                        float angle = (_data.Shape == WeaponShape.Circle ? f * 360 : Mathf.Lerp(-_halfAngle, _halfAngle, f)) * Mathf.Deg2Rad;
                        float radius = _data.Shape == WeaponShape.Circle
                            ? outer * Mathf.Clamp01(.18f + t * 1.15f - r * .14f)
                            : outer * (1 - r * .17f * (1 - t));
                        // Nan hoa nhẹ trên vòng đàn bầu; mép ngoài không vượt tầm sát thương.
                        if (_data.Shape == WeaponShape.Circle) radius *= 1 - .035f * (1 + Mathf.Cos(angle * 24)) * .5f;
                        point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    }
                    ring.SetPosition(i, point);
                }
            }
        }
    }
}
