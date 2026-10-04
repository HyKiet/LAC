#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Director
{
    // Chỉ cài trong bản kiểm thử khi có cờ và đường dẫn lệnh rõ ràng.
    public sealed class TelemetryTestClient : MonoBehaviour
    {
        private static string _commandPath;
        private string _lastCommand, _lastState;
        private float _next;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--lac-telemetry-client");
            if (at < 0 || at + 1 >= args.Length) return;
            _commandPath = args[at + 1];
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            SceneManager.sceneLoaded += Connect;
        }

        private static void Connect(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= Connect;
            var manager = FindAnyObjectByType<NetworkManagerLAC>();
            typeof(NetworkManagerLAC).GetField("_autoStartHost", Private).SetValue(manager, false);
            manager.networkAddress = "localhost";
            manager.StartClient();
            new GameObject("TelemetryTestClient", typeof(TelemetryTestClient));
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + .1f;
            var local = NetworkClient.localPlayer;
            if (local != null)
            {
                local.GetComponent<PlayerMovement>().enabled = false;
                local.GetComponent<LAC.Combat.WeaponAuto>().enabled = false;
            }
            var run = RunManager.Instance;
            string state = "connected=" + NetworkClient.isConnected + " host=" + NetworkServer.active;
            if (run != null) state += " wave=" + run.CurrentWave + " state=" + run.State + " telemetry=" + (run.Telemetry != null);
            foreach (var p in PlayerRegistry.All) state += " | " + p.netId + " hp=" + p.GetComponent<PlayerHealth>().Health;
            if (state != _lastState) { _lastState = state; Debug.Log("[TelemetryClient] " + state); }
            string command;
            try { command = File.Exists(_commandPath) ? File.ReadAllText(_commandPath).Trim() : ""; }
            catch (IOException) { return; } // Tiến trình điều khiển có thể đang ghi lệnh.
            if (command == "" || command == _lastCommand) return;
            _lastCommand = command;
            string action = command.Split(' ')[0];
            if (action == "dash" && local != null)
            {
                var dash = local.GetComponent<PlayerDash>();
                var method = typeof(PlayerDash).GetMethod("CmdDash", Private);
                method.Invoke(dash, null);
                method.Invoke(dash, null); // Hai yêu cầu sát nhau: host chỉ chấp nhận một.
            }
            else if (action == "leave") NetworkManager.singleton.StopClient();
            else if (action == "join" && !NetworkClient.active) NetworkManager.singleton.StartClient();
            else if (action == "quit") Application.Quit();
            Debug.Log("[TelemetryClient] command=" + command);
        }
    }
}
#endif
