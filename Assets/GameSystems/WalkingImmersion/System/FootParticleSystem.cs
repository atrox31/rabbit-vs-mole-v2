using System.Collections.Generic;
using UnityEngine;

namespace WalkingImmersionSystem
{
    public class FootParticleSystem : MonoBehaviour
    {
        private static Queue<ParticleSystem> _particlesQueue = new();
        private ParticleSystem _particleSystem;
        private static GameObject _poolObjectContainer;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if(_poolObjectContainer == null)
            {
                _poolObjectContainer = new GameObject("FootParticleSystemPool", typeof(FootParticleSystemPool));
            }
            transform.parent = _poolObjectContainer.transform;
        }

        public static int Count => _particlesQueue.Count;

        public static ParticleSystem Get 
        { 
            get 
            {
                if (_particlesQueue.Count == 0)
                {
                    DebugHelper.LogWarning(null, "FootParticleSystem: Pool is empty! Returning null.");
                    return null;
                }
                return _particlesQueue.Dequeue();
            }
        }

        public static void ClearParticlePool() => _particlesQueue.Clear();
        public static void Add(ParticleSystem particle) => _particlesQueue.Enqueue(particle);

        public void OnParticleSystemStopped()
        {
            _particlesQueue.Enqueue(_particleSystem);
            gameObject.SetActive(false);
        }
    }
}
