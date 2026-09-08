#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Net
{
    /// <summary>Kiểm thử hai tiến trình thật, chỉ bật bằng -lac-smoke host/client trên development build.</summary>
    public sealed class CoopSmokeProbe : MonoBehaviour
    {
        private string _role;
        private string _folder;
        private int _errors;
        private NetworkManagerLAC _manager;
        private CharacterId _expectedCharacter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-lac-smoke");
            if (index < 0 || index + 1 >= args.Length) return;
            var probe = new GameObject("CoopSmokeProbe").AddComponent<CoopSmokeProbe>();
            probe._role = args[index + 1];
            probe._folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SmokeResults");
            Directory.CreateDirectory(probe._folder);
            File.WriteAllText(Path.Combine(probe._folder, probe._role + ".txt"),
                "START: " + DateTime.UtcNow.ToString("O") + Environment.NewLine);
            Application.logMessageReceived += probe.OnLog;
            DontDestroyOnLoad(probe.gameObject);
        }

        private IEnumerator Start()
        {
            yield return null;
            _manager = NetworkManager.singleton as NetworkManagerLAC;
            if (_manager == null) { Fail("Missing manager"); yield break; }
            if (_role == "host") _manager.HostLobby();
            else _manager.JoinLobby("127.0.0.1");
            float deadline = Time.realtimeSinceStartup + 70f;
            while (_manager.Members.Count != 2 && Time.realtimeSinceStartup < deadline) yield return null;
            if (_manager.Members.Count != 2) { Fail("Lobby did not reach two players"); yield break; }
            Write("lobby", "members=2 players=" + PlayerRegistry.Count);
            if (PlayerRegistry.Count != 0) { Fail("Gameplay player spawned in lobby"); yield break; }
            if (_manager.Characters == null || _manager.Characters.Count < 2)
            { Fail("Character registry needs at least two entries"); yield break; }
            CharacterData expected = _manager.Characters.GetByIndex(_manager.Characters.Count - 1);
            if (expected == null) { Fail("Missing non-default character"); yield break; }
            _expectedCharacter = expected.Id;

            if (_role == "host")
            {
                // Khách chưa sẵn sàng: host tuyệt đối không được vượt qua ready barrier.
                if (_manager.BeginArena()) { Fail("Host bypassed ready barrier"); yield break; }
                _manager.ChooseCharacter(_expectedCharacter, true);
            }
            if (_role == "client")
            {
                if (_manager.BeginArena()) { Fail("Client started match"); yield break; }
                // Chậm lại để host có một cửa sổ kiểm ready barrier thật.
                yield return new WaitForSecondsRealtime(2f);
                _manager.ChooseCharacter(_expectedCharacter, false);
                yield return new WaitForSecondsRealtime(1f);
                _manager.ChooseCharacter(_expectedCharacter, true);
            }
            yield return new WaitForSecondsRealtime(2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(_folder, _role + "-lobby.png"));
            if (_role == "host")
            {
                while ((!_manager.CanStart || _manager.Members.Any(m => m.Character != _expectedCharacter)) &&
                       Time.realtimeSinceStartup < deadline) yield return null;
                if (!_manager.CanStart || _manager.Members.Any(m => m.Character != _expectedCharacter))
                { Fail("Ready or duplicate character choice did not synchronize"); yield break; }
                yield return new WaitForSecondsRealtime(3f);
                if (!_manager.BeginArena()) { Fail("Ready barrier did not open"); yield break; }
            }
            while (SceneManager.GetActiveScene().name != "Arena" && Time.realtimeSinceStartup < deadline) yield return null;
            while ((PlayerRegistry.Count != 2 || NetworkClient.localPlayer == null || RunManager.Instance == null || RunManager.Instance.CurrentWave == 0)
                && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(2f);
            if (PlayerRegistry.Count != 2 || NetworkClient.localPlayer == null || RunManager.Instance == null || RunManager.Instance.CurrentWave == 0)
            { Fail("Arena, players or wave missing"); yield break; }
            if (PlayerRegistry.All.Any(p => p.CharacterId != _expectedCharacter))
            { Fail("Character selection lost on scene change"); yield break; }
            if (FindObjectsByType<NetworkManagerLAC>().Length != 1)
            { Fail("Duplicate manager"); yield break; }
            if (EnemyRegistry.Count == 0) { Fail("No enemies"); yield break; }
            ScreenCapture.CaptureScreenshot(Path.Combine(_folder, _role + "-arena.png"));
            Write("arena", $"seed={RunManager.Instance.Seed} wave={RunManager.Instance.CurrentWave} players={PlayerRegistry.Count} enemies={EnemyRegistry.Count} character={_expectedCharacter} local={NetworkClient.localPlayer.netId} errors={_errors}");
            yield return new WaitForSecondsRealtime(4f);
            if (_errors > 0) { Fail("Runtime errors=" + _errors); yield break; }
            Write("PASS", "Lobby, ready barrier, authority, duplicate selection, scene transition, 2 players and enemies verified");
            Application.Quit(0);
        }

        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++;
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void Fail(string reason)
        {
            Write("FAIL", reason);
            Debug.LogError("[CoopSmoke] " + reason);
            Application.Quit(1);
        }
        private void Write(string phase, string value) => File.AppendAllText(Path.Combine(_folder, _role + ".txt"), phase + ": " + value + Environment.NewLine);
    }
}
#endif
