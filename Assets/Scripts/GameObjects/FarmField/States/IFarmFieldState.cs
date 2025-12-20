using System;
using System.Collections;
using Enums;
using RabbitVsMole;
using UnityEngine.ResourceManagement.Util;

namespace GameObjects.FarmField.States
{
    public interface IFarmFieldState
    {
        bool CanPlant { get; }
        bool CanWater { get; }
        bool CanHarvest { get; }
        bool CanCollapseMound { get; }
        bool CanDigMound { get; }
        bool CanRemoveRoots { get; }
        bool CanEnterMound { get; }

        /// <summary>
        /// Gets the AI priority configuration for this field state.
        /// </summary>
        /// <remarks>
        /// The priority is determined by the field state. However, if the field count exceeds 
        /// the conditional threshold, the critical priority value is used instead.
        /// <para>Priority mapping table:</para>
        /// <code>
        /// [State]          [Value]  [Critical (if > threshold)]
        /// GrownField       (100)    (100)
        /// PlantedField     (70)     (70)
        /// MoundedField     (60)     (80)
        /// UntouchedField   (50)     (50)
        /// RootedField      (40)     (90)
        /// </code>
        /// </remarks>
        AIPriority AIPriority { get; }

        IEnumerator Interact(PlayerType playerType,
            Func<ActionType, bool> notifyAndCheck,
            Action<IFarmFieldState> onDone);

        void CancelAction();
    }
}