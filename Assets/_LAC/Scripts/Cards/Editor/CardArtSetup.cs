#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardArtSetup
    {
        public const string IconFolder = "Assets/_LAC/Art/Sprites/UI/Cards/DongHo_2026";

        [MenuItem("LAC/Art/Apply Unified Card Icons")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Thoát Play Mode trước khi gán ảnh thẻ.");

            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var recipes = Resources.LoadAll<CardEvolutionData>("Evolutions");
            if (cards.Length != 12 || recipes.Length != 8)
                throw new InvalidOperationException("Cần đúng 12 thẻ nền và 8 tiến hoá.");
            // Kiểm đủ đầu vào trước khi sửa tham chiếu, tránh gán một nửa bộ ảnh.
            foreach (var card in cards) RequireFile(card.Id.ToString());
            foreach (var recipe in recipes) RequireFile(recipe.Id);

            foreach (var card in cards) Assign(card, card.Id.ToString());
            foreach (var recipe in recipes)
            {
                Assign(recipe, recipe.Id);
                Assign(recipe.Bonus, recipe.Id);
            }
            AssetDatabase.SaveAssets();
            Validate();
        }

        [MenuItem("LAC/Art/Validate Unified Card Icons")]
        public static void Validate()
        {
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var card in Resources.LoadAll<CardDefinition>("Cards")) Check(card.Icon, card.Id.ToString(), paths);
            foreach (var recipe in Resources.LoadAll<CardEvolutionData>("Evolutions"))
            {
                Check(recipe.Icon, recipe.Id, paths);
                if (recipe.Bonus.Icon != recipe.Icon) throw new Exception("Bonus sai icon: " + recipe.Id);
            }
            if (paths.Count != 20) throw new Exception("Cần đủ 20 icon riêng biệt.");
            Debug.Log("[CardArt] PASS: 20 distinct icons, correct card/recipe/bonus mapping, Sprite Single, Point, no mipmaps.");
        }

        public static string PathFor(string id) => $"{IconFolder}/CardIcon_{id}.png";

        private static void RequireFile(string id)
        {
            if (!File.Exists(PathFor(id))) throw new FileNotFoundException("Thiếu ảnh thẻ: " + id, PathFor(id));
        }

        private static void Assign(UnityEngine.Object target, string id)
        {
            string path = PathFor(id);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new Exception("Không nhập được Sprite: " + path);
            var serialized = new SerializedObject(target);
            serialized.FindProperty("_icon").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Check(Sprite icon, string id, System.Collections.Generic.HashSet<string> paths)
        {
            string path = AssetDatabase.GetAssetPath(icon);
            if (icon == null || path != PathFor(id) || !paths.Add(path)) throw new Exception("Icon thiếu/trùng/sai thẻ: " + id);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || importer.filterMode != FilterMode.Point || importer.mipmapEnabled)
                throw new Exception("Thiết lập import sai: " + id);
        }
    }
}
#endif
