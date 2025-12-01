using System.Collections.Generic;
using UnityEngine;

namespace WalkingImmersionSystem
{
    [CreateAssetMenu(fileName = "TerrainSound", menuName = "Walking Immersion System/Terrain Layer Sound")]
    public class TerrainLayerData : ScriptableObject
    {
        public TerrainLayer terrainLayer;
        public List<AudioClip> audioClips = new();
        public Color color { get; private set; } = Color.white;

        public void GenerateColor()
        {
            if (terrainLayer == null) return;
            color = TextureColorMapper.CalculateAverageColor(terrainLayer.diffuseTexture);
        }
    }
}