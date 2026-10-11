using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LAC.Core;
using LAC.Enemies;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Director
{
    /// <summary>Một bộ CSV cho mỗi ván, chỉ được tạo bởi host.</summary>
    public sealed class TelemetrySession
    {
        public const int SchemaVersion = 2;

        private sealed class Counters
        {
            internal int Hits, Damage, Downs, Revives, Dashes;
        }

        private sealed class Participant
        {
            internal PlayerCharacter Player;
            internal PlayerHealth Health;
            internal int Id, Hp, MaxHp, StartHp, StartMaxHp, WaveHp, WaveMaxHp;
            internal string Character;
            internal bool Connected = true, InWave;
            internal double JoinedAt, LeftAt, WaveJoinedAt, WaveLeftAt;
            internal readonly Counters Run = new Counters();
            internal Counters Wave = new Counters();
        }

        private readonly RunManager _run;
        private readonly List<Participant> _players = new List<Participant>(2);
        private TelemetryCsv _events, _waves, _runs, _directors;
        private readonly string _id = Guid.NewGuid().ToString("N");
        private readonly string _startedUtc = DateTime.UtcNow.ToString("O");
        private readonly double _startedAt = Time.unscaledTimeAsDouble;
        private readonly int _seed;
        private readonly DirectorMode _mode;
        private readonly SortedSet<string> _sources = new SortedSet<string>(StringComparer.Ordinal);
        private DirectorDecision _waveDecision;
        private int _decisionWave, _decisions, _adaptiveDecisions, _fallbackDecisions;
        private int _wave, _sequence, _kills, _waveKills, _waveSouls;
        private uint _waveDrums;
        private double _waveStartedAt, _nextFlush;
        private bool _finished, _failed;
        private int _playersAtStart, _peakPlayers, _clearedWaves;
        public string DirectoryPath { get; private set; }
        public bool IsRecording => !_finished && !_failed;
        private double Elapsed => Time.unscaledTimeAsDouble - _startedAt;

        public static TelemetrySession Begin(RunManager run)
        {
            if (!NetworkServer.active || run == null) return null;
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--lac-telemetry-off") >= 0) return null;
            string root = Path.Combine(Application.persistentDataPath, "Telemetry");
            int at = Array.IndexOf(args, "--lac-telemetry-dir");
            if (at >= 0 && at + 1 < args.Length) root = args[at + 1];
            return new TelemetrySession(run, root);
        }

        private TelemetrySession(RunManager run, string root)
        {
            _run = run;
            _seed = run.Seed;
            _mode = run.DirectorMode;
            Safe(() =>
            {
                DirectoryPath = Path.Combine(root, _id);
                Directory.CreateDirectory(DirectoryPath);
                _events = new TelemetryCsv(Path.Combine(DirectoryPath, "events.csv"),
                    "schema", "run_id", "seq", "elapsed_s", "wave", "event", "player_id", "character", "health", "max_health", "value", "detail");
                object[] summaryHeader = { "schema", "run_id", "started_utc", "build", "environment", "source", "seed", "wave", "result",
                    "duration_s", "run_players_start", "run_players_peak", "players_end", "waves_cleared", "team_kills", "team_souls", "team_drum_uses",
                    "player_id", "character", "connected", "exposure_s", "health_start", "max_health_start", "health_end", "max_health_end",
                    "hits_taken", "health_lost", "downs", "revives", "dashes" };
                object[] decisionHeader = { "mode", "director_state", "director_decisions", "adaptive_decisions", "fallback_decisions",
                    "decision_reason", "fallback_reason", "context_valid", "health_min", "damage_ratio_max_raw", "damage_ratio_max",
                    "context_downs", "connected_players", "participants", "late_joiners", "missing_context",
                    "baseline_interval", "baseline_burst", "baseline_duration", "baseline_cycle_count", "baseline_mix", "baseline_directions",
                    "applied_interval", "applied_burst", "applied_duration", "applied_cycle_count", "applied_mix", "applied_directions" };
                var summaryColumns = new List<object>(summaryHeader);
                summaryColumns.AddRange(decisionHeader);
                _waves = new TelemetryCsv(Path.Combine(DirectoryPath, "waves.csv"), summaryColumns.ToArray());
                _runs = new TelemetryCsv(Path.Combine(DirectoryPath, "runs.csv"), summaryColumns.ToArray());
                var directorColumns = new List<object> { "schema", "run_id", "elapsed_s", "wave", "source", "table" };
                directorColumns.AddRange(decisionHeader);
                _directors = new TelemetryCsv(Path.Combine(DirectoryPath, "directors.csv"), directorColumns.ToArray());
            });
            Event("run_started", value: _seed, detail: _startedUtc + ";build=" + Application.version
                + ";mode=" + _mode + ";source=pending_wave_decision");
            foreach (var player in PlayerRegistry.All) Join(player);
            _playersAtStart = ConnectedCount();
            if (!_failed) GameEvents.EnemyDied += OnEnemyDied;
            Flush();
            if (!_failed) Debug.Log("[Telemetry] CSV: " + DirectoryPath);
        }

        public void Join(PlayerCharacter player)
        {
            if (!IsRecording || player == null || Find(player) != null) return;
            var p = new Participant { Player = player, Health = player.GetComponent<PlayerHealth>(),
                Id = _players.Count + 1, Character = player.CharacterId.ToString(), JoinedAt = Elapsed };
            Snapshot(p);
            p.StartHp = p.Hp; p.StartMaxHp = p.MaxHp;
            _players.Add(p);
            _peakPlayers = Math.Max(_peakPlayers, ConnectedCount());
            if (_wave > 0) BeginParticipantWave(p);
            Event("player_joined", p);
        }

        public void Leave(PlayerCharacter player)
        {
            var p = Find(player);
            if (!IsRecording || p == null || !p.Connected) return;
            Snapshot(p);
            p.Connected = false;
            p.LeftAt = Elapsed;
            if (p.InWave) p.WaveLeftAt = Elapsed;
            Event("player_left", p);
        }

        public void StartWave(int wave)
        {
            if (!IsRecording || wave <= 0 || _wave == wave) return;
            if (_wave > 0) EndWave("interrupted");
            _wave = wave; _waveStartedAt = Elapsed; _waveKills = 0;
            _waveDecision = null;
            _waveSouls = _run.SoulsCollected; _waveDrums = _run.DrumActivations;
            foreach (var p in _players) if (p.Connected) BeginParticipantWave(p);
            Event("wave_started");
            Flush();
        }

        public void RecordWaveSpec(WaveSpec spec, string tableName)
        {
            if (!IsRecording || _wave == 0 || spec == null) return;
            var detail = new StringBuilder(tableName);
            detail.Append(";source=").Append(_waveDecision?.Source ?? "unrecorded");
            detail.Append(";mode=").Append(_mode);
            detail.Append(";mix=");
            foreach (var group in spec.Mix)
                if (group.Enemy != null) detail.Append(group.Enemy.name).Append(':').Append(group.Count).Append('|');
            detail.Append(";directions=");
            foreach (float direction in spec.Directions)
                detail.Append(direction.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            detail.Append(";interval=").Append(spec.SpawnInterval.ToString("R", CultureInfo.InvariantCulture));
            detail.Append(";burst=").Append(spec.Burst);
            detail.Append(";duration=").Append(spec.Duration.ToString("R", CultureInfo.InvariantCulture));
            detail.Append(";repeat=").Append(spec.Duration > 0f ? "1" : "0");
            Event("wave_spec", value: spec.TotalCount, detail: detail.ToString());
        }

        /// <summary>Ghi quyết định thật ở host, đúng một lần sau khi mở wave tương ứng.</summary>
        public void RecordDirectorDecision(DirectorDecision decision, int wave, string tableName)
        {
            if (!NetworkServer.active || !IsRecording || decision == null || wave != _wave
                || decision.WaveIndex != wave || _decisionWave == wave) return;
            _decisionWave = wave;
            _waveDecision = decision;
            _decisions++;
            if (decision.Source == "adaptive_rules" && !decision.IsFallback) _adaptiveDecisions++;
            if (decision.IsFallback) _fallbackDecisions++;
            _sources.Add(decision.Source ?? "unrecorded");
            var cells = new List<object> { SchemaVersion, _id, Elapsed, wave, decision.Source, tableName };
            AddDecisionCells(cells, decision);
            Safe(() => _directors.Row(cells.ToArray()));
            Event("director_decision", value: wave, detail: "mode=" + _mode + ";state=" + decision.State
                + ";source=" + decision.Source + ";reason=" + decision.Reason + ";fallback=" + decision.FallbackReason);
            Flush();
        }

        private void BeginParticipantWave(Participant p)
        {
            Snapshot(p);
            p.InWave = true; p.Wave = new Counters();
            p.WaveHp = p.Hp; p.WaveMaxHp = p.MaxHp;
            p.WaveJoinedAt = Elapsed; p.WaveLeftAt = 0;
        }

        public void EndWave(string result)
        {
            if (!IsRecording || _wave == 0) return;
            if (result == "cleared" || result == "survived") _clearedWaves++;
            Event("wave_ended", detail: result);
            foreach (var p in _players)
            {
                if (!p.InWave) continue;
                Summary(_waves, p, p.Wave, result, Elapsed - _waveStartedAt, _wave,
                    _waveKills, _run.SoulsCollected - _waveSouls, _run.DrumActivations - _waveDrums,
                    (p.Connected ? Elapsed : p.WaveLeftAt) - p.WaveJoinedAt, p.WaveHp, p.WaveMaxHp);
                p.InWave = false;
            }
            _wave = 0;
            _waveDecision = null;
            Flush();
        }

        // Ghi trước ReportPlayerDown/EndRun: sự kiện PlayerDamaged hiện phát sau khi ván đã kết thúc.
        public void Damage(PlayerCharacter player, int healthLost)
        {
            var p = Find(player);
            if (!IsRecording || _wave == 0 || p == null || !p.Connected || healthLost <= 0) return;
            Snapshot(p);
            p.Run.Hits++; p.Wave.Hits++;
            p.Run.Damage += healthLost; p.Wave.Damage += healthLost;
            Event("player_damaged", p, healthLost);
            if (p.Hp != 0) return;
            p.Run.Downs++; p.Wave.Downs++;
            Event("player_downed", p);
        }

        public void Revive(PlayerCharacter player)
        {
            var p = Find(player);
            if (!IsRecording || _wave == 0 || p == null || !p.Connected) return;
            Snapshot(p);
            p.Run.Revives++; p.Wave.Revives++;
            Event("player_revived", p, p.Hp);
        }

        public void Dash(PlayerCharacter player)
        {
            var p = Find(player);
            if (!IsRecording || _wave == 0 || p == null || !p.Connected) return;
            p.Run.Dashes++; p.Wave.Dashes++;
            Event("player_dashed", p);
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (!NetworkServer.active || !IsRecording || _wave == 0) return;
            _kills++; _waveKills++;
            Event("enemy_killed", value: enemy.Id, detail: enemy.Data != null ? enemy.Data.name : "");
        }

        public void Finish(string result)
        {
            if (_finished) return;
            EndWave(result == "victory" ? "cleared" : result);
            Event("run_ended", detail: result);
            foreach (var p in _players)
                Summary(_runs, p, p.Run, result, Elapsed, 0, _kills, _run.SoulsCollected,
                    _run.DrumActivations, (p.Connected ? Elapsed : p.LeftAt) - p.JoinedAt, p.StartHp, p.StartMaxHp);
            _finished = true;
            GameEvents.EnemyDied -= OnEnemyDied;
            Close();
        }

        public void Tick()
        {
            if (!IsRecording || Elapsed < _nextFlush) return;
            foreach (var p in _players) Snapshot(p);
            Flush();
        }

        private void Flush()
        {
            _nextFlush = Elapsed + 5;
            Safe(() => { _events?.Flush(); _waves?.Flush(); _runs?.Flush(); _directors?.Flush(); });
        }

        private void Event(string kind, Participant p = null, int value = 0, string detail = "")
        {
            Safe(() => _events.Row(SchemaVersion, _id, ++_sequence, Elapsed, _wave, kind,
                p?.Id ?? 0, p?.Character ?? "", p?.Hp ?? 0, p?.MaxHp ?? 0, value, detail));
        }

        private void Summary(TelemetryCsv writer, Participant p, Counters c, string result, double duration,
            int wave, int kills, int souls, uint drums, double exposure, int startHp, int startMaxHp)
        {
            Snapshot(p);
            string source = wave > 0 ? _waveDecision?.Source ?? "unrecorded"
                : _sources.Count > 0 ? string.Join("|", _sources) : "unrecorded";
            var cells = new List<object> { SchemaVersion, _id, _startedUtc, Application.version,
                Application.isEditor ? "editor" : Debug.isDebugBuild ? "development" : "release", source, _seed,
                wave, result, duration, _playersAtStart, _peakPlayers, ConnectedCount(), _clearedWaves, kills, souls, drums,
                p.Id, p.Character, p.Connected ? 1 : 0, exposure, startHp, startMaxHp, p.Hp, p.MaxHp,
                c.Hits, c.Damage, c.Downs, c.Revives, c.Dashes };
            // Một ván không có một context chung; chỉ dòng wave chứa snapshot quyết định.
            AddDecisionCells(cells, wave > 0 ? _waveDecision : null);
            Safe(() => writer.Row(cells.ToArray()));
        }

        private void AddDecisionCells(List<object> cells, DirectorDecision decision)
        {
            DirectorContext context = decision != null ? decision.Context : default;
            bool present = decision != null;
            bool valid = present && context.IsValid;
            cells.Add(_mode);
            cells.Add(decision != null ? decision.State.ToString() : "");
            cells.Add(_decisions); cells.Add(_adaptiveDecisions); cells.Add(_fallbackDecisions);
            cells.Add(decision?.Reason ?? ""); cells.Add(decision?.FallbackReason ?? "");
            cells.Add(decision != null ? (valid ? "1" : "0") : "");
            cells.Add(present ? FiniteCell(context.HealthMin) : "");
            cells.Add(present ? FiniteCell(context.DamageRatioMaxRaw) : "");
            cells.Add(present ? FiniteCell(context.DamageRatioMax) : "");
            cells.Add(present ? (object)context.Downs : "");
            cells.Add(present ? (object)context.ConnectedPlayers : "");
            cells.Add(present ? (object)context.Participants : "");
            cells.Add(present ? (object)context.LateJoiners : "");
            cells.Add(present ? context.MissingReason ?? "" : "");
            AddSpecCells(cells, decision?.Baseline);
            AddSpecCells(cells, decision?.Applied);
        }

        private static object FiniteCell(float value) => float.IsNaN(value) || float.IsInfinity(value) ? "" : (object)value;

        private static void AddSpecCells(List<object> cells, WaveSpec spec)
        {
            cells.Add(spec != null ? (object)spec.SpawnInterval : "");
            cells.Add(spec != null ? (object)spec.Burst : "");
            cells.Add(spec != null ? (object)spec.Duration : "");
            cells.Add(spec != null ? (object)spec.TotalCount : "");
            var mix = new StringBuilder();
            var directions = new StringBuilder();
            if (spec != null)
            {
                foreach (var group in spec.Mix)
                    if (group.Enemy != null) mix.Append(group.Enemy.name).Append(':').Append(group.Count).Append('|');
                foreach (float direction in spec.Directions)
                    directions.Append(direction.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            }
            cells.Add(mix.ToString()); cells.Add(directions.ToString());
        }

        private static void Snapshot(Participant p)
        {
            if (!p.Connected || p.Health == null) return;
            p.Hp = p.Health.Health; p.MaxHp = p.Health.MaxHealth;
        }

        private Participant Find(PlayerCharacter player)
        {
            foreach (var p in _players) if (ReferenceEquals(p.Player, player)) return p;
            return null;
        }

        private int ConnectedCount()
        {
            int count = 0;
            foreach (var p in _players) if (p.Connected) count++;
            return count;
        }

        private void Safe(Action write)
        {
            if (_failed || _finished) return;
            try { write(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is System.Security.SecurityException || e is ArgumentException || e is NotSupportedException)
            {
                _failed = true;
                GameEvents.EnemyDied -= OnEnemyDied;
                Debug.LogWarning("[Telemetry] Không ghi được CSV; ván vẫn tiếp tục. " + e.Message);
                Close();
            }
        }

        private void Close()
        {
            foreach (var writer in new[] { _events, _waves, _runs, _directors })
            {
                try { writer?.Dispose(); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is System.Security.SecurityException)
                {
                    if (!_failed) Debug.LogWarning("[Telemetry] Không đóng được CSV: " + e.Message);
                    _failed = true;
                }
            }
            _events = _waves = _runs = _directors = null;
        }
    }
}
