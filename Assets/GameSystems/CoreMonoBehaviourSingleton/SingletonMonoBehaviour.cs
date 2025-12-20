/// <summary>
/// Generic Singleton implementation that forces core components to implement
/// the CoreBootstrapComponent base class logic.
/// </summary>
public abstract class SingletonMonoBehaviour<T> : CoreBootstrapComponent where T : SingletonMonoBehaviour<T>
{
    private static T _instance;
    public static T Instance { get { return _instance; } }

    private static bool _isReady = false;
    public override bool IsReady => (_instance != null && _isReady);


    /// <summary>
    /// Called when the script instance is being loaded.
    /// Manages the Singleton lifecycle.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = (T)this;
            Initialize();
            _isReady = true;
        }
        else if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }
    }

    // The abstract Initialize method from CoreBootstrapComponent is implemented here
    // with a call to a new abstract/virtual method for derived class logic.
    protected sealed override void Initialize()
    {
        // This method is called once by Awake when the Singleton is set.
        // It then delegates to the Ready method which is where the derived class
        // puts its initialization logic.
        Ready();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Method for derived classes to place their initialization logic.
    /// This is called automatically from Awake, once the Singleton is set up.
    /// </summary>
    protected abstract void Ready();
}