using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace PlayerManagementSystem.AIBehaviour.Common
{
    public class AIDebugOutput : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_TextMeshProUGUI;
        static AIDebugOutput _instance;
        private string _lastMessage = string.Empty;
        private int _lastMessageCounter = 0;
        private List<string> _lines = new();
        private StringBuilder _stringBuilder = new();
        private const int MAX_LINES = 12;

        private void Awake()
        {
            _instance = this;
            if (m_TextMeshProUGUI == null)
            {
                Destroy(gameObject);
            }
            m_TextMeshProUGUI.text = string.Empty;
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        public static void LogMessage(string message) =>
            _instance?.WriteMessage(message, "black");
        
        public static void LogWarning(string message) =>
            _instance?.WriteMessage(message, "yellow");
        
        public static void LogError(string message) =>
            _instance?.WriteMessage(message, "red");
        
        private void WriteMessage(string message, string color)
        {
            bool isSameMessage = string.Equals(message, _lastMessage);
            
            if (isSameMessage)
            {
                _lastMessageCounter++;
            }
            else
            {
                _lastMessageCounter = 0;
                _lastMessage = message;
            }

            string finalMessage;
            if (_lastMessageCounter > 0)
            {
                finalMessage = $"({_lastMessageCounter})<color={color}>{message}</color>";
                
                // Update existing line if it exists
                if (_lines.Count > 0)
                {
                    _lines[0] = finalMessage;
                }
                else
                {
                    _lines.Insert(0, finalMessage);
                }
            }
            else
            {
                finalMessage = $"<color={color}>{message}</color>";
                _lines.Insert(0, finalMessage);
            }

            // Remove excess lines
            if (_lines.Count > MAX_LINES)
            {
                _lines.RemoveAt(_lines.Count - 1);
            }

            // Build text using StringBuilder for better performance
            _stringBuilder.Clear();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) _stringBuilder.Append("<br>");
                _stringBuilder.Append(_lines[i]);
            }
            
            m_TextMeshProUGUI.text = _stringBuilder.ToString();
        }
    }
}
