using UnityEngine;

namespace LAC.Cards
{
    public static class CardSelectionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<CardSelectionController>(FindObjectsInactive.Include) != null)
                return;

            GameObject prefab = Resources.Load<GameObject>("CardSelection");
            if (prefab != null)
            {
                Object.Instantiate(prefab).name = "CardSelection";
                return;
            }

            new GameObject("CardSelection", typeof(CardSelectionView), typeof(CardSelectionController));
        }
    }
}
