using Extensions;
using GameObjects.Base;
using PlayerManagementSystem.AIBehaviour.Common;
using UnityEngine;

namespace GameObjects
{
    public class FarmStorage : StorageBase
    {
        private bool m_carrotStealProgress = false;
        [SerializeField] ParticleSystem m_ParticleCarrotSteal;

        public bool CanStealCarrot => m_CarrotList.Count > 0;
        public bool StartStealCarrot()
        {
            if (!CanStealCarrot) return false;
            if (m_carrotStealProgress) return false;
            m_carrotStealProgress = true;
            m_ParticleCarrotSteal.Play();
            return true;
        }

        public bool EndStealCarrot()
        {
            if (!CanStealCarrot) return false;
            if (!m_carrotStealProgress) return false;
            DeleteCarrot();
            m_ParticleCarrotSteal.Stop();
            return true;
        }
        void Awake()
        {
            gameObject.UpdateTag(AIConsts.SUPPLY_TAG);
        }

    }
}
