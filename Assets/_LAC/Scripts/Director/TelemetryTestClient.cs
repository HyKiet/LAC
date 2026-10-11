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
        private LAC.VFX.ArenaStageVisuals _arenaVisuals;
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
            var manager = FindAnyObjectByType<NetworkManagerLAC>();
            if (manager == null) return;
            SceneManager.sceneLoaded -= Connect;
            typeof(NetworkManagerLAC).GetField("_autoStartHost", Private).SetValue(manager, false);
            manager.networkAddress = "localhost";
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--lac-class");
            if (at >= 0 && at + 1 < args.Length && Enum.TryParse(args[at + 1], true, out CharacterId choice))
                manager.SelectedCharacter = choice;
            manager.StartClient();
            new GameObject("TelemetryTestClient", typeof(TelemetryTestClient));
        }

        private void Start() => _arenaVisuals = FindAnyObjectByType<LAC.VFX.ArenaStageVisuals>();

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
            if (run != null) state += " wave=" + run.CurrentWave + " total=" + run.TotalWaves + " state=" + run.State + " telemetry=" + (run.Telemetry != null);
            if (run != null) state += " mode=" + run.DirectorMode + " director=" + run.DirectorState +
                " source=" + run.DirectorSource + " hpMin=" + run.DirectorHealthMin.ToString("F3") +
                " damage=" + run.DirectorDamageRatioMax.ToString("F3") + " downs=" + run.DirectorDowns +
                " context=" + run.DirectorContextValid + " fallback=" + run.DirectorFallbackReason;
            if (_arenaVisuals != null) state += " stage=" + _arenaVisuals.CurrentStage;
            foreach (var p in PlayerRegistry.All)
                state += " | " + p.netId + " hp=" + p.GetComponent<PlayerHealth>().Health
                    + " character=" + p.CharacterId + " animation=" + (p.Data != null && p.Data.AnimationSet != null ? p.Data.AnimationSet.name : "none");
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
            else if (action == "invalid") NetworkClient.Send(new NetworkManagerLAC.CharacterChoiceMessage { Character = (CharacterId)99 });
            else if (action == "duplicate") NetworkClient.Send(new NetworkManagerLAC.CharacterChoiceMessage { Character = CharacterId.Lancer });
            else if (action == "probe" && run != null)
            {
                Debug.Log("[TimedProbe] seed=" + run.Seed + " wave=" + run.CurrentWave + " state=" + run.State
                    + " duration=" + run.WaveDuration + " remaining=" + run.WaveSecondsRemaining.ToString("F2")
                    + " enemies=" + LAC.Enemies.EnemyRegistry.Count + " snapshots=" + LAC.Enemies.EnemySpawner.Instance.ReceivedSnapshots);
                var hud = FindAnyObjectByType<LAC.UI.DirectorHud>();
                Debug.Log("[DirectorClientProbe] mode=" + run.DirectorMode + " state=" + run.DirectorState +
                    " source=" + run.DirectorSource + " reason=" + run.DirectorReason +
                    " hpMin=" + run.DirectorHealthMin.ToString("F3") + " damage=" + run.DirectorDamageRatioMax.ToString("F3") +
                    " downs=" + run.DirectorDowns + " context=" + run.DirectorContextValid +
                    " fallback=" + run.DirectorFallbackReason + " hud=" + (hud != null && hud.IsVisible) +
                    " title=" + (hud != null ? hud.Headline : "") + " rejectedModeChange=" + !run.TrySetDirectorMode(DirectorMode.Fixed));
            }
            Debug.Log("[TelemetryClient] command=" + command);
        }
    }
}
#endif
