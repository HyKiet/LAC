using System;
using System.Collections.Generic;
using LAC.Player;

namespace LAC.Director
{
    /// <summary>Thu sát thương thực trên host, giữ mẫu cuối wave trước hồi máu và thẻ.</summary>
    public sealed class ContextAggregator
    {
        private sealed class Participant
        {
            internal PlayerCharacter Player;
            internal PlayerHealth Health;
            internal int StartMaxHealth, Downs;
            internal int EndHealth, EndMaxHealth;
            internal double Damage;
            internal bool Connected, CompleteHistory;
            internal bool Frozen;
        }

        private readonly List<Participant> _participants = new List<Participant>(2);
        private bool _recording;
        private DirectorContext _lastContext = DirectorContext.Missing("missing_wave");

        public bool IsRecording => _recording;

        public void Reset()
        {
            _recording = false;
            _participants.Clear();
            _lastContext = DirectorContext.Missing("missing_wave");
        }

        public void BeginWave()
        {
            Reset();
            _recording = true;
            foreach (PlayerCharacter player in PlayerRegistry.All) Add(player, true);
        }

        public void Join(PlayerCharacter player)
        {
            if (_recording || _participants.Count > 0) Add(player, false);
        }

        public void Leave(PlayerCharacter player)
        {
            Participant participant = Find(player);
            if (participant != null) participant.Connected = false;
        }

        public void RefreshParticipants()
        {
            if (!_recording && _participants.Count == 0) return;
            foreach (PlayerCharacter player in PlayerRegistry.All) Add(player, false);
            foreach (Participant participant in _participants)
            {
                if (!participant.Connected) continue;
                bool present = false;
                foreach (PlayerCharacter player in PlayerRegistry.All)
                    if (ReferenceEquals(player, participant.Player)) { present = true; break; }
                if (!present || participant.Player == null) participant.Connected = false;
            }
        }

        public void RecordDamage(PlayerCharacter player, int actualHealthLost)
        {
            if (!_recording || player == null || actualHealthLost <= 0) return;
            Participant participant = Find(player);
            if (participant == null) { Add(player, false); participant = Find(player); }
            if (participant == null || !participant.Connected) return;
            participant.Damage += actualHealthLost;
            if (participant.Health != null && !participant.Health.IsAlive && participant.Downs < int.MaxValue)
                participant.Downs++;
        }

        public DirectorContext Capture()
        {
            if (!_recording && _participants.Count == 0) return _lastContext;
            RefreshParticipants();
            foreach (Participant participant in _participants)
            {
                if (participant.Frozen) continue;
                participant.EndHealth = participant.Health != null ? participant.Health.Health : -1;
                participant.EndMaxHealth = participant.Health != null ? participant.Health.MaxHealth : 0;
                participant.Frozen = true;
            }
            _recording = false;
            float healthMin = 1f, damageMax = 0f;
            int connected = 0, lateJoiners = 0;
            long downs = 0;
            string missing = string.Empty;
            foreach (Participant participant in _participants)
            {
                downs += participant.Downs;
                if (!participant.CompleteHistory) lateJoiners++;
                if (participant.StartMaxHealth <= 0) missing = "invalid_player_health";
                else damageMax = Math.Max(damageMax, (float)(participant.Damage / participant.StartMaxHealth));
                if (!participant.Connected || participant.Player == null) continue;
                connected++;
                if (participant.EndMaxHealth <= 0 || participant.EndHealth < 0 || participant.EndHealth > participant.EndMaxHealth)
                    missing = "invalid_player_health";
                else healthMin = Math.Min(healthMin, (float)participant.EndHealth / participant.EndMaxHealth);
            }
            if (connected == 0) missing = "no_participants";
            else if (lateJoiners > 0 && string.IsNullOrEmpty(missing)) missing = "incomplete_late_join";
            _lastContext = new DirectorContext(string.IsNullOrEmpty(missing), connected > 0 ? healthMin : 0f,
                damageMax, (int)Math.Min(downs, int.MaxValue), connected, _participants.Count, lateJoiners, missing);
            return _lastContext;
        }

        private void Add(PlayerCharacter player, bool completeHistory)
        {
            if (player == null || Find(player) != null) return;
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            _participants.Add(new Participant { Player = player, Health = health,
                StartMaxHealth = health != null ? health.MaxHealth : 0,
                Connected = true, CompleteHistory = completeHistory });
        }

        private Participant Find(PlayerCharacter player)
        {
            if (ReferenceEquals(player, null)) return null;
            foreach (Participant participant in _participants)
                if (ReferenceEquals(participant.Player, player)) return participant;
            return null;
        }
    }
}
