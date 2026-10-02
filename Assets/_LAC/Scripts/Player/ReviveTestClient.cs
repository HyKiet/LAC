#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Player
{
    public sealed class ReviveTestClient : MonoBehaviour
    {
        private string _last;
        private float _next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-revive-client") < 0) return;
            Application.runInBackground = true; Application.targetFrameRate = 60;
            SceneManager.sceneLoaded += Connect;
        }
        private static void Connect(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Connect;
            var manager = FindAnyObjectByType<NetworkManagerLAC>();
            typeof(NetworkManagerLAC).GetField("_autoStartHost", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, false);
            manager.networkAddress = "localhost"; manager.StartClient();
            new GameObject("ReviveTestClient", typeof(ReviveTestClient));
        }
        private void Update()
        {
            if (NetworkClient.localPlayer == null || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + .1f;
            var local = NetworkClient.localPlayer;
            local.GetComponent<PlayerMovement>().enabled = false;
            local.GetComponent<LAC.Combat.WeaponAuto>().enabled = false;
            string state = RunManager.Instance.State + " seed=" + RunManager.Instance.Seed;
            foreach (var p in PlayerRegistry.All)
            {
                var h = p.GetComponent<PlayerHealth>();
                state += $" | {p.netId} hp={h.Health}/{h.MaxHealth} down={h.IsDowned} rescue={h.IsBeingRevived} progress={(int)(h.ReviveProgress * 4)} input={p.GetComponent<PlayerInputReader>().enabled}";
            }
            if (state == _last) return;
            _last = state; Debug.Log("[ReviveClient] " + state);
        }
    }
}
#endif
