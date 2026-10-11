using LAC.Director;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    public sealed partial class RunManager
    {
        [Header("Đạo diễn AI")]
        [SerializeField] private DirectorRulesData _directorRules;
        [SerializeField] private DirectorMode _configuredDirectorMode = DirectorMode.Adaptive;

        [SyncVar] private DirectorMode _directorMode;
        [SyncVar] private DirectorState _directorState;
        [SyncVar] private string _directorReason = "";
        [SyncVar] private string _directorSource = "fixed_table";
        [SyncVar] private string _directorFallbackReason = "";
        [SyncVar] private float _directorHealthMin;
        [SyncVar] private float _directorDamageRatioMax;
        [SyncVar] private int _directorDowns;
        [SyncVar] private bool _directorContextValid;

        private readonly AIDirector _director = new AIDirector();
        private readonly ContextAggregator _directorContext = new ContextAggregator();
        private DirectorContext _previousDirectorContext;
        private int _directorWave;

        public DirectorMode ConfiguredDirectorMode => _configuredDirectorMode;
        public DirectorMode DirectorMode => _directorMode;
        public DirectorState DirectorState => _directorState;
        public string DirectorReason => _directorReason;
        public string DirectorSource => _directorSource;
        public string DirectorFallbackReason => _directorFallbackReason;
        public float DirectorHealthMin => _directorHealthMin;
        public float DirectorDamageRatioMax => _directorDamageRatioMax;
        public int DirectorDowns => _directorDowns;
        public bool DirectorContextValid => _directorContextValid;
        public DirectorDecision CurrentDirectorDecision { get; private set; }

        public bool TrySetDirectorMode(DirectorMode mode)
        {
            if (NetworkServer.active || NetworkClient.active ||
                (mode != DirectorMode.Fixed && mode != DirectorMode.Adaptive)) return false;
            _configuredDirectorMode = mode;
            return true;
        }

        private void ResetDirectorForConnection()
        {
            _director.Reset();
            _directorContext.Reset();
            _previousDirectorContext = default;
            CurrentDirectorDecision = null;
            _directorWave = 0;
            _directorState = DirectorState.Balanced;
            _directorReason = _directorFallbackReason = "";
            _directorSource = "fixed_table";
            _directorHealthMin = _directorDamageRatioMax = 0f;
            _directorDowns = 0;
            _directorContextValid = false;
        }

        private void BeginDirectorRun()
        {
            ResetDirectorForConnection();
            // Chế độ đối chứng được chốt trước ván, kể cả các lượt Restart trong phòng.
            _directorMode = _configuredDirectorMode;
        }

        private void PrepareDirectorWave(int waveIndex)
        {
            // Capture đã đóng băng HP/MaxHP cuối wave; lần này chỉ cập nhật người vào/rời.
            if (waveIndex > 1) _previousDirectorContext = _directorContext.Capture();
            var baselineStream = new RandomStream(_seed, "wave" + waveIndex);
            WaveSpec baseline = _waves != null ? _waves.BuildSpec(waveIndex, baselineStream) : null;
            CurrentDirectorDecision = _director.Decide(_directorMode, waveIndex, baseline,
                _previousDirectorContext, _directorRules, _waves,
                new RandomStream(_seed, "director-wave" + waveIndex));
            _directorWave = waveIndex;
            _directorState = CurrentDirectorDecision.State;
            _directorReason = CurrentDirectorDecision.Reason;
            _directorSource = CurrentDirectorDecision.Source;
            _directorFallbackReason = CurrentDirectorDecision.FallbackReason ?? "";
            _directorHealthMin = SafetyConstraints.IsFinite(CurrentDirectorDecision.Context.HealthMin)
                ? CurrentDirectorDecision.Context.HealthMin : 0f;
            _directorDamageRatioMax = SafetyConstraints.IsFinite(CurrentDirectorDecision.Context.DamageRatioMaxRaw)
                ? CurrentDirectorDecision.Context.DamageRatioMaxRaw : 0f;
            _directorDowns = CurrentDirectorDecision.Context.Downs;
            _directorContextValid = CurrentDirectorDecision.Context.IsValid;
        }

        // Client không đọc đặc tả thích ứng; nó chỉ nhận sự kiện sinh và kết quả HUD.
        public WaveSpec GetHostWaveSpec(int waveIndex, WaveSpec baseline) =>
            NetworkServer.active && _directorWave == waveIndex && CurrentDirectorDecision != null
                ? CurrentDirectorDecision.Applied ?? baseline : baseline;

        [Server]
        public void RecordDirectorDamage(PlayerCharacter player, int healthLost)
        {
            if (_state == RunState.WaveActive) _directorContext.RecordDamage(player, healthLost);
        }

        [Server]
        public void RecordDirectorLeave(PlayerCharacter player) => _directorContext.Leave(player);

        private void CaptureDirectorContext()
        {
            if (_state == RunState.WaveActive) _previousDirectorContext = _directorContext.Capture();
        }
    }
}
