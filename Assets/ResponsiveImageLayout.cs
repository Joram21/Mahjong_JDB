using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ImageLayoutSettings
{
    [Header("Height Settings")]
    public float height = 100f;

    [Header("Position Settings")]
    public float yPosition = 0f;

    [Header("Optional: Width Settings")]
    public bool adjustWidth = false;
    public float width = 100f;
}

public class ResponsiveImageLayout : MonoBehaviour
{
    [Header("Layout Settings by Screen Category")]
    public ImageLayoutSettings portraitSettings = new ImageLayoutSettings();
    public ImageLayoutSettings tabletSettings = new ImageLayoutSettings();
    public ImageLayoutSettings landscapeSettings = new ImageLayoutSettings();

    [Header("Animation Settings")]
    public bool useAnimation = true;
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    public bool debugMode = false;

    private RectTransform rectTransform;
    private Image image;
    private ViewMan viewManager;
    private Coroutine animationCoroutine;
    private ScreenCategory lastAppliedCategory = (ScreenCategory)(-1); // Invalid initial value

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        // Find ViewManager in scene
        viewManager = FindObjectOfType<ViewMan>();

        if (rectTransform == null)
        {
            // Debug.LogError($"ResponsiveImageLayout on {gameObject.name} requires a RectTransform component!");
        }
    }

    void Start()
    {
        // Apply initial layout
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        if (rectTransform == null || viewManager == null) return;

        ScreenCategory currentCategory = viewManager.CurrentScreenCategory;

        // Skip if same category as last applied (prevents unnecessary updates)
        if (lastAppliedCategory == currentCategory) return;

        ImageLayoutSettings targetSettings = GetSettingsForCategory(currentCategory);

        if (useAnimation && Application.isPlaying)
        {
            AnimateToLayout(targetSettings);
        }
        else
        {
            ApplyLayoutImmediate(targetSettings);
        }

        lastAppliedCategory = currentCategory;

        if (debugMode)
        {
            // Debug.Log($"ResponsiveImageLayout on {gameObject.name} applied {currentCategory} layout: Height={targetSettings.height}, Y={targetSettings.yPosition}");
        }
    }

    private ImageLayoutSettings GetSettingsForCategory(ScreenCategory category)
    {
        switch (category)
        {
            case ScreenCategory.Portrait:
                return portraitSettings;
            case ScreenCategory.Tablet:
                return tabletSettings;
            case ScreenCategory.Landscape:
                return landscapeSettings;
            default:
                return portraitSettings;
        }
    }

    private void ApplyLayoutImmediate(ImageLayoutSettings settings)
    {
        Vector2 sizeDelta = rectTransform.sizeDelta;
        Vector3 anchoredPosition = rectTransform.anchoredPosition;

        // Apply height
        sizeDelta.y = settings.height;

        // Apply width if enabled
        if (settings.adjustWidth)
        {
            sizeDelta.x = settings.width;
        }

        // Apply Y position
        anchoredPosition.y = settings.yPosition;

        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void AnimateToLayout(ImageLayoutSettings targetSettings)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateLayoutCoroutine(targetSettings));
    }

    private System.Collections.IEnumerator AnimateLayoutCoroutine(ImageLayoutSettings targetSettings)
    {
        Vector2 startSize = rectTransform.sizeDelta;
        Vector3 startPosition = rectTransform.anchoredPosition;

        Vector2 targetSize = new Vector2(
            targetSettings.adjustWidth ? targetSettings.width : startSize.x,
            targetSettings.height
        );
        Vector3 targetPosition = new Vector3(startPosition.x, targetSettings.yPosition, startPosition.z);

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float normalizedTime = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(normalizedTime);

            // Interpolate size
            Vector2 currentSize = Vector2.Lerp(startSize, targetSize, curveValue);
            rectTransform.sizeDelta = currentSize;

            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            rectTransform.anchoredPosition = currentPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are exact
        rectTransform.sizeDelta = targetSize;
        rectTransform.anchoredPosition = targetPosition;

        animationCoroutine = null;
    }

    [ContextMenu("Apply Current Layout")]
    public void ForceApplyLayout()
    {
        lastAppliedCategory = (ScreenCategory)(-1); // Reset to force update
        ApplyLayout();
    }

    [ContextMenu("Test Portrait Layout")]
    public void TestPortraitLayout()
    {
        ApplyLayoutImmediate(portraitSettings);
    }

    [ContextMenu("Test Tablet Layout")]
    public void TestTabletLayout()
    {
        ApplyLayoutImmediate(tabletSettings);
    }

    [ContextMenu("Test Landscape Layout")]
    public void TestLandscapeLayout()
    {
        ApplyLayoutImmediate(landscapeSettings);
    }
}

