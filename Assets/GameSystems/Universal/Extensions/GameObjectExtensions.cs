using Unity.VisualScripting;
using UnityEngine;

namespace Extensions
{
    /// <summary>
    /// Extension methods for the GameObject class, providing utility functions.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Searches recursively through all child objects of the current GameObject 
        /// (the parent) to find a specific GameObject by name.
        /// </summary>
        /// <param name="childName">The exact name of the child object to find.</param>
        /// <returns>The found GameObject, or null if no child with the given name is found.</returns>
        public static GameObject FindChildByNameRecursive(this GameObject parent, string childName)
        {
            // Null check for the parent object (although 'this' should generally not be null)
            if (parent == null)
            {
                Debug.LogError("Parent object cannot be null.");
                return null;
            }

            // 1. Iterate through all Transform children of the parent
            foreach (Transform childTransform in parent.transform)
            {
                GameObject childObject = childTransform.gameObject;

                // 2. Check the current child's name
                if (childObject.name.Equals(childName))
                {
                    return childObject; // Found the target!
                }

                // 3. Recursive Search: Search the children of this child
                GameObject foundChild = FindChildByNameRecursive(childObject, childName);

                // If a match was found in the deeper hierarchy, return it immediately
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            // 4. If the loop completes without finding anything
            return null;
        }
        
        /// <summary>
        /// Sets the tag of the specified <see cref="GameObject"/> to the provided value.
        /// </summary>
        /// <remarks>If <paramref name="newTag"/> is <c>null</c> or empty, the method logs a warning and
        /// does not update the tag. If <paramref name="newTag"/> is not defined in the Tags &amp; Layers settings, an
        /// error is logged and the tag is not updated.</remarks>
        /// <param name="gameObject">The <see cref="GameObject"/> whose tag will be updated. Cannot be <c>null</c>.</param>
        /// <param name="newTag">The new tag to assign to the <paramref name="gameObject"/>. Must be a non-empty string and defined in the
        /// project's Tags &amp; Layers settings.</param>
        public static void UpdateTag(this GameObject gameObject, string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                Debug.LogWarning("Attempted to set an empty or null tag.");
                return;
            }

            try
            {
                gameObject.tag = newTag;
            }
            catch (UnityException)
            {
                DebugHelper.LogError(gameObject?.GetComponent<MonoBehaviour>(), $"Tag '{newTag}' is not defined in the Tags & Layers settings.");
            }
        }

        /// <summary>
        /// Finds the nearest component of type T relative to this GameObject.
        /// </summary>
        public static T FindNearest<T>(this GameObject origin) where T : Component
        {
            // Find all active components of type T in the scene
            T[] allObjects = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

            if (allObjects.Length == 0) return null;

            T nearest = null;
            float minDistanceSqr = float.MaxValue;
            Vector3 currentPos = origin.transform.position;

            foreach (T obj in allObjects)
            {
                // Skip if the object found is the one we are searching from
                if (obj.gameObject == origin) continue;

                // Using sqrMagnitude to avoid expensive square root calculations
                Vector3 directionToTarget = obj.transform.position - currentPos;
                float dSqr = directionToTarget.sqrMagnitude;

                if (dSqr < minDistanceSqr)
                {
                    minDistanceSqr = dSqr;
                    nearest = obj;
                }
            }

            return nearest;
        }
    }
}