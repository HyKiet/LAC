#if UNITY_EDITOR
using System.IO;
using LAC.Combat;
using LAC.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.VFX.Editor
{
    public static class CombatContentBuilder
    {
        private const string Root = "Assets/_LAC/";
        private static void Set(Object obj, string name, object value)
        {
            var so = new SerializedObject(obj); var p = so.FindProperty(name);
            if (value is float f) p.floatValue = f;
            else if (value is int i) p.intValue = i;
            else if (value is Color c) p.colorValue = c;
            else p.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj);
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var data = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (data == null) { data = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data, Root + path); }
            return data;
        }
        private static LineRenderer Line(string name, Transform parent, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.sharedMaterial = material; line.sortingOrder = 2;
            line.widthMultiplier = .04f; line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
        [MenuItem("LAC/Combat/Rebuild T31 T41 Content")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
            Directory.CreateDirectory(Root + "Data/VFX");
            AssetDatabase.Refresh();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var material = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/VFX/SoundWave.prefab").GetComponentInChildren<SpriteRenderer>().sharedMaterial;
                var root = new GameObject("InstrumentWave", typeof(InstrumentWave));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                var so = new SerializedObject(root.GetComponent<InstrumentWave>());
                var rings = so.FindProperty("_rings"); rings.arraySize = 3;
                for (int i = 0; i < 3; i++) rings.GetArrayElementAtIndex(i).objectReferenceValue = Line("Ring" + i, root.transform, material);
                so.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "Prefabs/VFX/InstrumentWave.prefab").GetComponent<InstrumentWave>();
                string[] names = { "ThachSanh", "Giong", "Tam" };
                var performance = AssetDatabase.LoadAssetAtPath<LAC.Core.PerformanceTestData>(Root + "Data/Tests/Resources/Tests/Performance.asset");
                var perfSo = new SerializedObject(performance);
                var profiles = perfSo.FindProperty("_instruments"); profiles.arraySize = names.Length;
                int profileIndex = 0;
                foreach (string name in names)
                {
                    var character = AssetDatabase.LoadAssetAtPath<CharacterData>(Root + "Data/Characters/" + name + ".asset");
                    if (character == null) throw new System.Exception("Missing character " + name);
                    var data = Asset<InstrumentWaveData>("Data/VFX/" + name + "Wave.asset");
                    Set(data, "_prefab", prefab); Set(data, "_shape", (int)character.WeaponShape);
                    Set(data, "_duration", name == "ThachSanh" ? .5f : name == "Giong" ? .2f : .18f);
                    Set(data, "_alpha", name == "Giong" ? .16f : .1f);
                    Set(data, "_tint", (Color)(name == "Giong" ? new Color32(79,166,148,255) : new Color32(156,207,192,255)));
                    Set(character, "_instrumentWave", data);
                    profiles.GetArrayElementAtIndex(profileIndex++).objectReferenceValue = data;
                }
                perfSo.ApplyModifiedPropertiesWithoutUndo();
                var rules = Asset<ReviveData>("Data/Characters/ReviveRules.asset");
                string path = Root + "Prefabs/Player/Player.prefab";
                var player = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var health = player.GetComponent<PlayerHealth>(); Set(health, "_reviveRules", rules);
                    var old = player.transform.Find("ReviveMarker"); if (old != null) Object.DestroyImmediate(old.gameObject);
                    var marker = new GameObject("ReviveMarker", typeof(ReviveMarker)); marker.transform.SetParent(player.transform, false);
                    var view = marker.GetComponent<ReviveMarker>();
                    var radius = Line("RescueRadius", marker.transform, material);
                    radius.startColor = radius.endColor = new Color(0.612f,.812f,.753f,.22f); radius.enabled = false;
                    var canvas = new GameObject("RescueLabel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                    canvas.transform.SetParent(marker.transform, false); canvas.transform.localPosition = new Vector3(0,1.1f,0);
                    canvas.transform.localScale = Vector3.one * .012f;
                    var cv = canvas.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.overrideSorting = true; cv.sortingOrder = 30;
                    ((RectTransform)canvas.transform).sizeDelta = new Vector2(360,70);
                    var panel = canvas.AddComponent<Image>(); panel.color = new Color32(21,19,15,235); panel.raycastTarget = false;
                    var group = canvas.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
                    var label = Rect("Label", canvas.transform, new Vector2(360,42), new Vector2(0,14)).gameObject.AddComponent<Text>();
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 23;
                    label.alignment = TextAnchor.MiddleCenter; label.color = new Color32(244,234,218,255); label.raycastTarget = false;
                    var track = Rect("Track", canvas.transform, new Vector2(260,10), new Vector2(0,-19));
                    var bg = track.gameObject.AddComponent<Image>(); bg.color = new Color32(71,64,58,255); bg.raycastTarget = false;
                    var fill = Rect("Fill", track, Vector2.zero, Vector2.zero); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
                    var fg = fill.gameObject.AddComponent<Image>(); fg.color = new Color32(237,187,62,255); fg.raycastTarget = false;
                    Set(view,"_health",health); Set(view,"_group",group); Set(view,"_fill",fill); Set(view,"_label",label); Set(view,"_radius",radius);
                    PrefabUtility.SaveAsPrefabAsset(player,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(player); }
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = size; rt.anchoredPosition = position;
            return rt;
        }
    }
}
#endif
