namespace RabbitVsMole.InteractableGameObject.Enums
{
    public enum ActionType
    {
        None,
        // farm field
        PlantSeed,
        WaterField,
        HarvestCarrot,
        RemoveRoots,
        // underground field
        StealCarrotFromUndergroundField,
        DigUndergroundWall,
        // mound
        DigMound,
        CollapseMound,
        EnterMound,
        ExitMound,
        // storages
        PickSeed, 
        PickWater,
        PutDownCarrot,
        StealCarrotFromStorage,
    }
}