using System.Collections.Generic;
using LAC.Core;
using LAC.Combat;

namespace LAC.Cards
{
    public static class CardDeck
    {
        public static List<CardDefinition> Draw(IReadOnlyList<CardDefinition> definitions,
            PlayerUpgradeState state, int count, ISet<CardId> avoid = null, WeaponShape? shape = null)
        {
            var eligible = new List<CardDefinition>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                CardDefinition card = definitions[i];
                if (card != null && state.GetStacks(card.Id) < card.MaxStacks
                    && (!shape.HasValue || card.Supports(shape.Value)))
                    eligible.Add(card);
            }

            var result = new List<CardDefinition>(count);
            DrawPass(eligible, result, count, avoid, true);
            DrawPass(eligible, result, count, avoid, false);
            return result;
        }

        private static void DrawPass(List<CardDefinition> pool, List<CardDefinition> result,
            int count, ISet<CardId> avoid, bool excludeAvoided)
        {
            while (result.Count < count)
            {
                float total = 0f;
                for (int i = 0; i < pool.Count; i++)
                {
                    CardDefinition candidate = pool[i];
                    if (result.Contains(candidate)) continue;
                    if (excludeAvoided && avoid != null && avoid.Contains(candidate.Id)) continue;
                    total += candidate.Weight;
                }

                if (total <= 0f) return;
                float roll = RunRandom.Cards.Range(0f, total);
                CardDefinition chosen = null;
                for (int i = 0; i < pool.Count; i++)
                {
                    CardDefinition candidate = pool[i];
                    if (result.Contains(candidate)) continue;
                    if (excludeAvoided && avoid != null && avoid.Contains(candidate.Id)) continue;
                    roll -= candidate.Weight;
                    if (roll > 0f) continue;
                    chosen = candidate;
                    break;
                }

                if (chosen == null) return;
                result.Add(chosen);
            }
        }
    }
}
