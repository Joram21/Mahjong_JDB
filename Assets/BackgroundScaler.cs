using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] private RectTransform backgroundRectTransform;

    [Header("Debug Info")]
    [SerializeField] private Vector2 currentScreenSize;
    [SerializeField] private Vector2 currentScale;
    [SerializeField] private float scaleRatio;

    private ViewMan viewMan;
    private Vector2 lastScreenSize;

    private void Start()
    {
        // Find ViewMan if not assigned
        if (viewMan == null)
            viewMan = FindAnyObjectByType<ViewMan>();

        // Get background RectTransform if not assigned
        if (backgroundRectTransform == null)
            backgroundRectTransform = GetComponent<RectTransform>();

        // Initial scale calculation
        UpdateScale();
    }

    private void Update()
    {
        currentScreenSize = new Vector2(Screen.width, Screen.height);

        // Only update if screen size changed
        if (currentScreenSize != lastScreenSize)
        {
            UpdateScale();
            lastScreenSize = currentScreenSize;
        }
    }

    private void UpdateScale()
    {
        if (backgroundRectTransform == null) return;

        // Ensure we have valid screen dimensions
        currentScreenSize = new Vector2(Screen.width, Screen.height);

        // Safety check for valid dimensions
        if (currentScreenSize.x <= 0 || currentScreenSize.y <= 0)
        {
            // Debug.LogWarning("Invalid screen dimensions, skipping scale update");
            return;
        }

        // Calculate aspect ratios
        float referenceAspect = referenceResolution.x / referenceResolution.y; // 1920/1080 = 1.78
        float currentAspect = currentScreenSize.x / currentScreenSize.y;

        // Calculate scales based on your specific requirements
        float scaleX, scaleY;
        if (currentAspect < referenceAspect) // Portrait or narrower than reference
        {
            // X scale increases to compensate for width reduction
            scaleX = referenceAspect / currentAspect;
            scaleY = 1f;
        }
        else // Landscape or wider than reference
        {
            // Normal scaling for landscape
            scaleX = 1f;
            scaleY = currentAspect / referenceAspect;
        }

        // Validate scale values before applying
        if (float.IsNaN(scaleX) || float.IsNaN(scaleY) ||
            float.IsInfinity(scaleX) || float.IsInfinity(scaleY))
        {
            // Debug.LogError("Invalid scale calculated, using default values");
            scaleX = 1f;
            scaleY = 1f;
        }

        // Apply the calculated scales
        currentScale = new Vector2(scaleX, scaleY);
        backgroundRectTransform.localScale = currentScale;

        // Store ratio for debugging
        scaleRatio = scaleX;
    }

    // Alternative method if you want the specific behavior from your examples
    // where X and Y can have different scales
    private void UpdateScaleAlternative()
    {
        if (backgroundRectTransform == null) return;

        // Calculate individual scale factors
        float scaleX = referenceResolution.x / currentScreenSize.x;
        float scaleY = referenceResolution.y / currentScreenSize.y;

        // Apply different scales to X and Y (this will distort the image)
        currentScale = new Vector2(scaleX, scaleY);
        backgroundRectTransform.localScale = currentScale;
    }

    // Method to manually refresh scale (can be called from ViewMan)
    public void RefreshScale()
    {
        UpdateScale();
    }

    // Editor helper to set reference resolution to current screen size
    [ContextMenu("Set Reference to Current Screen")]
    private void SetReferenceToCurrentScreen()
    {
        referenceResolution = new Vector2(Screen.width, Screen.height);
        UpdateScale();
    }
}