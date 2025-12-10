using Extensions;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace WalkingImmersionSystem
{
    public class WalkingImmersionSystemController : MonoBehaviour
    {
        private Dictionary<ModelAnchor, Transform> _modelAnchorDictionary = new Dictionary<ModelAnchor, Transform>();

        // this is universal so its load only once
        private static Dictionary<TerrainLayer, TerrainLayerData> _terrainLayers = null;

        public bool PlayFootParticles = true;
        public bool PlayFootSound = true;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private ParticleSystem _particleSystemForFoots;
        private const int PARTICLE_POOL_SIZE = 16; // Increased pool size to prevent empty queue issues

        // Performance optimization: cache last color per particle system to avoid unnecessary changes
        private Dictionary<ParticleSystem, Color> _particleColorCache = new Dictionary<ParticleSystem, Color>();
        // Performance optimization: throttle sound playback to avoid too many simultaneous AudioSource
        private float _lastSoundPlayTime = 0f;
        private const float SOUND_COOLDOWN = 0.1f; // Minimum time between footstep sounds
        // Performance optimization: throttle particle playback to avoid too many simultaneous particles
        private float _lastParticlePlayTime = 0f;
        private const float PARTICLE_COOLDOWN = 0.15f; // Minimum time between particle effects
        // Performance optimization: global throttling for footstep events to prevent too frequent calls
        private float _lastFootstepEventTime = 0f;
        private const float FOOTSTEP_EVENT_COOLDOWN = 0.2f; // Minimum time between footstep events
        // Performance optimization: cache last detected surface to avoid repeated GetAlphamaps() calls
        private Vector3 _lastSurfaceCheckPosition = Vector3.zero;
        private TerrainLayer _cachedSurface = null;
        private const float SURFACE_CACHE_DISTANCE = 2.0f; // Cache surface if position changed less than this (increased for better performance)
        // Additional cooldown for expensive GetAlphamaps() calls
        private float _lastSurfaceCheckTime = 0f;
        private const float SURFACE_CHECK_COOLDOWN = 0.1f; // Minimum time between expensive GetAlphamaps() calls

        private TerrainSurfaceDetector _terrainSurfaceDetector;

        private void Awake()
        {
            if (!Setup())
            {
                Destroy(this);
            }
        }

        private bool Setup()
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"WalkingImmersionSystemController warrning! Animator component not found, component is required to work!.");
                return false;
            }

            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if(leftFoot == null || rightFoot == null)
            {
                Debug.LogError($"WalkingImmersionSystemController error! Foots not found.");
                return false;
            }

            _modelAnchorDictionary.Add(ModelAnchor.LeftFoot, leftFoot);
            _modelAnchorDictionary.Add(ModelAnchor.RightFoot, rightFoot);

            if (_particleSystemForFoots == null)
            {
                return Error.Message($"WalkingImmersionSystemController error! _particleSystemForFoots - not assign.");
            }

            if (_rigidbody == null)
            {
                return Error.Message($"WalkingImmersionSystemController error! _rigidbody - not assign.");
            }

            if (!PrepareLayerData())
            {
                return Error.Message($"WalkingImmersionSystemController error! PrepareLayerData()");
            }

            if (!SetupTerrain())
            {
                return Error.Message($"WalkingImmersionSystemController error! SetupTerrain()");
            }

            if (!PrepareParticlesPool())
            {
                return Error.Message($"WalkingImmersionSystemController error! PrepareParticlesPool()");
            }

            return true;
        }

        private bool PrepareParticlesPool()
        {
            if (_particleSystemForFoots == null)
                return Error.Message("WalkingImmersionSystemController error! PrepareParticlesPool() - _particleSystemForFoots = null");

            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var particleSystem = Instantiate(_particleSystemForFoots);
                var pmain = particleSystem.main;
                pmain.playOnAwake = false;
                pmain.loop = false;
                pmain.stopAction = ParticleSystemStopAction.Callback;
                particleSystem.gameObject.SetActive(false);

                FootParticleSystem.Add(particleSystem);
            }
            return true;
        }

        private bool SetupTerrain()
        {
            var terrain = FindFirstObjectByType<Terrain>();
            if (terrain == null)
                return Error.Message($"WalkingImmersionSystemController error! SetupTerrain-> FindFirstObjectByType<Terrain>() is null");


            _terrainSurfaceDetector = terrain.GetOrAddComponent<TerrainSurfaceDetector>();
            if (_terrainSurfaceDetector == null) 
                return Error.Message($"WalkingImmersionSystemController error! SetupTerrain-> GetOrAddComponent<TerrainSurfaceDetector>() is null");

            return true;
        }

        private bool PrepareLayerData()
        {
            if (_terrainLayers != null) return true;
            if (SoundConfigLoader.GetTerrainLayerData().Count == 0) return false;

            _terrainLayers = new();
            List<AudioClip> clipsToPreload = new List<AudioClip>();
            
            foreach (var terrainLayerData in SoundConfigLoader.GetTerrainLayerData())
            {
                _terrainLayers.Add(terrainLayerData.terrainLayer, terrainLayerData);
                _terrainLayers[terrainLayerData.terrainLayer].GenerateColor();
                
                // Collect all audio clips for preloading
                if (terrainLayerData.audioClips != null)
                {
                    clipsToPreload.AddRange(terrainLayerData.audioClips);
                }
            }
            
            // Preload all footstep audio clips to prevent loading delays during gameplay
            if (clipsToPreload.Count > 0 && AudioManager.IsInstanceActive)
            {
                AudioManager.PreloadClips(clipsToPreload);
            }
            
            return true;
        }

        public void EvPlayFootstep(ModelAnchor side)
        {
            // Early exit if both features are disabled - no need to do any work
            if (!PlayFootSound && !PlayFootParticles)
                return;

            float currentTime = Time.time;
            if (currentTime - _lastFootstepEventTime < FOOTSTEP_EVENT_COOLDOWN)
                return;
            _lastFootstepEventTime = currentTime;

            Vector3 currentPosition = transform.position;
            
            // PERFORMANCE OPTIMIZATION: Cache surface detection result
            // GetAlphamaps() is extremely expensive - cache result if position hasn't changed much
            TerrainLayer surface = null;
            // Use sqrMagnitude instead of Distance for better performance (avoids sqrt calculation)
            float sqrDistanceFromLastCheck = (currentPosition - _lastSurfaceCheckPosition).sqrMagnitude;
            float sqrCacheDistance = SURFACE_CACHE_DISTANCE * SURFACE_CACHE_DISTANCE;
            
            // Check if we need to update surface detection
            bool needsSurfaceCheck = sqrDistanceFromLastCheck > sqrCacheDistance || _cachedSurface == null;
            
            // Additional cooldown for expensive GetAlphamaps() calls to prevent frame drops
            if (needsSurfaceCheck && (currentTime - _lastSurfaceCheckTime) < SURFACE_CHECK_COOLDOWN)
            {
                needsSurfaceCheck = false; // Use cached result if cooldown hasn't passed
            }
            
            if (needsSurfaceCheck)
            {
                // Only call expensive GetAlphamaps() if position changed significantly AND cooldown passed
                surface = _terrainSurfaceDetector.GetMainTextureName(currentPosition);
                _cachedSurface = surface;
                _lastSurfaceCheckPosition = currentPosition;
                _lastSurfaceCheckTime = currentTime;
            }
            else
            {
                // Use cached surface if position hasn't changed much or cooldown active
                surface = _cachedSurface;
            }

            if (surface == null) return;

            // Use TryGetValue instead of ContainsKey + indexer to avoid double lookup
            if (_terrainLayers.TryGetValue(surface, out TerrainLayerData layerData))
            {
                
                if (PlayFootSound)
                    PlaySoundForSurface(layerData, _rigidbody.position);

                if (PlayFootParticles)
                    PlayParticlesForAnchor(
                        _modelAnchorDictionary[side],
                        _particleSystemForFoots,
                        layerData);
                
            }
        }

        /// <summary>
        /// Plays the footstep sound based on the detected surface name.
        /// </summary>
        /// <param name="layerData">The terrain layer data containing audio clips.</param>
        /// <param name="position">Vector3 position of sound</param>
        protected void PlaySoundForSurface(TerrainLayerData layerData, Vector3 position)
        {
            if (layerData == null || layerData.audioClips.Count == 0) return;
            
            // Throttle sound playback to avoid too many simultaneous AudioSource instances
            // This prevents performance issues when animation events fire very frequently
            float currentTime = Time.time;
            if (currentTime - _lastSoundPlayTime < SOUND_COOLDOWN)
                return;
            
            _lastSoundPlayTime = currentTime;
            AudioManager.PlaySound3D(layerData.audioClips.GetRandomElement(), position);
        }

        /// <summary>
        /// Plays particles effect on anchor.
        /// </summary>
        /// <param name="transformCoords">Transform where particles must spawn.</param>
        /// <param name="particleSystem">The particle system prefab to instantiate.</param>
        /// <param name="layerData">The terrain layer data containing color information.</param>
        private void PlayParticlesForAnchor(Transform transformCoords, ParticleSystem particleSystem, TerrainLayerData layerData)
        {
            if (transformCoords == null) return;
            if (particleSystem == null) return;

            // PERFORMANCE OPTIMIZATION: Throttle particle playback
            // Prevents too many particle systems from being active simultaneously
            float currentTime = Time.time;
            if (currentTime - _lastParticlePlayTime < PARTICLE_COOLDOWN)
                return;
            _lastParticlePlayTime = currentTime;

            if(FootParticleSystem.Count == 0) return;
            var particle = FootParticleSystem.Get;
            if (particle == null) return; // Safety check in case pool is empty

            const float intensity = 1.2f;
            Color particleColor = layerData.color * intensity;

            // PERFORMANCE CRITICAL: Changing main.startColor is extremely expensive!
            // It forces Unity to recalculate the entire particle system structure.
            // We optimize by:
            // 1. Only changing color if it's significantly different (cached comparison)
            // 2. Setting position BEFORE activating (avoids unnecessary updates)
            // 3. Using SetActive only if not already active
            
            particle.transform.position = transformCoords.position;
            
            bool needsColorUpdate = true;
            if (_particleColorCache.TryGetValue(particle, out Color lastColor))
            {
                // Fast color comparison - only update if color changed significantly
                float colorDelta = Mathf.Abs(particleColor.r - lastColor.r) + 
                                  Mathf.Abs(particleColor.g - lastColor.g) + 
                                  Mathf.Abs(particleColor.b - lastColor.b);
                needsColorUpdate = colorDelta > 0.05f; // Threshold to avoid micro-changes
            }

            if (needsColorUpdate)
            {
                var main = particle.main;
                main.startColor = new ParticleSystem.MinMaxGradient(particleColor);
                _particleColorCache[particle] = particleColor;
            }

            // Only activate if not already active to avoid unnecessary overhead
            if (!particle.gameObject.activeSelf)
            {
                particle.gameObject.SetActive(true);
            }
            
            // Play the particle system
            particle.Play();
        }
    }
}