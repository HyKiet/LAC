using UnityEngine;

namespace LAC.Enemies
{
    [CreateAssetMenu(menuName = "LAC/Enemy Catalog")]
    public sealed class EnemyCatalogData : ScriptableObject
    {
        [SerializeField] private EnemyData[] _enemies;
        public int IndexOf(EnemyData data) => System.Array.IndexOf(_enemies, data);
        public EnemyData Get(int index) => index >= 0 && index < _enemies.Length ? _enemies[index] : null;
    }
}
