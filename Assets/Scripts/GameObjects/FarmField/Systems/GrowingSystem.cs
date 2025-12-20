namespace GameObjects.FarmField.Systems
{
    public class GrowingSystem
    {
        private readonly FarmField _farmfield;
        private readonly WateringSystem _wateringSystem;

        public GrowingSystem(
            FarmField farmField,
            WateringSystem wateringSystem)
        {
            _farmfield = farmField;
            _wateringSystem = wateringSystem;
        }
    }
}