using UnityEngine;

namespace GameObjects.System
{
    public class HideOnPlay : MonoBehaviour
    {
        private void Awake()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if(meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }
}
