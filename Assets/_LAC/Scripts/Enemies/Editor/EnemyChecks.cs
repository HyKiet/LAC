#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using LAC.Core;
using LAC.Player;
using UnityEditor;
using UnityEngine;

namespace LAC.Enemies.Editor
{
    public static class EnemyChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string LastResult { get; private set; }
        private static bool _running;
        private static void Set(object target,string field,object value) => target.GetType().GetField(field,Private).SetValue(target,value);
        private static void Check(bool okay,string message) { if(!okay) throw new Exception(message); }
        [MenuItem("LAC/Tests/Check T34 Enemies (Play Mode)")]
        public static async void Run()
        {
            if(_running || !Application.isPlaying || !Mirror.NetworkServer.active) return;
            _running=true; LastResult="RUNNING";
            var waves=UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            var player=PlayerRegistry.All[0]; var weapon=player.GetComponent<LAC.Combat.WeaponAuto>();
            var spawner=EnemySpawner.Instance; var run=RunManager.Instance;
            try
            {
                waves.enabled=false; weapon.enabled=false;
                run.ReportPlayerDown(); run.ReportPlayerDown(); run.RestartRun();
                // RpcRespawn của host cũng qua hàng đợi; đợi xong trước khi bố trí bài thử.
                await Task.Delay(300);
                spawner.ResetRun(); spawner.BeginWave(1);
                player.GetComponent<Rigidbody2D>().position=new Vector2(0,6); player.transform.position=new Vector2(0,6);
                var health=player.GetComponent<PlayerHealth>(); Set(health,"_invulnerableUntil",Time.time+30f);
                var catalog=Resources.Load<EnemyCatalogData>("Enemies/EnemyCatalog");
                var ranged=spawner.Spawn(catalog.Get(1),new Vector2(5,6));
                await Task.Delay(650);
                Check(UnityEngine.Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length>0,"Ma Troi did not fire");
                Check(Vector2.Distance(ranged.Position,player.transform.position)>4f,"Ma Troi did not hold distance");
                LAC.Drum.EnemyProjectileClearable.ClearAll();
                Check(UnityEngine.Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length==0,"Drum failed to return hostile shots to pool");
                ranged.enabled=false;
                ranged.GetComponent<Rigidbody2D>().position=new Vector2(.7f,6); ranged.transform.position=new Vector2(.7f,6);
                Set(health,"_invulnerableUntil",0f); int hp=health.Health;
                spawner.FireProjectile(ranged,Vector2.left); await Task.Delay(250);
                Check(health.Health==hp-1,"Enemy projectile damage incorrect");
                Set(health,"_invulnerableUntil",Time.time+30f); hp=health.Health;
                spawner.FireProjectile(ranged,Vector2.left); await Task.Delay(250);
                Check(health.Health==hp,"Projectile ignored invulnerability");
                spawner.ResetRun(); spawner.BeginWave(1);
                var tank=spawner.Spawn(catalog.Get(2),new Vector2(.6f,6)); tank.enabled=true;
                Check(tank.Health==40 && tank.Data.ContactDamage==3,"Bu Nhin stats");
                Set(health,"_invulnerableUntil",0f); hp=health.Health; await Task.Delay(600);
                Check(health.Health==hp-3,"Bu Nhin contact damage");
                Set(health,"_invulnerableUntil",Time.time+30f);
                spawner.ResetRun(); spawner.BeginWave(1);
                var charger=spawner.Spawn(catalog.Get(3),new Vector2(-5,6)); charger.enabled=true;
                await Task.Delay(400); spawner.StartCharge(charger,Vector2.right); var before=charger.Position;
                Check(charger.IsWindingUp,"Ma Da has no warning"); await Task.Delay(250);
                Check(Vector2.Distance(before,charger.Position)<.1f,"Ma Da moves during warning");
                await Task.Delay(600); Check(charger.Position.x>before.x+1.5f,"Ma Da failed to charge");
                charger.ApplyDrumShockwave(Vector2.zero,0f,1f); Check(!charger.IsCharging && charger.IsStunned,"Drum failed to cancel charge");
                spawner.ResetRun(); spawner.BeginWave(1);
                var splitter=spawner.Spawn(catalog.Get(4),Vector2.zero); splitter.enabled=true; int parent=splitter.Id;
                spawner.KillEnemy(splitter);
                Check(EnemyRegistry.Count==2,"Quy Nho did not split twice");
                Check(EnemyRegistry.TryGet(-(parent*2+1),out var child) && child.Data==catalog.Get(0),"Child identity/data wrong");
                var next=spawner.Spawn(catalog.Get(0),Vector2.one*5); Check(next.Id==parent+1,"Split consumed schedule ID");
                var table=AssetDatabase.LoadAssetAtPath<WaveTable>("Assets/_LAC/Data/Waves/WaveTable_CoDinh.asset");
                for(int w=1;w<=table.TotalWaves;w++) { int population=0; foreach(var group in table.BuildSpec(w,new RandomStream(123,"test")).Mix) population+=group.Count*group.Enemy.PopulationCost; Check(population<=40,"Wave exceeds 40 after splits"); }
                spawner.ResetRun(); Check(EnemyRegistry.Count==0,"Reset left enemies");
                LastResult="ALL PASSED"; Debug.Log("[EnemyChecks] "+LastResult);
            }
            catch(Exception e) { LastResult=e.ToString(); Debug.LogException(e); }
            finally
            {
                foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include,FindObjectsSortMode.None)) e.enabled=true;
                waves.enabled=true; weapon.enabled=true; _running=false;
            }
        }
    }
}
#endif
