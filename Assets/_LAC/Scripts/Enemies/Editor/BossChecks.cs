#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Combat;
using LAC.Core;
using LAC.Drum;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Enemies.Editor
{
    public static class BossChecks
    {
        public static string LastResult { get; private set; } = "Not run";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Move(PlayerCharacter player, Vector2 position)
        { player.GetComponent<Rigidbody2D>().position=position; player.transform.position=position; }
        [MenuItem("LAC/Tests/Check T39 Boss (Play Mode)")]
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || LastResult == "RUNNING") return;
            LastResult="RUNNING";
            var run=RunManager.Instance; var spawner=EnemySpawner.Instance;
            var player=PlayerRegistry.All[0]; var health=player.GetComponent<PlayerHealth>();
            var weapon=player.GetComponent<WeaponAuto>(); weapon.enabled=false;
            try
            {
                run.RestartRun();
                await Task.Delay(350);
                spawner.ResetRun();
                typeof(PlayerHealth).GetField("_invulnerableUntil",Private).SetValue(health,Time.time+120);
                typeof(RunManager).GetMethod("StartWave",Private).Invoke(run,new object[]{16});
                await Task.Delay(1800);
                Check(EnemyRegistry.Count==1,"Wave 16 must spawn only the boss");
                var boss=EnemyRegistry.Alive[0];
                Check(boss.IsBoss && boss.MaxHealth==800,"Solo boss health/data");
                Check(boss.GetComponent<Mirror.NetworkIdentity>()==null,"Boss has NetworkIdentity");
                Move(player,new Vector2(-7,-5));
                spawner.StartBossAction(boss,BossAction.Fan,Vector2.left);
                await Task.Delay(1050);
                Check(UnityEngine.Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length==5,"Fan must fire five projectiles");
                EnemyProjectileClearable.ClearAll();
                boss.GetComponent<Rigidbody2D>().position=Vector2.zero; boss.transform.position=Vector2.zero;
                spawner.StartBossAction(boss,BossAction.Charge,Vector2.right);
                await Task.Delay(400); Check(boss.Position.magnitude<.1f,"Boss moved during charge warning");
                await Task.Delay(900); Check(boss.Position.x>2,"Boss charge did not move");
                boss.ApplyDrumShockwave(Vector2.zero,0,1);
                Check(boss.CurrentBossAction==BossAction.Rest && boss.IsStunned,"Drum did not interrupt boss");
                DamageSystem.ApplyToEnemy(boss,399,player.transform.position);
                Check(boss.Health==401 && boss.BossPhase==1,"Phase transitioned too early");
                DamageSystem.ApplyToEnemy(boss,1,player.transform.position);
                Check(boss.BossPhase==2 && spawner.BossPhase==2,"Phase transition at half health");
                await Task.Delay(1100);
                spawner.StartBossAction(boss,BossAction.Summon,Vector2.up);
                await Task.Delay(1050);
                Check(EnemyRegistry.Count==5,"Boss must summon four Ma Da");
                foreach(var e in EnemyRegistry.Alive) if(!e.IsBoss) { Check(e.Data.Behaviour==EnemyBehaviour.Charger,"Wrong summoned type"); e.enabled=false; }
                Move(player,boss.Position+Vector2.left);
                typeof(PlayerHealth).GetField("_invulnerableUntil",Private).SetValue(health,0f);
                int hp=health.Health;
                spawner.StartBossAction(boss,BossAction.Sweep,Vector2.up);
                await Task.Delay(1050); Check(health.Health==hp-2,"Sweep inside radius must deal two damage");
                Move(player,new Vector2(-7,-5)); hp=health.Health;
                spawner.StartBossAction(boss,BossAction.Sweep,Vector2.up);
                await Task.Delay(1050); Check(health.Health==hp,"Sweep damaged outside its radius");
                typeof(PlayerHealth).GetField("_invulnerableUntil",Private).SetValue(health,Time.time+120);
                for(int i=0;i<12;i++) spawner.SummonBossAdds(boss);
                Check(EnemyRegistry.Count==40,"Summon cap must be 40 including boss");
                DamageSystem.ApplyToEnemy(boss,10000,player.transform.position);
                await Task.Delay(300);
                Check(run.State==RunState.Victory && EnemyRegistry.Count==0 && !spawner.BossActive,"Boss death must end run and clear adds");
                run.RestartRun(); await Task.Delay(500);
                Check(run.CurrentWave==1 && !spawner.BossActive,"Restart retained boss state");
                LastResult="ALL PASSED"; Debug.Log("[BossChecks] "+LastResult);
            }
            catch(Exception e) { LastResult=e.ToString(); Debug.LogException(e); }
            finally
            {
                weapon.enabled=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include,FindObjectsSortMode.None)) e.enabled=true;
            }
        }
    }
}
#endif
