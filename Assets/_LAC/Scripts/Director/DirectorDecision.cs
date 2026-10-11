using LAC.Core;

namespace LAC.Director
{
    public enum DirectorMode { Fixed, Adaptive }
    public enum DirectorState { Balanced, Relief, Variety }

    /// <summary>Một quyết định bất biến; host thi hành Applied, HUD/CSV đọc phần giải thích.</summary>
    public sealed class DirectorDecision
    {
        public DirectorMode Mode { get; }
        public DirectorState State { get; }
        public int WaveIndex { get; }
        public DirectorContext Context { get; }
        public string Source { get; }
        public string Reason { get; }
        public string FallbackReason { get; }
        public WaveSpec Baseline { get; }
        public WaveSpec Applied { get; }
        public bool IsFallback => !string.IsNullOrEmpty(FallbackReason);
        public bool IsApplicable => Applied != null;

        public DirectorDecision(DirectorMode mode, DirectorState state, int waveIndex,
            DirectorContext context, string source, string reason, string fallbackReason,
            WaveSpec baseline, WaveSpec applied)
        {
            Mode = mode;
            State = state;
            WaveIndex = waveIndex;
            Context = context;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            FallbackReason = fallbackReason ?? string.Empty;
            Baseline = baseline;
            Applied = applied;
        }
    }
}
