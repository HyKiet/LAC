#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LAC.VFX;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LAC.Player.Editor
{
    /// <summary>Nhập mẫu Forge32 đã duyệt. Chuyển động cơ bản biến đổi cả ảnh, không tách tay/chân/tre.</summary>
    public static class ThanhGiongForgeContent
    {
        public const string SourcePath = "Assets/_LAC/Art/Source/ThanhGiongForge32.png";
        public const string PortraitPath = "Assets/_LAC/Art/Sprites/ThanhGiongForge32.png";
        public const string SheetPath = "Assets/_LAC/Art/Sprites/ThanhGiongForgeMotion.png";
        public const string SetPath = "Assets/_LAC/Data/Animations/ThanhGiongForge.asset";
        private const int Cell = 64;

        public static string Build()
        {
            RequireEdit();
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var atlas = new Texture2D(256, 384, TextureFormat.RGBA32, false);
            try
            {
                byte[] bytes = File.ReadAllBytes(SourcePath);
                if (!source.LoadImage(bytes) || source.width != 32 || source.height != 32)
                    throw new InvalidOperationException("Cần đúng mẫu Forge32 đã duyệt.");
                Color32[] pixels = source.GetPixels32();
                if (pixels.Any(c => c.a != 0 && c.a != 255)) throw new InvalidOperationException("Alpha phải nhị phân.");
                File.WriteAllBytes(PortraitPath, bytes);
                var output = new Color32[256 * 384];
                for (int frame = 0; frame < 24; frame++)
                {
                    float angle = 0f; int x = 0, y = 0;
                    if (frame < 4) y = frame == 2 ? 1 : 0;
                    else if (frame < 12)
                    {
                        int i = frame - 4;
                        y = new[] { 0, 1, 2, 1, 0, 1, 2, 1 }[i];
                        angle = new[] { -3f, 0f, 3f, 0f, 3f, 0f, -3f, 0f }[i];
                    }
                    else if (frame < 20)
                    {
                        int i = frame - 12;
                        angle = new[] { -4f, -8f, -10f, -2f, 8f, 10f, 4f, 0f }[i];
                        x = new[] { -1, -1, -1, 0, 2, 2, 1, 0 }[i];
                    }
                    else { angle = new[] { 0f, -25f, -55f, -80f }[frame - 20]; y = frame == 20 ? 0 : -1; }
                    Color32[] pose = Pose(pixels, angle, x, y);
                    int ox = frame % 4 * Cell, oy = (5 - frame / 4) * Cell;
                    for (int row = 0; row < Cell; row++) Array.Copy(pose, row * Cell, output, (oy + row) * 256 + ox, Cell);
                }
                atlas.SetPixels32(output); atlas.Apply(); File.WriteAllBytes(SheetPath, atlas.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(atlas); }
            Slice(PortraitPath, new[] { Rect("GiongForge_Portrait", 0, 0, 32, 32, 12, 14) });
            Slice(SheetPath, Enumerable.Range(0, 24).Select(i => Rect(Name(i), i % 4 * Cell, (5 - i / 4) * Cell, Cell, Cell, 32, 24)).ToArray());
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<SpriteAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            var so = new SerializedObject(set); var clips = so.FindProperty("_clips"); clips.arraySize = 5;
            SetClip(clips, 0, AnimState.Idle, sprites.Where(s => s.name.Contains("Idle")).ToArray(), 4f, true, false);
            SetClip(clips, 1, AnimState.Walk, sprites.Where(s => s.name.Contains("Walk")).ToArray(), 14f, true, false);
            SetClip(clips, 2, AnimState.Attack, sprites.Where(s => s.name.Contains("Attack")).ToArray(), 8f / .3f, false, false);
            SetClip(clips, 3, AnimState.Hurt, new[] { sprites.Single(s => s.name == "GiongForge_Idle_0") }, 1f, false, false);
            SetClip(clips, 4, AnimState.Death, sprites.Where(s => s.name.Contains("Down")).ToArray(), 12f, false, true);
            so.FindProperty("_walkReferenceSpeed").floatValue = 4f;
            so.FindProperty("_playHurtClip").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Forge32: mẫu liền tre; 24 frame biến đổi cả ảnh, không phải 24 dáng vẽ mới; PPU32/Point; tre rời và cartoon tắt; giữ gameplay.";
        }

        public static void ApplyIfAvailable()
        {
            RequireEdit();
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            var portrait = AssetDatabase.LoadAllAssetsAtPath(PortraitPath).OfType<Sprite>().SingleOrDefault();
            if (set == null || portrait == null) return;
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
            var so = new SerializedObject(data);
            so.FindProperty("_bodySprite").objectReferenceValue = set.Get(AnimState.Idle).Frames[0];
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.FindProperty("_detachedWeapon").objectReferenceValue = null;
            so.FindProperty("_cartoonVisual").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
        }

        private static Color32[] Pose(Color32[] source, float angle, int offsetX, int offsetY)
        {
            var pose = new Color32[Cell * Cell];
            float c = Mathf.Cos(angle * Mathf.Deg2Rad), s = Mathf.Sin(angle * Mathf.Deg2Rad);
            for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
            {
                // Giữ tâm cơ thể, không xoay actor/collider. Pivot nguồn lệch trái vì tre ở bên phải.
                float dx = x - 32 - offsetX, dy = y - 24 - offsetY;
                int sx = Mathf.RoundToInt(dx * c + dy * s + 12), sy = Mathf.RoundToInt(-dx * s + dy * c + 14);
                if (sx >= 0 && sx < 32 && sy >= 0 && sy < 32) pose[y * Cell + x] = source[sy * 32 + sx];
            }
            return pose;
        }
        private static string Name(int i) => i < 4 ? "GiongForge_Idle_" + i : i < 12 ? "GiongForge_Walk_" + (i - 4) : i < 20 ? "GiongForge_Attack_" + (i - 12) : "GiongForge_Down_" + (i - 20);
        private static SpriteRect Rect(string name,int x,int y,int width,int height,int px,int py) => new SpriteRect {
            name=name,rect=new Rect(x,y,width,height),alignment=SpriteAlignment.Custom,pivot=new Vector2(px/(float)width,py/(float)height)
        };
        private static void SetClip(SerializedProperty clips,int index,AnimState state,Sprite[] frames,float fps,bool loop,bool hold)
        {
            var clip=clips.GetArrayElementAtIndex(index);clip.FindPropertyRelative("_state").enumValueIndex=(int)state;
            var list=clip.FindPropertyRelative("_frames");list.arraySize=frames.Length;
            for(int i=0;i<frames.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
            clip.FindPropertyRelative("_fps").floatValue=fps;clip.FindPropertyRelative("_loop").boolValue=loop;clip.FindPropertyRelative("_holdLastFrame").boolValue=hold;
        }
        private static void Slice(string path,SpriteRect[] rects)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Thiếu sprite provider.");provider.InitSpriteEditorDataProvider();
            var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(capability==null)throw new InvalidOperationException("Thiếu edit capability.");
            foreach(var c in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot})
                if(!capability.GetEditCapability().HasCapability(c))throw new InvalidOperationException("Không hỗ trợ "+c);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(names==null)throw new InvalidOperationException("Thiếu ID provider.");
            var old=provider.GetSpriteRects();foreach(var r in rects)r.spriteID=old.FirstOrDefault(o=>o.name==r.name)?.spriteID??GUID.Generate();
            provider.SetSpriteRects(rects);names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
        }
        private static void RequireEdit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Thoát Play trước khi nhập Forge32.");
        }
    }
}
#endif
