#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.VFX.Editor
{
    public static class CombatChecks
    {
        public static string LastResult { get; private set; } = "Not run";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Check(bool okay, string message) { if (!okay) throw new Exception(message); }
        private static void Down(PlayerCharacter p)
        {
            typeof(PlayerHealth).GetField("_invulnerableUntil", Private).SetValue(p.GetComponent<PlayerHealth>(), 0f);
            Check(DamageSystem.ApplyToPlayer(p, 999, Vector2.zero), "Down failed");
        }
        private static void Move(PlayerCharacter p, Vector2 position) => p.GetComponent<PlayerMovement>().ServerRespawn(position);
        [MenuItem("LAC/Tests/Check T31 T41 (Two Players)")]
        public static async void Run()
        {
            if (!Application.isPlaying || PlayerRegistry.Count != 2 || LastResult == "RUNNING") return;
            LastResult = "RUNNING";
            try
            {
                var run = RunManager.Instance;
                if (run.IsOver) { run.RestartRun(); await Task.Delay(500); }
                UnityEngine.Object.FindAnyObjectByType<WaveManager>().enabled = false;
                EnemySpawner.Instance.ResetRun();
                var a = PlayerRegistry.All[0]; var b = PlayerRegistry.All[1];
                var ah = a.GetComponent<PlayerHealth>(); var bh = b.GetComponent<PlayerHealth>();
                foreach (var p in PlayerRegistry.All) p.GetComponent<WeaponAuto>().enabled = false;
                Move(a, new Vector2(-5, 0)); Move(b, Vector2.zero); await Task.Delay(700);
                Down(b); await Task.Delay(600);
                Check(bh.IsDowned && run.State == RunState.WaveActive, "First down ended co-op");
                Check(PlayerRegistry.Nearest(b.transform.position) == a, "Enemy still targets downed player");
                bh.ServerApplyMaxHealthBonus(2, 2);
                Check(!bh.IsAlive, "Health upgrade revived player");
                await Task.Delay(3200); Check(!bh.IsAlive && bh.ReviveProgress == 0, "Rescued from outside radius");
                Move(a, new Vector2(.8f, 0)); await Task.Delay(1300);
                Check(!bh.IsAlive && bh.ReviveProgress > .2f, "No progress / revived too early");
                Move(a, new Vector2(-5, 0)); await Task.Delay(600);
                Check(bh.ReviveProgress == 0 && !bh.IsBeingRevived, "Leaving radius did not reset progress");
                Move(a, new Vector2(.8f, 0)); await Task.Delay(1700);
                Check(!bh.IsAlive, "Old progress carried into new rescue");
                await Task.Delay(1500);
                Check(bh.IsAlive && bh.Health == Mathf.CeilToInt(bh.MaxHealth * bh.ReviveRules.HealthFraction), "Revive health incorrect");
                Check(!DamageSystem.ApplyToPlayer(b, 1, Vector2.zero), "Missing revive invulnerability");
                Check(run.State == RunState.WaveActive, "Revive ended run");
                Move(b, new Vector2(5, 0)); await Task.Delay(700);
                Down(a); await Task.Delay(300);
                Check(!run.IsOver, "Alive counter was not restored");
                Down(b); await Task.Delay(300); Check(run.State == RunState.Defeat, "Both down must lose");
                run.RestartRun(); await Task.Delay(700);
                Check(ah.IsAlive && bh.IsAlive && ah.ReviveProgress == 0 && bh.ReviveProgress == 0, "Restart retained downed state");
                Check(run.CurrentWave == 1, "Restart wave");
                CheckWaves();
                LastResult = "ALL PASSED"; Debug.Log("[CombatChecks] " + LastResult);
            }
            catch (Exception e) { LastResult = e.ToString(); Debug.LogException(e); }
        }
        public static void CheckWaves()
        {
            foreach (string name in new[] { "ThachSanh", "Giong", "Tam" })
            {
                var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_LAC/Data/Characters/" + name + ".asset");
                var data = character.InstrumentWave;
                Check(data != null && data.Shape == character.WeaponShape, "Wrong visual profile " + name);
                var pool = PoolRegistry.Get(data.Prefab, 24, 256);
                var wave = pool.Get(Vector3.zero, Quaternion.identity);
                wave.Play(pool, data, Vector2.right, character.AttackRange, character.ArcHalfAngle, character.ProjectileSpeed);
                Check(wave.GetComponent<Mirror.NetworkIdentity>() == null, "Networked visual");
                foreach (var line in wave.GetComponentsInChildren<LineRenderer>())
                {
                    Check(line.sortingOrder < 99 && line.sharedMaterial.shader.name == "LAC/SpriteAdditive", "Visual readability layers/material");
                    Check(line.startColor.a <= .22f, "Too opaque");
                    for (int i = 0; i < line.positionCount; i++)
                        if (data.Shape != WeaponShape.Line)
                            Check(line.GetPosition(i).magnitude <= character.AttackRange, "Wave exceeds range");
                }
                pool.Release(wave);
                var reused = pool.Get(Vector3.zero, Quaternion.identity);
                Check(reused == wave, "Effect not reused"); pool.Release(reused);
            }
        }
    }
}
#endif
