#if UNITY_EDITOR
using System.IO;
using LAC.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LAC.Enemies.Editor
{
    public static class EnemyContentBuilder
    {
        private const string Root = "Assets/_LAC/";
        private static void Set(Object obj, string field, object value)
        {
            var s = new SerializedObject(obj); var p = s.FindProperty(field);
            if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is string text) p.stringValue = text;
            else p.objectReferenceValue = value as Object;
            s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj);
        }
        private static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        private static Sprite Sprite(string name, int kind)
        {
            string path = Root + "Art/Sprites/Enemy_" + name + ".png";
            var tex = new Texture2D(32,32,TextureFormat.RGBA32,false);
            var pixels = new Color[1024];
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                float dx=x-15.5f; bool body=false; Color c=Color.clear;
                if(kind==0) { body=dx*dx+(y-16)*(y-16)<85 || (y>16 && y<30 && Mathf.Abs(dx-(y-16)*.3f)<(30-y)*.38f); c=C("#9CCFC0"); }
                if(kind==1) { body=(y>5&&y<22&&Mathf.Abs(dx)<4)||(y>17&&y<21&&Mathf.Abs(dx)<13)||(y>23&&y<27&&Mathf.Abs(dx)<10)||(y>20&&y<24&&Mathf.Abs(dx)<6); c=y>23?C("#C08D20"):C("#B37F4F"); }
                if(kind==2) { body=y>3&&y<28&&Mathf.Abs(dx)<(y<18?11-(18-y)*.35f:11-(y-18)*.8f); c=C("#4FA694"); }
                if(kind==3) { body=dx*dx+(y-15)*(y-15)<95 || (y>21&&y<29&&Mathf.Abs(dx)>5&&Mathf.Abs(dx)<10); c=C("#2F7480"); }
                if(kind==4) { body=dx*dx+(y-15.5f)*(y-15.5f)<100; c=dx*dx+(y-15.5f)*(y-15.5f)<40?C("#FF7A55"):C("#A82018"); }
                if(body) { if(kind<4 && y>=17&&y<=19&&(x==12||x==13||x==18||x==19)) c=C("#F4EADA"); pixels[y*32+x]=c; }
            }
            tex.SetPixels(pixels); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
            importer.spritePixelsPerUnit=kind==4?100:32; importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        [MenuItem("LAC/Enemies/Rebuild T34 Content")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
            Directory.CreateDirectory(Root+"Data/Enemies/Resources/Enemies");
            Directory.CreateDirectory(Root+"Prefabs/Enemies/Resources/Enemies");
            AssetDatabase.Refresh();
            string[] names={"MaTroi","BuNhin","MaDa","QuyNho"}; string[] labels={"Ma Trơi","Bù Nhìn","Ma Da","Quỷ Nhỏ"};
            int[] hp={8,40,12,15}; float[] speed={2,1.5f,6,2.5f}; int[] behaviour={1,0,2,3};
            var all=new EnemyData[5]; all[0]=AssetDatabase.LoadAssetAtPath<EnemyData>(Root+"Data/Enemies/CoHon.asset");
            for(int i=0;i<4;i++)
            {
                string path=Root+"Data/Enemies/"+names[i]+".asset";
                var data=AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if(data==null){ data=ScriptableObject.CreateInstance<EnemyData>(); AssetDatabase.CreateAsset(data,path); }
                Set(data,"_displayName",labels[i]); Set(data,"_behaviour",behaviour[i]); Set(data,"_maxHealth",hp[i]); Set(data,"_moveSpeed",speed[i]);
                Set(data,"_contactDamage",i==1?3:1); Set(data,"_attackRange",i==0?9f:.7f); Set(data,"_attackInterval",i==0?2f:.8f);
                Set(data,"_bodySprite",Sprite(names[i],i)); Set(data,"_animationSet",null);
                Set(data,"_splitChild",i==3?all[0]:null); Set(data,"_soulDropMin",i==1?3:1); Set(data,"_soulDropMax",i==1?3:1);
                all[i+1]=data;
            }
            string catalogPath=Root+"Data/Enemies/Resources/Enemies/EnemyCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<EnemyCatalogData>(catalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<EnemyCatalogData>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            var so=new SerializedObject(catalog); var array=so.FindProperty("_enemies"); array.arraySize=all.Length;
            for(int i=0;i<all.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=all[i]; so.ApplyModifiedPropertiesWithoutUndo();
            string matPath=Root+"Art/Materials/EnemyTelegraph.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("LAC/EnemyTelegraph"));AssetDatabase.CreateAsset(mat,matPath);}
            var preview=EditorSceneManager.NewPreviewScene();
            var go=new GameObject("EnemyProjectile"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=Sprite("Shot",4); sr.sortingOrder=100;
            go.AddComponent<EnemyProjectile>();
            PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/Enemies/Resources/Enemies/EnemyProjectile.prefab");
            EditorSceneManager.ClosePreviewScene(preview);
            string enemyPath=Root+"Prefabs/Enemies/Enemy.prefab";
            var enemy=PrefabUtility.LoadPrefabContents(enemyPath);
            var line=enemy.GetComponentInChildren<LineRenderer>(true);
            if(line==null){ var warning=new GameObject("ChargeWarning"); warning.transform.SetParent(enemy.transform,false); line=warning.AddComponent<LineRenderer>(); }
            line.sharedMaterial=mat; line.useWorldSpace=false; line.positionCount=2; line.startWidth=line.endWidth=.08f;
            line.startColor=line.endColor=C("#FF7A55"); line.sortingOrder=99; line.enabled=false;
            PrefabUtility.SaveAsPrefabAsset(enemy,enemyPath); PrefabUtility.UnloadPrefabContents(enemy);
            // Tăng dần thành phần, giữ nhịp và số hướng của bảng T-44. Quỷ Nhỏ tốn 2 suất.
            var table=AssetDatabase.LoadAssetAtPath<WaveTable>(Root+"Data/Waves/WaveTable_CoDinh.asset");
            so=new SerializedObject(table); var waves=so.FindProperty("_waves");
            for(int w=0;w<waves.arraySize;w++)
            {
                int budget=Mathf.Min(6+w*2,40); int ranged=w>=1?Mathf.Min(2+w/4,5):0;
                int tanks=w>=3?Mathf.Min(1+w/5,3):0; int chargers=w>=5?Mathf.Min(2+w/5,4):0;
                int splitters=w>=7?Mathf.Min(1+w/6,3):0;
                int[] counts={budget-ranged-tanks-chargers-splitters*2,ranged,tanks,chargers,splitters};
                var groups=waves.GetArrayElementAtIndex(w).FindPropertyRelative("_groups"); groups.arraySize=5;
                for(int i=0;i<5;i++){var g=groups.GetArrayElementAtIndex(i);g.FindPropertyRelative("_enemy").objectReferenceValue=all[i];g.FindPropertyRelative("_count").intValue=counts[i];}
            }
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            BossContentBuilder.Build();
            Debug.Log("[EnemyContent] T34 assets ready");
        }
    }
}
#endif
