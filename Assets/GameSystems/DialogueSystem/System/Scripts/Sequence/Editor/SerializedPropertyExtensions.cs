using UnityEditor;

namespace DialogueSystem.Editor
{
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Attempts to find a child property by name within the current SerializedProperty.
        /// This is often necessary for complex properties (classes/structs) in older Unity versions.
        /// </summary>
        /// <param name="property">The SerializedProperty to search within (must be a complex type).</param>
        /// <param name="name">The name of the child property to find.</param>
        /// <returns>The found child SerializedProperty, or null if not found.</returns>
        public static SerializedProperty FindProperty(this SerializedProperty property, string name)
        {
            // We need to iterate through the children of the current property.
            // Using property.Copy() prevents modification of the original property.
            SerializedProperty iterator = property.Copy();

            // Go down into the complex property. true means it also iterates into children.
            // The first call returns true and moves the iterator to the first child.
            if (iterator.Next(true))
            {
                do
                {
                    // Check if the current child property name matches the target name.
                    if (iterator.name == name)
                    {
                        // Found it! Return a copy of the found property.
                        return iterator.Copy();
                    }

                    // false means we only iterate through siblings (to stay within the original property)
                } while (iterator.Next(false));
            }

            // If the loop finishes without finding the property, return null.
            return null;
        }
    }
}