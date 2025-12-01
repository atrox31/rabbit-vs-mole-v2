using System.Collections.Generic;
using UnityEngine;

namespace GameObjects
{
    public class FarmFieldGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private FarmField.FarmField fieldPrefab;

        [Header("Generation Settings")]
        [SerializeField] private int _rows = 4;
        [SerializeField] private int _cols = 4;
        [SerializeField] private float _spacing = 1.2f;

        [Header("Linking")]
        [SerializeField] private UndergroundFieldGenerator _linkedFieldGenerator;

        private List<FarmField.FarmField> _fieldList;

        private void Awake()
        {
            _fieldList = new List<FarmField.FarmField>();
            GenerateFields();
        }

        private void Start()
        {
            LinkFields();
        }

        private void GenerateFields()
        {
            if (fieldPrefab == null)
            {
                Debug.LogError("Field prefab is not assigned in the Inspector.", this);
                return;
            }

            float startX = -(_cols - 1) * _spacing / 2f;
            float startZ = -(_rows - 1) * _spacing / 2f;

            for (int x = 0; x < _cols; x++)
            {
                for (int z = 0; z < _rows; z++)
                {
                    Vector3 spawnPosition = new Vector3(
                        transform.position.x + startX + _spacing * x,
                        transform.position.y,
                        transform.position.z + startZ + _spacing * z
                    );

                    _fieldList.Add(Instantiate(fieldPrefab, spawnPosition, Quaternion.identity, transform));
                }
            }
        }

        private void LinkFields()
        {
            if (_linkedFieldGenerator == null)
            {
                Debug.LogWarning("Linked Field Generator is not assigned in the Inspector.", this);
                return;
            }

            // Czekamy a¿ po³¹czony generator bêdzie gotowy
            if (!_linkedFieldGenerator.IsReady())
            {
                Debug.LogError("The linked generator is not ready. Make sure it runs before this one.");
                return;
            }

            var undergroundFields = _linkedFieldGenerator.GetFieldList();

            if (undergroundFields.Count != _fieldList.Count)
            {
                Debug.LogError("Mismatch in fieldPrefab counts between generators. Linking aborted.");
                return;
            }

            // £¹czymy pola
            for (int i = 0; i < _fieldList.Count; i++)
            {
                _fieldList[i].LinkField(undergroundFields[i]);
                undergroundFields[i].LinkField(_fieldList[i]);
            }
        }
    }
}