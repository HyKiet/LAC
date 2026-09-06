using UnityEngine;

namespace LAC.Cards
{
    [CreateAssetMenu(fileName = "Card", menuName = "LAC/Cards/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private CardId _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 5)] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        [SerializeField, Min(0.001f)] private float _weight = 1f;
        [SerializeField] private Color _accent = new Color(0.82f, 0.62f, 0.28f, 1f);

        public CardId Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxStacks => _maxStacks;
        public float Weight => _weight;
        public Color Accent => _accent;

#if UNITY_EDITOR
        public void EditorConfigure(CardId id, string displayName, string description,
            int maxStacks, float weight, Color accent)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _maxStacks = Mathf.Max(1, maxStacks);
            _weight = Mathf.Max(0.001f, weight);
            _accent = accent;
        }
#endif
    }
}
