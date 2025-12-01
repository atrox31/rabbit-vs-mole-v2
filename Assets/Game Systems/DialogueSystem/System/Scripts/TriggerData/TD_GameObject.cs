using System;
using UnityEngine;

namespace DialogueSystem.TriggerData
{
    [Serializable]
    public class TD_GameObjectData
    {
        public string GameObjectName; // Name reference to GameObject in DialogueSequence
    }

    public class TD_GameObject : TriggerDataBase<TD_GameObjectData>
    {
        public override object GetOutputValue(string portName, DialogueSequence sequence = null)
        {
            if (TypedData == null || string.IsNullOrEmpty(TypedData.GameObjectName))
                return null;

            // Get GameObject from sequence by name
            GameObject gameObject = null;
            if (sequence != null)
            {
                gameObject = sequence.GetGameObjectByName(TypedData.GameObjectName);
            }

            if (gameObject == null)
            {
                Debug.LogWarning($"TD_GameObject: GameObject with name '{TypedData.GameObjectName}' not found in DialogueSequence.");
                return null;
            }

            switch (portName)
            {
                case "Position":
                    return gameObject.transform.position;
                case "Rotation":
                    return gameObject.transform.rotation;
                case "Scale":
                    return gameObject.transform.localScale;
                case "GameObject":
                    return gameObject;
                default:
                    return null;
            }
        }

        public override string[] GetOutputPortNames()
        {
            return new[] { "GameObject", "Position", "Rotation", "Scale" };
        }

        public override Type GetOutputPortType(string portName)
        {
            switch (portName)
            {
                case "Position":
                case "Scale":
                    return typeof(Vector3);
                case "Rotation":
                    return typeof(Quaternion);
                case "GameObject":
                    return typeof(GameObject);
                default:
                    return null;
            }
        }
    }
}

