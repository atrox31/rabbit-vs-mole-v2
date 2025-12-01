using System.Collections.Generic;
using UnityEngine;

namespace GameObjects
{
    public class UndergroundFieldGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private UndergroundField field;
        [SerializeField] private UndergroundWall blocker;

        [Header("Generation Settings")]
        [SerializeField] private int _rows = 4;
        [SerializeField] private int _cols = 4;
        [SerializeField] private float _spacing = 1.2f;
    
        private bool _initialized = false;
        private List<UndergroundField> _fieldList;

        public List<UndergroundField> GetFieldList()
        {
            return _fieldList;
        }

        public bool IsReady()
        {
            return _initialized;
        }

        private void Awake()
        {
            _fieldList = new List<UndergroundField>();
            GenerateFields();
        }

        private void GenerateFields()
        {
            // Sprawdzanie, czy prefabrykaty s¹ przypisane, aby unikn¹æ b³êdów
            if (field == null || blocker == null)
            {
                Debug.LogError("Field or Blocker prefab is not assigned in the Inspector.", this);
                return;
            }

            // Offset, aby siatka by³a wyœrodkowana na Transformie Generatora
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

                    UndergroundField newField = Instantiate(field, spawnPosition, Quaternion.identity, transform);
                    _fieldList.Add(newField);

                    // Losowa rotacja blokera, zawsze co 90 stopni
                    Quaternion blockerRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                    UndergroundWall newBlocker = Instantiate(blocker, spawnPosition, blockerRotation, transform);
                
                    newField.LinkBlocker(newBlocker);
                }
            }
        
            _initialized = true;
        }
    }
}