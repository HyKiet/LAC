#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Drum
{
    /// <summary>Client kiểm thử độc lập, chỉ cài khi có --lac-drum-test-client.</summary>
    public sealed class DrumNetworkTestClient : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private uint _lastActivation;
        private int _lastEnergy = -1;
        private bool _lastReady;
        private int _step;
        private double _actAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-drum-test-client") < 0) return;
            Application.runInBackground = true;
            SceneManager.sceneLoaded += Connect;
        }
        private static void Connect(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Connect;
            var manager = FindAnyObjectByType<NetworkManagerLAC>();
            typeof(NetworkManagerLAC).GetField("_autoStartHost", Private).SetValue(manager, false);
            manager.networkAddress = "localhost";
            manager.StartClient();
            DontDestroyOnLoad(new GameObject("DrumNetworkTestClient", typeof(DrumNetworkTestClient)));
        }
        private void Update()
        {
            var run = RunManager.Instance;
            if (run == null || NetworkClient.localPlayer == null) return;
            if (_lastActivation != run.DrumActivations || _lastEnergy != run.DrumEnergy || _lastReady != run.DrumReady)
            {
                Debug.Log($"[DrumClient] activation={run.DrumActivations} energy={run.DrumEnergy} ready={run.DrumReady} cooldown={run.DrumCooldownRemaining:F1}");
                _lastActivation = run.DrumActivations; _lastEnergy = run.DrumEnergy; _lastReady = run.DrumReady;
            }
            var player = NetworkClient.localPlayer;
            player.GetComponent<LAC.Combat.WeaponAuto>().enabled = false;
            if (_step == 0 && run.DrumReady)
            {
                Vector2 start = run.DrumPosition + Vector2.left * 3f;
                player.GetComponent<Rigidbody2D>().position = start;
                player.transform.position = start;
                typeof(PlayerMovement).GetProperty("Facing").SetValue(player.GetComponent<PlayerMovement>(), Vector2.right);
                _actAt = Time.realtimeSinceStartupAsDouble + 1;
                _step = 1;
            }
            if (_step == 1 && Time.realtimeSinceStartupAsDouble >= _actAt)
            {
                var dash = player.GetComponent<PlayerDash>();
                typeof(PlayerMovement).GetProperty("Facing").SetValue(player.GetComponent<PlayerMovement>(), Vector2.right);
                typeof(PlayerDash).GetMethod("BeginLocalDash", Private).Invoke(dash, null);
                typeof(PlayerDash).GetMethod("CmdDash", Private).Invoke(dash, null);
                Debug.Log($"[DrumClient] actual dash sent start={player.transform.position} owned={dash.isOwned}");
                _actAt = Time.realtimeSinceStartupAsDouble + 1;
                _step = 2;
            }
            if (_step == 2 && Time.realtimeSinceStartupAsDouble >= _actAt)
            {
                Debug.Log($"[DrumClient] dash ended position={player.transform.position} activation={run.DrumActivations}");
                _step = 3;
            }
        }
    }
}
#endif
