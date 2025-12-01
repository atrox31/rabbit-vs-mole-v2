using UnityEngine;

namespace WalkingImmersionSystem
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
    }
}