using System;
using System.Collections.Generic;
using LAC.Core;
using LAC.Enemies;
using UnityEngine;

namespace LAC.Director
{
    /// <summary>Hàng rào cho đặc tả; kiểm tra vị trí/suất sống vẫn thi hành ở mỗi lượt spawn.</summary>
    public static class SafetyConstraints
    {
        public static int MaxConcurrent => 40;
        public static float MinimumSpawnDistance => 3f;
        public static int MaximumScheduledSpawns => 4096;
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool TryValidateContext(DirectorContext context, out string reason)
        {
            if (!context.IsValid)
                return Invalid(string.IsNullOrEmpty(context.MissingReason) ? "missing_context" : context.MissingReason, out reason);
            if (!IsFinite(context.HealthMin) || context.HealthMin < 0f || context.HealthMin > 1f ||
                !IsFinite(context.DamageRatioMaxRaw) || context.DamageRatioMaxRaw < 0f ||
                context.Downs < 0 || context.ConnectedPlayers <= 0 || context.Participants < context.ConnectedPlayers ||
                context.LateJoiners < 0 || context.LateJoiners > context.Participants)
                return Invalid("invalid_context_values", out reason);
            if (context.LateJoiners > 0) return Invalid("incomplete_late_join", out reason);
            reason = string.Empty;
            return true;
        }

        public static bool TryValidateBaseline(WaveSpec spec, int waveIndex, WaveTable table, out string reason)
        {
            if (table == null || table.TotalWaves <= 0 || waveIndex < 1 || waveIndex > table.TotalWaves)
                return Invalid("invalid_wave_table", out reason);
            if (table.MaxConcurrent <= 0 || !IsFinite(table.SafeSpawnDistance) || table.SafeSpawnDistance < MinimumSpawnDistance)
                return Invalid("invalid_spawn_safety", out reason);
            if (!TryValidateSpec(spec, Math.Min(MaxConcurrent, table.MaxConcurrent), waveIndex == table.TotalWaves, out reason))
                return false;
            if (!AllowedMix(spec, IntroducedTypes(table, waveIndex), out reason)) return false;
            reason = string.Empty;
            return true;
        }

        public static bool TryValidateApplied(WaveSpec candidate, WaveSpec baseline, int waveIndex,
            WaveTable table, DirectorState state, out string reason)
        {
            if (!TryValidateBaseline(candidate, waveIndex, table, out reason)) return false;
            if (baseline == null || candidate.Duration != baseline.Duration)
                return Invalid("duration_changed", out reason);
            if (candidate.SpawnInterval < baseline.SpawnInterval || candidate.Burst > baseline.Burst)
                return Invalid("spawn_pressure_increased", out reason);
            if (candidate.Directions.Length != baseline.Directions.Length)
                return Invalid("direction_count_changed", out reason);
            if (PopulationBudget(candidate) > PopulationBudget(baseline) || candidate.TotalCount > baseline.TotalCount)
                return Invalid("population_budget_increased", out reason);
            if (state == DirectorState.Relief && !SameMix(candidate, baseline))
                return Invalid("relief_mix_changed", out reason);
            reason = string.Empty;
            return true;
        }

        public static long PopulationBudget(WaveSpec spec)
        {
            long budget = 0;
            if (spec == null) return 0;
            foreach (WaveSpec.Group group in spec.Mix)
                if (group.Enemy != null) budget += (long)group.Count * group.Enemy.PopulationCost;
            return budget;
        }

        public static WaveSpec Clone(WaveSpec spec)
        {
            if (spec == null) return null;
            return new WaveSpec((WaveSpec.Group[])spec.Mix.Clone(), (float[])spec.Directions.Clone(),
                spec.SpawnInterval, spec.Burst, spec.Duration);
        }

