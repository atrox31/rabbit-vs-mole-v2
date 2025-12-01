using UnityEngine;

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
            ps.transform.SetParent(null);
            ps.Play();
            var mainModule = ps.main;
            mainModule.stopAction = ParticleSystemStopAction.Destroy;
        }

    }
}