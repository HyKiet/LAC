using System.Collections.Generic;
using LAC.Cards;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    // Cùng NetworkIdentity của RunManager; không thêm đối tượng mạng vào prefab/scene.
    public sealed partial class RunManager
    {
        public struct CardGrant
        {
            public uint Player;
            public CardId Card;
        }

        private sealed class Selection
        {
            internal PlayerCharacter Player;
            internal List<CardDefinition> Offer;
            internal int Revision;
            internal bool Picked;
            internal bool Finished;
            internal double FeedbackDeadline;
        }

        private readonly SyncList<CardGrant> _cardGrants = new SyncList<CardGrant>();
        private readonly Dictionary<int, Selection> _cardSelections = new Dictionary<int, Selection>();
        private readonly Dictionary<uint, int> _cardRerolls = new Dictionary<uint, int>();
        private sealed class ClientCardReplay
        {
            internal PlayerUpgradeState State;
            internal CharacterData Data;
            internal IReadOnlyList<CardDefinition> Cards;
        }

        private readonly Dictionary<uint, List<CardDefinition>> _clientCardHistories = new Dictionary<uint, List<CardDefinition>>();
        private readonly Dictionary<uint, ClientCardReplay> _clientCardReplays = new Dictionary<uint, ClientCardReplay>();
        private static readonly CardDefinition[] EmptyCardHistory = System.Array.Empty<CardDefinition>();
        private bool _clientHistoryDirty = true;
        private bool _clientHistoryBound;
        private CardDefinition[] _cardDefinitions;
        private CardSelectionRulesData _cardRules;
        private CardSelectionRulesData CardRules => _cardRules != null ? _cardRules :
            _cardRules = Resources.Load<CardSelectionRulesData>("CardSelectionRules");
        [SyncVar] private int _cardRun;
        private int _clientCardRun = -1;
        private int _selectionToken;
        private double _cardDeadline;
        private bool _cardsActive;

        private CardDefinition[] CardDefinitions => _cardDefinitions ??=
            Resources.LoadAll<CardDefinition>("Cards");

        private CardDefinition FindCard(CardId id)
        {
            foreach (CardDefinition card in CardDefinitions)
                if (card.Id == id) return card;
            return null;
        }

        private void ResetCardSelections()
        {
            _cardsActive = false;
            _cardRun++;
            _selectionToken++;
            _cardSelections.Clear();
            _cardRerolls.Clear();
            _cardGrants.Clear();
            foreach (PlayerCharacter player in PlayerRegistry.All)
                if (player != null) player.Upgrades.ResetRun();
        }

        private void BeginCardSelections()
        {
            _selectionToken++;
            if (CardRules == null)
            {
                Debug.LogError("[Cards] Thiếu CardSelectionRules trong Resources.");
                return;
            }
            _cardDeadline = NetworkTime.time + CardRules.SelectionSeconds;
            _cardSelections.Clear();
            // Chốt người tham gia tại đầu lượt. Người vào muộn bắt đầu chọn từ lượt sau.
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!connection.isReady || connection.identity == null) continue;
                PlayerCharacter player = connection.identity.GetComponent<PlayerCharacter>();
                if (player == null || !player.IsAlive) continue;
                var selection = new Selection
                {
                    Player = player,
                    Offer = CardDeck.Draw(CardDefinitions, player.Upgrades, 3, shape: player.Data.WeaponShape)
                };
                selection.Picked = selection.Finished = selection.Offer.Count == 0;
                _cardSelections.Add(connection.connectionId, selection);
                if (!_cardRerolls.ContainsKey(player.netId)) _cardRerolls.Add(player.netId, CardRules.RerollsPerRun);
                SendCardOffer(connection, selection);
            }
            _cardsActive = true;
        }

        private void SendCardOffer(NetworkConnectionToClient connection, Selection selection)
        {
            var ids = new CardId[selection.Offer.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = selection.Offer[i].Id;
            TargetCardOffer(connection, _selectionToken, selection.Revision, ids,
                _cardRerolls[selection.Player.netId], _cardDeadline);
        }

        [TargetRpc]
        private void TargetCardOffer(NetworkConnectionToClient target, int token, int revision,
            CardId[] cards, int rerolls, double deadline)
        {
            CardSelectionController.Instance?.ReceiveOffer(token, revision, cards, rerolls, deadline);
        }

        [Command(requiresAuthority = false)]
        public void CmdPickCard(int token, int revision, CardId card,
            NetworkConnectionToClient sender = null)
        {
            if (!TrySelection(sender, token, out Selection selection) || selection.Picked
                || selection.Revision != revision || NetworkTime.time >= _cardDeadline) return;
            foreach (CardDefinition offered in selection.Offer)
            {
                if (offered.Id != card) continue;
                AcceptCard(sender, selection, offered);
                return;
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdRerollCards(int token, int revision, NetworkConnectionToClient sender = null)
        {
            if (!TrySelection(sender, token, out Selection selection) || selection.Picked
                || selection.Revision != revision || NetworkTime.time >= _cardDeadline
                || _cardRerolls[selection.Player.netId] <= 0) return;
            var avoid = new HashSet<CardId>();
            foreach (CardDefinition card in selection.Offer) avoid.Add(card.Id);
            selection.Offer = CardDeck.Draw(CardDefinitions, selection.Player.Upgrades, 3, avoid,
                selection.Player.Data.WeaponShape);
            _cardRerolls[selection.Player.netId]--;
            selection.Revision++;
            SendCardOffer(sender, selection);
        }

        private bool TrySelection(NetworkConnectionToClient sender, int token, out Selection selection)
        {
            selection = null;
            return _cardsActive && _state == RunState.CardSelection && token == _selectionToken
                && sender != null && sender.identity != null
                && _cardSelections.TryGetValue(sender.connectionId, out selection)
                && selection.Player != null && selection.Player.IsAlive
                && selection.Player.netIdentity == sender.identity;
        }

        private void AcceptCard(NetworkConnectionToClient connection, Selection selection, CardDefinition card)
        {
            int previousEvolutions = selection.Player.Upgrades.Evolutions.Count;
            if (!selection.Player.Upgrades.Apply(card, selection.Player.GetComponent<PlayerHealth>())) return;
            var evolutions = selection.Player.Upgrades.Evolutions;
            var evolvedIds = new string[evolutions.Count - previousEvolutions];
            for (int i = 0; i < evolvedIds.Length; i++) evolvedIds[i] = evolutions[previousEvolutions + i].Id;
            selection.Picked = true;
            // ACK giữ animation trên máy chậm; thời hạn chặn client treo cả ván vô hạn.
            selection.FeedbackDeadline = NetworkTime.time + CardHoverVisual.ConsumeDuration
                + evolvedIds.Length * CardSelectionView.EvolutionSeconds + 3d;
            _cardGrants.Add(new CardGrant { Player = selection.Player.netId, Card = card.Id });
            TargetCardAccepted(connection, _selectionToken, card.Id, evolvedIds);
        }

        [TargetRpc]
        private void TargetCardAccepted(NetworkConnectionToClient target, int token, CardId card, string[] evolvedIds)
        {
            CardSelectionController.Instance?.ReceiveAccepted(token, card, evolvedIds);
        }

        [Command(requiresAuthority = false)]
        public void CmdCardFeedbackComplete(int token, NetworkConnectionToClient sender = null)
        {
            if (TrySelection(sender, token, out Selection selection) && selection.Picked)
                selection.Finished = true;
        }

        private void Update()
        {
            if (isClient && !isServer) ApplyCardHistory();
            if (!isServer || !_cardsActive) return;
            if (_state != RunState.CardSelection)
            {
                _cardsActive = false;
                _cardSelections.Clear();
                return;
            }

            bool complete = true;
            foreach (var entry in _cardSelections)
            {
                Selection selection = entry.Value;
                if (!NetworkServer.connections.TryGetValue(entry.Key, out var connection)
                    || connection.identity == null || selection.Player == null || !selection.Player.IsAlive
                    || connection.identity != selection.Player.netIdentity) continue;
                if (!selection.Picked && NetworkTime.time >= _cardDeadline)
                {
                    // Host tự chọn một thẻ hợp lệ bằng cùng nguồn ngẫu nhiên gameplay.
                    if (selection.Offer.Count > 0)
                        AcceptCard(connection, selection, selection.Offer[RunRandom.Cards.Range(0, selection.Offer.Count)]);
                    else selection.Picked = selection.Finished = true;
                }
                if (selection.Picked && NetworkTime.time >= selection.FeedbackDeadline)
                    selection.Finished = true;
                if (!selection.Finished) complete = false;
            }
            if (!complete) return;
            _cardsActive = false;
            ReportCardSelectionComplete();
        }

        private void ApplyCardHistory()
        {
            BindClientCardHistory();
            if (_clientCardRun != _cardRun)
            {
                _clientCardRun = _cardRun;
                _clientCardReplays.Clear();
                _clientHistoryDirty = true;
            }
            if (_clientHistoryDirty)
            {
                // Giữ thứ tự grant; dựng lại chỉ khi SyncList đổi, không quét mỗi frame.
                _clientCardHistories.Clear();
                for (int i = 0; i < _cardGrants.Count; i++)
                {
                    CardGrant grant = _cardGrants[i];
                    if (!_clientCardHistories.TryGetValue(grant.Player, out var history))
                    {
                        history = new List<CardDefinition>(15);
                        _clientCardHistories.Add(grant.Player, history);
                    }
                    history.Add(FindCard(grant.Card));
                }
                _clientHistoryDirty = false;
            }
            // Lịch sử chỉ chứa định danh, tự đồng bộ cho người vào muộn. Không gửi chỉ số.
            var players = PlayerRegistry.All;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerCharacter player = players[i];
                // Grant có thể tới trước nhân vật/dữ liệu; đợi để tiến hoá đúng kiểu vũ khí.
                if (player == null || player.Data == null || player.Upgrades == null) continue;
                IReadOnlyList<CardDefinition> history = _clientCardHistories.TryGetValue(player.netId, out var cards)
                    ? cards : EmptyCardHistory;
                if (_clientCardReplays.TryGetValue(player.netId, out var replay)
                    && replay.State == player.Upgrades && replay.Data == player.Data
                    && SameCardHistory(replay.Cards, history))
                {
                    replay.Cards = history;
                    continue;
                }
                player.Upgrades.ReplayCards(history);
                _clientCardReplays[player.netId] = new ClientCardReplay
                { State = player.Upgrades, Data = player.Data, Cards = history };
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Initial deserialize không phát OnChange; luôn nạp snapshot đầu phiên.
            ResetClientCardHistory();
            BindClientCardHistory();
        }

        public override void OnStopClient()
        {
            if (_clientHistoryBound) _cardGrants.OnChange -= OnClientCardHistoryChanged;
            _clientHistoryBound = false;
            ResetClientCardHistory();
            base.OnStopClient();
        }

        private void BindClientCardHistory()
        {
            if (_clientHistoryBound) return;
            _cardGrants.OnChange += OnClientCardHistoryChanged;
            _clientHistoryBound = true;
        }

        private void OnClientCardHistoryChanged(SyncList<CardGrant>.Operation operation, int index, CardGrant grant)
            => _clientHistoryDirty = true;

        private void ResetClientCardHistory()
        {
            _clientCardRun = -1;
            _clientHistoryDirty = true;
            _clientCardHistories.Clear();
            _clientCardReplays.Clear();
        }

        private static bool SameCardHistory(IReadOnlyList<CardDefinition> previous, IReadOnlyList<CardDefinition> current)
        {
            if (ReferenceEquals(previous, current)) return true;
            if (previous.Count != current.Count) return false;
            for (int i = 0; i < current.Count; i++) if (previous[i] != current[i]) return false;
            return true;
        }
    }
}
