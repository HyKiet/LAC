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
    /// <summary>Chuẩn hóa ảnh AI và thay phần hiển thị của lớp Warrior bằng Thánh Gióng.</summary>
    public static class ThanhGiongContent
    {
        public const string SourcePath = "Assets/_LAC/Art/Source/ThanhGiong.png";
        public const string SheetPath = "Assets/_LAC/Art/Sprites/ThanhGiong.png";
        public const string SetPath = "Assets/_LAC/Data/Animations/ThanhGiong.asset";
        private const string CharacterPath = "Assets/_LAC/Data/Characters/Warrior.asset";
        private static readonly string[] Rows = { "Idle", "Walk", "Attack", "Hurt" };

        public static string Build()
        {
            RequireEditMode();
            var palette = AssetDatabase.LoadAssetAtPath<PaletteData>("Assets/_LAC/Art/Palettes/DongHo24.asset");
            if (palette == null || !File.Exists(SourcePath)) throw new InvalidOperationException("Thiếu ảnh nguồn hoặc bảng màu Đông Hồ.");
            var colors = new List<Color32>();
            for (int i = 0; i < palette.Count; i++)
                if (palette[i].Family != PigmentFamily.Son) colors.Add(palette[i].Color);
            if (colors.Count != 20) throw new InvalidOperationException("Cần đúng 20 màu dành cho người chơi.");

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(384, 256, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(SourcePath)) || source.width != 1536 || source.height != 1024)
                    throw new InvalidOperationException("Ảnh nguồn phải là 1536×1024, lưới 6×4.");
                Color32[] input = source.GetPixels32();
                var output = new Color32[sheet.width * sheet.height];
                // Ảnh phóng lớn có viền alpha mềm; lấy mẫu điểm và cắt alpha để không có quầng trên sân tối.
                for (int y = 0; y < sheet.height; y++)
                    for (int x = 0; x < sheet.width; x++)
                    {
                        Color32 pixel = input[(y * 4 + 2) * source.width + x * 4 + 2];
                        if (pixel.a < 192) continue;
                        int best = 0, distance = int.MaxValue;
                        for (int i = 0; i < colors.Count; i++)
                        {
                            int dr = pixel.r - colors[i].r, dg = pixel.g - colors[i].g, db = pixel.b - colors[i].b;
                            int candidate = dr * dr + dg * dg + db * db;
                            if (candidate < distance) { distance = candidate; best = i; }
                        }
                        Color32 chosen = colors[best]; chosen.a = 255;
                        output[y * sheet.width + x] = chosen;
                    }
                sheet.SetPixels32(output); sheet.Apply();
                File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(sheet); }

            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Importer không hỗ trợ Sprite Editor.");
            provider.InitSpriteEditorDataProvider();
            var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (edit == null) throw new InvalidOperationException("Không có quyền sửa frame; dừng cắt ảnh.");
            var capability = edit.GetEditCapability();
            foreach (var required in new[] { EEditCapability.CreateAndDeleteSprite, EEditCapability.EditSpriteName,
                         EEditCapability.EditSpriteRect, EEditCapability.EditPivot })
                if (!capability.HasCapability(required)) throw new InvalidOperationException("Importer không hỗ trợ " + required);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names == null) throw new InvalidOperationException("Thiếu provider định danh sprite.");
            SpriteRect[] old = provider.GetSpriteRects();
            var rects = new List<SpriteRect>();
            for (int row = 0; row < Rows.Length; row++)
                for (int frame = 0; frame < 6; frame++)
                {
                    string name = "ThanhGiong_" + Rows[row] + "_" + frame.ToString("00");
                    rects.Add(new SpriteRect {
                        name = name, rect = new Rect(frame * 64, (3 - row) * 64, 64, 64),
                        alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, .375f),
                        spriteID = old.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate()
                    });
                }
            provider.SetSpriteRects(rects.ToArray());
            names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            ApplyIfAvailable(); AssetDatabase.SaveAssets();
            return "Thánh Gióng: 24 frame, 64×64, PPU 32, alpha nhị phân, 20 màu; giữ ID Warrior và chỉ số hiện tại.";
        }

        public static void ApplyIfAvailable()
        {
            RequireEditMode();
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) return;
            if (sprites.Length != 24) throw new InvalidOperationException("Thánh Gióng cần đủ 24 frame.");
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(CharacterPath);
            if (character == null || character.Id != CharacterId.Warrior)
                throw new InvalidOperationException("Không tìm thấy lớp Warrior hợp lệ.");
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<SpriteAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            var so = new SerializedObject(set); var clips = so.FindProperty("_clips"); clips.arraySize = 5;
            for (int row = 0; row < 4; row++)
                ConfigureClip(clips.GetArrayElementAtIndex(row), (AnimState)row,
                    sprites.Where(s => s.name.StartsWith("ThanhGiong_" + Rows[row] + "_")).OrderBy(s => s.name).ToArray(),
                    row == 0 ? 8 : row == 1 ? 12 : row == 2 ? 30 : 18, row < 2, false);
            Sprite idle = sprites.Single(s => s.name == "ThanhGiong_Idle_00");
            ConfigureClip(clips.GetArrayElementAtIndex(4), AnimState.Death,
                new[] { sprites.Single(s => s.name == "ThanhGiong_Hurt_02") }, 1, false, true);
            so.FindProperty("_playHurtClip").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            so = new SerializedObject(character);
            so.FindProperty("_displayName").stringValue = "Thánh Gióng";
            so.FindProperty("_weaponName").stringValue = "Tre ngà";
            so.FindProperty("_description").stringValue = "Vung tre quét phía trước. Bền bỉ khi áp sát.";
            so.FindProperty("_bodySprite").objectReferenceValue = idle;
            so.FindProperty("_portrait").objectReferenceValue = idle;
            so.FindProperty("_animationSet").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(character);
        }

        private static void ConfigureClip(SerializedProperty clip, AnimState state, Sprite[] frames, float fps, bool loop, bool hold)
        {
            if (frames.Length == 0) throw new InvalidOperationException("Thiếu frame " + state);
            clip.FindPropertyRelative("_state").enumValueIndex = (int)state;
            clip.FindPropertyRelative("_fps").floatValue = fps;
            clip.FindPropertyRelative("_loop").boolValue = loop;
            clip.FindPropertyRelative("_holdLastFrame").boolValue = hold;
            var list = clip.FindPropertyRelative("_frames"); list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Thoát Play mode trước khi sửa ảnh/dữ liệu nhân vật.");
        }
    }
}
#endif
