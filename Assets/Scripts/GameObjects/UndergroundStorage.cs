using Extensions;
using GameObjects.Base;
using PlayerManagementSystem.AIBehaviour.Common;

namespace GameObjects
{
    public class UndergroundStorage : StorageBase
    {

        void Awake()
        {
            gameObject.UpdateTag(AIConsts.SUPPLY_TAG);
        }
    }
}
