using UnityEngine;

namespace GameObjects.Base
{
    public abstract class CarrotBase : MonoBehaviour
    {
        public bool IsReady { get; protected set; } = false;

        public abstract bool Delete();

        public abstract bool Grow(FarmField.FarmField parentField);

        public virtual void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}