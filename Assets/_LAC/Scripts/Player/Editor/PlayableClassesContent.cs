#if UNITY_EDITOR
using System;
using System.Linq;
using LAC.Combat;
using LAC.Net;
using LAC.UI;
using LAC.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Player.Editor
{
    public static class PlayableClassesContent
    {
        private const string Root = "Assets/_LAC/";
        private const string Tiny = "Assets/ThirdParty/TinySwords/Units/";

        [MenuItem("LAC/Content/Apply Warrior Archer Lancer")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play mode trước.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Root + "Scenes/Arena.unity") throw new InvalidOperationException("Mở Arena trước.");
            BuildCharacters();
            var manager = UnityEngine.Object.FindAnyObjectByType<NetworkManagerLAC>();
            var so = new SerializedObject(manager);
            so.FindProperty("_autoStartHost").boolValue = false;
            so.FindProperty("autoCreatePlayer").boolValue = false;
            so.FindProperty("maxConnections").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            var debugHud = manager.GetComponent<Mirror.NetworkManagerHUD>();
            if (debugHud != null) debugHud.enabled = false;
            string path = Root + "Prefabs/UI/CharacterSelection.prefab";
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("CharacterSelection", typeof(CharacterSelection));
                SceneManager.MoveGameObjectToScene(go, preview);
                so = new SerializedObject(go.GetComponent<CharacterSelection>());
                so.FindProperty("_characters").objectReferenceValue = manager.Characters;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            if (UnityEngine.Object.FindAnyObjectByType<CharacterSelection>() == null)
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
            // API lưu prefab để cả instance trong scene lẫn mọi bản build cùng bỏ bụi.
            var decor = PrefabUtility.LoadPrefabContents(Root + "Prefabs/Environment/ArenaStageDecor.prefab");
            try
            {
                foreach (var renderer in decor.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite).Contains("/Bushes/"))
                        UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                PrefabUtility.SaveAsPrefabAsset(decor, Root + "Prefabs/Environment/ArenaStageDecor.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(decor); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[Classes] Warrior, Archer, Lancer, selection and grass removal ready.");
        }

        public static void BuildCharacters()
        {
            Move("Data/Characters/Giong.asset", "Data/Characters/Warrior.asset");
            Move("Data/Characters/Tam.asset", "Data/Characters/Archer.asset");
            Move("Data/Characters/ThachSanh.asset", "Data/Characters/Lancer.asset");
            BuildWeapons();
            CreateClass("Warrior", "Yellow Units/Warrior/Warrior_", "Attack1", CharacterId.Warrior,
                WeaponShape.Arc, 10, 4, 2.2f, .65f, 4, "Kiếm", "Chém cung phía trước. Bền bỉ khi áp sát.");
            CreateClass("Archer", "Blue Units/Archer/Archer_", "Shoot", CharacterId.Archer,
                WeaponShape.Line, 6, 5.5f, 8, .7f, 4, "Cung", "Bắn tên tầm xa. Sau lướt, tên kế tiếp ×2 sát thương.");
            CreateClass("Lancer", "Purple Units/Lancer/Lancer_", "Right_Attack", CharacterId.Lancer,
                WeaponShape.Thrust, 8, 4.5f, 3.2f, .85f, 5, "Thương", "Đâm xuyên hàng. Đứng thẳng hướng với đám quái.");
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistry>(Root + "Data/Characters/CharacterRegistry.asset");
            var so = new SerializedObject(registry); var list = so.FindProperty("_characters"); list.arraySize = 3;
            string[] names = { "Warrior", "Archer", "Lancer" };
            for (int i = 0; i < names.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CharacterData>(Root + "Data/Characters/" + names[i] + ".asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            ThanhGiongContent.ApplyIfAvailable();
        }

        private static void Move(string oldPath, string newPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(Root + newPath) != null) return;
            string error = AssetDatabase.MoveAsset(Root + oldPath, Root + newPath);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }

        private static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(Tiny + path).OfType<Sprite>()
            .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();

        private static void CreateClass(string name, string sheet, string attack, CharacterId id, WeaponShape shape,
            int health, float speed, float range, float interval, int damage, string weapon, string description)
        {
            string path = Root + "Data/Animations/" + name + "_TinySwords.asset";
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<SpriteAnimationSet>(); AssetDatabase.CreateAsset(set, path); }
            var so = new SerializedObject(set); var clips = so.FindProperty("_clips");
            clips.arraySize = id == CharacterId.Lancer ? 8 : 4;
            var idle = Frames(sheet + "Idle.png");
            Clip(clips.GetArrayElementAtIndex(0), AnimState.Idle, idle, 10, true);
            Clip(clips.GetArrayElementAtIndex(1), AnimState.Walk, Frames(sheet + "Run.png"), 12, true);
            // 8 khung Archer ở 18 FPS hoàn tất trước đòn 0.7 giây.
            Clip(clips.GetArrayElementAtIndex(2), AnimState.Attack, Frames(sheet + attack + ".png"), 18, false);
            Clip(clips.GetArrayElementAtIndex(3), AnimState.Death, new[] { idle[0] }, 1, false, true);
            if (id == CharacterId.Lancer)
            {
                string[] directions = { "Up", "Down", "UpRight", "DownRight" };
                for (int i = 0; i < 4; i++) Clip(clips.GetArrayElementAtIndex(i + 4), (AnimState)(i + 5),
                    Frames(sheet + directions[i] + "_Attack.png"), 18, false);
            }
            so.FindProperty("_playHurtClip").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(Root + "Data/Characters/" + name + ".asset");
            data.name = name; so = new SerializedObject(data);
            so.FindProperty("_id").enumValueIndex = (int)id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_weaponName").stringValue = weapon;
            so.FindProperty("_weaponShape").enumValueIndex = (int)shape;
            so.FindProperty("_maxHealth").intValue = health;
            so.FindProperty("_moveSpeed").floatValue = speed;
            so.FindProperty("_attackRange").floatValue = range;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_baseDamage").intValue = damage;
            so.FindProperty("_arcHalfAngle").floatValue = 55;
            so.FindProperty("_thrustHalfWidth").floatValue = .4f;
            so.FindProperty("_directionalAttack").boolValue = id == CharacterId.Lancer;
            so.FindProperty("_projectileSpeed").floatValue = 16;
            so.FindProperty("_projectilePrefab").objectReferenceValue = id == CharacterId.Archer
                ? AssetDatabase.LoadAssetAtPath<Projectile>(Root + "Prefabs/Projectiles/Arrow.prefab") : null;
            so.FindProperty("_bodySprite").objectReferenceValue = idle[0];
            so.FindProperty("_portrait").objectReferenceValue = id == CharacterId.Lancer
                ? Frames(sheet + "Right_Attack.png")[0] : idle[0];
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.FindProperty("_instrumentWave").objectReferenceValue = null;
            so.FindProperty("_spawnSoundWave").boolValue = false;
            so.FindProperty("_dashDistance").floatValue = 4;
            so.FindProperty("_dashDuration").floatValue = .15f;
            so.FindProperty("_dashCooldown").floatValue = id == CharacterId.Archer ? .8f : 1;
            so.FindProperty("_dashDamageMultiplier").floatValue = id == CharacterId.Archer ? 2 : 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Clip(SerializedProperty clip, AnimState state, Sprite[] frames, float fps, bool loop, bool hold = false)
        {
            if (frames.Length == 0) throw new InvalidOperationException("Thiếu frames cho " + state);
            clip.FindPropertyRelative("_state").enumValueIndex = (int)state;
            clip.FindPropertyRelative("_fps").floatValue = fps;
            clip.FindPropertyRelative("_loop").boolValue = loop;
            clip.FindPropertyRelative("_holdLastFrame").boolValue = hold;
            var list = clip.FindPropertyRelative("_frames"); list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static void BuildWeapons()
        {
            string projectilePath = Root + "Prefabs/Projectiles/Arrow.prefab";
            var go = PrefabUtility.LoadPrefabContents(Root + "Prefabs/Projectiles/Projectile.prefab");
            try
            {
                go.name = "Arrow";
                go.transform.localScale = Vector3.one;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = Frames("Blue Units/Archer/Arrow.png")[0];
                renderer.color = Color.white;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/Materials/EnemyTelegraph.mat");
                var so = new SerializedObject(go.GetComponent<Projectile>());
                so.FindProperty("_radius").floatValue = .12f; so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, projectilePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                go = new GameObject("SpearThrust", typeof(LineRenderer), typeof(SpearThrust));
                SceneManager.MoveGameObjectToScene(go, preview);
                var line = go.GetComponent<LineRenderer>();
                line.useWorldSpace = false; line.widthMultiplier = .035f; line.sortingOrder = 8;
                line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/Materials/EnemyTelegraph.mat");
                var so = new SerializedObject(go.GetComponent<SpearThrust>());
                so.FindProperty("_line").objectReferenceValue = line; so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, Root + "Prefabs/VFX/SpearThrust.prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            go = PrefabUtility.LoadPrefabContents(Root + "Prefabs/Player/Player.prefab");
            try
            {
                var so = new SerializedObject(go.GetComponent<WeaponAuto>());
                so.FindProperty("_thrustPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SpearThrust>(Root + "Prefabs/VFX/SpearThrust.prefab");
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, Root + "Prefabs/Player/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
    }
}
#endif
