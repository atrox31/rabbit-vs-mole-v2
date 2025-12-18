using UnityEngine;

/// <summary>
/// Non-generic base class for all core components that need to be ready
/// before the game starts. Used for listing in the Bootstrap scene.
/// </summary>
public abstract class CoreBootstrapComponent : MonoBehaviour
{
    /// <summary>
    /// Abstract property that must be implemented by all derived classes (Singletons).
    /// Bootstrap scene will wait until this returns true.
    /// </summary>
    public abstract bool IsReady { get; }

    /// <summary>
    /// Abstract method for derived class initialization logic, called when the Singleton is set up.
    /// </summary>
    protected abstract void Initialize();
    public abstract void OnGameStart();
}