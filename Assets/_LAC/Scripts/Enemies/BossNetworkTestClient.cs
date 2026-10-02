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
    public sealed class BossNetworkTestClient : MonoBehaviour
    {
        private float _nextLog;
        private string _last;
        private bool _checked;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--lac-boss-client")<0) return;
            Application.runInBackground=true; SceneManager.sceneLoaded+=Connect;
        }
        private static void Connect(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=Connect;
            var manager=FindAnyObjectByType<NetworkManagerLAC>();
            typeof(NetworkManagerLAC).GetField("_autoStartHost",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,false);
            manager.networkAddress="localhost"; manager.StartClient();
            new GameObject("BossNetworkTestClient",typeof(BossNetworkTestClient));
        }
        private void Update()
        {
            if(NetworkClient.localPlayer==null || Time.unscaledTime<_nextLog) return;
            _nextLog=Time.unscaledTime+.2f;
            NetworkClient.localPlayer.GetComponent<LAC.Combat.WeaponAuto>().enabled=false;
            var spawner=EnemySpawner.Instance; var run=RunManager.Instance;
            if(spawner==null || run==null) return;
            Enemy boss=null; foreach(var enemy in EnemyRegistry.Alive) if(enemy.IsBoss) {boss=enemy;break;}
            string state=$"seed={run.Seed} wave={run.CurrentWave} state={run.State} hp={spawner.BossHealth}/{spawner.BossMaximum} phase={spawner.BossPhase} count={EnemyRegistry.Count} action={(boss!=null?boss.CurrentBossAction.ToString():"none")} shots={FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length}";
            if(state!=_last){Debug.Log("[BossClient] "+state);_last=state;}
            if(boss!=null && !_checked)
            {
                _checked=true;
                float hp=boss.Health;
                bool applied=LAC.Combat.DamageSystem.ApplyToEnemy(boss,999,Vector2.zero);
                Debug.Log($"[BossClient] client damage rejected={!applied && hp==boss.Health}; health synchronized={boss.Health==spawner.BossHealth && boss.MaxHealth==spawner.BossMaximum}");
            }
        }
    }
}
#endif
