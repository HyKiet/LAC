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
    /// <summary>Nguồn AI giữ nguyên; dựng sprite nhị phân và dữ liệu vũ khí qua Unity API.</summary>
    public static class ThanhGiongChibiContent
    {
        public const string BodyPath = "Assets/_LAC/Art/Sprites/ThanhGiongChibi.png";
        public const string WeaponPath = "Assets/_LAC/Art/Sprites/TreNga.png";
        public const string SetPath = "Assets/_LAC/Data/Animations/ThanhGiongChibi.asset";
        public const string VisualPath = "Assets/_LAC/Data/Animations/TreNgaVisual.asset";
        private const string Source = "Assets/_LAC/Art/Source/";

        public static string Build()
        {
            RequireEdit();
            var palette = AssetDatabase.LoadAssetAtPath<PaletteData>("Assets/_LAC/Art/Palettes/DongHo24.asset");
            var colors = new List<Color32>();
            if (palette == null) throw new InvalidOperationException("Thiếu palette Đông Hồ.");
            for (int i = 0; i < palette.Count; i++) if (palette[i].Family != PigmentFamily.Son) colors.Add(palette[i].Color);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(144, 144, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(Source + "ThanhGiongChibi.png")) || source.width != source.height)
                    throw new InvalidOperationException("Cần nguồn cơ thể vuông 3×3.");
                Color32[] pixels = source.GetPixels32(), output = new Color32[144 * 144];
                var bounds = Enumerable.Range(0, 9).Select(i => Bounds(pixels, source.width, source.height,
                    Mathf.RoundToInt(i % 3 * source.width / 3f), Mathf.RoundToInt((2 - i / 3) * source.height / 3f),
                    Mathf.FloorToInt(source.width / 3f), Mathf.FloorToInt(source.height / 3f))).ToArray();
                float scale = 32f / bounds[0].height;
                if (bounds.Max(b => b.width) * scale > 42f) scale = 42f / bounds.Max(b => b.width);
                for (int frame = 0; frame < 9; frame++)
                {
                    RectInt b = bounds[frame];
                    // Cùng hệ số cho mọi frame. Căn đỉnh đầu theo trục thân, chân theo mặt đất;
                    // không dùng bề rộng chân đang sải để làm tâm, tránh rung ngang.
                    RectInt head = Bounds(pixels, source.width, source.height, b.x, b.y + b.height / 2, b.width, b.height - b.height / 2);
                    float center = head.x + head.width * .5f;
                    for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                    {
                        int sx = Mathf.FloorToInt(center + (x - 24f + .5f) / scale);
                        int sy = Mathf.FloorToInt(b.y + (y - 8f + .5f) / scale);
                        if (sx < b.xMin || sx >= b.xMax || sy < b.yMin || sy >= b.yMax) continue;
                        Color32 p = pixels[sy * source.width + sx];
                        if (p.a < 192) continue;
                        output[((2 - frame / 3) * 48 + y) * 144 + frame % 3 * 48 + x] = Nearest(p, colors);
                    }
                }
                sheet.SetPixels32(output); sheet.Apply(); File.WriteAllBytes(BodyPath, sheet.EncodeToPNG());
                if (!source.LoadImage(File.ReadAllBytes(Source + "TreNga.png"))) throw new InvalidOperationException("Nguồn tre lỗi.");
                pixels = source.GetPixels32();
                RectInt bamboo = Bounds(pixels, source.width, source.height, 0, 0, source.width, source.height);
                float factor = 48f / bamboo.width;
                var weapon = new Texture2D(64, 16, TextureFormat.RGBA32, false);
                try
                {
                    output = new Color32[64 * 16];
                    for (int y = 0; y < 16; y++) for (int x = 8; x < 56; x++)
                    {
                        int sx = Mathf.FloorToInt(bamboo.x + (x - 8f + .5f) / factor);
                        int sy = Mathf.FloorToInt(bamboo.center.y + (y - 8f + .5f) / factor);
                        if (sy < bamboo.yMin || sy >= bamboo.yMax) continue;
                        Color32 p = pixels[sy * source.width + sx];
                        if (p.a >= 192) output[y * 64 + x] = Nearest(p, colors);
                    }
                    weapon.SetPixels32(output); weapon.Apply(); File.WriteAllBytes(WeaponPath, weapon.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(weapon); }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(sheet); }
            Slice(BodyPath, Enumerable.Range(0, 9).Select(i => new SpriteRect {
                name = i < 2 ? "GiongChibi_Idle_" + i : i < 8 ? "GiongChibi_Run_" + (i - 2) : "GiongChibi_Down",
                rect = new Rect(i % 3 * 48, (2 - i / 3) * 48, 48, 48),
                alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, 20f / 48f)
            }).ToArray());
            Slice(WeaponPath, new[] { new SpriteRect {
                name = "TreNga", rect = new Rect(0, 0, 64, 16), alignment = SpriteAlignment.Custom, pivot = new Vector2(14f / 64f, .5f)
            }});

            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<SpriteAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var so = new SerializedObject(set); var clips = so.FindProperty("_clips"); clips.arraySize = 5;
            SetClip(clips, 0, AnimState.Idle, sprites.Where(s => s.name.Contains("Idle")).ToArray(), 2f, true, false);
            SetClip(clips, 1, AnimState.Walk, sprites.Where(s => s.name.Contains("Run")).ToArray(), 12f, true, false);
            // Clip Attack dự phòng cho công cụ cũ; runtime vũ khí rời không chiếm locomotion.
            SetClip(clips, 2, AnimState.Attack, new[] { sprites.First(s => s.name.Contains("Idle_0")) }, 1f, false, false);
            SetClip(clips, 3, AnimState.Hurt, new[] { sprites.First(s => s.name.Contains("Idle_0")) }, 1f, false, false);
            SetClip(clips, 4, AnimState.Death, new[] { sprites.First(s => s.name.Contains("Down")) }, 1f, false, true);
            so.FindProperty("_walkReferenceSpeed").floatValue = 4f;
            so.FindProperty("_playHurtClip").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            var visual = AssetDatabase.LoadAssetAtPath<DetachedWeaponData>(VisualPath);
            if (visual == null) { visual = ScriptableObject.CreateInstance<DetachedWeaponData>(); AssetDatabase.CreateAsset(visual, VisualPath); }
            so = new SerializedObject(visual);
            so.FindProperty("_sprite").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath(WeaponPath).OfType<Sprite>().Single();
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(visual);
            ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Chibi: 9 frame 48×48/PPU32 (thân ~32px); Idle2/Run6/Down1; TreNga riêng, giữ chỉ số/ID.";
        }

        public static void ApplyIfAvailable()
        {
            RequireEdit();
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            var visual = AssetDatabase.LoadAssetAtPath<DetachedWeaponData>(VisualPath);
            if (set == null || visual == null) return;
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
            var so = new SerializedObject(data);
            Sprite idle = set.Get(AnimState.Idle).Frames[0];
            so.FindProperty("_bodySprite").objectReferenceValue = idle;
            so.FindProperty("_portrait").objectReferenceValue = idle;
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.FindProperty("_detachedWeapon").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
            ThanhGiongCartoonContent.ApplyIfAvailable();
        }

        private static void SetClip(SerializedProperty clips, int index, AnimState state, Sprite[] frames, float fps, bool loop, bool hold)
        {
            var c = clips.GetArrayElementAtIndex(index); c.FindPropertyRelative("_state").enumValueIndex = (int)state;
            var list = c.FindPropertyRelative("_frames"); list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            c.FindPropertyRelative("_fps").floatValue = fps;
            c.FindPropertyRelative("_loop").boolValue = loop;
            c.FindPropertyRelative("_holdLastFrame").boolValue = hold;
        }

        private static RectInt Bounds(Color32[] pixels, int width, int height, int x, int y, int w, int h)
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int py = y; py < Mathf.Min(height, y + h); py++) for (int px = x; px < Mathf.Min(width, x + w); px++)
                if (pixels[py * width + px].a >= 192) { minX = Mathf.Min(minX, px); minY = Mathf.Min(minY, py); maxX = Mathf.Max(maxX, px); maxY = Mathf.Max(maxY, py); }
            if (maxX < minX) throw new InvalidOperationException("Ô sprite rỗng.");
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Color32 Nearest(Color32 p, List<Color32> colors)
        {
            int best = 0, distance = int.MaxValue;
            for (int i = 0; i < colors.Count; i++)
            {
                int r = p.r - colors[i].r, g = p.g - colors[i].g, b = p.b - colors[i].b, d = r * r + g * g + b * b;
                if (d < distance) { best = i; distance = d; }
            }
            Color32 result = colors[best]; result.a = 255; return result;
        }

        private static void Slice(string path, SpriteRect[] rects)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Thiếu sprite provider.");
            provider.InitSpriteEditorDataProvider();
            var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (edit == null) throw new InvalidOperationException("Thiếu quyền sửa frame.");
            foreach (var capability in new[] { EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteName,
                EEditCapability.EditSpriteRect, EEditCapability.EditPivot })
                if (!edit.GetEditCapability().HasCapability(capability)) throw new InvalidOperationException("Không hỗ trợ " + capability);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names == null) throw new InvalidOperationException("Thiếu sprite ID provider.");
            SpriteRect[] old = provider.GetSpriteRects();
            foreach (var rect in rects) rect.spriteID = old.FirstOrDefault(r => r.name == rect.name)?.spriteID ?? GUID.Generate();
            provider.SetSpriteRects(rects); names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
        }

        private static void RequireEdit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Thoát Play trước khi dựng sprite.");
        }
    }
}
#endif
