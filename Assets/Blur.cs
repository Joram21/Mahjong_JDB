using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Blur: MonoBehaviour
{
    [Header("Blur Panel Setup")]
    [SerializeField] private Image darkOverlay;        // Semi-transparent dark layer
    [SerializeField] private Image glassPanel;         // Main panel with glass effect
    [SerializeField] private Image noiseOverlay;       // Subtle texture overlay
    [SerializeField] private CanvasGroup[] backgroundCanvasGroups; // Canvas groups to dim

    [Header("Visual Settings")]
    [SerializeField][Range(0f, 1f)] private float darkOverlayAlpha = 0.6f;
    [SerializeField][Range(0f, 1f)] private float glassPanelAlpha = 0.15f;
    [SerializeField][Range(0f, 1f)] private float noiseIntensity = 0.1f;
    [SerializeField][Range(0f, 1f)] private float backgroundDimAmount = 0.4f;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateNoise = true;
    [SerializeField] private float noiseAnimationSpeed = 0.5f;

    private Color originalDarkColor;
    private Color originalGlassColor;
    private Color originalNoiseColor;
    private Coroutine animationCoroutine;
    private Material noiseMaterial;

    void Awake()
    {
        SetupColors();
        SetupNoiseMaterial();

        // Start hidden
        SetVisibility(0f);
    }

    void SetupColors()
    {
        if (darkOverlay != null)
            originalDarkColor = new Color(0f, 0f, 0f, darkOverlayAlpha);

        if (glassPanel != null)
            originalGlassColor = new Color(1f, 1f, 1f, glassPanelAlpha);

        if (noiseOverlay != null)
            originalNoiseColor = new Color(1f, 1f, 1f, noiseIntensity);
    }

    void SetupNoiseMaterial()
    {
        if (noiseOverlay != null)
        {
            // Create a material instance for the noise overlay
            noiseMaterial = new Material(Shader.Find("UI/Default"));
            noiseOverlay.material = noiseMaterial;
        }
    }

    void OnEnable()
    {
        ShowBlur();
    }

    void OnDisable()
    {
        HideBlur();
    }

    public void ShowBlur()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateBlurIn());
    }

    public void HideBlur()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateBlurOut());
    }

    IEnumerator AnimateBlurIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = fadeInCurve.Evaluate(elapsed / fadeInDuration);

            SetVisibility(progress);
            DimBackgroundCanvases(progress);

            yield return null;
        }

        SetVisibility(1f);
        DimBackgroundCanvases(1f);

        if (animateNoise)
            StartCoroutine(AnimateNoise());
    }

    IEnumerator AnimateBlurOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = 1f - fadeInCurve.Evaluate(elapsed / fadeInDuration);

            SetVisibility(progress);
            DimBackgroundCanvases(progress);

            yield return null;
        }

        SetVisibility(0f);
        DimBackgroundCanvases(0f);
    }

    void SetVisibility(float alpha)
    {
        if (darkOverlay != null)
        {
            Color color = originalDarkColor;
            color.a *= alpha;
            darkOverlay.color = color;
        }

        if (glassPanel != null)
        {
            Color color = originalGlassColor;
            color.a *= alpha;
            glassPanel.color = color;
        }

        if (noiseOverlay != null)
        {
            Color color = originalNoiseColor;
            color.a *= alpha;
            noiseOverlay.color = color;
        }
    }

    void DimBackgroundCanvases(float progress)
    {
        foreach (var canvasGroup in backgroundCanvasGroups)
        {
            if (canvasGroup != null)
            {
                float targetAlpha = 1f - (backgroundDimAmount * progress);
                canvasGroup.alpha = targetAlpha;

                // Optionally disable interaction
                canvasGroup.interactable = progress < 0.5f;
            }
        }
    }

    IEnumerator AnimateNoise()
    {
        if (noiseOverlay == null || noiseMaterial == null) yield break;

        Vector2 offset = Vector2.zero;

        while (gameObject.activeInHierarchy)
        {
            offset += Vector2.one * noiseAnimationSpeed * Time.unscaledDeltaTime;
            noiseMaterial.SetTextureOffset("_MainTex", offset);
            yield return null;
        }
    }

    // Public methods to adjust settings at runtime
    public void SetDarkOverlayAlpha(float alpha)
    {
        darkOverlayAlpha = Mathf.Clamp01(alpha);
        originalDarkColor.a = darkOverlayAlpha;
    }

    public void SetGlassPanelAlpha(float alpha)
    {
        glassPanelAlpha = Mathf.Clamp01(alpha);
        originalGlassColor.a = glassPanelAlpha;
    }

    public void SetBackgroundDimAmount(float amount)
    {
        backgroundDimAmount = Mathf.Clamp01(amount);
    }

    void OnDestroy()
    {
        if (noiseMaterial != null)
            DestroyImmediate(noiseMaterial);
    }
}