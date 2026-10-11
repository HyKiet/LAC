using System;
using System.Collections.Generic;
using LAC.Core;
using LAC.Enemies;
using UnityEngine;

namespace LAC.Director
{
    /// <summary>FSM dựa trên luật. Chỉ host gọi, đúng một lần trước mỗi wave.</summary>
    public sealed class AIDirector
    {
        private DirectorState _state = DirectorState.Balanced;
        public DirectorState State => _state;
        public void Reset() => _state = DirectorState.Balanced;

        public DirectorDecision Decide(DirectorMode mode, int waveIndex, WaveSpec baseline,
            DirectorContext context, DirectorRulesData rules, WaveTable table, RandomStream stream)
        {
            if (!SafetyConstraints.TryValidateBaseline(baseline, waveIndex, table, out string invalid))
            {
                Reset();
                return Decision(mode, waveIndex, context, "invalid_baseline", "Cấu hình đợt không hợp lệ; dừng sinh quái.",
                    invalid, baseline, null);
            }
            WaveSpec original = SafetyConstraints.Clone(baseline);
            if (mode == DirectorMode.Fixed)
            {
                Reset();
                return Decision(mode, waveIndex, context, "fixed_table", "Bảng cố định: giữ nhịp sinh gốc.", null, original, SafetyConstraints.Clone(original));
            }
            if (mode != DirectorMode.Adaptive)
                return Fallback(mode, waveIndex, context, original, "invalid_mode");
            if (waveIndex == 1 || waveIndex == table.TotalWaves)
            {
                Reset();
                return Decision(mode, waveIndex, context, "fixed_table", waveIndex == 1
                    ? "Đợt mở đầu: giữ cấu hình gốc." : "Đợt boss: giữ cấu hình gốc.", null, original, SafetyConstraints.Clone(original));
            }
            if (rules == null) return Fallback(mode, waveIndex, context, original, "missing_rules");
            if (!rules.TryValidate(out invalid)) return Fallback(mode, waveIndex, context, original, invalid);
            if (waveIndex < rules.FirstAdaptiveWave || waveIndex > rules.LastAdaptiveWave)
            {
                Reset();
                return Decision(mode, waveIndex, context, "fixed_table", "Đợt ngoài phạm vi điều tiết: giữ cấu hình gốc.", null, original, SafetyConstraints.Clone(original));
            }
            if (!SafetyConstraints.TryValidateContext(context, out invalid))
            {
                var invalidContext = new DirectorContext(false, context.HealthMin, context.DamageRatioMaxRaw,
                    context.Downs, context.ConnectedPlayers, context.Participants, context.LateJoiners, invalid);
                return Fallback(mode, waveIndex, invalidContext, original, invalid);
            }

            bool relief = context.HealthMin <= rules.ReliefHealthThreshold || context.DamageRatioMax >= rules.ReliefDamageThreshold || context.Downs > 0;
            bool heldRelief = _state == DirectorState.Relief &&
                (context.HealthMin < rules.ReliefExitHealth || context.DamageRatioMax > rules.ReliefExitDamage || context.Downs > 0);
            WaveSpec applied;
            string reason;
            if (relief || heldRelief)
            {
                _state = DirectorState.Relief;
                applied = new WaveSpec((WaveSpec.Group[])original.Mix.Clone(), (float[])original.Directions.Clone(),
                    original.SpawnInterval * rules.ReliefIntervalMultiplier,
                    Math.Max(1, Mathf.FloorToInt(original.Burst * rules.ReliefBurstMultiplier)), original.Duration);
                reason = heldRelief && !relief ? "Tiếp tục giãn nhịp sinh để đội hồi phục." : "Giãn nhịp sinh để hỗ trợ người chơi yếu nhất.";
            }
            else if (context.HealthMin >= rules.VarietyHealthThreshold && context.DamageRatioMax <= rules.VarietyDamageThreshold && context.Downs == 0)
            {
                if (stream == null) return Fallback(mode, waveIndex, context, original, "missing_director_random");
                _state = DirectorState.Variety;
                applied = BuildVariety(original, rules, table, waveIndex, stream);
                reason = "Đổi hướng và thành phần quái trong ngân sách gốc.";
            }
            else
            {
                _state = DirectorState.Balanced;
                applied = SafetyConstraints.Clone(original);
                reason = "Giữ nhịp sinh gốc vì đội đang ổn định.";
            }
            if (!SafetyConstraints.TryValidateApplied(applied, original, waveIndex, table, _state, out invalid))
                return Fallback(mode, waveIndex, context, original, invalid);
            return Decision(mode, waveIndex, context, "adaptive_rules", reason, null, original, applied);
        }

        private static WaveSpec BuildVariety(WaveSpec baseline, DirectorRulesData rules, WaveTable table,
            int waveIndex, RandomStream stream)
        {
            float rotation = stream.Range(rules.VarietyMinRotation, rules.VarietyMaxRotation);
            if (stream.Chance(0.5f)) rotation = -rotation;
            float[] directions = (float[])baseline.Directions.Clone();
            for (int i = 0; i < directions.Length; i++) directions[i] = Mathf.Repeat(directions[i] + rotation, 1f);
            var mix = new List<WaveSpec.Group>(baseline.Mix);
            List<EnemyData> introduced = SafetyConstraints.IntroducedTypes(table, waveIndex);
            int from = stream.Range(0, mix.Count);
            WaveSpec.Group source = mix[from];
            var alternatives = new List<EnemyData>();
            foreach (EnemyData enemy in introduced)
                if (enemy != source.Enemy && enemy.PopulationCost <= source.Enemy.PopulationCost) alternatives.Add(enemy);
            if (alternatives.Count > 0)
            {
                EnemyData replacement = alternatives[stream.Range(0, alternatives.Count)];
                if (source.Count == 1) mix.RemoveAt(from);
                else mix[from] = new WaveSpec.Group(source.Enemy, source.Count - 1);
                int target = mix.FindIndex(group => group.Enemy == replacement);
                if (target >= 0) mix[target] = new WaveSpec.Group(replacement, mix[target].Count + 1);
                else mix.Add(new WaveSpec.Group(replacement, 1));
            }
            return new WaveSpec(mix.ToArray(), directions, baseline.SpawnInterval, baseline.Burst, baseline.Duration);
        }

        private DirectorDecision Fallback(DirectorMode mode, int waveIndex, DirectorContext context, WaveSpec baseline, string reason)
        {
            Reset();
            return Decision(mode, waveIndex, context, "fixed_fallback", "Thiếu dữ liệu an toàn: dùng bảng cố định.",
                reason, baseline, SafetyConstraints.Clone(baseline));
        }

        private DirectorDecision Decision(DirectorMode mode, int waveIndex, DirectorContext context, string source,
            string reason, string fallbackReason, WaveSpec baseline, WaveSpec applied) =>
            new DirectorDecision(mode, _state, waveIndex, context, source, reason, fallbackReason, baseline, applied);
    }
}
