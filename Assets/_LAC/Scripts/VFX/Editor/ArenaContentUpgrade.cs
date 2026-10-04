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

        [MenuItem("LAC/Content/Apply 15 Waves and ThirdParty Art")]
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
            so.FindProperty("_fallbackTotalWaves").intValue = 15;
            so.ApplyModifiedPropertiesWithoutUndo();
            var previous = UnityEngine.Object.FindFirstObjectByType<ArenaStageVisuals>();
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ArenaContent] 15 waves, Gióng Warrior and three decorative stages ready.");
        }

        private static void UpdateWaves()
        {
            var table = AssetDatabase.LoadAssetAtPath<WaveTable>(Root + "Data/Waves/WaveTable_CoDinh.asset");
            var so = new SerializedObject(table);
            var waves = so.FindProperty("_waves");
            // Bỏ đợt đệm trước trùm; giữ nguyên tiến trình 1–14 và cấu hình trùm cuối.
            if (waves.arraySize == 16) waves.DeleteArrayElementAtIndex(14);
            if (waves.arraySize != 15) throw new InvalidOperationException("Bảng phải có 15 hoặc 16 đợt.");
            waves.GetArrayElementAtIndex(13).FindPropertyRelative("_label").stringValue = "14 · Âm Phủ — trước trùm";
            waves.GetArrayElementAtIndex(14).FindPropertyRelative("_label").stringValue = "15 · Chằn Tinh";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite[] Frames(string path)
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(Tiny + path).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            if (frames.Length == 0) throw new InvalidOperationException("Thiếu sprite: " + path);
            return frames;
        }

        private static void UpdateGiong()
        {
            string path = Root + "Data/Animations/Giong_TinySwords.asset";
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<SpriteAnimationSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            const string unit = "Units/Yellow Units/Warrior/Warrior_";
            var idle = Frames(unit + "Idle.png");
            var so = new SerializedObject(set);
            var clips = so.FindProperty("_clips"); clips.arraySize = 4;
            Clip(clips.GetArrayElementAtIndex(0), AnimState.Idle, idle, 10, true);
            Clip(clips.GetArrayElementAtIndex(1), AnimState.Walk, Frames(unit + "Run.png"), 12, true);
            Clip(clips.GetArrayElementAtIndex(2), AnimState.Attack, Frames(unit + "Attack1.png"), 12, false);
            // Gói không có Death: giữ một khung tĩnh, vòng cứu của T-41 biểu diễn trạng thái gục.
            Clip(clips.GetArrayElementAtIndex(3), AnimState.Death, new[] { idle[0] }, 1, false, true);
            so.FindProperty("_playHurtClip").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(Root + "Data/Characters/Giong.asset");
            so = new SerializedObject(character);
            so.FindProperty("_bodySprite").objectReferenceValue = idle[0];
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
                    stages.GetArrayElementAtIndex(stage).FindPropertyRelative("_firstWave").intValue = 1 + stage * 5;
                    stages.GetArrayElementAtIndex(stage).FindPropertyRelative("_root").objectReferenceValue = group;
                    // Trang trí thưa ở mép, vẽ dưới người/quái và không mang collider.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            bool bush = stage != 2 && (stage == 1 || i % 2 == 0);
                            string sprite = bush ? "Terrain/Decorations/Bushes/Bushe" + (1 + i % 4) + ".png"
                                : "Terrain/Decorations/Rocks/Rock" + (1 + i % 4) + ".png";
                            AddDecor(group.transform, sprite, new Vector2(-15 + i * 5, side * (8.2f + (i % 2) * .3f)), tints[stage], bush ? 1.5f : 2f);
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
