#if UNITY_EDITOR
using System.IO;
using LAC.Core;
using LAC.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LAC.Enemies.Editor
{
    public static class BossContentBuilder
    {
        private const string Root = "Assets/_LAC/";
        private static void Set(Object obj, string name, object value)
        {
            var so = new SerializedObject(obj); var p = so.FindProperty(name);
            if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is string s) p.stringValue = s;
            else p.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj);
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var data = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (data == null) { data = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data, Root + path); }
            return data;
        }
        [MenuItem("LAC/Enemies/Rebuild T39 Boss")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
            var rules = Asset<BossData>("Data/Enemies/ChanTinhRules.asset");
            Set(rules, "_summon", AssetDatabase.LoadAssetAtPath<EnemyData>(Root + "Data/Enemies/MaDa.asset"));
            var data = Asset<EnemyData>("Data/Enemies/ChanTinh.asset");
            Set(data, "_displayName", "Chằn Tinh"); Set(data, "_behaviour", (int)EnemyBehaviour.Boss);
            Set(data, "_boss", rules); Set(data, "_maxHealth", 800); Set(data, "_contactDamage", 2);
            Set(data, "_attackRange", 1.1f); Set(data, "_spawnDelay", 1.5f); Set(data, "_projectileSpeed", 5f);
            Set(data, "_bodySprite", MakeSprite());
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalogData>(Root + "Data/Enemies/Resources/Enemies/EnemyCatalog.asset");
            var so = new SerializedObject(catalog); var enemies = so.FindProperty("_enemies");
            if (catalog.IndexOf(data) < 0) { enemies.arraySize++; enemies.GetArrayElementAtIndex(enemies.arraySize - 1).objectReferenceValue = data; }
            so.ApplyModifiedPropertiesWithoutUndo();
            var table = AssetDatabase.LoadAssetAtPath<WaveTable>(Root + "Data/Waves/WaveTable_CoDinh.asset");
            so = new SerializedObject(table); var waves = so.FindProperty("_waves");
            var final = waves.GetArrayElementAtIndex(waves.arraySize - 1);
            final.FindPropertyRelative("_label").stringValue = "Chằn Tinh";
            var groups = final.FindPropertyRelative("_groups"); groups.arraySize = 1;
            groups.GetArrayElementAtIndex(0).FindPropertyRelative("_enemy").objectReferenceValue = data;
            groups.GetArrayElementAtIndex(0).FindPropertyRelative("_count").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            BuildHud(); AssetDatabase.SaveAssets();
            Debug.Log($"[BossContent] Wave {table.TotalWaves} and boss HUD ready");
        }
        private static Sprite MakeSprite()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Color dark = new Color32(17,46,62,255), body = new Color32(47,116,128,255);
            Color light = new Color32(79,166,148,255), gold = new Color32(237,187,62,255);
            for (int y=0; y<size; y++) for (int x=0; x<size; x++)
            {
                float dx=x-48, dy=y-37, ring=Mathf.Sqrt(dx*dx+dy*dy*1.7f);
                bool coil=ring>17 && ring<37;
                bool neck=x>39 && x<59 && y>34 && y<71;
                bool head=y>59 && y<84 && Mathf.Abs(dx)<(y>74?15:22);
                bool horns=y>=80 && y<93 && Mathf.Abs(dx)>9 && Mathf.Abs(dx)<15-(y-80)*.25f;
                if (!(coil||neck||head||horns)) continue;
                Color c=horns?gold:((x+y)%9<2?light:body);
                if(coil && (ring<20||ring>34)) c=dark;
                if(head && y>72 && y<77 && Mathf.Abs(dx)>7 && Mathf.Abs(dx)<15) c=gold;
                if(head && y>62 && y<65 && Mathf.Abs(dx)<12) c=dark;
                pixels[y*size+x]=c;
            }
            texture.SetPixels(pixels); texture.Apply();
            string path=Root+"Art/Sprites/Enemy_ChanTinh.png";
            File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=32;
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
            rect.sizeDelta=size; rect.anchoredPosition=position; return rect;
        }
        private static void BuildHud()
        {
            var preview=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("BossHud",typeof(RectTransform),typeof(CanvasGroup),typeof(BossHud));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
            var rect=(RectTransform)root.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var group=root.GetComponent<CanvasGroup>(); group.alpha=0; group.blocksRaycasts=false; group.interactable=false;
            var panel=Rect("BossHealth",root.transform,new Vector2(600,56),new Vector2(0,-100));
            var bg=panel.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color=new Color32(21,19,15,245); bg.raycastTarget=false;
            var track=Rect("Track",panel,new Vector2(568,12),new Vector2(0,-36));
            var image=track.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=new Color32(71,64,58,255); image.raycastTarget=false;
            var fill=Rect("Fill",track,Vector2.zero,Vector2.zero); fill.anchorMin=Vector2.zero; fill.anchorMax=Vector2.one; fill.offsetMin=fill.offsetMax=Vector2.zero;
            image=fill.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=new Color32(237,187,62,255); image.raycastTarget=false;
            var label=Rect("Label",panel,new Vector2(580,30),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=20; label.alignment=TextAnchor.MiddleCenter;
            label.color=new Color32(244,234,218,255); label.raycastTarget=false; label.text="CHẰN TINH";
            var hud=root.GetComponent<BossHud>(); Set(hud,"_group",group); Set(hud,"_fill",fill); Set(hud,"_label",label);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/UI/Resources/UI/BossHud.prefab");
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
#endif
