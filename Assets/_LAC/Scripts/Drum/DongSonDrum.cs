using LAC.Core;
using LAC.VFX;
using UnityEngine;

namespace LAC.Drum
{
    public sealed class DongSonDrum : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _face;
        [SerializeField] private SoundWave _shockwave;
        private RunManager _run;
        private ObjectPool<SoundWave> _pool;
        private float _struckAt = -10f;
        public void Bind(RunManager run) => _run = run;
        private void Start()
        {
            if (_shockwave != null) _pool = PoolRegistry.Get(_shockwave, prewarm: 2, softLimit: 4);
        }
        private void Update()
        {
            if (_run == null) return;
            transform.position = _run.DrumPosition;
            float pulse = _run.DrumReady ? 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            float hit = Mathf.Clamp01(1f - (Time.time - _struckAt) / 0.35f);
            _face.transform.localScale = Vector3.one * (pulse + hit * 0.15f);
            _face.color = _run.DrumReady ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f);
        }
        public void PlayShockwave()
        {
            _struckAt = Time.time;
            if (_pool == null) return;
            float radius = ArenaBounds.Instance != null ? ArenaBounds.Instance.Size.magnitude * 0.5f : 20f;
            var wave = _pool.Get(_run.DrumPosition, Quaternion.identity);
            wave.Play(_pool, 0.8f, radius, new Color32(156, 207, 192, 255));
        }
    }
}
