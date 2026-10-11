#if UNITY_EDITOR
using System;
using LAC.Core;
using LAC.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LAC.Director.Editor
{
    public static class DirectorContent
    {
        [MenuItem("LAC/Content/Apply Rule Based Director")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/_LAC/Scenes/Arena.unity")
                throw new InvalidOperationException("Mở Arena trước.");
            EnsureUiSettings();
            var rules = AssetDatabase.LoadAssetAtPath<DirectorRulesData>(
                "Assets/_LAC/Data/Director/Resources/Director/DirectorRules.asset");
            if (rules == null) throw new InvalidOperationException("Thiếu DirectorRules.asset.");
            var run = UnityEngine.Object.FindAnyObjectByType<RunManager>(FindObjectsInactive.Include);
            if (run == null) throw new InvalidOperationException("Thiếu RunManager.");
            var serialized = new SerializedObject(run);
            serialized.FindProperty("_directorRules").objectReferenceValue = rules;
            serialized.FindProperty("_configuredDirectorMode").enumValueIndex = (int)DirectorMode.Adaptive;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            const string prefabPath = "Assets/_LAC/Prefabs/UI/DirectorHud.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var root = new GameObject("DirectorHud", typeof(DirectorHud));
                try { prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if (UnityEngine.Object.FindAnyObjectByType<DirectorHud>(FindObjectsInactive.Include) == null)
            {
                var hud = UnityEngine.Object.FindAnyObjectByType<WaveHud>(FindObjectsInactive.Include);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (hud != null) instance.transform.SetParent(hud.transform.parent, false);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DirectorContent] Adaptive/Fixed, luật SO và HUD đã gắn vào Arena.");
        }

        private static void EnsureUiSettings()
        {
            const string folder = "Assets/_LAC/Data/UI/Resources";
            if (!AssetDatabase.IsValidFolder("Assets/_LAC/Data/UI"))
                AssetDatabase.CreateFolder("Assets/_LAC/Data", "UI");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_LAC/Data/UI", "Resources");
            if (!AssetDatabase.IsValidFolder(folder + "/DirectorUI")) AssetDatabase.CreateFolder(folder, "DirectorUI");
            string path = folder + "/DirectorUI/TMP Settings.asset";
            string oldPath = folder + "/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(oldPath) != null)
                AssetDatabase.MoveAsset(oldPath, path);
            if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(path) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMPro.TMP_Settings>(), path);
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(path));
            settings.FindProperty("m_leadingCharacters").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "/DirectorUI/Leading.txt");
            settings.FindProperty("m_followingCharacters").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "/DirectorUI/Following.txt");
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
