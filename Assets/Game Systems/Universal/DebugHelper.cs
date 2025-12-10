using System.Diagnostics;
using UnityEngine;

public static class DebugHelper
{
    /// <summary>
    /// Logs an error message to the Unity Console, including the name of the source object and the calling method.
    /// </summary>
    /// <remarks>The log entry includes the name of the source object's GameObject and the fully qualified
    /// name of the method that called this method. This provides additional context for debugging by identifying the
    /// source of the error.</remarks>
    /// <param name="sourceObject">The <see cref="MonoBehaviour"/> instance associated with the error. This object will be highlighted in the Unity
    /// Editor when the error is clicked in the Console.</param>
    /// <param name="message">The error message to log.</param>
    public static void LogError(MonoBehaviour sourceObject, string message)
    {
        // 1. Get the calling method's name
        // StackFrame(1) looks one level up the call stack (i.e., the method that called THIS method).
        var method = new StackFrame().GetMethod();
        string methodName = method.DeclaringType.Name + "." + method.Name;

        // 2. Format the output string
        string formattedMessage = $"[{sourceObject.gameObject.name} -> {methodName}] {message}";

        // 3. Call the standard Unity Debug.LogError, passing the object context 
        // to enable double-clicking the console error to highlight the object.
        UnityEngine.Debug.LogError(formattedMessage, sourceObject.gameObject);
    }
    
    /// <summary>
    /// Logs a formatted message to the Unity console, including the name of the source object and the calling method.
    /// </summary>
    /// <remarks>The log message is formatted to include the name of the source object's GameObject and the
    /// fully qualified name of the method that called this method. This allows for easier debugging by providing
    /// context about where the log entry originated.</remarks>
    /// <param name="sourceObject">The <see cref="MonoBehaviour"/> instance associated with the log entry. This object is used as the context for
    /// the log message.</param>
    /// <param name="message">The message to log. This will be prefixed with the source object's name and the calling method's name.</param>
    public static void Log(MonoBehaviour sourceObject, string message)
    {
        // 1. Get the calling method's name
        // StackFrame(1) looks one level up the call stack (i.e., the method that called THIS method).
        var method = new StackFrame(1).GetMethod();
        string methodName = method.DeclaringType.Name + "." + method.Name;

        // 2. Format the output string
        string formattedMessage = $"[{sourceObject.gameObject.name} -> {methodName}] {message}";

        // 3. Call the standard Unity Debug.LogError, passing the object context 
        // to enable double-clicking the console error to highlight the object.
        UnityEngine.Debug.Log(formattedMessage, sourceObject.gameObject);
    }

    /// <summary>
    /// Logs a warning message to the Unity Console, including the name of the source object and the calling method.
    /// </summary>
    /// <remarks>The log entry includes the name of the GameObject associated with <paramref
    /// name="sourceObject"/>  and the fully qualified name of the method that invoked <see cref="LogWarning"/>.  This
    /// additional context can help identify the source of the warning during debugging.</remarks>
    /// <param name="sourceObject">The <see cref="MonoBehaviour"/> instance that serves as the context for the log entry.  This object will be
    /// highlighted in the Unity Editor when the log message is clicked.</param>
    /// <param name="message">The warning message to log. This message will be prefixed with the name of the source object  and the calling
    /// method for additional context.</param>
    public static void LogWarning(MonoBehaviour sourceObject, string message)
    {
        // 1. Get the calling method's name
        // StackFrame(1) looks one level up the call stack (i.e., the method that called THIS method).
        var method = new StackFrame(1).GetMethod();
        string methodName = method.DeclaringType.Name + "." + method.Name;

        // 2. Format the output string
        string formattedMessage = $"[{sourceObject.gameObject.name} -> {methodName}] {message}";

        // 3. Call the standard Unity Debug.LogError, passing the object context 
        // to enable double-clicking the console error to highlight the object.
        UnityEngine.Debug.LogWarning(formattedMessage, sourceObject.gameObject);
    }
}