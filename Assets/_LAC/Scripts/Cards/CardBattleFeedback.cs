using System.Collections.Generic;
using LAC.Core;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Cards
{
    /// <summary>Đọc thẻ/máu đã đồng bộ; mỗi máy tự phát biểu diễn, không gửi thêm RPC.</summary>
    public sealed class CardBattleFeedback : MonoBehaviour
    {
        private readonly List<CardBattleObserver> _players = new List<CardBattleObserver>(2);
        private CardBattleFeedbackData _data;
        private ObjectPool<CardBattleEffect> _pool;
        private RunManager _run;
        private CardDefinition[] _cards;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly int[] _presented = new int[4];
        public int Presented(CardBattleEffect.Kind kind) => _presented[(int)kind];
        public int LastHealAmount { get; private set; }
#endif
        public int ActiveEffects => _pool?.CountActive ?? 0;
        public int PooledEffects => _pool?.CountTotal ?? 0;

        // Lấy baseline sau replay lịch sử ở Update, tránh báo nhận lại thẻ khi vừa vào mạng.
        private void LateUpdate()
        {
            if (_run != RunManager.Instance) BindRun(RunManager.Instance);
            if (_run == null || !NetworkClient.isConnected) { ClearPlayers(); return; }
            if (_pool == null)
            {
                _data = Resources.Load<CardBattleFeedbackData>("CardBattleFeedback");
                if (_data == null || _data.EffectPrefab == null) { enabled = false; return; }
                _cards = Resources.LoadAll<CardDefinition>("Cards");
                _pool = new ObjectPool<CardBattleEffect>(_data.EffectPrefab, transform, _data.Capacity, _data.Capacity);
            }
            for (int i = _players.Count - 1; i >= 0; i--)
                if (!_players[i].Available) { _players[i].Dispose(); _players.RemoveAt(i); }
            for (int n = 0; n < PlayerRegistry.Count; n++)
            {
                var player = PlayerRegistry.All[n];
                if (player == null || player.Data == null || player.Upgrades == null) continue;
                bool bound = false;
                for (int i = 0; i < _players.Count; i++) if (_players[i].Player == player) { bound = true; break; }
                if (!bound) _players.Add(new CardBattleObserver(this, player, _cards, _data));
            }
            for (int i = 0; i < _players.Count; i++) _players[i].Tick(_run);
        }

        internal void Emit(CardBattleEffect.Kind kind, PlayerCharacter player, Vector2 direction, int amount = 0)
        {
            // Là biểu diễn: khi đầy, bỏ nét trang trí thay vì cấp phát hay ảnh hưởng gameplay.
            if (_pool == null || _pool.CountIdle == 0 || player == null || !player.isActiveAndEnabled || !player.IsAlive) return;
            Vector3 position = player.transform.position + Vector3.down * .2f;
            _pool.Get(position, Quaternion.identity).Play(_pool, _data, kind, player, direction, amount);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _presented[(int)kind]++;
            if (kind == CardBattleEffect.Kind.Heal) LastHealAmount = amount;
#endif
        }

        private void BindRun(RunManager run)
        {
            if (_run != null) { _run.RunStarted -= ResetRun; _run.RunEnded -= OnRunEnded; }
            ClearPlayers();
            _run = run;
            if (_run != null) { _run.RunStarted += ResetRun; _run.RunEnded += OnRunEnded; }
        }
        private void ResetRun()
        {
            _pool?.ReleaseAll();
            for (int i = 0; i < _players.Count; i++) _players[i].Reset();
        }
        private void OnRunEnded(bool _) => ResetRun();
        private void ClearPlayers()
        {
            foreach (var player in _players) player.Dispose();
            _players.Clear();
            _pool?.ReleaseAll();
        }
        private void OnDisable() => BindRun(null);
        private void OnDestroy() => _pool?.Clear();
    }
}
