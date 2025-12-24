using RabbitVsMole.InteractableGameObject.Base;
using System.Collections;
using UnityEngine;
using Extensions;

namespace RabbitVsMole.InteractableGameObject.Visuals
{
    public class FarmCarrotVisual : VisualBase
    {
        [SerializeField] private ParticleSystem _glowOnReadyParticles;
        public override void Hide()
        {
            StartAnimation(false);
            _glowOnReadyParticles.DetachAndDestroy();
        }

        public void StartGlow()
        {
            _glowOnReadyParticles.SafePlay();
        }
    }
}