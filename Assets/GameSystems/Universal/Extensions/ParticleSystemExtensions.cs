using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace Extensions
{
    /// <summary>
    /// This static class holds all extension methods for Unity's built-in classes.
    /// </summary>
    public static class ParticleSystemExtensions
    {
        /// <summary>
        /// Detach particle system from parrent and play. When its finish - destroy
        /// </summary>
        /// <param name="ps">The ParticleSystem instance this method is being called on.</param>
        public static void DetachAndPlay(this ParticleSystem ps)
        {
            if (ps == null)
                return;

            ps.transform.SetParent(null);
            ps.Play();
            var mainModule = ps.main;
            mainModule.stopAction = ParticleSystemStopAction.Destroy;
        }
        public static void DetachAndDestroy(this ParticleSystem ps)
        {
            if (ps == null)
                return;

            ps.transform.SetParent(null);
            ps.Play();
            var mainModule = ps.main;
            mainModule.loop = false;
            mainModule.stopAction = ParticleSystemStopAction.Destroy;
            
            // Apply stopAction to all child particle systems
            foreach (Transform child in ps.transform)
            {
                var childParticleSystems = child.GetComponentsInChildren<ParticleSystem>();
                foreach (var childPs in childParticleSystems)
                {
                    var childMainModule = childPs.main;
                    childMainModule.loop = false;
                    childMainModule.stopAction = ParticleSystemStopAction.Destroy;
                }
            }
        }
        
        public static void SafePlay(this ParticleSystem particles)
        {
            if (particles != null)
            {
                particles.Play();
            }
        }
        
        public static void SafeStop(this ParticleSystem particles)
        {
            if (particles != null)
            {
                particles.Stop();
            }
        }
    }
}