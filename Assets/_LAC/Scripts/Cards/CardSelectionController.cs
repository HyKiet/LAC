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
        public static bool IsAvailable { get; private set; }
        public static bool CombatInputLocked { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsAvailable = false;
            CombatInputLocked = false;
        }

        [SerializeField] private CardDefinition[] _definitions;
        [SerializeField, Min(0f)] private float _selectionFeedbackSeconds = 0.12f;
        [SerializeField, Range(0f, 30f)] private float _songTienSpreadDegrees = 7f;
        [SerializeField, Min(0f)] private float _bocPhaRadius = 1.75f;

        private readonly HashSet<CardId> _lastOffer = new HashSet<CardId>();
        private CardSelectionView _view;
        private RunManager _run;
        private PlayerCharacter _player;
        private PlayerUpgradeState _state;
        private PlayerInputReader _input;
        private bool _inputWasEnabled;
        private float _previousTimeScale = 1f;
        private int _rerollsRemaining = 2;
        private bool _selectionOpen;
        private bool _committing;
        private bool _pendingOpen;
        private bool _ownsAvailability;
        private bool _ownsPause;
        private Coroutine _finishSelection;

        private void Awake()
        {
            if (IsAvailable)
            {
                Destroy(gameObject);
                return;
            }

            IsAvailable = true;
            _ownsAvailability = true;
            DontDestroyOnLoad(gameObject);
            _view = GetComponent<CardSelectionView>();
            if (_view == null) _view = gameObject.AddComponent<CardSelectionView>();
            if (_definitions == null || _definitions.Length == 0)
                _definitions = Resources.LoadAll<CardDefinition>("Cards");
        }

        private void OnDestroy()
        {
            if (_run != null) Unbind();
            if (_ownsAvailability) IsAvailable = false;
            RestoreCombat();
        }

        private void Update()
        {
            if (_run == null && RunManager.Instance != null) Bind(RunManager.Instance);
            if ((_pendingOpen || _selectionOpen) && _player == null) FindLocalPlayer();
            if (_pendingOpen && _player != null) OpenSelection();
        }

        private void Bind(RunManager run)
        {
            _run = run;
            _run.WaveCleared += OnWaveCleared;
            _run.RunStarted += OnRunStarted;
            _run.RunEnded += OnRunEnded;

            if (_run.State == RunState.CardSelection) OnWaveCleared(_run.CurrentWave);
        }

        private void Unbind()
        {
            _run.WaveCleared -= OnWaveCleared;
            _run.RunStarted -= OnRunStarted;
            _run.RunEnded -= OnRunEnded;
            _run = null;
        }

        private void OnRunStarted()
        {
            CloseSelection();
            _rerollsRemaining = 2;
            _lastOffer.Clear();
            FindLocalPlayer();
            if (_state != null) _state.ResetRun();
            _view.RefreshOwned(_definitions, _state);
        }

        private void OnRunEnded(bool _)
        {
            _pendingOpen = false;
            CloseSelection();
        }

        private void OnWaveCleared(int _)
        {
            // Lựa chọn ảnh hưởng trạng thái thật nên chỉ host thực hiện. Chơi một người vẫn
            // đi qua đúng host mode của Mirror, không có nhánh gameplay riêng.
            if (!NetworkServer.active || _run == null || _run.IsOver || _run.IsFinalWave) return;
            _pendingOpen = true;
            FindLocalPlayer();
            if (_player != null) OpenSelection();
        }

        private void FindLocalPlayer()
        {
            for (int i = 0; i < PlayerRegistry.Count; i++)
            {
                PlayerCharacter candidate = PlayerRegistry.All[i];
                if (candidate == null || !candidate.isLocalPlayer) continue;

                _player = candidate;
                _state = candidate.Upgrades;
                _state.ConfigureProjectileEffects(_songTienSpreadDegrees, _bocPhaRadius);
                _input = candidate.GetComponent<PlayerInputReader>();
                _view.RefreshOwned(_definitions, _state);
                return;
            }
        }

        private void OpenSelection()
        {
            if (_selectionOpen || _committing || _state == null) return;
            _pendingOpen = false;

            List<CardDefinition> offer = CardDeck.Draw(_definitions, _state, 3);
            if (offer.Count == 0)
            {
                CompleteWithoutSelection();
                return;
            }

            _lastOffer.Clear();
            for (int i = 0; i < offer.Count; i++) _lastOffer.Add(offer[i].Id);

            PauseCombat();
            _selectionOpen = true;
            _view.Show(offer, _state, _rerollsRemaining, Pick, Reroll);
        }

        private void Reroll()
        {
            if (!_selectionOpen || _committing || _rerollsRemaining <= 0) return;
            _rerollsRemaining--;

            List<CardDefinition> offer = CardDeck.Draw(_definitions, _state, 3, _lastOffer);
            _lastOffer.Clear();
            for (int i = 0; i < offer.Count; i++) _lastOffer.Add(offer[i].Id);
            _view.Show(offer, _state, _rerollsRemaining, Pick, Reroll);
        }

        private void Pick(CardDefinition card)
        {
            if (!_selectionOpen || _committing || card == null) return;
            _committing = true;

            PlayerHealth health = _player != null ? _player.GetComponent<PlayerHealth>() : null;
            if (!_state.Apply(card, health))
            {
                _committing = false;
                return;
            }

            _view.MarkSelected(card, _player != null ? _player.transform : null);
            _view.RefreshOwned(_definitions, _state);
            _finishSelection = StartCoroutine(FinishSelectionAfterFeedback());
        }

        private IEnumerator FinishSelectionAfterFeedback()
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(_selectionFeedbackSeconds, CardHoverVisual.ConsumeDuration));
            _finishSelection = null;
            CloseSelection();
            if (_run != null && NetworkServer.active)
                _run.ReportCardSelectionComplete();
            _committing = false;
        }

        private void CompleteWithoutSelection()
        {
            CloseSelection();
            if (_run != null && NetworkServer.active)
                _run.ReportCardSelectionComplete();
        }

        private void PauseCombat()
        {
            // Đợt thường kết thúc ngay lúc quái cuối chết. Không lưu số 0 tạm thời
            // của hit-stop làm tốc độ cần khôi phục sau khi chọn thẻ.
            HitStop.Cancel();
            _previousTimeScale = Time.timeScale;
            _inputWasEnabled = _input != null && _input.enabled;
            if (_input != null) _input.enabled = false;
            CombatInputLocked = true;
            _ownsPause = true;
            Time.timeScale = 0f;
        }

        private void RestoreCombat()
        {
            if (!_ownsPause) return;
            _ownsPause = false;
            Time.timeScale = _previousTimeScale;
            PlayerHealth health = _player != null ? _player.GetComponent<PlayerHealth>() : null;
            if (_input != null) _input.enabled = _inputWasEnabled && (health == null || health.IsAlive);
            CombatInputLocked = false;
        }

        private void CloseSelection()
        {
            // Không để phản hồi chọn của ván cũ chuyển đợt trong ván vừa khởi động lại.
            if (_finishSelection != null)
            {
                StopCoroutine(_finishSelection);
                _finishSelection = null;
            }
            _committing = false;
            _pendingOpen = false;
            _selectionOpen = false;
            if (_view != null) _view.Hide();
            RestoreCombat();
        }
    }
}
