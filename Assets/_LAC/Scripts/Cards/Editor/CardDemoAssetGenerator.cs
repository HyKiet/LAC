#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LAC.Combat;
using LAC.Core;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardDemoAssetGenerator
    {
        private const string DataFolder = "Assets/_LAC/Data/Cards/Resources/Cards";
        private const string IconFolder = "Assets/_LAC/Art/Sprites/UI/Cards/AI_Demo";
        private const string PrefabFolder = "Assets/_LAC/Prefabs/UI/Cards/Resources";
        private const string PrefabPath = PrefabFolder + "/CardSelection.prefab";

        [DidReloadScripts]
        private static void AfterScriptsReloaded() => EditorApplication.delayCall += EnsureMissingAssets;

        [MenuItem("LAC/Demo/Rebuild Card Demo Assets")]
        public static void RebuildFromMenu()
        {
            EnsureAssets(true);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void EnsureMissingAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureAssets(false);
        }

        private static void EnsureAssets(bool overwrite)
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);
            EnsureIconImports();

            CardDefinition[] cards =
            {
                EnsureCard("CuongCong", CardId.CuongCong, "Cường Công",
                    "Cộng 20% sát thương cơ bản. Cộng theo chỉ số gốc.", 3, Hex("EDBB3E"), overwrite),
                EnsureCard("LienKich", CardId.LienKich, "Liên Kích",
                    "Cộng 15% tốc độ đánh cơ bản (giảm khoảng nghỉ giữa hai đòn).", 3, Hex("FBDD82"), overwrite),
                EnsureCard("SinhLuc", CardId.SinhLuc, "Sinh Lực",
                    "Tăng 25 máu tối đa và hồi ngay 25 máu.", 3, Hex("4FA694"), overwrite),
                EnsureCard("BoPhap", CardId.BoPhap, "Bộ Pháp",
                    "Giảm 20% thời gian hồi lướt.", 1, Hex("9CCFC0"), overwrite),
                EnsureCard("SongTien", CardId.SongTien, "Song Tiễn",
                    "Bắn 2 đạn lệch góc nhỏ; mỗi đạn gây 70% sát thương hiện tại.", 1, Hex("9CCFC0"), overwrite),
                EnsureCard("XuyenTam", CardId.XuyenTam, "Xuyên Tâm",
                    "Đạn xuyên thêm 2 kẻ địch, tối đa chạm 3 mục tiêu khác nhau.", 1, Hex("E0CFAF"), overwrite),
                EnsureCard("BocPha", CardId.BocPha, "Bộc Phá",
                    "Lần chạm đầu phát nổ, gây 30% sát thương đạn lên địch xung quanh.", 1, Hex("B37F4F"), overwrite)
            };

            EnsurePrefab(cards);
            if (overwrite) Validate(cards);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static CardDefinition EnsureCard(string fileName, CardId id, string displayName,
            string description, int maxStacks, Color accent, bool overwrite)
        {
            string path = $"{DataFolder}/{fileName}.asset";
            CardDefinition card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDefinition>();
                AssetDatabase.CreateAsset(card, path);
                overwrite = true;
            }

            if (overwrite)
                card.EditorConfigure(id, displayName, description, maxStacks, 1f, accent);
            var serialized = new SerializedObject(card);
            SerializedProperty icon = serialized.FindProperty("_icon");
            if (overwrite || icon.objectReferenceValue == null)
            {
                icon.objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/CardIcon_{fileName}.png");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(card);
            }
            return card;
        }

        private static void EnsureIconImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                bool dirty = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || importer.maxTextureSize != 512
                    || !importer.alphaIsTransparency;
                if (!dirty) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
            return color;
        }

        private static void EnsurePrefab(CardDefinition[] cards)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;

            var root = new GameObject("CardSelection");
            root.AddComponent<CardSelectionView>();
            CardSelectionController controller = root.AddComponent<CardSelectionController>();
            var serialized = new SerializedObject(controller);
            SerializedProperty definitions = serialized.FindProperty("_definitions");
            definitions.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
                definitions.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string assetPath)
        {
            string absolute = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(absolute)) Directory.CreateDirectory(absolute);
        }

        private static void Validate(CardDefinition[] cards)
        {
            if (cards.Length != 7) throw new InvalidOperationException("Card demo must contain exactly seven cards.");
            var ids = new HashSet<CardId>();
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] == null || !ids.Add(cards[i].Id))
                    throw new InvalidOperationException("Card demo contains a missing or duplicate definition.");

            var stateGo = new GameObject("CardDemoValidationState") { hideFlags = HideFlags.HideAndDontSave };
            var projectileGo = new GameObject("CardDemoValidationProjectile") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                PlayerUpgradeState state = stateGo.AddComponent<PlayerUpgradeState>();
                CardDefinition damage = Find(cards, CardId.CuongCong);
                for (int i = 0; i < damage.MaxStacks; i++)
                    if (!state.Apply(damage, null)) throw new InvalidOperationException("A valid stack was rejected.");
                if (state.Apply(damage, null)) throw new InvalidOperationException("Card stack limit was exceeded.");
                if (!Mathf.Approximately(state.DamageMultiplier, 1.6f))
                    throw new InvalidOperationException("Cường Công is not additive from base damage.");

                state.ResetRun();
                for (int i = 0; i < cards.Length; i++) state.Apply(cards[i], null);
                if (state.ProjectileCount != 2 || state.ProjectileHitLimit != 3 || !state.Explodes)
                    throw new InvalidOperationException("Projectile card combination is incomplete.");
                state.ResetRun();
                for (int i = 0; i < cards.Length; i++)
                    if (state.GetStacks(cards[i].Id) != 0) throw new InvalidOperationException("Run reset left card stacks behind.");

                RunRandom.Initialize(123456);
                List<CardDefinition> first = CardDeck.Draw(cards, state, 3);
                if (first.Count != 3 || first[0].Id == first[1].Id || first[0].Id == first[2].Id || first[1].Id == first[2].Id)
                    throw new InvalidOperationException("Card draw did not return three unique cards.");
                var avoid = new HashSet<CardId> { first[0].Id, first[1].Id, first[2].Id };
                List<CardDefinition> reroll = CardDeck.Draw(cards, state, 3, avoid);
                for (int i = 0; i < reroll.Count; i++)
                    if (avoid.Contains(reroll[i].Id)) throw new InvalidOperationException("Reroll repeated a card despite alternatives.");

                Projectile projectile = projectileGo.AddComponent<Projectile>();
                projectile.OnSpawned();
                projectile.Launch(null, Vector2.right, 10f, 5, 1f, 3, true, 2f, 0.3f);
                projectile.OnDespawned();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var hits = (HashSet<int>)typeof(Projectile).GetField("_hitEnemyIds", flags).GetValue(projectile);
                bool exploded = (bool)typeof(Projectile).GetField("_hasExploded", flags).GetValue(projectile);
                bool explodes = (bool)typeof(Projectile).GetField("_explodes", flags).GetValue(projectile);
                if (hits.Count != 0 || exploded || explodes)
                    throw new InvalidOperationException("Pooled projectile did not reset hit/explosion state.");

                Debug.Log("[CardDemo] Validation passed: draw, reroll, stack limits, reset, projectile combo and pool reset.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stateGo);
                UnityEngine.Object.DestroyImmediate(projectileGo);
            }
        }

        private static CardDefinition Find(CardDefinition[] cards, CardId id)
        {
            for (int i = 0; i < cards.Length; i++) if (cards[i].Id == id) return cards[i];
            throw new InvalidOperationException($"Missing card definition: {id}");
        }
    }
}
#endif
