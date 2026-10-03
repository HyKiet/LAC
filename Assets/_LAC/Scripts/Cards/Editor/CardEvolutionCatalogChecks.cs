#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.Cards.Editor
{
    public static class CardEvolutionCatalogChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static bool _running;

        [MenuItem("LAC/Tests/Validate Eight Evolutions (Edit Mode)")]
        public static void Validate()
        {
            Require(!Application.isPlaying, "Thoát Play Mode trước khi kiểm dữ liệu.");
            CardEvolutionCatalog.EditorSetTestRecipes(null);
            var recipes = CardEvolutionCatalog.Recipes.ToArray();
            var cards = Resources.LoadAll<CardDefinition>("Cards");
            Require(recipes.Length == 8 && cards.Length == 12, "Phải có đúng 8 tiến hoá và 12 thẻ nền.");
            var assets = cards.Cast<UnityEngine.Object>().Concat(recipes).Concat(recipes.Select(r => r.Bonus)).ToArray();
            var before = assets.Select(EditorJsonUtility.ToJson).ToArray();
            var go = new GameObject("EvolutionCatalogChecks") { hideFlags = HideFlags.HideAndDontSave };
            var replayGo = new GameObject("EvolutionReplayChecks") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var character = go.AddComponent<PlayerCharacter>();
                var state = go.AddComponent<PlayerUpgradeState>();
                Set(state, "_character", character);
                var replayCharacter = replayGo.AddComponent<PlayerCharacter>();
                var replay = replayGo.AddComponent<PlayerUpgradeState>();
                Set(replay, "_character", replayCharacter);
                var characters = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/_LAC/Data/Characters" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                Require(characters.Length == 3, "Phải kiểm đủ ba nhân vật.");
                foreach (var data in characters)
                {
                    Set(character, "_data", data); Set(replayCharacter, "_data", data);
                    foreach (var recipe in recipes)
                    {
                        Require(recipe.Validate(cards, out var error), error);
                        Require(recipe.Ingredients.Sum(i => i.Stacks) <= 15, "Công thức vượt số lượt chọn.");
                        state.ResetRun(); replay.ResetRun();
                        var history = Ingredients(recipe).ToArray();
                        for (int i = 0; i < history.Length - 1; i++) state.Apply(history[i], null);
                        Require(!state.HasEvolution(recipe.Id), "Tiến hoá trước ngưỡng: " + recipe.Id);
                        state.Apply(history.Last(), null);
                        bool supported = recipe.Bonus.Supports(data.WeaponShape)
                            && recipe.Ingredients.All(i => i.Card.Supports(data.WeaponShape));
                        Require(state.HasEvolution(recipe.Id) == supported, "Sai lọc vũ khí: " + recipe.Id);
                        if (supported) CheckDocumentedEffects(recipe.Id, state, data.MaxHealth);
                        // Tính độc lập từ asset: nguyên liệu một lần, mỗi bonus đủ điều kiện một lần.
                        var ranks = new Dictionary<CardId, int>();
                        float expectedDamage = 1 + history.Sum(c =>
                            {
                                int rank = ranks.TryGetValue(c.Id, out int current) ? current + 1 : 1;
                                ranks[c.Id] = rank;
                                return c.DamageBonus * c.StatScaleAtStack(rank);
                            })
                            + state.Evolutions.Sum(r => r.Bonus.DamageBonus);
                        Near(state.DamageMultiplier, expectedDamage);
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            replay.ResetRun();
                            foreach (var card in history.Reverse()) replay.Apply(card, null);
                            Require(Signature(state) == Signature(replay), "Replay khác thứ tự bị lệch: " + recipe.Id);
                        }
                        int count = state.Evolutions.Count;
                        foreach (var ingredient in recipe.Ingredients)
                            while (state.GetStacks(ingredient.Card.Id) < ingredient.Card.MaxStacks) state.Apply(ingredient.Card, null);
                        Require(state.Evolutions.Count == count, "Áp tiến hoá trùng: " + recipe.Id);
                        state.ResetRun();
                        Require(state.Evolutions.Count == 0 && state.ProjectileCount == 1 && state.DamageMultiplier == 1,
                            "Reset giữ phần thưởng.");
                    }
                }
                // Hai công thức chung có thể cùng bật ở một lựa chọn, không phụ thuộc thứ tự asset.
                Set(character, "_data", characters.First(c => c.WeaponShape == WeaponShape.Arc));
                state.ResetRun();
                Grant(state, cards, CardId.CuongCong, 3); Grant(state, cards, CardId.SinhLuc, 2);
                Grant(state, cards, CardId.ThietBich, 1);
                Require(state.Evolutions.Count == 0, "Tiến hoá sớm ở tổ hợp đồng thời.");
                Grant(state, cards, CardId.ThietBich, 1);
                Require(state.HasEvolution("ThanhGiong") && state.HasEvolution("KimCang") && state.Evolutions.Count == 2,
                    "Thiếu một trong hai tiến hoá đồng thời.");
                Near(state.DamageMultiplier, 2f); Near(state.HitInvulnerabilityMultiplier, 1.75f);
                for (int i = 0; i < assets.Length; i++) Require(before[i] == EditorJsonUtility.ToJson(assets[i]), "Asset bị thay đổi.");
                Debug.Log("[EvolutionCatalog] ALL PASSED: eight recipes x three characters, thresholds, effects, weapon filter, reverse replay, simultaneous, reset, immutable assets.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(replayGo);
                CardEvolutionCatalog.EditorSetTestRecipes(null);
            }
        }

        // Dùng cả ở host một người lẫn Editor host + development client riêng.
        [MenuItem("LAC/Tests/Check Eight Evolutions Play Mode (restarts run)")]
        public static async void Run()
        {
            if (_running || !Application.isPlaying || !NetworkServer.active) return;
            _running = true;
            var run = RunManager.Instance;
            var ui = CardSelectionController.Instance;
            var players = PlayerRegistry.All.ToArray();
            var characters = players.Select(p => p.CharacterId).ToArray();
            var definitions = Get<CardDefinition[]>(run, "_cardDefinitions");
            try
            {
                foreach (var recipe in CardEvolutionCatalog.Recipes)
                {
                    // Dùng Tấm cho công thức đạn và Gióng cho công thức chung.
                    foreach (var player in players) player.SetCharacter(recipe.Bonus.RequiresProjectile ? CharacterId.Tam : CharacterId.Giong);
                    // Reset máu sau khi đổi nhân vật để dùng đúng máu gốc của bài test.
                    await Restart(run, players);
                    var history = Ingredients(recipe).ToArray();
                    var grants = Get<SyncList<RunManager.CardGrant>>(run, "_cardGrants");
                    foreach (var player in players)
                        foreach (var card in history.Take(history.Length - 1))
                        {
                            player.Upgrades.Apply(card, player.GetComponent<PlayerHealth>());
                            grants.Add(new RunManager.CardGrant { Player = player.netId, Card = card.Id });
                        }
                    // Chỉ thu hẹp đề nghị sau khi client đã dựng xong lịch sử từ danh mục thật.
                    await Task.Delay(600);
                    Set(run, "_cardDefinitions", new[] { history.Last() });
                    ClearWave(); await Task.Delay(500);
                    Pick(ui); Pick(ui);
                    await Task.Delay(1400);
                    Require(Get<GameObject>(ui.GetComponent<CardSelectionView>(), "_evolutionPanel").activeSelf,
                        "Thiếu thông báo: " + recipe.Id);
                    Require(run.State == RunState.CardSelection && Time.timeScale == 0, "Không chờ thông báo.");
                    await Wait(() => run.CurrentWave == 2 && run.State == RunState.WaveActive, 20000);
                    await Task.Delay(600);
                    foreach (var player in players)
                    {
                        Require(player.Upgrades.HasEvolution(recipe.Id), "Người chơi thiếu tiến hoá: " + recipe.Id);
                        CheckDocumentedEffects(recipe.Id, player.Upgrades, player.Data.MaxHealth);
                        Require(player.GetComponent<PlayerHealth>().MaxHealth == player.Data.MaxHealth + player.Upgrades.MaxHealthBonus,
                            "Máu tối đa trên host không khớp tiến hoá: " + recipe.Id);
                    }
                    Require(!CardSelectionController.CombatInputLocked && Time.timeScale == 1, "Không phục hồi input.");
                    Debug.Log($"[EvolutionCatalog] PLAY PASS {recipe.Id}: players={players.Length}; {Signature(players[0].Upgrades)}");
                }
                await Restart(run, players);
                Require(players.All(p => p.Upgrades.Evolutions.Count == 0), "Reset còn tiến hoá.");
                Debug.Log($"[EvolutionCatalog] PLAY ALL PASSED: 8/8, players={players.Length}, actual offers, double click, UI, ACK, restart.");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                if (Application.isPlaying && run != null)
                {
                    Set(run, "_cardDefinitions", definitions);
                    for (int i = 0; i < players.Length; i++) if (players[i] != null)
                        players[i].SetCharacter(characters[i]);
                    await Restart(run, players);
                    foreach (var player in players) if (player != null)
                        Set(player.GetComponent<PlayerHealth>(), "_invulnerableUntil", 0f);
                }
                _running = false;
            }
        }

        private static IEnumerable<CardDefinition> Ingredients(CardEvolutionData recipe) =>
            recipe.Ingredients.SelectMany(i => Enumerable.Repeat(i.Card, i.Stacks));

        // Giá trị nghiệm thu từ docs/CARDS.md, độc lập với asset để bắt sai dữ liệu bonus.
        private static void CheckDocumentedEffects(string id, PlayerUpgradeState state, int baseHealth)
        {
            switch (id)
            {
                case "BatTu":
                    Require(state.MaxHealthBonus == Mathf.CeilToInt(baseHealth * .8f - .00001f) && state.WaveHeal == 3,
                        "Bất Tử sai máu/hồi đầu đợt.");
                    break;
                case "KimCang":
                    Require(state.MaxHealthBonus == Mathf.CeilToInt(baseHealth * .6f - .00001f), "Kim Cang sai máu.");
                    Near(state.HitInvulnerabilityMultiplier, 1.6f);
                    break;
                case "LacPhong":
                    Near(state.DashCooldownMultiplier, .65f); Near(state.MoveSpeedMultiplier, 1.36f);
                    break;
                case "LuaThien":
                    Near(state.DamageMultiplier, 1.5f); Near(state.AttackSpeedMultiplier, .84f);
                    Near(state.ExplosionRadius, 2.5f); Near(state.ExplosionDamageRatio, .6f);
                    break;
                case "NoThan":
                    Require(state.ProjectileHitLimit == 5, "Nỏ Thần sai giới hạn xuyên.");
                    Near(state.AttackRangeMultiplier, 1.5f);
                    break;
                case "ThanhGiong":
                    Near(state.DamageMultiplier, 2f); Near(state.HitInvulnerabilityMultiplier, 1.45f);
                    break;
                case "TiengDan":
                    Near(state.DamageMultiplier, 1.7f); Near(state.AttackSpeedMultiplier, 1.15f);
                    Near(state.AttackRangeMultiplier, 1.3f);
                    break;
                case "TramTrung":
                    Require(state.ProjectileCount == 4, "Trăm Trứng sai số đạn.");
                    Near(state.ProjectileDamageMultiplier, .525f); Near(state.ProjectileSpreadDegrees, 14f);
                    Near(state.AttackSpeedMultiplier, 1.45f);
                    break;
                default: throw new Exception("[EvolutionCatalog] Công thức chưa có giá trị nghiệm thu: " + id);
            }
        }
        private static void Grant(PlayerUpgradeState state, CardDefinition[] cards, CardId id, int count)
        { for (int i = 0; i < count; i++) state.Apply(cards.Single(c => c.Id == id), null); }
        public static string Signature(PlayerUpgradeState s) => string.Join(",", s.Evolutions.Select(r => r.Id).OrderBy(id => id))
            + $"|{s.DamageMultiplier:F4}|{s.AttackSpeedMultiplier:F4}|{s.DashCooldownMultiplier:F4}|{s.MoveSpeedMultiplier:F4}"
            + $"|{s.AttackRangeMultiplier:F4}|{s.HitInvulnerabilityMultiplier:F4}|{s.MaxHealthBonus}|{s.WaveHeal}"
            + $"|{s.ProjectileCount}|{s.ProjectileDamageMultiplier:F4}|{s.ProjectileHitLimit}|{s.ProjectileSpreadDegrees:F4}"
            + $"|{s.ExplosionRadius:F4}|{s.ExplosionDamageRatio:F4}";
        private static async Task Restart(RunManager run, PlayerCharacter[] players)
        {
            Set(run, "_cardDefinitions", null);
            for (int i = 0; i < players.Length; i++) run.ReportPlayerDown();
            await Task.Delay(150); run.RestartRun(); await Task.Delay(300);
            foreach (var player in players) if (player != null)
                Set(player.GetComponent<PlayerHealth>(), "_invulnerableUntil", Time.time + 10000f);
        }
        private static void ClearWave()
        {
            var waves = UnityEngine.Object.FindAnyObjectByType<WaveManager>();
            Set(waves, "_waveStartedAt", Time.time - 1000f);
            typeof(WaveManager).GetMethod("ReleaseDueSpawns", Private).Invoke(waves, null);
            for (int i = EnemyRegistry.Count - 1; i >= 0; i--)
                DamageSystem.ApplyToEnemy(EnemyRegistry.Alive[i], int.MaxValue, Vector2.zero);
        }
        private static void Pick(CardSelectionController ui)
        {
            var slot = Get<Array>(ui.GetComponent<CardSelectionView>(), "_slots").GetValue(0);
            ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
        }
        private static async Task Wait(Func<bool> condition, int timeout)
        {
            double end = Time.realtimeSinceStartupAsDouble + timeout / 1000d;
            while (!condition()) { Require(Application.isPlaying && Time.realtimeSinceStartupAsDouble < end, "Hết thời gian chờ."); await Task.Delay(50); }
        }
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Near(float actual, float expected) => Require(Mathf.Abs(actual - expected) < .0001f, $"{actual} != {expected}");
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("[EvolutionCatalog] " + message); }
    }
}
#endif
