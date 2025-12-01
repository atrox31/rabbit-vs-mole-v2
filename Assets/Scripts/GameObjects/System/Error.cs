using UnityEngine;

public static class Error
{
    public static bool Message(string message)
    {
        Debug.LogError(message);
        return false;
    }
}