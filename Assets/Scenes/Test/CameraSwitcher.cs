using UnityEngine;

namespace TestBot
{
    public class CameraSwitcher : MonoBehaviour
    {
        static CameraSwitcher _instance;
        [SerializeField] Transform _rabbitCameraTransfrom;
        [SerializeField] Transform _moleCameraTransform;
        Camera _camera;

        private void Awake()
        {
            if(!gameObject.TryGetComponent(out _camera))
            {
                DebugHelper.LogError(this, "Can not find camera component");
            }
            _instance = this;
        }

        public void SwitchToRabbit()
        {
            transform.SetPositionAndRotation(_rabbitCameraTransfrom.position, _rabbitCameraTransfrom.rotation);
        }

        public void SwitchToMole()
        {
            transform.SetPositionAndRotation(_moleCameraTransform.position, _moleCameraTransform.rotation);
        }
    }
}