using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ensures there is always only one EventSystem in the scene.
/// Persists across scene loads and handles duplicate EventSystems automatically.
/// </summary>
public class EventSystemManager : MonoBehaviour
{
    private static EventSystemManager instance;
    private static EventSystem eventSystem;

    [Header("EventSystem Settings")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        // Check if we already have an instance
        if (instance != null && instance != this)
        {
            if (debugMode)
                // Debug.Log($"[EventSystemManager] Destroying duplicate EventSystemManager on {gameObject.name}");
            
            Destroy(gameObject);
            return;
        }

        // Set this as the singleton instance
        instance = this;

        // Make persistent across scenes if desired
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Ensure we have an EventSystem
        EnsureSingleEventSystem();
    }

    private void Start()
    {
        // Double-check on Start in case other EventSystems were created
        EnsureSingleEventSystem();
    }

    private void OnEnable()
    {
        // Subscribe to scene loaded event to handle new scenes
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from scene events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // When a new scene loads, ensure we still have only one EventSystem
        EnsureSingleEventSystem();
    }

    /// <summary>
    /// Ensures there is exactly one EventSystem in the scene
    /// </summary>
    private void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length == 0)
        {
            // No EventSystem found, create one
            CreateEventSystem();
        }
        else if (eventSystems.Length == 1)
        {
            // Perfect, we have exactly one
            eventSystem = eventSystems[0];
            
            // if (debugMode)
            // Debug.Log($"[EventSystemManager] Single EventSystem confirmed: {eventSystem.name}");
        }
        else
        {
            // Multiple EventSystems found, keep the first one and destroy the rest
            if (debugMode)
                // Debug.Log($"[EventSystemManager] Found {eventSystems.Length} EventSystems, removing duplicates");

            eventSystem = eventSystems[0];

            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (debugMode)
                    // Debug.Log($"[EventSystemManager] Destroying duplicate EventSystem: {eventSystems[i].name}");
                
                Destroy(eventSystems[i].gameObject);
            }
        }
    }

    /// <summary>
    /// Creates a new EventSystem with StandaloneInputModule
    /// </summary>
    private void CreateEventSystem()
    {
        // if (debugMode)
        // Debug.Log("[EventSystemManager] Creating new EventSystem");

        // Create EventSystem GameObject
        GameObject eventSystemGO = new GameObject("EventSystem");
        
        // Add EventSystem component
        eventSystem = eventSystemGO.AddComponent<EventSystem>();
        
        // Add StandaloneInputModule for handling input
        eventSystemGO.AddComponent<StandaloneInputModule>();

        // Make it persistent if this manager is persistent
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(eventSystemGO);
        }
    }

    /// <summary>
    /// Gets the current EventSystem instance
    /// </summary>
    public static EventSystem GetEventSystem()
    {
        if (instance != null)
        {
            instance.EnsureSingleEventSystem();
            return eventSystem;
        }
        return EventSystem.current;
    }

    /// <summary>
    /// Forces a check to ensure single EventSystem (useful for manual calls)
    /// </summary>
    public static void ValidateEventSystem()
    {
        if (instance != null)
        {
            instance.EnsureSingleEventSystem();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            eventSystem = null;
        }
    }
}