using UnityEngine;

namespace WalkingImmersionSystem
{
    /// <summary>
    /// Static utility class responsible for calculating the average color
    /// for a Texture2D assets.
    /// </summary>
    public static class TextureColorMapper
    {
        /// <summary>
        /// Calculates the average color of a Texture2D by sampling all its pixels.
        /// </summary>
        /// <param name="texture">The texture to analyze. Requires Read/Write Enabled flag.</param>
        /// <returns>The average color of the texture, or Color.magenta if reading fails.</returns>
        public static Color CalculateAverageColor(Texture2D texture)
        {
            if (texture == null)
            {
                return Color.magenta;
            }

            try
            {
                // Get the raw color data for all pixels
                Color[] pixels = texture.GetPixels();
                long totalPixels = pixels.Length;

                float r = 0f;
                float g = 0f;
                float b = 0f;

                // Sum up the RGB values
                foreach (Color pixel in pixels)
                {
                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                }

                // Calculate the average by dividing the sum by the total number of pixels
                Color averageColor = new Color(
                    r / totalPixels,
                    g / totalPixels,
                    b / totalPixels,
                    1f // Alpha is always 1 for the average color
                );

                return averageColor;
            }
            catch (UnityException e)
            {
                // This exception typically happens if Read/Write Enabled is NOT set.
                Debug.LogError($"Failed to calculate average color for texture '{texture.name}'. Ensure 'Read/Write Enabled' is set in the Import Settings. Error: {e.Message}");
                return Color.magenta; // Return a distinct error color
            }
        }
    }
}