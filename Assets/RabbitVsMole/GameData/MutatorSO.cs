using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RabbitVsMole.GameData.Mutator
{

    [CreateAssetMenu(fileName = "NewMutator", menuName = "Stats/Mutator")]
    public class MutatorSO : ScriptableObject
    {
        public string mutatorName;
        public string description;
        public Image image;

        public Category category;
        public List<MutatorSO> incompatibleWith;

        // List of changes this mutator applies
        [SerializeReference]
        public List<IMutatorEffect> effects = new List<IMutatorEffect>();

        public void Apply(GameStats stats)
        {
            foreach (var effect in effects)
                effect.Apply(stats);
        }

        /// <summary>
        /// Verifies if there are any conflicting mutators in the given list.
        /// Returns a list of conflict messages, empty if no conflicts found.
        /// </summary>
        public static List<string> VerifyConflicts(List<MutatorSO> mutators)
        {
            var conflicts = new List<string>();

            if (mutators == null || mutators.Count <= 1)
                return conflicts;

            for (int i = 0; i < mutators.Count; i++)
            {
                var mutatorA = mutators[i];
                if (mutatorA == null) continue;

                // Check explicit incompatibilities
                for (int j = i + 1; j < mutators.Count; j++)
                {
                    var mutatorB = mutators[j];
                    if (mutatorB == null) continue;

                    // Check if A is incompatible with B
                    if (mutatorA.incompatibleWith != null && mutatorA.incompatibleWith.Contains(mutatorB))
                    {
                        conflicts.Add($"'{mutatorA.mutatorName}' is incompatible with '{mutatorB.mutatorName}'");
                    }

                    // Check if B is incompatible with A
                    if (mutatorB.incompatibleWith != null && mutatorB.incompatibleWith.Contains(mutatorA))
                    {
                        conflicts.Add($"'{mutatorB.mutatorName}' is incompatible with '{mutatorA.mutatorName}'");
                    }
                }
            }

            return conflicts;
        }
    }

    public enum Category { RabbitPositive, RabbitNegative, MolePositive, MoleNegative, BothPositive, BothNegative, Neutral }
}