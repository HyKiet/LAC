using LAC.Enemies;
using UnityEngine;

namespace LAC.UI
{
    public sealed class BossHud : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private UnityEngine.UI.Text _label;
        private EnemySpawner _spawner;
        private float _health = -1;
        private int _phase;
        private float _pulse;
        private void Start() => _spawner = EnemySpawner.Instance;
        private void Update()
        {
            bool active = _spawner != null && _spawner.BossActive;
            _group.alpha = active ? 1 : 0;
            if (!active) { _phase = 0; return; }
            if (_phase != _spawner.BossPhase) _pulse = 1;
            if (_phase != _spawner.BossPhase || _health != _spawner.BossHealth)
            {
                _phase = _spawner.BossPhase;
                _health = _spawner.BossHealth;
                _fill.anchorMax = new Vector2(Mathf.Clamp01(_health / _spawner.BossMaximum), 1);
                _label.text = $"CHẰN TINH  ·  PHA {_phase}  ·  {Mathf.CeilToInt(_health)} / {Mathf.CeilToInt(_spawner.BossMaximum)}";
            }
            _pulse = Mathf.MoveTowards(_pulse, 0, Time.unscaledDeltaTime);
            _label.transform.localScale = Vector3.one * (1 + Mathf.Sin(_pulse * Mathf.PI) * 0.08f);
        }
    }
}
