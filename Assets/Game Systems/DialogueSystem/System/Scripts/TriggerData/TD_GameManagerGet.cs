using System;
using UnityEngine;

namespace DialogueSystem.TriggerData
{
    public enum GameManagerDataType
    {
        String,
        Int,
        Float,
        Bool
    }

    [Serializable]
    public class TD_GameManagerGetData
    {
        public string FieldName;
        public GameManagerDataType DataType;
    }

    public class TD_GameManagerGet : TriggerDataBase<TD_GameManagerGetData>
    {
        /// <summary>
        /// Event handler for retrieving values by field name.
        /// Subscribe to this event in your GameManager (or similar object) to provide values.
        /// Returns null if the field is not found or not yet subscribed.
        /// </summary>
        public static Func<string, object> OnGetValue;

        public override object GetOutputValue(string portName, DialogueSequence sequence = null)
        {
            if (TypedData == null || string.IsNullOrEmpty(TypedData.FieldName))
                return null;

            if (portName != "Value")
                return null;

            object value = null;
            if (OnGetValue != null)
            {
                value = OnGetValue(TypedData.FieldName);
            }
            else
            {
                Debug.LogWarning("TD_GameManagerGet: OnGetValue event is not subscribed. Subscribe to DialogueSystem.TriggerData.TD_GameManagerGet.OnGetValue in your GameManager.");
                return null;
            }

            if (value == null)
                return null;

            // Convert to the requested type
            try
            {
                switch (TypedData.DataType)
                {
                    case GameManagerDataType.String:
                        return value.ToString();
                    case GameManagerDataType.Int:
                        return Convert.ToInt32(value);
                    case GameManagerDataType.Float:
                        return Convert.ToSingle(value);
                    case GameManagerDataType.Bool:
                        return Convert.ToBoolean(value);
                    default:
                        return value;
                }
            }
            catch
            {
                Debug.LogWarning($"TD_GameManagerGet: Failed to convert value to {TypedData.DataType}");
                return null;
            }
        }

        public override string[] GetOutputPortNames()
        {
            return new[] { "Value" };
        }

        public override Type GetOutputPortType(string portName)
        {
            if (portName == "Value")
            {
                // If TypedData is available, use its DataType
                if (TypedData != null)
                {
                    switch (TypedData.DataType)
                    {
                        case GameManagerDataType.String:
                            return typeof(string);
                        case GameManagerDataType.Int:
                            return typeof(int);
                        case GameManagerDataType.Float:
                            return typeof(float);
                        case GameManagerDataType.Bool:
                            return typeof(bool);
                    }
                }
                // Default to object if data not set yet
                return typeof(object);
            }
            return null;
        }
    }
}

