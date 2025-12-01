using GameObjects.Base;
using UnityEngine;

namespace GameObjects
{
    public class UndergroundField : FieldBase
    {
        private UndergroundWall _fieldBlocker;

        /// <summary>
        /// Sprawdza, czy na polu jest blokada.
        /// </summary>
        public bool HasBlocker => _fieldBlocker != null;

        /// <summary>
        /// Usuwa œcianê blokuj¹c¹, jeœli istnieje.
        /// </summary>
        public bool DeleteWall()
        {
            if (!HasBlocker) return false;
            return _fieldBlocker.DeleteWallImmediately();
        }

        /// <summary>
        /// Usuwa œcianê blokuj¹c¹, jeœli istnieje.
        /// </summary>
        public bool DestroyWallWithAnimation()
        {
            if (!HasBlocker) return false;
            return _fieldBlocker.DestroyWallWithAnimation();
        }

        public void UnlinkBlocker()
        {
            _fieldBlocker = null;
        }

        /// <summary>
        /// Tworzy marchewkê pod ziemi¹.
        /// </summary>
        public override bool CreateCarrot()
        {
            if (HasCarrot) return false;

            _carrotObject = Instantiate(_carrotPrefab, transform.position, Quaternion.identity, transform);
            _haveCarrot = true;

            return true;
        }

        /// <summary>
        /// Implementacja abstrakcyjnej metody z klasy bazowej. Usuwa œcianê i marchewkê przed utworzeniem kopca.
        /// </summary>
        public override void PrepareForMoundCreation()
        {
            DeleteWall();
            DeleteCarrot(); // Wykorzystujemy metodê z klasy bazowej
        }

        /// <summary>
        /// £¹czy to pole z blokad¹.
        /// </summary>
        public void LinkBlocker(UndergroundWall blocker)
        {
            if (blocker == null) return;

            _fieldBlocker = blocker;
            blocker.LinkField(this);
        }
    }
}