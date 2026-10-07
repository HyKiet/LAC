using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    public sealed class SpearThrust : MonoBehaviour, IPoolable
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField, Min(0.02f)] private float _duration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float _alpha = 0.32f;
        private ObjectPool<SpearThrust> _owner;
        private float _age;
        private Color _tint;

        public void Play(ObjectPool<SpearThrust> owner, Vector2 facing, float range, float halfWidth, Color tint)
        {
            _owner = owner;
            _age = 0f;
            _tint = tint;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            _line.positionCount = 5;
            _line.SetPosition(0, new Vector3(0, -halfWidth, 0));
            _line.SetPosition(1, new Vector3(range, -halfWidth, 0));
            _line.SetPosition(2, new Vector3(range, halfWidth, 0));
            _line.SetPosition(3, new Vector3(0, halfWidth, 0));
            _line.SetPosition(4, new Vector3(0, -halfWidth, 0));
            Apply();
        }

        public void OnSpawned() => _age = 0f;
        public void OnDespawned() => _owner = null;

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _duration)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }
            Apply();
        }

        private void Apply()
        {
            Color color = _tint;
            color.a = _alpha * (1f - Mathf.Clamp01(_age / _duration));
            _line.startColor = _line.endColor = color;
        }
    }
}
