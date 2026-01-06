using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Small logging helper intended for shipping builds:
/// - <see cref="Log"/> is compiled out in non-dev builds
/// - <see cref="LogWarning"/> and <see cref="LogError"/> always remain
/// </summary>
public static class DebugHelper
{
    private static string Format(MonoBehaviour sourceObject, string message, string memberName, int lineNumber)
    {
        string objectName = sourceObject != null ? sourceObject.gameObject.name : "undefined";
        return $"[{objectName} -> {memberName}:{lineNumber}] {message}";
    }

    [HideInCallstack]
    public static void LogError(
        MonoBehaviour sourceObject,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Debug.LogError(Format(sourceObject, message, memberName, lineNumber), sourceObject ? sourceObject.gameObject : null);
    }

    [HideInCallstack]
    public static void LogWarning(
        MonoBehaviour sourceObject,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Debug.LogWarning(Format(sourceObject, message, memberName, lineNumber), sourceObject ? sourceObject.gameObject : null);
    }

    /// <summary>
    /// Plain logs are dev-only (compiled out in Shipping).
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    [HideInCallstack]
    public static void Log(
        MonoBehaviour sourceObject,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Debug.Log(Format(sourceObject, message, memberName, lineNumber), sourceObject ? sourceObject.gameObject : null);
    }
}