#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LAC.Utils;
using LAC.VFX;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LAC.Player.Editor
{
    /// <summary>Chuẩn hóa frame toàn thân AI; chỉ tre ngà tách renderer.</summary>
    public static class ThanhGiongPixelContent
    {
        public const string SourcePath = "Assets/_LAC/Art/Source/ThanhGiongPixelBody.png";
        public const string BambooSourcePath = "Assets/_LAC/Art/Source/TreNgaPixel.png";
        public const string SheetPath = "Assets/_LAC/Art/Sprites/ThanhGiongPixel.png";
        public const string WeaponPath = "Assets/_LAC/Art/Sprites/TreNgaPixel.png";
        public const string SetPath = "Assets/_LAC/Data/Animations/ThanhGiongPixel.asset";
        public const string VisualPath = "Assets/_LAC/Data/Animations/TreNgaPixel.asset";
        private const int Ppu = 32;

        public static string Build()
        {
            RequireEdit();
            var palette = AssetDatabase.LoadAssetAtPath<PaletteData>("Assets/_LAC/Art/Palettes/DongHo24.asset");
            if (palette == null) throw new InvalidOperationException("Thiếu palette Đông Hồ.");
            Color32[] colors = Enumerable.Range(0, palette.Count).Where(i => palette[i].Family != PigmentFamily.Son)
                .Select(i => (Color32)palette[i].Color).ToArray();
            var handOffsets = new Vector2[16];
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(SourcePath)) || source.width != source.height)
                    throw new InvalidOperationException("Cần nguồn vuông toàn thân 4×4.");
                Color32[] input = source.GetPixels32(), output = new Color32[256 * 256];
                var bounds = Enumerable.Range(0, 16).Select(i => Bounds(input, source.width, source.height,
                    new RectInt(i % 4 * source.width / 4, (3 - i / 4) * source.height / 4, source.width / 4, source.height / 4))).ToArray();
                float scale = 40f / bounds[0].height;
                if (bounds.Max(b => b.width) * scale > 54f) scale = 54f / bounds.Max(b => b.width);
                for (int frame = 0; frame < 16; frame++)
                {
                    RectInt b = bounds[frame];
                    RectInt head = Bounds(input, source.width, source.height, new RectInt(b.x, b.y + b.height / 2, b.width, b.height - b.height / 2));
                    float center = frame < 12 ? head.x + head.width * .5f : b.x + b.width * .5f;
                    // Lấy tâm da bàn tay phía trước từ nguồn trước khi ép palette; mặt ở nửa trên,
                    // quần sáng và giáp vàng không thuộc dải da. Gục không cần vũ khí.
                    Vector2 handSum = Vector2.zero; int handCount = 0;
                    for (int hy = b.yMin + b.height / 5; hy < b.yMin + b.height / 2; hy++)
                        for (int hx = Mathf.RoundToInt(center + b.width * .17f); hx < b.xMax; hx++)
                        {
                            Color32 c = input[hy * source.width + hx];
                            if (c.a < 192 || c.r < c.g * 1.1f || c.g < c.b * 1.15f || c.b < c.g * .55f || c.g < 130) continue;
                            handSum += new Vector2(hx, hy); handCount++;
                        }
                    if (frame < 12 && handCount == 0) throw new InvalidOperationException("Không tìm thấy điểm cầm: " + frame);
                    Vector2 sourceHand = handCount > 0 ? handSum / handCount : new Vector2(center, b.yMin);
                    handOffsets[frame] = new Vector2(Mathf.Round((sourceHand.x - center) * scale),
                        Mathf.Round(8f + (sourceHand.y - b.yMin) * scale) - 24f) / Ppu;
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    {
                        int sx = Mathf.FloorToInt(center + (x - 32f + .5f) / scale);
                        int sy = Mathf.FloorToInt(b.y + (y - 8f + .5f) / scale);
                        if (sx < b.xMin || sx >= b.xMax || sy < b.yMin || sy >= b.yMax) continue;
                        Color32 c = input[sy * source.width + sx];
                        if (c.a >= 192) output[((3 - frame / 4) * 64 + y) * 256 + frame % 4 * 64 + x] = Nearest(c, colors);
                    }
                }
                Write(SheetPath, output, 256, 256);
                if (!source.LoadImage(File.ReadAllBytes(BambooSourcePath))) throw new InvalidOperationException("Thiếu nguồn tre.");
                input = source.GetPixels32();
                RectInt bamboo = Bounds(input, source.width, source.height, new RectInt(0, 0, source.width, source.height));
                float factor = 56f / bamboo.width;
                if (bamboo.height * factor > 10f) throw new InvalidOperationException("Tre phải thon, không phải chùy.");
                float heightFactor = 6f / bamboo.height;
                output = new Color32[64 * 16];
                for (int y = 0; y < 16; y++) for (int x = 4; x < 60; x++)
                {
                    int sx = Mathf.FloorToInt(bamboo.x + (x - 4f + .5f) / factor);
                    int sy = Mathf.FloorToInt(bamboo.center.y + (y - 8f + .5f) / heightFactor);
                    if (sy < bamboo.yMin || sy >= bamboo.yMax) continue;
                    Color32 c = input[sy * source.width + sx];
                    if (c.a >= 192) output[y * 64 + x] = Nearest(c, colors);
                }
                Write(WeaponPath, output, 64, 16);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }

            Slice(SheetPath, Enumerable.Range(0, 16).Select(i => Rect(Name(i),
                i % 4 * 64, (3 - i / 4) * 64, 64, 64, 32, 24)).ToArray());
            Slice(WeaponPath, new[] { Rect("TreNgaPixel", 0, 0, 64, 16, 10, 8) });
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var set = LoadOrCreate<SpriteAnimationSet>(SetPath); var so = new SerializedObject(set);
            var clips = so.FindProperty("_clips"); clips.arraySize = 5;
            var idle = sprites.Where(s => s.name.Contains("Idle")).ToArray();
            SetClip(clips, 0, AnimState.Idle, idle, 4f, true, false);
            SetClip(clips, 1, AnimState.Walk, sprites.Where(s => s.name.Contains("Run")).ToArray(), 16f, true, false);
            // Tre dùng timeline host; Attack toàn thân dự phòng không ngắt chân đang chạy.
            SetClip(clips, 2, AnimState.Attack, new[] { idle[0] }, 1f, false, false);
            SetClip(clips, 3, AnimState.Hurt, new[] { idle[0] }, 1f, false, false);
            SetClip(clips, 4, AnimState.Death, sprites.Where(s => s.name.Contains("Down")).ToArray(), 12f, false, true);
            so.FindProperty("_walkReferenceSpeed").floatValue = 4f;
            so.FindProperty("_playHurtClip").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            var visual = LoadOrCreate<DetachedWeaponData>(VisualPath); so = new SerializedObject(visual);
            so.FindProperty("_sprite").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath(WeaponPath).OfType<Sprite>().Single();
            var grips = so.FindProperty("_bodyGrips"); grips.arraySize = 12;
            for (int i = 0; i < 12; i++)
            {
                var grip = grips.GetArrayElementAtIndex(i);
                grip.FindPropertyRelative("_body").objectReferenceValue = sprites.Single(s => s.name == Name(i));
                grip.FindPropertyRelative("_offset").vector2Value = handOffsets[i];
            }
            so.FindProperty("_handOffset").vector2Value = new Vector2(.36f, -.03f);
            so.FindProperty("_tipDistance").floatValue = 49f / Ppu;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(visual);
            ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Pixel toàn thân: 16 frame 64×64/PPU32; thân ~40px; Idle4/Run8/Down4; chỉ tre tách riêng; giữ chỉ số/timeline host.";
        }

        private static string Name(int i) => i < 2 ? "GiongPixel_Idle_" + i : i < 10 ? "GiongPixel_Run_" + (i - 2) : i < 12 ? "GiongPixel_Idle_" + (i - 8) : "GiongPixel_Down_" + (i - 12);

        public static void ApplyIfAvailable()
        {
            RequireEdit();
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            var visual = AssetDatabase.LoadAssetAtPath<DetachedWeaponData>(VisualPath);
            if (set == null || visual == null) return;
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
            var so = new SerializedObject(data); Sprite idle = set.Get(AnimState.Idle).Frames[0];
            so.FindProperty("_bodySprite").objectReferenceValue = idle;
            so.FindProperty("_portrait").objectReferenceValue = idle;
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.FindProperty("_detachedWeapon").objectReferenceValue = visual;
            so.FindProperty("_cartoonVisual").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
            ThanhGiongForgeContent.ApplyIfAvailable();
        }

        private static RectInt Bounds(Color32[] input, int width, int height, RectInt rect)
        {
            int minX=width,minY=height,maxX=-1,maxY=-1;
            for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
                if(input[y*width+x].a>=192){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            if(maxX<minX)throw new InvalidOperationException("Ô sprite rỗng.");return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        private static Color32 Nearest(Color32 c,Color32[] colors)
        {
            int distance=int.MaxValue;Color32 best=default;
            foreach(Color32 p in colors){int r=p.r-c.r,g=p.g-c.g,b=p.b-c.b,d=r*r+g*g+b*b;if(d<distance){distance=d;best=p;}}
            best.a=255;return best;
        }
        private static void Write(string path, Color32[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try { tex.SetPixels32(pixels); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;
        }
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
            importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=Ppu;importer.filterMode=FilterMode.Point;
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
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Thoát Play trước khi dựng pixel.");
        }
    }
}
#endif
