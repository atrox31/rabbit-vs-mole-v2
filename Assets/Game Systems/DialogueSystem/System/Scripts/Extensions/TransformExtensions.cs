using UnityEngine;

namespace DialogueSystem
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Recursively sets the layer of the current Transform and all its children.
        /// </summary>
        /// <param name="parent">The starting Transform.</param>
        /// <param name="newLayer">The new layer index to apply (e.g., 8, 9, 10).</param>
        public static void SetLayerRecursively(this Transform parent, int newLayer)
        {
            parent.gameObject.layer = newLayer;
            foreach (Transform child in parent)
            {
                SetLayerRecursively(child, newLayer);
            }
        }
    }
}