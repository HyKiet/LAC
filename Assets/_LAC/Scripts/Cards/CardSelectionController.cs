using System.Collections;
using System.Collections.Generic;
using LAC.Core;
using LAC.Player;
using LAC.VFX;
using Mirror;
using UnityEngine;

namespace LAC.Cards
{
    public sealed class CardSelectionController : MonoBehaviour
    {
        public static CardSelectionController Instance { get; private set; }
        public static bool IsAvailable => Instance != null;
        public static bool CombatInputLocked { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; CombatInputLocked = false; }

        [SerializeField] private CardDefinition[] _definitions;
        [SerializeField, Min(0f)] private float _selectionFeedbackSeconds = 0.12f;
        private CardSelectionView _view;
        private RunManager _run;
        private PlayerCharacter _player;
        private PlayerUpgradeState _state;
        private PlayerInputReader _input;
        private bool _inputWasEnabled;
        private float _previousTimeScale = 1f;
        private int _rerollsRemaining = 2;
        private int _token;
        private int _revision;
        private double _deadline;
        private int _displayedCountdown = -1;
        private bool _selectionOpen;
        private bool _committing;
        private bool _ownsPause;
        private bool _requestPending;
        private CardId[] _pendingOffer;
        private Coroutine _finishSelection;
        private string[] _acceptedEvolutions;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _view = GetComponent<CardSelectionView>();
            if (_view == null) _view = gameObject.AddComponent<CardSelectionView>();
            // Prefab demo cũ chỉ chứa 7 tham chiếu; luôn nạp cùng danh mục với host.
            _definitions = Resources.LoadAll<CardDefinition>("Cards");
            if (!TryGetComponent<CardBattleFeedback>(out _)) gameObject.AddComponent<CardBattleFeedback>();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (_state != null) _state.Changed -= RefreshOwned;
            Unbind();
            CloseSelection();
            Instance = null;
        }

        private void Update()
        {
            if (_run == null && RunManager.Instance != null) Bind(RunManager.Instance);
            if (!NetworkClient.isConnected)
            {
                if (_ownsPause) CloseSelection();
                _token = _revision = 0;
                _player = null;
                if (_state != null) _state.Changed -= RefreshOwned;
                _state = null;
                return;
            }
            if (_player == null) FindLocalPlayer();
            if (_run == null) return;
            if (_run.State != RunState.CardSelection)
            {
                if (_ownsPause) CloseSelection();
                return;
            }
            if (!_ownsPause) PauseCombat();
            if (_pendingOffer != null && _state != null) ShowPendingOffer();
            if (_selectionOpen && !_committing)
            {
                float remaining = Mathf.Max(0f, (float)(_deadline - NetworkTime.time));
                int seconds = Mathf.CeilToInt(remaining);
                if (seconds != _displayedCountdown)
                {
                    _displayedCountdown = seconds;
                    _view.SetStatus(seconds > 0 ? $"TỰ CHỌN SAU {seconds} GIÂY" : "ĐANG TỰ CHỌN…");
                    if (seconds == 0) _view.SetButtonsEnabled(false);
                }
            }
        }

        private void Bind(RunManager run)
        {
            _run = run;
            _run.RunStarted += OnRunStarted;
            _run.RunEnded += OnRunEnded;
            _run.WaveStarted += OnWaveStarted;
        }

        private void Unbind()
        {
            if (_run == null) return;
            _run.RunStarted -= OnRunStarted;
            _run.RunEnded -= OnRunEnded;
            _run.WaveStarted -= OnWaveStarted;
            _run = null;
        }

        private void OnRunStarted()
        {
            CloseSelection();
            _rerollsRemaining = 2;
            FindLocalPlayer();
            _view.RefreshOwned(_definitions, _state);
        }

        private void OnRunEnded(bool _) => CloseSelection();
        private void OnWaveStarted(int _) { CloseSelection(); _view.RefreshOwned(_definitions, _state); }

