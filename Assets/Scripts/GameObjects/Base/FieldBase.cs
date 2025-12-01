using UnityEngine;

namespace GameObjects.Base
{
    public abstract class FieldBase : MonoBehaviour
    {
        // Pola, które mog¹ byæ ustawione w edytorze Unity
        [Header("Prefabs")]
        [SerializeField] protected Mound _moundPrefab;
        [SerializeField] protected CarrotBase _carrotPrefab;

        // Pola stanu
        protected bool _haveMound = false;
        protected Mound _moundObject;
        protected bool _haveCarrot = false;
        protected CarrotBase _carrotObject;
        protected FieldBase _linkedField;

        /// <summary>
        /// Sprawdza, czy pole ma kopiec.
        /// </summary>
        public bool HasMound => _moundObject != null;

        /// <summary>
        /// Sprawdza, czy kopiec jest w pe³ni uformowany.
        /// </summary>
        public bool IsMoundReady => HasMound && _moundObject.IsReady;

        /// <summary>
        /// Sprawdza, czy mo¿na stworzyæ kopiec.
        /// </summary>
        public bool CanCreateMound => !IsMoundReady;

        /// <summary>
        /// Abstrakcyjna metoda do przygotowania pola przed stworzeniem kopca (np. usuniêcie blokera).
        /// </summary>
        public abstract void PrepareForMoundCreation();

        /// <summary>
        /// Tworzy kopiec na polu i na po³¹czonym polu.
        /// </summary>
        public virtual bool CreateMound()
        {
            if (HasMound) return false;

            _moundObject = Instantiate(_moundPrefab, transform.position, Quaternion.identity, transform);
            _haveMound = true;

            PrepareForMoundCreation();

            if (HasLinkedField)
            {
                _linkedField.CreateMound();
            }

            return true;
        }

        /// <summary>
        /// Niszczy kopiec na polu i na po³¹czonym polu.
        /// </summary>
        public virtual bool DestroyMound()
        {
            if (!HasMound) return false;

            _moundObject.Delete();
            _moundObject = null;
            _haveMound = false;

            if (HasLinkedField)
            {
                _linkedField.DestroyMound();
            }

            return true;
        }

        /// <summary>
        /// Sprawdza, czy pole ma marchewkê.
        /// </summary>
        public bool HasCarrot => _carrotObject != null;

        /// <summary>
        /// Abstrakcyjna metoda do tworzenia marchewki.
        /// </summary>
        public abstract bool CreateCarrot();

        /// <summary>
        /// Usuwa marchewkê z pola i po³¹czonego pola.
        /// </summary>
        public bool DeleteCarrot()
        {
            if (!HasCarrot) return false;
            bool success = _carrotObject.Delete();
            if (success)
            {
                _carrotObject = null;
                if (HasLinkedField)
                {
                    _linkedField.DeleteCarrot();
                }
            }
            return success;
        }

        /// <summary>
        /// Sprawdza, czy pole jest po³¹czone z innym.
        /// </summary>
        public bool HasLinkedField => _linkedField != null;

        /// <summary>
        /// £¹czy pole z innym polem.
        /// </summary>
        public void LinkField(FieldBase field)
        {
            _linkedField = field;
        }

        public FieldBase GetLinkedField() { return _linkedField; }
    }
}