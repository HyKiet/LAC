#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LAC.VFX;

namespace LAC.Core.Editor
{
    public static class TenWaveContent
    {
        private const string Root = "Assets/_LAC/";

        [MenuItem("LAC/Content/Apply 10 Wave Survival")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "Scenes/Arena.unity") throw new InvalidOperationException("Mở Arena trước.");
            ApplyTable();
            var run = UnityEngine.Object.FindFirstObjectByType<RunManager>();
            var so = new SerializedObject(run);
            so.FindProperty("_fallbackTotalWaves").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
            string path = Root + "Prefabs/Environment/ArenaStageDecor.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var decor = new SerializedObject(prefab.GetComponent<ArenaStageVisuals>());
                var stages = decor.FindProperty("_stages");
                int[] starts = { 1, 4, 7 };
                for (int i = 0; i < stages.arraySize; i++)
                    stages.GetArrayElementAtIndex(i).FindPropertyRelative("_firstWave").intValue = starts[i];
                decor.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TenWaveContent] 9 timed waves (20–60s), final boss wave 10, stages 1/4/7.");
        }

        public static void ApplyTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<WaveTable>(Root + "Data/Waves/WaveTable_CoDinh.asset");
            var so = new SerializedObject(table);
            so.FindProperty("_safeSpawnDistance").floatValue = 3f;
            var waves = so.FindProperty("_waves");
            // Giữ đủ loại quái và boss, nén đường cong cũ thay vì chỉ cắt 5 đợt cuối.
            if (waves.arraySize == 15)
            {
                int[] remove = { 11, 8, 6, 3, 1 };
                foreach (int index in remove) waves.DeleteArrayElementAtIndex(index);
            }
            if (waves.arraySize != 10) throw new InvalidOperationException("Bảng phải có 10 hoặc 15 đợt.");
            float[] intervals = { 1.5f, 1.4f, 1.3f, 1.25f, 1.2f, 1.1f, 1f, .95f, .9f };
            int[] bursts = { 1, 1, 2, 2, 2, 3, 3, 3, 4 };
            for (int i = 0; i < 10; i++)
            {
                var wave = waves.GetArrayElementAtIndex(i);
                string stage = i < 3 ? "Sân Đình" : i < 6 ? "Ruộng Lúa" : "Âm Phủ";
                wave.FindPropertyRelative("_label").stringValue = i == 9 ? "10 · Chằn Tinh" : $"{i + 1:00} · {stage} — sinh tồn";
                wave.FindPropertyRelative("_duration").floatValue = i == 9 ? 0f : 20f + i * 5f;
                if (i == 9) continue;
                wave.FindPropertyRelative("_spawnInterval").floatValue = intervals[i];
                wave.FindPropertyRelative("_burst").intValue = bursts[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }
    }
}
#endif
