using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MaskReel : MonoBehaviour
{
    [Header("Reel Configuration")]
    [SerializeField] private RectTransform symbolContainer;
    [SerializeField] private GameObject multiplierSymbolPrefab;
    [SerializeField] private float symbolSpacing = 100f;
    [SerializeField] private int totalSymbols = 10;

    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 1000f;
    [SerializeField] private float spinDuration = 2f;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Multiplier Values")]
    [SerializeField] private int[] multiplierValues = { 2, 3, 5, 10, 15, 20, 25, 50 };
    [SerializeField] private float[] multiplierWeights = { 30, 25, 20, 15, 5, 3, 1.5f, 0.5f };

    [Header("Visual Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1f;

    // Private variables
    private List<GameObject> symbols = new List<GameObject>();
    private bool isSpinning = false;
    private int selectedMultiplier = 2;

    // Events
    public System.Action<int> OnSpinComplete;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        CreateSymbols();

        // Start invisible for fade-in effect
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // Fade in when instantiated
        StartCoroutine(FadeIn());
    }

    private void CreateSymbols()
    {
        // Clear existing symbols
        foreach (GameObject symbol in symbols)
        {
            if (symbol != null)
                Destroy(symbol);
        }
        symbols.Clear();

        // Create symbols in a vertical line
        for (int i = 0; i < totalSymbols; i++)
        {
            GameObject symbolObj = Instantiate(multiplierSymbolPrefab, symbolContainer);

            // Position symbols vertically
            RectTransform symbolRect = symbolObj.GetComponent<RectTransform>();
            symbolRect.anchoredPosition = new Vector2(0, i * symbolSpacing);

            // Assign random multiplier value
            int multiplierValue = GetWeightedRandomMultiplier();
            SetupSymbolMultiplier(symbolObj, multiplierValue);

            symbols.Add(symbolObj);
        }
    }

    private int GetWeightedRandomMultiplier()
    {
        if (multiplierValues.Length != multiplierWeights.Length)
        {
            // Debug.LogError("Multiplier values and weights arrays must have same length!");
            return multiplierValues[0];
        }

        // Calculate total weight
        float totalWeight = 0f;
        for (int i = 0; i < multiplierWeights.Length; i++)
        {
            totalWeight += multiplierWeights[i];
        }

        // Get random value
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // Find corresponding multiplier
        for (int i = 0; i < multiplierValues.Length; i++)
        {
            currentWeight += multiplierWeights[i];
            if (randomValue <= currentWeight)
            {
                return multiplierValues[i];
            }
        }

        // Fallback
        return multiplierValues[0];
    }

    private void SetupSymbolMultiplier(GameObject symbolObj, int multiplierValue)
    {
        // Set up the symbol to display the multiplier value
        Text multiplierText = symbolObj.GetComponentInChildren<Text>();
        if (multiplierText != null)
        {
            multiplierText.text = multiplierValue + "x";
        }

        // You can also set up images, colors, etc. based on multiplier value
        Image symbolImage = symbolObj.GetComponent<Image>();
        if (symbolImage != null)
        {
            // Change color based on multiplier rarity
            symbolImage.color = GetColorForMultiplier(multiplierValue);
        }

        // Store the multiplier value in a component for later retrieval
        MultiplierSymbol multiplierSymbol = symbolObj.GetComponent<MultiplierSymbol>();
        if (multiplierSymbol == null)
            multiplierSymbol = symbolObj.AddComponent<MultiplierSymbol>();

        multiplierSymbol.multiplierValue = multiplierValue;
    }

    private Color GetColorForMultiplier(int multiplier)
    {
        // Return different colors based on multiplier value
        if (multiplier >= 50) return Color.red;      // Very rare
        if (multiplier >= 20) return Color.magenta;  // Rare
        if (multiplier >= 10) return Color.yellow;   // Uncommon
        if (multiplier >= 5) return Color.cyan;      // Common
        return Color.white;                          // Basic
    }

    public void StartSpin()
    {
        if (isSpinning) return;

        StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SpinCoroutine()
    {
        isSpinning = true;

        // Select target multiplier
        selectedMultiplier = GetWeightedRandomMultiplier();
        // Debug.Log($"[MaskReel] Target multiplier: {selectedMultiplier}x");

        float elapsedTime = 0f;
        Vector2 startPosition = symbolContainer.anchoredPosition;

        // Calculate spin distance (multiple full rotations plus final position)
        float fullRotations = 3f; // Number of full spins
        float finalOffset = Random.Range(0f, symbolSpacing); // Random final position
        float totalDistance = (fullRotations * totalSymbols * symbolSpacing) + finalOffset;

        while (elapsedTime < spinDuration)
        {
            float progress = elapsedTime / spinDuration;
            float curveValue = spinCurve.Evaluate(progress);

            float currentDistance = curveValue * totalDistance;
            Vector2 newPosition = startPosition + Vector2.down * currentDistance;

            symbolContainer.anchoredPosition = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap to final position showing selected multiplier
        SnapToSelectedMultiplier();

        isSpinning = false;

        // Wait a moment then trigger completion
        yield return new WaitForSeconds(0.5f);

        OnSpinComplete?.Invoke(selectedMultiplier);

        // Debug.Log($"[MaskReel] Spin complete! Selected: {selectedMultiplier}x");
    }

    private void SnapToSelectedMultiplier()
    {
        // Find a symbol with the selected multiplier and position it in the center
        GameObject targetSymbol = null;

        foreach (GameObject symbol in symbols)
        {
            MultiplierSymbol multiplierSymbol = symbol.GetComponent<MultiplierSymbol>();
            if (multiplierSymbol != null && multiplierSymbol.multiplierValue == selectedMultiplier)
            {
                targetSymbol = symbol;
                break;
            }
        }

        if (targetSymbol != null)
        {
            // Position the container so the target symbol is centered
            Vector2 targetLocalPos = targetSymbol.GetComponent<RectTransform>().anchoredPosition;
            symbolContainer.anchoredPosition = -targetLocalPos;

            // Highlight the winning symbol
            HighlightSymbol(targetSymbol);
        }
    }

    private void HighlightSymbol(GameObject symbol)
    {
        // Add visual highlight to the winning symbol
        Image symbolImage = symbol.GetComponent<Image>();
        if (symbolImage != null)
        {
            StartCoroutine(PulseSymbol(symbolImage));
        }
    }

    private IEnumerator PulseSymbol(Image symbolImage)
    {
        Color originalColor = symbolImage.color;
        float pulseSpeed = 3f;
        float pulses = 3f;

        for (float i = 0; i < pulses; i++)
        {
            // Pulse to bright
            float elapsedTime = 0f;
            while (elapsedTime < (1f / pulseSpeed))
            {
                float alpha = Mathf.Lerp(1f, 0.3f, elapsedTime * pulseSpeed);
                symbolImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Pulse back
            elapsedTime = 0f;
            while (elapsedTime < (1f / pulseSpeed))
            {
                float alpha = Mathf.Lerp(0.3f, 1f, elapsedTime * pulseSpeed);
                symbolImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // Return to original color
        symbolImage.color = originalColor;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            canvasGroup.alpha = alpha;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void FadeOut(System.Action onComplete = null)
    {
        StartCoroutine(FadeOutCoroutine(onComplete));
    }

    private IEnumerator FadeOutCoroutine(System.Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < fadeInDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeInDuration);
            canvasGroup.alpha = alpha;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    public int GetSelectedMultiplier()
    {
        return selectedMultiplier;
    }
}

// Simple component to store multiplier value on symbol GameObjects
public class MultiplierSymbol : MonoBehaviour
{
    public int multiplierValue = 2;
}