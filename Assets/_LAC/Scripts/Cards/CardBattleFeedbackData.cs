using UnityEngine;

namespace LAC.Cards
{
    [CreateAssetMenu(menuName = "LAC/Cards/Battle Feedback", fileName = "CardBattleFeedback")]
    public sealed class CardBattleFeedbackData : ScriptableObject
    {
        [SerializeField] private CardBattleEffect _effectPrefab;
        [SerializeField, Range(8, 32)] private int _capacity = 24;
        [SerializeField] private Color _upgradeColor = new Color(237f / 255f, 187f / 255f, 62f / 255f, .22f);
        [SerializeField] private Color _healColor = new Color(156f / 255f, 207f / 255f, 192f / 255f, .22f);
        [SerializeField] private Color _windColor = new Color(79f / 255f, 166f / 255f, 148f / 255f, .18f);
        [SerializeField] private Color _shieldColor = new Color(156f / 255f, 207f / 255f, 192f / 255f, .22f);
        [SerializeField, Min(.1f)] private float _upgradeSeconds = .85f;
        [SerializeField, Min(.1f)] private float _healSeconds = .9f;
        [SerializeField, Min(.1f)] private float _windSeconds = .4f;
        [SerializeField, Min(.1f)] private float _shieldSeconds = .35f;
        [SerializeField, Min(.1f)] private float _windInterval = .2f;
        [SerializeField, Min(.01f)] private float _strokeWidth = .055f;
        public CardBattleEffect EffectPrefab => _effectPrefab;
        public int Capacity => Mathf.Clamp(_capacity, 8, 32);
        public float WindInterval => Mathf.Max(.1f, _windInterval);
        public float StrokeWidth => _strokeWidth;
        public Color ColorFor(CardBattleEffect.Kind kind) => kind switch
        {
            CardBattleEffect.Kind.Heal => _healColor,
            CardBattleEffect.Kind.Wind => _windColor,
            CardBattleEffect.Kind.Shield => _shieldColor,
            _ => _upgradeColor
        };
        public float SecondsFor(CardBattleEffect.Kind kind) => Mathf.Max(.1f, kind switch
        {
            CardBattleEffect.Kind.Heal => _healSeconds,
            CardBattleEffect.Kind.Wind => _windSeconds,
            CardBattleEffect.Kind.Shield => _shieldSeconds,
            _ => _upgradeSeconds
        });
    }
}
