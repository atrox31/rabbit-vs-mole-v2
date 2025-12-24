using NUnit.Framework;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FieldGenerator : MonoBehaviour
    {
        public enum FieldType { Farm, Undergorund}
        [Header("Editor type varibles")]
        [SerializeField] private FieldType fieldType;
        [SerializeField] private FieldGenerator _linkedFieldGenerator;

        [Header("Prefabs")]
        [SerializeField] private FarmFieldBase _farmFieldPrefab;
        [SerializeField] private UndergroundFieldBase _undergroundFieldPrefab;
        [SerializeField] private float _YAdjusment = 0.01f;

        private FieldBase Prefab =>
            fieldType == FieldType.Farm
            ? _farmFieldPrefab
            : _undergroundFieldPrefab;

        private int _gridSizeXY = 4;

        private List<GameObject> _fieldList = new();
        public int FieldListSize =>
            _fieldList.Count;

        private void Awake()
        {
            GenerateFields();
            GetComponent<MeshRenderer>().enabled = false;
        }

        private void GenerateFields()
        {
            // 1. Get the size of the generator's model
            Renderer baseRenderer = GetComponent<Renderer>();
            if (baseRenderer == null) return;

            Vector3 baseSize = baseRenderer.bounds.size;
            Vector3 baseMinCorner = baseRenderer.bounds.min;

            // 2. Calculate the size each field should have (in world space)
            float cellWidth = baseSize.x / _gridSizeXY;
            float cellHeight = baseSize.z / _gridSizeXY;

            // 3. Get the actual size of the prefab when instantiated as child with localScale (1,1,1)
            // This accounts for all internal scales in the prefab hierarchy (e.g., model with 0.25 scale)
            // and parent scale, giving us the actual world size
            GameObject tempField = Instantiate(Prefab.gameObject, transform);
            tempField.transform.localScale = Vector3.one;
            tempField.transform.localPosition = Vector3.zero;
            
            Renderer tempRenderer = tempField.GetComponentInChildren<Renderer>();
            Vector3 prefabSizeAtScaleOne;
            if (tempRenderer != null)
            {
                // This gives us the prefab's actual size in world space when localScale is (1,1,1)
                // It already accounts for internal prefab scales and parent scale
                prefabSizeAtScaleOne = tempRenderer.bounds.size;
            }
            else
            {
                prefabSizeAtScaleOne = Vector3.one;
            }
            DestroyImmediate(tempField);

            for (int x = 0; x < _gridSizeXY; x++)
            {
                for (int y = 0; y < _gridSizeXY; y++)
                {
                    // Create the field
                    GameObject field = Instantiate(Prefab.gameObject, transform);

                    // 4. Calculate Scale to fit perfectly in the cell
                    // prefabSizeAtScaleOne already accounts for internal prefab scales and parent scale
                    // Formula: localScale * prefabSizeAtScaleOne = targetWorldSize (cellWidth)
                    // Therefore: localScale = cellWidth / prefabSizeAtScaleOne
                    float scaleX = cellWidth / prefabSizeAtScaleOne.x;
                    float scaleZ = cellHeight / prefabSizeAtScaleOne.z;
                    field.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

                    // 5. Calculate Position in local space
                    // Convert world bounds corners to local space
                    Vector3 localMinCorner = transform.InverseTransformPoint(baseMinCorner);
                    Vector3 localMaxPoint = transform.InverseTransformPoint(baseRenderer.bounds.max);
                    
                    // Get parent scale for position calculations
                    Vector3 parentScale = transform.lossyScale;
                    
                    // Calculate local cell dimensions accounting for parent scale
                    float localCellWidth = cellWidth / parentScale.x;
                    float localCellHeight = cellHeight / parentScale.z;
                    
                    // Calculate position: start from local min corner, add cell offset and center
                    float posX = localMinCorner.x + (x * localCellWidth) + (localCellWidth / 2f);
                    float posZ = localMinCorner.z + (y * localCellHeight) + (localCellHeight / 2f);
                    
                    // Set Y slightly above the generator surface to avoid Z-fighting
                    float posY = _YAdjusment;

                    field.transform.localPosition = new Vector3(posX, posY, posZ);
                    _fieldList.Add(field);
                }
            }
        }
        
        IEnumerator Start()
        {
            LinkFields();
            yield return null;
        }

        public FieldBase GetField(int index)
        {
            if(index < 0 || index >= _fieldList.Count)
            {
                DebugHelper.LogError(this, "Get field index out of bounds");
                return null;
            }
            return _fieldList[index].GetComponent<FieldBase>();
        }

        private void LinkFields()
        {
            if (_linkedFieldGenerator == null)
            {
                DebugHelper.LogError(this, "Cannot link fields, linked field is not... linked");
                return;
            }

            if (_linkedFieldGenerator.FieldListSize != FieldListSize)
            {
                DebugHelper.LogError(this, "Field size in linked field generators is diffrent");
                return;
            }

            var listCount = _fieldList.Count;
            for (int i = 0; i < listCount; i++)
            {
                var otherField = _linkedFieldGenerator.GetField((listCount - 1) - i);
                var myField = _fieldList[i].GetComponent<FieldBase>();

                if (myField != null && otherField != null)
                    myField.LinkedField = otherField;
                else
                    DebugHelper.LogWarning(this, $"Field at index {i} or its counterpart could not be linked.");
            }
        }
    }
}