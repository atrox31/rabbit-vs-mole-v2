using System.Collections.Generic;
using GameObjects.Misc;
using UnityEngine;

namespace GameObjects.Base
{
    public class StorageBase : MonoBehaviour
    {
        [SerializeField] protected CarrotModelInStorage m_Carrot;
        [SerializeField] protected Transform m_CarrotSpawnPoint;

        protected List<CarrotModelInStorage> m_CarrotList = new List<CarrotModelInStorage>();

        public bool AddCarrot()
        {
            m_CarrotList.Add(Instantiate(m_Carrot, m_CarrotSpawnPoint.position, Quaternion.identity, transform));
            return true;
        }

        public bool CanDeleteCarrot => m_CarrotList.Count > 0;
        public bool DeleteCarrot()
        {
            if (!CanDeleteCarrot) return false;
            var random_carrot = m_CarrotList[Random.Range(0, m_CarrotList.Count)] as CarrotModelInStorage;
            random_carrot.Delete();
            m_CarrotList.Remove(random_carrot);
            return true;
        }

    }
}