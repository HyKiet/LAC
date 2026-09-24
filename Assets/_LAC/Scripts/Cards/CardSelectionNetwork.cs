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
        private readonly Dictionary<uint, int> _clientGrantCounts = new Dictionary<uint, int>();
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
                    Offer = CardDeck.Draw(CardDefinitions, player.Upgrades, 3)
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
            selection.Offer = CardDeck.Draw(CardDefinitions, selection.Player.Upgrades, 3, avoid);
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
            if (!selection.Player.Upgrades.Apply(card, selection.Player.GetComponent<PlayerHealth>())) return;
            selection.Picked = true;
            // ACK giữ animation trên máy chậm; thời hạn chặn client treo cả ván vô hạn.
            selection.FeedbackDeadline = NetworkTime.time + CardHoverVisual.ConsumeDuration + 3d;
            _cardGrants.Add(new CardGrant { Player = selection.Player.netId, Card = card.Id });
            TargetCardAccepted(connection, _selectionToken, card.Id);
        }

        [TargetRpc]
        private void TargetCardAccepted(NetworkConnectionToClient target, int token, CardId card)
        {
            CardSelectionController.Instance?.ReceiveAccepted(token, card);
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
            if (_clientCardRun != _cardRun)
            {
                _clientCardRun = _cardRun;
                _clientGrantCounts.Clear();
            }
            // Lịch sử chỉ chứa định danh, tự đồng bộ cho người vào muộn. Không gửi chỉ số.
            foreach (PlayerCharacter player in PlayerRegistry.All)
            {
                if (player == null) continue;
                int count = 0;
                foreach (CardGrant grant in _cardGrants)
                    if (grant.Player == player.netId) count++;
                if (_clientGrantCounts.TryGetValue(player.netId, out int previous) && previous == count) continue;
                player.Upgrades.ResetRun();
                foreach (CardGrant grant in _cardGrants)
                    if (grant.Player == player.netId) player.Upgrades.Apply(FindCard(grant.Card), null);
                _clientGrantCounts[player.netId] = count;
            }
        }
    }
}
