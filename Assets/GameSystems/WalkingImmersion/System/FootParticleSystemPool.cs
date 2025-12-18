using UnityEngine;

namespace WalkingImmersionSystem
{
    public class FootParticleSystemPool : MonoBehaviour
    {
        private void OnDestroy()
        {
            FootParticleSystem.ClearParticlePool();
        }
    }
}