#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LAC.Enemies
{
    public sealed class EnemyNetworkTestClient : MonoBehaviour
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private int _step;
        private float _checkAt;
        private int _lastCount=-1;
        private int _lastSeed;
        private int _lastShots=-1;
        private int _lastCharges=-1;
        private float _nextLogAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--lac-enemy-test-client")<0) return;
            Application.runInBackground=true; SceneManager.sceneLoaded+=Connect;
        }
        private static void Connect(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=Connect;
            var manager=FindAnyObjectByType<NetworkManagerLAC>();
            typeof(NetworkManagerLAC).GetField("_autoStartHost",Private).SetValue(manager,false);
            manager.networkAddress="localhost"; manager.StartClient();
            DontDestroyOnLoad(new GameObject("EnemyNetworkTestClient",typeof(EnemyNetworkTestClient)));
        }
        private void Update()
        {
            if(NetworkClient.localPlayer==null || RunManager.Instance==null || EnemySpawner.Instance==null) return;
            NetworkClient.localPlayer.GetComponent<LAC.Combat.WeaponAuto>().enabled=false;
            var currentRun=RunManager.Instance;
            if(_lastSeed!=currentRun.Seed)
            {
                Debug.Log($"[EnemyClient] seed={currentRun.Seed} wave={currentRun.CurrentWave}");
                _lastSeed=currentRun.Seed;
            }
            if(Time.unscaledTime>=_nextLogAt)
            {
                _nextLogAt=Time.unscaledTime+.1f;
                int shots=FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length;
                int charges=0; foreach(var e in EnemyRegistry.Alive) if(e.IsWindingUp||e.IsCharging) charges++;
                if(shots!=_lastShots || charges!=_lastCharges)
                    Debug.Log($"[EnemyClient] shots={shots} charges={charges} drum={currentRun.DrumActivations}");
                _lastShots=shots; _lastCharges=charges;
            }
            if(_lastCount!=EnemyRegistry.Count)
            {
                _lastCount=EnemyRegistry.Count;
                Debug.Log($"[EnemyClient] count={_lastCount} snapshots={EnemySpawner.Instance.ReceivedSnapshots}");
            }
            // Host test bố trí quái 1000 đứng tại (6,6), tất cả quái bất động để đo riêng correction.
            if(_step==0 && EnemyRegistry.Count>=70 && EnemyRegistry.TryGet(1000,out var enemy))
            {
                foreach(var e in EnemyRegistry.Alive) typeof(Enemy).GetField("_stunnedUntil",Private).SetValue(e,Time.time+120f);
                enemy.GetComponent<Rigidbody2D>().position=new Vector2(8,6); enemy.transform.position=new Vector2(8,6);
                _checkAt=Time.unscaledTime+1.5f; _step=1;
                Debug.Log("[EnemyClient] injected 2-unit drift");
            }
            if(_step==1 && Time.unscaledTime>=_checkAt && EnemyRegistry.TryGet(1000,out var corrected))
            {
                float error=Vector2.Distance(corrected.Position,new Vector2(6,6));
                Debug.Log($"[EnemyClient] correction error={error:F3} pass={error<.2f}");
                var s=EnemySpawner.Instance; var run=RunManager.Instance; uint before=s.ReceivedSnapshots;
                s.ApplySnapshotRoster(new int[0],new int[0],new Vector2[0],new Vector2[0],99999,before+100,NetworkTime.time,run.Seed+1,run.CurrentWave);
                s.ApplySnapshotRoster(new int[0],new int[0],new Vector2[0],new Vector2[0],99999,before-1,NetworkTime.time,run.Seed,run.CurrentWave);
                Debug.Log($"[EnemyClient] stale packets rejected={before==s.ReceivedSnapshots && EnemyRegistry.Count>=70}");
                var player=NetworkClient.localPlayer.GetComponent<LAC.Player.PlayerCharacter>();
                Debug.Log($"[EnemyClient] client damage rejected={!LAC.Combat.DamageSystem.ApplyToPlayer(player,1,Vector2.zero)}");
                _step=2;
            }
            if(_step==2 && EnemyRegistry.TryGet(-2007,out var first) && EnemyRegistry.TryGet(-2008,out var second))
            {
                Debug.Log($"[EnemyClient] split children synced={first.Data.DisplayName==second.Data.DisplayName} count={EnemyRegistry.Count}");
                _step=3;
            }
        }
    }
}
#endif
