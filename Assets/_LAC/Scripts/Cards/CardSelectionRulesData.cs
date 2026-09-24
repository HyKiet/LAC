using UnityEngine;

namespace LAC.Cards
{
    [CreateAssetMenu(menuName = "LAC/Cards/Selection Rules")]
    public sealed class CardSelectionRulesData : ScriptableObject
    {
        [SerializeField, Min(1f)] private float _selectionSeconds = 10f;
        [SerializeField, Min(0)] private int _rerollsPerRun = 2;
        public float SelectionSeconds => _selectionSeconds;
        public int RerollsPerRun => _rerollsPerRun;
    }
}
