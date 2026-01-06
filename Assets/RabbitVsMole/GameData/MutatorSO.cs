using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace RabbitVsMole.GameData.Mutator
{

    [CreateAssetMenu(fileName = "NewMutator", menuName = "Stats/Mutator")]
    public class MutatorSO : ScriptableObject
    {
        public LocalizedString mutatorName;
        public LocalizedString description;
        public Sprite image;

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

        public string GetLocalizedName() => TryGetLocalized(mutatorName);
        public string GetLocalizedDescription() => TryGetLocalized(description);

        private static string TryGetLocalized(LocalizedString localizedString)
        {
            // Avoid Unity Localization exception when TableReference is empty.
            if (localizedString == null)
                return null;
            if (string.IsNullOrEmpty(localizedString.TableReference.TableCollectionName) &&
                localizedString.TableReference == null)
                return null;
            return localizedString.GetLocalizedString();
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
                var mutatorALabel = mutatorA.GetLocalizedName() ?? mutatorA.name;

                // Check explicit incompatibilities
                for (int j = i + 1; j < mutators.Count; j++)
                {
                    var mutatorB = mutators[j];
                    if (mutatorB == null) continue;
                    var mutatorBLabel = mutatorB.GetLocalizedName() ?? mutatorB.name;

                    // Check if A is incompatible with B
                    if (mutatorA.incompatibleWith != null && mutatorA.incompatibleWith.Contains(mutatorB))
                    {
                        conflicts.Add($"'{mutatorALabel}' is incompatible with '{mutatorBLabel}'");
                    }

                    // Check if B is incompatible with A
                    if (mutatorB.incompatibleWith != null && mutatorB.incompatibleWith.Contains(mutatorA))
                    {
                        conflicts.Add($"'{mutatorBLabel}' is incompatible with '{mutatorALabel}'");
                    }
                }
            }

            return conflicts;
        }
    }

    public enum Category { RabbitPositive, RabbitNegative, MolePositive, MoleNegative, BothPositive, BothNegative, Neutral, EditorOnly }
}