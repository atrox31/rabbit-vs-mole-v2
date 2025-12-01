using UnityEngine;

namespace GameObjects.Misc
{
    public class RandomRotation : MonoBehaviour
    {
        private enum Axis
        {
            X, Y, Z
        }
        [SerializeField] private Axis _axis;

        private float GetAxisValue(Axis axis)
        {
            return axis == _axis ? 1.0f : 0.0f;
        }

        void Start()
        {
            transform.Rotate(
                new Vector3(
                    GetAxisValue(Axis.X),
                    GetAxisValue(Axis.Y),
                    GetAxisValue(Axis.Z)),
                Random.Range(0.0f, 360.0f),
                Space.Self);
        }

    }
}
