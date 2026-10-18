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
    /// <summary>Chu kỳ chạy/vung tre thống nhất dáng, giữ ảnh gốc v1 để đối chiếu.</summary>
    public static class ThanhGiongMotionContent
    {
        public const string SourcePath = "Assets/_LAC/Art/Source/ThanhGiongMotion.png";
        public const string SheetPath = "Assets/_LAC/Art/Sprites/ThanhGiongMotion.png";

        public static string Build()
        {
            RequireEdit();
            var palette = AssetDatabase.LoadAssetAtPath<PaletteData>("Assets/_LAC/Art/Palettes/DongHo24.asset");
            if (palette == null) throw new InvalidOperationException("Thiếu palette.");
            var colors = new List<Color32>();
            for (int i = 0; i < palette.Count; i++)
                if (palette[i].Family != PigmentFamily.Son) colors.Add(palette[i].Color);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(SourcePath)) || source.width != source.height || source.width < 256)
                    throw new InvalidOperationException("Cần sheet vuông 4×4.");
                float cell = source.width / 4f;
                Color32[] input = source.GetPixels32(), output = new Color32[256 * 256];
                for (int frame = 0; frame < 16; frame++)
                {
                    int column = frame % 4, row = frame / 4;
                    // Một phép đổi tỷ lệ/pivot chung cho cả chuỗi, không co giãn từng dáng riêng.
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 64; x++)
                        {
                            int sx = Mathf.FloorToInt(((x - 32f) / .92f + 32f) * cell / 64f);
                            int sy = Mathf.FloorToInt(((y - 24f) / .92f + 27f) * cell / 64f);
                            if (sx < 0 || sx >= cell || sy < 0 || sy >= cell) continue;
                            int px = Mathf.Clamp(Mathf.RoundToInt(column * cell) + sx, 0, source.width - 1);
                            int py = Mathf.Clamp(Mathf.RoundToInt((3 - row) * cell) + sy, 0, source.height - 1);
                            Color32 p = input[py * source.width + px];
                            if (p.a < 192) continue;
                            int best = 0, distance = int.MaxValue;
                            for (int i = 0; i < colors.Count; i++)
                            {
                                int r = p.r - colors[i].r, g = p.g - colors[i].g, b = p.b - colors[i].b;
                                int candidate = r * r + g * g + b * b;
                                if (candidate < distance) { best = i; distance = candidate; }
                            }
                            Color32 color = colors[best]; color.a = 255;
                            output[((3 - row) * 64 + y) * 256 + column * 64 + x] = color;
                        }
                }
                sheet.SetPixels32(output); sheet.Apply(); File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(sheet); }
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Không có sprite provider.");
            provider.InitSpriteEditorDataProvider();
            var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (edit == null) throw new InvalidOperationException("Không thể sửa frame.");
            foreach (var capability in new[] { EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteName,
                         EEditCapability.EditSpriteRect, EEditCapability.EditPivot })
                if (!edit.GetEditCapability().HasCapability(capability)) throw new InvalidOperationException("Không hỗ trợ " + capability);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names == null) throw new InvalidOperationException("Không có sprite ID provider.");
            var old = provider.GetSpriteRects(); var rects = new List<SpriteRect>();
            for (int frame = 0; frame < 16; frame++)
            {
                string name = "ThanhGiong_" + (frame < 8 ? "Run_" : "Swing_") + (frame % 8).ToString("00");
                rects.Add(new SpriteRect {
                    name = name, rect = new Rect(frame % 4 * 64, (3 - frame / 4) * 64, 64, 64),
                    pivot = new Vector2(.5f, .375f), alignment = SpriteAlignment.Custom,
                    spriteID = old.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate()
                });
            }
            provider.SetSpriteRects(rects.ToArray());
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport(); ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Motion v2: Walk 8 frame/16 FPS, Swing 8 frame, Idle cùng dáng; giữ damage/chu kỳ; wind-up/contact 0.15s, tổng 0.30s.";
        }

        public static void ApplyIfAvailable()
        {
            RequireEdit();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) return;
            if (sprites.Length != 16) throw new InvalidOperationException("Cần đủ 16 frame motion.");
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(ThanhGiongContent.SetPath);
            if (set == null) throw new InvalidOperationException("Dựng Thánh Gióng v1 trước.");
            var run = sprites.Where(s => s.name.Contains("_Run_")).OrderBy(s => s.name).ToArray();
            var swing = sprites.Where(s => s.name.Contains("_Swing_")).OrderBy(s => s.name).ToArray();
            var so = new SerializedObject(set); var clips = so.FindProperty("_clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i);
                var state = (AnimState)clip.FindPropertyRelative("_state").enumValueIndex;
                Sprite[] frames = state == AnimState.Idle ? new[] { swing[7] } : state == AnimState.Walk ? run : state == AnimState.Attack ? swing : null;
                if (frames == null) continue;
                var list = clip.FindPropertyRelative("_frames"); list.arraySize = frames.Length;
                for (int j = 0; j < frames.Length; j++) list.GetArrayElementAtIndex(j).objectReferenceValue = frames[j];
                clip.FindPropertyRelative("_fps").floatValue = state == AnimState.Walk ? 16 : state == AnimState.Attack ? 26.666667f : 1;
            }
            so.FindProperty("_walkReferenceSpeed").floatValue = 4f;
            // Hurt cũ cúi toàn thân, cắt ngang vung tre khi áp sát. SpriteFlash vẫn báo trúng đòn.
            so.FindProperty("_playHurtClip").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/Warrior.asset");
            so = new SerializedObject(data);
            so.FindProperty("_bodySprite").objectReferenceValue = swing[7];
            so.FindProperty("_portrait").objectReferenceValue = swing[7];
            // Dùng cùng dữ liệu host cho timeline/VFX, không kéo dài riêng ảnh ở client.
            var duration = so.FindProperty("_attackAnimationDuration");
            if (duration != null) duration.floatValue = .30f;
            var contact = so.FindProperty("_attackContactNormalized");
            if (contact != null) contact.floatValue = .5f;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
        }

        private static void RequireEdit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Thoát Play trước khi sửa motion.");
        }
    }
}
#endif