        public static List<EnemyData> IntroducedTypes(WaveTable table, int waveIndex)
        {
            var types = new List<EnemyData>();
            if (table == null) return types;
            for (int wave = 1; wave <= Math.Min(waveIndex, table.TotalWaves); wave++)
            {
                WaveSpec spec = table.BuildSpec(wave, null);
                foreach (WaveSpec.Group group in spec.Mix)
                    if (group.Count > 0 && group.Enemy != null && group.Enemy.Behaviour != EnemyBehaviour.Boss && !types.Contains(group.Enemy))
                        types.Add(group.Enemy);
            }
            return types;
        }

        private static bool TryValidateSpec(WaveSpec spec, int budget, bool finalWave, out string reason)
        {
            if (spec == null || spec.Mix.Length == 0 || spec.Directions.Length == 0 || spec.Directions.Length > 8)
                return Invalid("empty_or_invalid_spec", out reason);
            if (!IsFinite(spec.SpawnInterval) || !IsFinite(spec.Duration) || spec.Duration < 0f ||
                (finalWave ? spec.Duration != 0f || spec.SpawnInterval < 0f : spec.Duration <= 0f || spec.SpawnInterval <= 0f) ||
                spec.Burst < 1 || spec.Burst > budget)
                return Invalid("invalid_spawn_timing", out reason);
            // Giới hạn bộ nhớ cho bảng hỏng; scheduler timed kẹp chu kỳ tối thiểu 0.1s.
            if (!finalWave && (double)spec.Duration / Math.Max(.1d, spec.SpawnInterval) * spec.Burst > MaximumScheduledSpawns)
                return Invalid("schedule_over_budget", out reason);
            long count = 0, population = 0;
            foreach (WaveSpec.Group group in spec.Mix)
            {
                if (group.Enemy == null || group.Count <= 0 || !Enum.IsDefined(typeof(EnemyBehaviour), group.Enemy.Behaviour))
                    return Invalid("invalid_enemy_group", out reason);
                if (group.Enemy.Behaviour == EnemyBehaviour.Boss && (!finalWave || spec.Mix.Length != 1 || group.Count != 1))
                    return Invalid("boss_in_regular_wave", out reason);
                if (finalWave && group.Enemy.Behaviour != EnemyBehaviour.Boss)
                    return Invalid("final_wave_without_boss", out reason);
                if (group.Enemy.Behaviour == EnemyBehaviour.Splitter &&
                    (group.Enemy.SplitChild == null || !Enum.IsDefined(typeof(EnemyBehaviour), group.Enemy.SplitChild.Behaviour) ||
                     group.Enemy.SplitChild.Behaviour == EnemyBehaviour.Splitter || group.Enemy.SplitChild.Behaviour == EnemyBehaviour.Boss))
                    return Invalid("invalid_split_children", out reason);
                count += group.Count;
                population += (long)group.Count * group.Enemy.PopulationCost;
                if (population > budget) return Invalid("population_over_budget", out reason);
            }
            if (count <= 0 || count > int.MaxValue || count != spec.TotalCount)
                return Invalid("invalid_total_count", out reason);
            foreach (float direction in spec.Directions)
                if (!IsFinite(direction) || direction < 0f || direction >= 1f)
                    return Invalid("invalid_direction", out reason);
            reason = string.Empty;
            return true;
        }

        private static bool AllowedMix(WaveSpec spec, List<EnemyData> introduced, out string reason)
        {
            foreach (WaveSpec.Group group in spec.Mix)
                if (group.Enemy.Behaviour != EnemyBehaviour.Boss && !introduced.Contains(group.Enemy))
                    return Invalid("enemy_not_introduced", out reason);
            reason = string.Empty;
            return true;
        }

        private static bool SameMix(WaveSpec a, WaveSpec b)
        {
            if (a.Mix.Length != b.Mix.Length) return false;
            for (int i = 0; i < a.Mix.Length; i++)
                if (a.Mix[i].Enemy != b.Mix[i].Enemy || a.Mix[i].Count != b.Mix[i].Count) return false;
            return true;
        }

        private static bool Invalid(string value, out string reason) { reason = value; return false; }
    }
}
