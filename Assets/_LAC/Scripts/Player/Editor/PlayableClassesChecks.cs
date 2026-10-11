#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Net;
using LAC.UI;
using LAC.VFX;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace LAC.Player.Editor
{
    public static class PlayableClassesChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string LastResult { get; private set; } = "Not run";
        [MenuItem("LAC/Tests/Check Playable Classes (Play Mode)")]
        public static async void Run()
        {
            if (!Application.isPlaying || NetworkClient.active) throw new InvalidOperationException("Mở màn chọn trong Play mode.");
            LastResult = "Running";
            var manager = NetworkManager.singleton as NetworkManagerLAC;
            var menu = UnityEngine.Object.FindAnyObjectByType<CharacterSelection>();
            var waveManager = UnityEngine.Object.FindAnyObjectByType<WaveManager>(FindObjectsInactive.Include);
            bool wasEnabled = waveManager.enabled;
            try
            {
                Check(menu.IsVisible && PlayerRegistry.Count == 0 && RunManager.Instance.CurrentWave == 0, "Pre-match spawned early");
                Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1, "EventSystem count");
                var decor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LAC/Prefabs/Environment/ArenaStageDecor.prefab");
                Check(!decor.GetComponentsInChildren<SpriteRenderer>(true).Any(r => AssetDatabase.GetAssetPath(r.sprite).Contains("/Bushes/")), "Bush remains");
                Check(!WeaponAuto.ThrustContains(new Vector2(-.01f, 0), Vector2.right, 3.2f, .4f), "Thrust hits behind");
                Check(!WeaponAuto.ThrustContains(new Vector2(3.201f, 0), Vector2.right, 3.2f, .4f), "Thrust exceeds range");
                Check(!WeaponAuto.ThrustContains(new Vector2(2, .401f), Vector2.right, 3.2f, .4f), "Thrust exceeds width");
                var lance = manager.Characters.Get(CharacterId.Lancer);
                Check(PlayerAnimatorDriver.AttackState(lance, Vector2.up) == AnimState.AttackUp, "Up attack");
                Check(PlayerAnimatorDriver.AttackState(lance, Vector2.down) == AnimState.AttackDown, "Down attack");
                Check(PlayerAnimatorDriver.AttackState(lance, new Vector2(-1, 1).normalized) == AnimState.AttackUpDiagonal, "Diagonal attack");
                for (int i = 0; i < 3; i++)
                {
                    waveManager.enabled = false;
                    menu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.transform.parent.name == "Class_" + manager.Characters.GetByIndex(i).DisplayName).onClick.Invoke();
                    Check(menu.Selected == manager.Characters.GetByIndex(i).Id, "Choice callback");
                    menu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "StartHost").onClick.Invoke();
                    await Wait(() => NetworkClient.localPlayer != null, "Host spawn");
                    var player = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
                    Check(player.CharacterId == menu.Selected, "Selected class lost");
                    Check(player.GetComponent<PlayerHealth>().Health == player.Data.MaxHealth, "Wrong spawn HP");
                    Check(RunManager.Instance.CurrentWave == 1 && RunManager.Instance.TotalWaves == 10, "Run did not start");
                    await Task.Delay(80);
                    Check(!menu.IsVisible, "Selection remains over game");
                    var animator = player.GetComponent<SpriteAnimator>();
                    Check(animator.HasSet && player.Data.AnimationSet.Get(AnimState.Attack).Duration < player.Data.AttackInterval * .8f, "Attack budget");
                    player.GetComponent<PlayerMovement>().enabled = false;
                    var health = player.GetComponent<PlayerHealth>();
                    typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(health, float.MaxValue);
                    var weapon = player.GetComponent<WeaponAuto>(); weapon.enabled = false;
                    var spawner = EnemySpawner.Instance; spawner.ResetRun(); spawner.BeginWave(1);
                    Vector2 origin = player.transform.position;
                    var enemyData = Resources.Load<EnemyCatalogData>("Enemies/EnemyCatalog").Get(0);
                    Enemy Add(Vector2 offset)
                    {
                        var e = spawner.Spawn(enemyData, origin + offset); e.enabled = false; return e;
                    }
                    var front = Add(new Vector2(1, 0));
                    var second = Add(new Vector2(player.Data.AttackRange - .1f, .15f));
                    var side = Add(new Vector2(0, 1.5f));
                    var behind = Add(new Vector2(-1.5f, 0));
                    var beyond = Add(new Vector2(player.Data.AttackRange + 1, 0));
                    float initial = front.Health;
                    typeof(WeaponAuto).GetField("_nextShotAt", Private).SetValue(weapon, 0f);
                    typeof(WeaponAuto).GetMethod("Update", Private).Invoke(weapon, null);
                    Check(weapon.CurrentTarget == front, "Auto target nearest");
                    if (player.Data.WeaponShape == WeaponShape.Line)
                    {
                        Check(front.Health == initial, "Arrow damages before flight");
                        await Task.Delay(500);
                        Check(front.Health == initial - player.Data.BaseDamage, "Arrow damage");
                        Check(second.Health == initial && side.Health == initial && behind.Health == initial && beyond.Health == initial, "Arrow unearned splash/pierce");
                        typeof(WeaponAuto).GetMethod("OnDashed", Private).Invoke(weapon, null);
                        typeof(WeaponAuto).GetField("_nextShotAt", Private).SetValue(weapon, 0f);
                        typeof(WeaponAuto).GetMethod("Update", Private).Invoke(weapon, null);
                        await Task.Delay(500);
                        Check(!front.IsAlive, "Empowered arrow missing");
                        Check(player.Data.ProjectilePrefab.GetComponent<NetworkIdentity>() == null, "Arrow network identity");
                    }
                    else
                    {
                        Check(front.Health == initial - player.Data.BaseDamage && second.Health == initial - player.Data.BaseDamage, "Melee did not hit both aligned targets");
                        Check(side.Health == initial && behind.Health == initial && beyond.Health == initial, "Melee outside shape/range");
                    }
                    foreach (var enemy in EnemyRegistry.Alive) enemy.enabled = true;
                    spawner.ResetRun();
                    manager.StopHost();
                    await Wait(() => !NetworkClient.active && !NetworkServer.active && PlayerRegistry.Count == 0, "Stop host");
                    await Task.Delay(300);
                }
                LastResult = "ALL PASSED: pre-match, three choices, spawn HP, 10 waves, arc, arrow, dash bonus, thrust, directions, no bushes, reconnect.";
                Debug.Log("[ClassesChecks] " + LastResult);
            }
            catch (Exception e) { LastResult = "FAILED: " + e; Debug.LogError("[ClassesChecks] " + LastResult); }
            finally
            {
                foreach (var enemy in EnemyRegistry.Alive) enemy.enabled = true;
                waveManager.enabled = wasEnabled;
            }
        }

        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static async Task Wait(Func<bool> condition, string message)
        {
            for (int i = 0; i < 100; i++) { if (condition()) return; await Task.Delay(50); }
            throw new InvalidOperationException(message + " timeout");
        }
    }
}
#endif
