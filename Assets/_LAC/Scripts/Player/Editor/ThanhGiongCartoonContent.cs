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
    /// <summary>Cắt nguồn cartoon qua Unity API; giữ AA/màu mẫu duyệt, không lượng tử hóa pixel.</summary>
    public static class ThanhGiongCartoonContent
    {
        public const string AtlasPath = "Assets/_LAC/Art/Sprites/ThanhGiongCartoon.png";
        public const string FullPath = "Assets/_LAC/Art/Sprites/ThanhGiongCartoonFull.png";
        public const string PartsPath = "Assets/_LAC/Data/Animations/ThanhGiongCartoonParts.asset";
        public const string SetPath = "Assets/_LAC/Data/Animations/ThanhGiongCartoon.asset";
        public const string WeaponPath = "Assets/_LAC/Data/Animations/TreNgaCartoon.asset";

        public static string Build()
        {
            RequireEdit();
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var atlas = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            var full = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                source.LoadImage(File.ReadAllBytes("Assets/_LAC/Art/Source/ThanhGiongCartoonParts.png"));
                if (source.width != 1536 || source.height != 1024) throw new InvalidOperationException("Nguồn parts khác layout 1536×1024 đã duyệt.");
                atlas.SetPixels32(new Color32[1024 * 1024]); full.SetPixels32(new Color32[256 * 256]);
                // Nguồn AI không tuân thủ hàng bằng nhau: dùng vùng đã quan sát và bbox alpha.
                Draw(source, new RectInt(80, 0, 450, 665), atlas, new Vector2(256, 584), 384);
                Draw(source, new RectInt(650, 450, 260, 190), atlas, new Vector2(640, 832), 62);
                Draw(source, new RectInt(1120, 450, 280, 190), atlas, new Vector2(896, 832), 62);
                Draw(source, new RectInt(130, 700, 285, 235), atlas, new Vector2(640, 576), 68);
                Draw(source, new RectInt(580, 700, 285, 235), atlas, new Vector2(896, 576), 68);
                RectInt bamboo = Bounds(source, new RectInt(945, 735, 590, 180));
                DrawBounds(source, bamboo, atlas, new Vector2(256, 320 - bamboo.height * 440f / bamboo.width * .5f), 440f / bamboo.width);
                atlas.Apply(); File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
                // Ghép bản toàn thân cho portrait và afterimage; renderer runtime dùng các mảnh.
                Stamp(atlas, new RectInt(512, 768, 256, 256), new Vector2(128, 96), full, new Vector2(128 - .18f * 172.8f, 80 - .34f * 172.8f), .54f);
                Stamp(atlas, new RectInt(768, 768, 256, 256), new Vector2(128, 96), full, new Vector2(128 + .18f * 172.8f, 80 - .34f * 172.8f), .54f);
                Stamp(atlas, new RectInt(0, 512, 512, 512), new Vector2(256, 168), full, new Vector2(128, 80), .54f);
                Stamp(atlas, new RectInt(512, 512, 256, 256), new Vector2(128, 98), full, new Vector2(128 - .32f * 172.8f, 80 - .03f * 172.8f), .54f);
                Stamp(atlas, new RectInt(768, 512, 256, 256), new Vector2(128, 98), full, new Vector2(128 + .36f * 172.8f, 80 - .03f * 172.8f), .54f);
                full.Apply(); File.WriteAllBytes(FullPath, full.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(atlas); UnityEngine.Object.DestroyImmediate(full); }
            Slice(AtlasPath, 320f, new[] {
                Rect("GiongCartoon_Body",0,512,512,512,256,168),
                Rect("GiongCartoon_FootLeft",512,768,256,256,128,96), Rect("GiongCartoon_FootRight",768,768,256,256,128,96),
                Rect("GiongCartoon_HandLeft",512,512,256,256,128,98), Rect("GiongCartoon_HandRight",768,512,256,256,128,98),
                Rect("GiongCartoon_Bamboo",0,256,512,128,80,64)
            });
            Slice(FullPath, 172.8f, new[] { Rect("GiongCartoon_Full",0,0,256,256,128,80) });
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            Sprite fullSprite = AssetDatabase.LoadAllAssetsAtPath(FullPath).OfType<Sprite>().Single();
            var parts = LoadOrCreate<CartoonCharacterData>(PartsPath); var so = new SerializedObject(parts);
            foreach (var pair in new[] { ("_body","Body"), ("_leftFoot","FootLeft"), ("_rightFoot","FootRight"), ("_leftHand","HandLeft"), ("_rightHand","HandRight") })
                so.FindProperty(pair.Item1).objectReferenceValue = sprites.Single(s => s.name == "GiongCartoon_" + pair.Item2);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(parts);
            var weapon = LoadOrCreate<DetachedWeaponData>(WeaponPath); so = new SerializedObject(weapon);
            so.FindProperty("_sprite").objectReferenceValue = sprites.Single(s => s.name.EndsWith("Bamboo"));
            so.FindProperty("_handOffset").vector2Value = new Vector2(.36f, -.03f);
            so.FindProperty("_tipDistance").floatValue = 395f / 320f;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(weapon);
            var set = LoadOrCreate<SpriteAnimationSet>(SetPath); so = new SerializedObject(set);
            var clips = so.FindProperty("_clips"); clips.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i); clip.FindPropertyRelative("_state").enumValueIndex = i;
                var frames = clip.FindPropertyRelative("_frames"); frames.arraySize = 1; frames.GetArrayElementAtIndex(0).objectReferenceValue = fullSprite;
                clip.FindPropertyRelative("_fps").floatValue = 1f;
                clip.FindPropertyRelative("_loop").boolValue = i < 2; clip.FindPropertyRelative("_holdLastFrame").boolValue = i == 4;
            }
            so.FindProperty("_walkReferenceSpeed").floatValue = 4f; so.FindProperty("_playHurtClip").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Cartoon: 6 mảnh HD/PPU320 + toàn thân256/PPU172.8; Bilinear/AA; chân/tay/tre độc lập; giữ ID/chỉ số.";
        }

        public static void ApplyIfAvailable()
        {
            RequireEdit();
            var parts = AssetDatabase.LoadAssetAtPath<CartoonCharacterData>(PartsPath);
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            var weapon = AssetDatabase.LoadAssetAtPath<DetachedWeaponData>(WeaponPath);
            if (!parts || !set || !weapon) return;
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
            var so = new SerializedObject(data);
            Sprite full = set.Get(AnimState.Idle).Frames[0];
            so.FindProperty("_bodySprite").objectReferenceValue = full; so.FindProperty("_portrait").objectReferenceValue = full;
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.FindProperty("_detachedWeapon").objectReferenceValue = weapon; so.FindProperty("_cartoonVisual").objectReferenceValue = parts;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var data = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!data) { data = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(data, path); } return data;
        }
        private static SpriteRect Rect(string name, int x, int y, int w, int h, int px, int py) => new SpriteRect {
            name = name, rect = new Rect(x,y,w,h), pivot = new Vector2(px/(float)w,py/(float)h), alignment = SpriteAlignment.Custom
        };
        private static RectInt Bounds(Texture2D source, RectInt topLeft)
        {
            int loX=source.width,loY=source.height,hiX=-1,hiY=-1;
            for(int y=source.height-topLeft.yMax;y<source.height-topLeft.yMin;y++) for(int x=topLeft.xMin;x<topLeft.xMax;x++)
                if(source.GetPixel(x,y).a>.75f){loX=Mathf.Min(loX,x);loY=Mathf.Min(loY,y);hiX=Mathf.Max(hiX,x);hiY=Mathf.Max(hiY,y);}
            if(hiX<loX)throw new InvalidOperationException("Mảnh rỗng."); return new RectInt(loX,loY,hiX-loX+1,hiY-loY+1);
        }
        private static void Draw(Texture2D source, RectInt crop, Texture2D dest, Vector2 bottomCenter, int height)
        {
            RectInt b=Bounds(source,crop); DrawBounds(source,b,dest,bottomCenter,height/(float)b.height);
        }
        private static void DrawBounds(Texture2D source, RectInt b, Texture2D dest, Vector2 bottomCenter, float scale)
        {
            int w=Mathf.RoundToInt(b.width*scale),h=Mathf.RoundToInt(b.height*scale),ox=Mathf.RoundToInt(bottomCenter.x-w*.5f),oy=Mathf.RoundToInt(bottomCenter.y);
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                Color c=source.GetPixelBilinear((b.x+(x+.5f)/scale)/source.width,(b.y+(y+.5f)/scale)/source.height);
                if(c.a<.1f)c=Color.clear; else if(c.a>.98f)c.a=1f;
                dest.SetPixel(ox+x,oy+y,c);
            }
        }
        private static void Stamp(Texture2D source, RectInt rect, Vector2 pivot, Texture2D dest, Vector2 origin, float scale)
        {
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float sx=(x-origin.x)/scale+pivot.x,sy=(y-origin.y)/scale+pivot.y;
                if(sx<0||sy<0||sx>=rect.width||sy>=rect.height)continue;
                Color c=source.GetPixelBilinear((rect.x+sx)/source.width,(rect.y+sy)/source.height);
                if(c.a<.01f)continue; Color old=dest.GetPixel(x,y);float a=c.a+old.a*(1f-c.a);
                Color result=(c*c.a+old*old.a*(1f-c.a))/Mathf.Max(a,.001f);result.a=a;dest.SetPixel(x,y,result);
            }
        }
        private static void Slice(string path,float ppu,SpriteRect[] rects)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=ppu;
            importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Thiếu sprite provider.");provider.InitSpriteEditorDataProvider();
            var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();if(edit==null)throw new InvalidOperationException("Thiếu quyền sửa frame.");
            foreach(var c in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot})
                if(!edit.GetEditCapability().HasCapability(c))throw new InvalidOperationException("Không hỗ trợ "+c);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(names==null)throw new InvalidOperationException("Thiếu ID provider.");
            var old=provider.GetSpriteRects();foreach(var r in rects)r.spriteID=old.FirstOrDefault(o=>o.name==r.name)?.spriteID??GUID.Generate();
            provider.SetSpriteRects(rects);names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
        }
        private static void RequireEdit(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Thoát Play trước khi dựng cartoon.");}
    }
}
#endif
