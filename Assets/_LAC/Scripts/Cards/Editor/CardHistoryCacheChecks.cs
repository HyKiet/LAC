#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LAC.Combat;
using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace LAC.Cards.Editor
{
    public static class CardHistoryCacheChecks
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("LAC/Tests/Validate Client Card History Cache (Edit Mode)")]
        public static void Validate()
        {
            Require(!Application.isPlaying && PlayerRegistry.Count == 0, "Cần Edit Mode không có player runtime.");
            var objects = new List<GameObject>();
            var players = new List<PlayerCharacter>();
            var notifications = new Dictionary<uint, int>();
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            var data = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g)))
                .First(c => c.WeaponShape == WeaponShape.Arc);
            var go = new GameObject("CardHistoryCacheChecks") { hideFlags = HideFlags.HideAndDontSave };
            objects.Add(go);
            go.AddComponent<NetworkIdentity>();
            var run = go.AddComponent<RunManager>();
            BindIdentity(run);
            var grants = Get<SyncList<RunManager.CardGrant>>(run, "_cardGrants");
            grants.IsWritable = () => true;
            grants.IsRecording = () => false;
            var tick = (Action)Delegate.CreateDelegate(typeof(Action), run,
                typeof(RunManager).GetMethod("ApplyCardHistory", Fields));
            try
            {
                var one = CreatePlayer(101, data); var two = CreatePlayer(102, data);
                // Snapshot tới trước bind callback; OnStartClient phải nạp toàn bộ.
                grants.Add(Grant(101, CardId.CuongCong)); grants.Add(Grant(102, CardId.LienKich));
                grants.Add(Grant(101, CardId.CuongCong));
                run.OnStartClient(); tick(); CheckCounts(1, 1); CheckState(one); CheckState(two);
                object cache = Get<Dictionary<uint, List<CardDefinition>>>(run, "_clientCardHistories")[101];
                for (int i = 0; i < 10000; i++) tick();
                CheckCounts(1, 1);
                Require(ReferenceEquals(cache, Get<Dictionary<uint, List<CardDefinition>>>(run, "_clientCardHistories")[101]),
                    "Idle vẫn dựng lại lịch sử.");

                grants.Add(Grant(101, CardId.ThietBich)); tick(); CheckCounts(2, 1);
                grants.Insert(0, Grant(102, CardId.SinhLuc)); tick(); CheckCounts(2, 2);
                grants[1] = Grant(102, CardId.CuongNo); tick(); CheckCounts(3, 3);
                CheckState(one); CheckState(two);
                grants.RemoveAt(0); tick(); CheckCounts(3, 4); CheckState(two);
                grants.Clear(); tick(); CheckCounts(4, 5); CheckState(one); CheckState(two);

                // Grant chưa có player vẫn được giữ; dữ liệu chưa có không dùng fallback Circle.
                grants.Add(Grant(103, CardId.CuongCong)); tick(); CheckCounts(4, 5);
                var late = CreatePlayer(103, null); tick(); Require(notifications[103] == 0, "Đánh dấu trước dữ liệu.");
                Set(late, "_data", data); tick(); Require(notifications[103] == 1, "Mất grant tới sớm.");
                CheckState(late);
                PlayerRegistry.Unregister(late);
                var replacement = CreatePlayer(103, data); tick();
                Require(notifications[103] == 1, "Component mới cùng ID không được replay.");
                CheckState(replacement);

                // Đổi run và clear có thể được nhận theo cả hai thứ tự.
                Set(run, "_cardRun", 1); tick(); grants.Clear(); tick(); CheckState(replacement);
                grants.Add(Grant(101, CardId.CuongNo)); tick();
                grants.Clear(); tick(); Set(run, "_cardRun", 2); tick(); CheckState(one);

                // Mô phỏng deserialize full không có callback, cùng độ dài nhưng khác ID.
                run.OnStopClient();
                var snapshot = Get<IList<RunManager.CardGrant>>(grants, "objects");
                snapshot.Clear(); snapshot.Add(Grant(101, CardId.LienKich));
                run.OnStartClient(); tick(); CheckState(one);
                Near(one.Upgrades.DamageMultiplier, 1f); Near(one.Upgrades.AttackSpeedMultiplier, 1.15f);
                // Hook stop/start không nhân đăng ký callback.
                Require(grants.OnChange.GetInvocationList().Length == 1, "Bind callback trùng.");
                Debug.Log("[CardHistoryCache] ALL PASSED: initial/quiet snapshot, ADD/INSERT/SET/REMOVE/CLEAR, affected players only, 10,000 idle ticks, late player/data, replacement same ID, run reset ordering, reconnect.");
            }
            finally
            {
                run.OnStopClient();
                foreach (var player in players) if (player != null) PlayerRegistry.Unregister(player);
                foreach (var obj in objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }

            PlayerCharacter CreatePlayer(uint id, CharacterData characterData)
            {
                var obj = new GameObject("CardCachePlayer") { hideFlags = HideFlags.HideAndDontSave };
                objects.Add(obj);
                obj.AddComponent<NetworkIdentity>();
                var state = obj.AddComponent<PlayerUpgradeState>();
                var player = obj.AddComponent<PlayerCharacter>(); players.Add(player);
                BindIdentity(player);
                typeof(NetworkIdentity).GetProperty("netId").SetValue(obj.GetComponent<NetworkIdentity>(), id);
                Set(player, "_upgrades", state); Set(player, "_data", characterData); Set(state, "_character", player);
                notifications[id] = 0;
                state.Changed += () => notifications[id]++;
                PlayerRegistry.Register(player);
                return player;
            }
            void CheckCounts(int one, int two) => Require(notifications[101] == one && notifications[102] == two,
                $"Số notification lệch: {notifications[101]}/{notifications[102]} != {one}/{two}.");
            void CheckState(PlayerCharacter player)
            {
                var referenceGo = new GameObject("CardCacheReference") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var reference = referenceGo.AddComponent<PlayerUpgradeState>();
                    Set(reference, "_character", player);
                    foreach (var grant in grants)
                        if (grant.Player == player.netId) reference.Apply(cards.First(c => c.Id == grant.Card), null);
                    Require(CardEvolutionCatalogChecks.Signature(reference) == CardEvolutionCatalogChecks.Signature(player.Upgrades),
                        "Cache khác replay gốc: " + player.netId);
                    foreach (CardId id in Enum.GetValues(typeof(CardId)))
                        Require(reference.GetStacks(id) == player.Upgrades.GetStacks(id), "Sai cấp: " + id);
                }
                finally { UnityEngine.Object.DestroyImmediate(referenceGo); }
            }
        }
        private static RunManager.CardGrant Grant(uint player, CardId card) => new RunManager.CardGrant { Player = player, Card = card };
        // NetworkIdentity.Awake không chạy trong Edit Mode; gán liên kết giống runtime.
        private static void BindIdentity(NetworkBehaviour behaviour) => typeof(NetworkBehaviour)
            .GetProperty("netIdentity").SetValue(behaviour, behaviour.GetComponent<NetworkIdentity>());
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
        private static void Near(float actual, float expected) => Require(Mathf.Abs(actual - expected) < .0001f, "Chỉ số lệch.");
        private static void Require(bool valid, string reason) { if (!valid) throw new InvalidOperationException("[CardHistoryCache] " + reason); }
    }
}
#endif
