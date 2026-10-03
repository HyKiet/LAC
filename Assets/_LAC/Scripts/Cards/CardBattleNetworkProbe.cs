#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Reflection;
using LAC.Core;
using LAC.Net;
using LAC.Player;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LAC.Cards
{
    /// <summary>Client kiểm vào muộn và vệt tốc độ; chỉ chạy với --lac-card-battle-probe.</summary>
    public sealed class CardBattleNetworkProbe : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private double _joinedAt, _pickAt;
        private bool _snapshotReported, _movementStarted;
        private int _selectionWave;
        private Keyboard _keyboard;
        private bool _ownsKeyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lac-card-battle-probe") < 0) return;
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
            DontDestroyOnLoad(new GameObject("CardBattleNetworkProbe", typeof(CardBattleNetworkProbe)));
        }

        private void Update()
        {
            var run = RunManager.Instance;
            var ui = CardSelectionController.Instance;
            if (run == null || ui == null || NetworkClient.localPlayer == null || PlayerRegistry.Count != 2) return;
            if (_joinedAt == 0d) _joinedAt = Time.realtimeSinceStartupAsDouble;
            var feedback = ui.GetComponent<CardBattleFeedback>();
            if (feedback == null) return;
            if (!_snapshotReported && Time.realtimeSinceStartupAsDouble - _joinedAt >= .6d)
            {
                _snapshotReported = true;
                int inherited = 0;
                for (int i = 0; i < PlayerRegistry.Count; i++)
                    if (!PlayerRegistry.All[i].isOwned) inherited += PlayerRegistry.All[i].Upgrades.GetStacks(CardId.KhinhThan);
                Debug.Log($"[CardBattleProbe] SNAPSHOT state={run.State} inheritedKhinh={inherited}"
                    + $" upgrade={feedback.Presented(CardBattleEffect.Kind.Upgrade)} heal={feedback.Presented(CardBattleEffect.Kind.Heal)} pool={feedback.PooledEffects}");
            }
            if (run.State == RunState.CardSelection && (bool)typeof(CardSelectionController).GetField("_selectionOpen", Private).GetValue(ui))
            {
                if (_selectionWave != run.CurrentWave)
                { _selectionWave = run.CurrentWave; _pickAt = Time.realtimeSinceStartupAsDouble + .8d; }
                if (Time.realtimeSinceStartupAsDouble >= _pickAt)
                {
                    var slots = (Array)typeof(CardSelectionView).GetField("_slots", Private).GetValue(ui.GetComponent<CardSelectionView>());
                    var slot = slots.GetValue(0);
                    ((Button)slot.GetType().GetField("Button").GetValue(slot)).onClick.Invoke();
                    _pickAt = double.MaxValue;
                }
            }
            var local = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
            if (!_movementStarted && run.State == RunState.WaveActive && run.CurrentWave >= 4
                && local.Upgrades.GetStacks(CardId.KhinhThan) > 0)
            {
                _movementStarted = true;
                StartCoroutine(CheckMovement(feedback));
            }
        }

        private IEnumerator CheckMovement(CardBattleFeedback feedback)
        {
            _keyboard = Keyboard.current;
            if (_keyboard == null) { _keyboard = InputSystem.AddDevice<Keyboard>(); _ownsKeyboard = true; }
            yield return new WaitForSecondsRealtime(.5f);
            int wind = feedback.Presented(CardBattleEffect.Kind.Wind);
            SetKeys(Key.D);
            yield return new WaitForSecondsRealtime(.9f);
            SetKeys();
            yield return new WaitForSecondsRealtime(.7f);
            int walking = feedback.Presented(CardBattleEffect.Kind.Wind) - wind;
            Debug.Log($"[CardBattleProbe] WALK wind={walking}");
            wind = feedback.Presented(CardBattleEffect.Kind.Wind);
            SetKeys(Key.Space);
            yield return new WaitForSecondsRealtime(.1f);
            SetKeys();
            yield return new WaitForSecondsRealtime(.8f);
            int dash = feedback.Presented(CardBattleEffect.Kind.Wind) - wind;
            Debug.Log($"[CardBattleProbe] DASH wind={dash}");
            wind = feedback.Presented(CardBattleEffect.Kind.Wind);
            yield return new WaitForSecondsRealtime(.6f);
            int idle = feedback.Presented(CardBattleEffect.Kind.Wind) - wind;
            SetKeys(Key.A);
            yield return new WaitForSecondsRealtime(.65f);
            SetKeys();
            yield return new WaitForSecondsRealtime(.6f);
            int resumed = feedback.Presented(CardBattleEffect.Kind.Wind) - wind;
            if (walking >= 2 && walking <= 6 && dash == 0 && idle == 0 && resumed >= 1 && resumed <= 5)
                Debug.Log($"[CardBattleProbe] MOVEMENT PASSED walk={walking} dash={dash} idle={idle} resume={resumed} pool={feedback.PooledEffects}");
            else Debug.LogError($"[CardBattleProbe] MOVEMENT FAILED walk={walking} dash={dash} idle={idle} resume={resumed}");
        }

        private void SetKeys(params Key[] keys)
        { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }

        private void OnDestroy()
        {
            SetKeys();
            if (_ownsKeyboard && _keyboard != null) InputSystem.RemoveDevice(_keyboard);
        }
    }
}
#endif
