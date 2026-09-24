#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LAC.Cards
{
    /// <summary>Client kiểm thử hai tiến trình; chỉ bật bằng --lac-card-test-client ở development build.</summary>
    public sealed class CardNetworkTestClient : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private int _wave;
        private double _openedAt;
        private int _step;
        private double _nextAction;
        private int _reportedWave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-card-test-client") < 0) return;
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
            DontDestroyOnLoad(new GameObject("CardNetworkTestClient", typeof(CardNetworkTestClient)));
        }

        private void Update()
        {
            var run = RunManager.Instance;
            var ui = CardSelectionController.Instance;
            if (run == null || ui == null || NetworkClient.localPlayer == null) return;
            if (run.State == RunState.WaveActive)
            {
                _wave = 0;
                if (_reportedWave != run.CurrentWave)
                {
                    _reportedWave = run.CurrentWave;
                    foreach (var player in PlayerRegistry.All)
                    {
                        string stacks = "";
                        foreach (CardId id in Enum.GetValues(typeof(CardId))) stacks += $"{id}:{player.Upgrades.GetStacks(id)},";
                        Debug.Log($"[CardNetClient] wave={run.CurrentWave} player={player.netId} cards={stacks} timeScale={Time.timeScale}");
                    }
                }
                return;
            }
            if (run.State != RunState.CardSelection || !Get<bool>(ui, "_selectionOpen")) return;
            if (_wave != run.CurrentWave)
            {
                _wave = run.CurrentWave;
                _openedAt = Time.realtimeSinceStartupAsDouble;
                _step = 0;
                _nextAction = _openedAt + 3d;
                Debug.Log($"[CardNetClient] offer wave={_wave}, rerolls={Get<int>(ui, "_rerollsRemaining")}");
            }
            if (Time.realtimeSinceStartupAsDouble < _nextAction || Get<bool>(ui, "_committing")) return;
            // Đợt 2 cố ý không bấm để kiểm tra tự chọn sau đúng 10 giây.
            if (_wave == 2) return;
            if (_step < 2 && Get<int>(ui, "_rerollsRemaining") > 0)
            {
                typeof(CardSelectionController).GetMethod("Reroll", Private).Invoke(ui, null);
                _step++;
                _nextAction = Time.realtimeSinceStartupAsDouble + .5d;
                return;
            }
            var view = ui.GetComponent<CardSelectionView>();
            var slots = Get<Array>(view, "_slots");
            var slot = slots.GetValue(0);
            var button = (UnityEngine.UI.Button)slot.GetType().GetField("Button").GetValue(slot);
            button.onClick.Invoke();
            Debug.Log($"[CardNetClient] picked wave={_wave} elapsed={Time.realtimeSinceStartupAsDouble - _openedAt:F2}");
            _nextAction = double.MaxValue;
        }

        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    }
}
#endif
