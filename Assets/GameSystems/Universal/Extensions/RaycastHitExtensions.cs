using UnityEngine;

namespace Extensions
{
    public static class RaycastHitExtensions
    {
        public static bool IsInteractable(this RaycastHit hit, out IInteractable interactable)
        {
            interactable = null;
            return hit.collider?.gameObject.TryGetComponent(out interactable) ?? false;
        }
    }
}
