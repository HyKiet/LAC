#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LAC.Core;
using LAC.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.VFX.Editor
{
    public static class ArenaContentUpgrade
    {
        private const string Root = "Assets/_LAC/";
        private const string Tiny = "Assets/ThirdParty/TinySwords/";
        private const string PrefabPath = Root + "Prefabs/Environment/ArenaStageDecor.prefab";

        [MenuItem("LAC/Content/Apply 10 Waves and ThirdParty Art")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước khi sửa nội dung.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Root + "Scenes/Arena.unity")
                throw new InvalidOperationException("Mở scene Arena trước.");
            UpdateWaves();
            UpdateGiong();
            BuildDecor();
            var run = UnityEngine.Object.FindFirstObjectByType<RunManager>();
            var so = new SerializedObject(run);
            so.FindProperty("_fallbackTotalWaves").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
            var previous = UnityEngine.Object.FindFirstObjectByType<ArenaStageVisuals>();
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ArenaContent] 10 waves, three playable classes and rock stages ready.");
        }

        private static void UpdateWaves()
        {
            LAC.Core.Editor.TenWaveContent.ApplyTable();
        }

        private static Sprite[] Frames(string path)
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(Tiny + path).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            if (frames.Length == 0) throw new InvalidOperationException("Thiếu sprite: " + path);
            return frames;
        }

        private static void UpdateGiong() => LAC.Player.Editor.PlayableClassesContent.BuildCharacters();

        private static void Clip(SerializedProperty clip, AnimState state, Sprite[] frames, float fps, bool loop, bool hold = false)
        {
            clip.FindPropertyRelative("_state").enumValueIndex = (int)state;
            clip.FindPropertyRelative("_fps").floatValue = fps;
            clip.FindPropertyRelative("_loop").boolValue = loop;
            clip.FindPropertyRelative("_holdLastFrame").boolValue = hold;
            var list = clip.FindPropertyRelative("_frames"); list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static void BuildDecor()
        {
            if (!AssetDatabase.IsValidFolder(Root + "Prefabs/Environment"))
                AssetDatabase.CreateFolder(Root + "Prefabs", "Environment");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("ArenaStageDecor", typeof(ArenaStageVisuals));
                SceneManager.MoveGameObjectToScene(root, preview);
                var so = new SerializedObject(root.GetComponent<ArenaStageVisuals>());
                var stages = so.FindProperty("_stages"); stages.arraySize = 3;
                string[] names = { "SanDinh", "RuongLua", "AmPhu" };
                Color[] tints = { new Color(.42f,.40f,.32f,1), new Color(.36f,.46f,.35f,1), new Color(.32f,.40f,.46f,1) };
                for (int stage = 0; stage < 3; stage++)
                {
                    var group = new GameObject(names[stage]); group.transform.SetParent(root.transform, false);
                    group.SetActive(stage == 0);
                    stages.GetArrayElementAtIndex(stage).FindPropertyRelative("_firstWave").intValue = 1 + stage * 3;
                    stages.GetArrayElementAtIndex(stage).FindPropertyRelative("_root").objectReferenceValue = group;
                    // Trang trí thưa ở mép, vẽ dưới người/quái và không mang collider.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            string sprite = "Terrain/Decorations/Rocks/Rock" + (1 + i % 4) + ".png";
                            AddDecor(group.transform, sprite, new Vector2(-15 + i * 5, side * (8.2f + (i % 2) * .3f)), tints[stage], 2f);
                        }
                        for (int i = 0; i < 3; i++)
                            AddDecor(group.transform, "Terrain/Decorations/Rocks/Rock" + (i + 1) + ".png",
                                new Vector2(side * 16.5f, -5 + i * 5), tints[stage], 2f);
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static void AddDecor(Transform parent, string spritePath, Vector2 position, Color tint, float scale)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(spritePath), typeof(SpriteRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = Frames(spritePath)[0]; renderer.color = tint; renderer.sortingOrder = -80;
        }
    }
}
#endif