        private void FindLocalPlayer()
        {
            if (NetworkClient.localPlayer == null) return;
            _player = NetworkClient.localPlayer.GetComponent<PlayerCharacter>();
            if (_player == null) return;
            _state = _player.Upgrades;
            _state.Changed -= RefreshOwned;
            _state.Changed += RefreshOwned;
            _input = _player.GetComponent<PlayerInputReader>();
            if (_ownsPause)
            {
                _inputWasEnabled = _input != null && _input.enabled;
                if (_input != null) _input.enabled = false;
            }
            _view.RefreshOwned(_definitions, _state);
        }

        private void RefreshOwned() => _view.RefreshOwned(_definitions, _state);

        public void ReceiveOffer(int token, int revision, CardId[] cards, int rerolls, double deadline)
        {
            if (token < _token || (token == _token && revision < _revision)) return;
            _token = token;
            _revision = revision;
            _rerollsRemaining = rerolls;
            _deadline = deadline;
            _pendingOffer = cards;
            _requestPending = false;
        }

        private void ShowPendingOffer()
        {
            var offer = new List<CardDefinition>(3);
            foreach (CardId id in _pendingOffer)
            {
                CardDefinition card = FindCard(id);
                if (card != null) offer.Add(card);
            }
            _pendingOffer = null;
            _selectionOpen = true;
            _displayedCountdown = -1;
            _view.Show(offer, _state, _rerollsRemaining, Pick, Reroll);
            if (offer.Count == 0) { _committing = true; _view.ShowWaiting(); }
        }

        private CardDefinition FindCard(CardId id)
        {
            foreach (CardDefinition card in _definitions) if (card.Id == id) return card;
            return null;
        }

        private void Reroll()
        {
            if (!_selectionOpen || _committing || _requestPending || _rerollsRemaining <= 0) return;
            _requestPending = true;
            _view.SetButtonsEnabled(false);
            _run.CmdRerollCards(_token, _revision);
        }

        private void Pick(CardDefinition card)
        {
            if (!_selectionOpen || _committing || _requestPending || card == null) return;
            _requestPending = true;
            _view.SetButtonsEnabled(false);
            _run.CmdPickCard(_token, _revision, card.Id);
        }

        public void ReceiveAccepted(int token, CardId id, string[] evolvedIds = null)
        {
            if (token != _token || _committing) return;
            if (_pendingOffer != null) { FindLocalPlayer(); if (_state != null) ShowPendingOffer(); }
            _committing = true;
            _acceptedEvolutions = evolvedIds;
            _view.MarkSelected(FindCard(id), _player != null ? _player.transform : null);
            _view.SetStatus("ĐÃ CHỌN NÂNG CẤP");
            _finishSelection = StartCoroutine(FinishSelectionAfterFeedback());
        }

        private IEnumerator FinishSelectionAfterFeedback()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(_selectionFeedbackSeconds, CardHoverVisual.ConsumeDuration));
            if (_acceptedEvolutions != null)
            {
                foreach (string id in _acceptedEvolutions)
                {
                    CardEvolutionData recipe = CardEvolutionCatalog.Find(id);
                    if (recipe == null) continue;
                    _view.ShowEvolution(recipe);
                    yield return new WaitForSecondsRealtime(CardSelectionView.EvolutionSeconds);
                }
            }
            _finishSelection = null;
            _view.RefreshOwned(_definitions, _state);
            _view.ShowWaiting();
            if (_run != null && NetworkClient.isConnected) _run.CmdCardFeedbackComplete(_token);
        }

        private void PauseCombat()
        {
            HitStop.Cancel();
            _previousTimeScale = Time.timeScale;
            _inputWasEnabled = _input != null && _input.enabled;
            if (_input != null) _input.enabled = false;
            CombatInputLocked = true;
            _ownsPause = true;
            Time.timeScale = 0f;
            _view.ShowWaiting();
        }

        private void CloseSelection()
        {
            if (_finishSelection != null) StopCoroutine(_finishSelection);
            _finishSelection = null;
            _pendingOffer = null;
            _acceptedEvolutions = null;
            _displayedCountdown = -1;
            _committing = _selectionOpen = _requestPending = false;
            if (_view != null) _view.Hide();
            if (!_ownsPause) return;
            _ownsPause = false;
            Time.timeScale = _previousTimeScale;
            if (_input != null) _input.enabled = _inputWasEnabled && (_player == null || _player.IsAlive);
            CombatInputLocked = false;
        }
    }
}
