using UnityEngine;

namespace WalkingImmersionSystem
{
    public class TerrainSurfaceDetector : MonoBehaviour
    {
        private Terrain terrain;
        private TerrainData terrainData;
        private TerrainLayer[] terrainLayers;
        private int alphaMapWidth;

        void Awake()
        {
            // Get references to terrain components
            terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                Debug.LogError("Terrain component not found on this GameObject.");
                return;
            }

            terrainData = terrain.terrainData;
            terrainLayers = terrainData.terrainLayers;
            alphaMapWidth = terrainData.alphamapWidth;
        }

        /// <summary>
        /// Returns the dominant terrain layer (texture) at a given world position.
        /// </summary>
        /// <param name="worldPos">The global position (e.g., player's position).</param>
        /// <returns>The name of the dominant terrain layer, or null if not found.</returns>
        public TerrainLayer GetMainTextureName(Vector3 worldPos)
        {
            if (terrainData == null || terrainLayers == null || terrainLayers.Length == 0)
            {
                return null;
            }

            // 1. Convert world position to normalized terrain coordinates (0 to 1)
            Vector3 terrainLocalPos = worldPos - terrain.transform.position;
            Vector3 splatMapCoord = new Vector3(
                terrainLocalPos.x / terrainData.size.x,
                0,
                terrainLocalPos.z / terrainData.size.z
            );

            // 2. Convert normalized coordinates to Alpha Map indices
            int x = (int)(splatMapCoord.x * alphaMapWidth);
            int z = (int)(splatMapCoord.z * alphaMapWidth);

            // Clamp the coordinates to prevent out-of-bounds access
            x = Mathf.Clamp(x, 0, alphaMapWidth - 1);
            z = Mathf.Clamp(z, 0, alphaMapWidth - 1);

            // 3. Get the Alpha Map data (contains texture weights at that point)
            float[,,] alphaMap = terrainData.GetAlphamaps(x, z, 1, 1);

            // 4. Find the dominant texture index
            int mainTextureIndex = 0;
            float maxWeight = 0f;

            int numTextures = alphaMap.GetLength(2);
            for (int i = 0; i < numTextures; i++)
            {
                if (alphaMap[0, 0, i] > maxWeight)
                {
                    maxWeight = alphaMap[0, 0, i];
                    mainTextureIndex = i;
                }
            }

            // 5. Return the name of the corresponding TerrainLayer
            if (mainTextureIndex < terrainLayers.Length)
            {
                return terrainLayers[mainTextureIndex];
            }

            return null;
        }
    }
}