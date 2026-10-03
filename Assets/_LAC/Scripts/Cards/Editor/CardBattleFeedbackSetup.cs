#if UNITY_EDITOR
using LAC.VFX;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardBattleFeedbackSetup
    {
        private const string PrefabPath = "Assets/_LAC/Prefabs/UI/Cards/CardBattleEffect.prefab";
        private const string DataPath = "Assets/_LAC/Data/Cards/Resources/CardBattleFeedback.asset";

        [MenuItem("LAC/Cards/Create Battle Feedback Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Thoát Play mode trước khi tạo asset.");
            var prefab = AssetDatabase.LoadAssetAtPath<CardBattleEffect>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("CardBattleEffect");
                try
                {
                    var filter = root.AddComponent<MeshFilter>();
                    var renderer = root.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_LAC/Art/Materials/SpriteAdditive.mat");
                    renderer.sortingOrder = 4;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    var numberRoot = new GameObject("HealedHealth");
                    numberRoot.transform.SetParent(root.transform, false);
                    numberRoot.transform.localPosition = new Vector3(.05f, .75f, 0f);
                    var number = numberRoot.AddComponent<PixelNumber>();
                    var digits = new Sprite[10];
                    for (int i = 0; i < 10; i++)
                        digits[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_LAC/Art/Placeholder/Digit_{i}.png");
                    var slots = new SpriteRenderer[3];
                    var existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LAC/Prefabs/VFX/DamageNumber.prefab");
                    Material digitMaterial = existing.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        var child = new GameObject("Digit_" + i);
                        child.transform.SetParent(numberRoot.transform, false);
                        slots[i] = child.AddComponent<SpriteRenderer>();
                        slots[i].sharedMaterial = digitMaterial;
                        slots[i].sortingOrder = 8;
                    }
                    var serializedNumber = new SerializedObject(number);
                    SetArray(serializedNumber.FindProperty("_digits"), digits);
                    SetArray(serializedNumber.FindProperty("_slots"), slots);
                    serializedNumber.ApplyModifiedPropertiesWithoutUndo();
                    numberRoot.SetActive(false);
                    var effect = root.AddComponent<CardBattleEffect>();
                    var serialized = new SerializedObject(effect);
                    serialized.FindProperty("_filter").objectReferenceValue = filter;
                    serialized.FindProperty("_renderer").objectReferenceValue = renderer;
                    serialized.FindProperty("_number").objectReferenceValue = number;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<CardBattleEffect>();
                }
                finally { Object.DestroyImmediate(root); }
            }
            if (AssetDatabase.LoadAssetAtPath<CardBattleFeedbackData>(DataPath) == null)
            {
                var data = ScriptableObject.CreateInstance<CardBattleFeedbackData>();
                var serialized = new SerializedObject(data);
                serialized.FindProperty("_effectPrefab").objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[CardBattle] Prefab + dữ liệu sẵn sàng; asset đã có được giữ nguyên.");
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
