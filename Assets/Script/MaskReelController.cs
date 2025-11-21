using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MaskReelController : MonoBehaviour
{
    [Header("Reel Setup")]
    [SerializeField] private RectTransform symbolsContainer;
    [SerializeField] private GameObject maskSymbolPrefab;
    [SerializeField] private int totalSymbols = 31;
    [SerializeField] private int visibleSymbols = 4;
    [SerializeField] private float symbolSpacing = 100f;

    [Header("Animation")]
    [SerializeField] private Animator reelAnimator;
    [SerializeField] private string spinStopAnimationName = "MaskReel_SpinAndStop";
    [SerializeField] private string idleAnimationName = "MaskReel_Idle";

    [Header("Timing System")]
    [SerializeField] private bool useAnimationEvents = true;
    [SerializeField] private float fallbackSpinDuration = 4f;
    [SerializeField] private float maxWaitTime = 10f;

    [Header("Symbol Database")]
    [SerializeField] private MaskReelSymbolData[] maskSymbolDatabase;

    [Header("Target Symbol System")]
    public MaskReelSymbolData inspectorTargetSymbol; // Assign in Inspector
    [SerializeField] private bool useTargetSymbol = true; // Toggle to use target vs random
    [SerializeField] private bool logTargetChanges = true; // Log when target changes mid-spin
    [SerializeField] private float targetChangeThrottleTime = 0.1f; // Minimum time between changes

    // Core data
    private List<MaskReelSymbolData> reelStrip = new List<MaskReelSymbolData>();
    private List<GameObject> symbolObjects = new List<GameObject>();
    private MaskReelSymbolData currentTargetSymbol = null;
    private float lastTargetChangeTime = 0f;
    private int targetChangeCount = 0;
    private bool isSpinning = false;
    private bool waitingForAnimationComplete = false;

    // Result tracking
    private int selectedMultiplier = 1;
    private MaskReelSymbolData resultSymbol;

    // Events - needed for MaskReelClickHandler
    public System.Action OnSpinStart;
    public System.Action<int, MaskReelSymbolData> OnSpinComplete;
    public static MaskReelController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ValidateComponents();
        CreateInitialReelStrip();
        CreateSymbolObjects();
    }


    private void Start()
    {
        SetIdleState();
        LogCurrentReelState();
    }

    // ==========================================
    // TARGET SYMBOL SYSTEM
    // ==========================================

    /// <summary>
    /// Set the target symbol that will appear as the result
    /// Works mid-spin by updating both data and visual symbols
    /// Perfect for calling from Animation Events!
    /// </summary>
    public void SetTargetSymbol(MaskReelSymbolData symbol, bool forceUpdate = false)
    {
        // Optional throttling for frequent calls
        if (!forceUpdate && targetChangeThrottleTime > 0f)
        {
            float timeSinceLastChange = Time.time - lastTargetChangeTime;
            if (timeSinceLastChange < targetChangeThrottleTime)
            {
                return;
            }
        }

        MaskReelSymbolData previousTarget = currentTargetSymbol;
        currentTargetSymbol = symbol;
        lastTargetChangeTime = Time.time;

        if (symbol != null)
        {
            // Update the reel strip data
            if (reelStrip.Count >= 3)
            {
                reelStrip[2] = currentTargetSymbol;
            }

            // CRITICAL: Update the visual symbol that will be the result
            UpdateResultSymbolVisual();

            if (isSpinning)
            {
                targetChangeCount++;
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// Set target symbol without throttling - use for animation events
    /// </summary>
    public void SetTargetSymbolImmediate(MaskReelSymbolData symbol)
    {
        SetTargetSymbol(symbol, forceUpdate: true);
    }

    /// <summary>
    /// Apply the target symbol from Inspector settings or use random
    /// </summary>
    public void ApplyTargetFromInspector()
    {
        // In API mode, don't override target set by API
        if (IsAPIMode() && currentTargetSymbol != null)
        {
            return;
        }

        // Original inspector logic for demo mode
        if (useTargetSymbol && inspectorTargetSymbol != null)
        {
            SetTargetSymbol(inspectorTargetSymbol);
        }
        else
        {
            MaskReelSymbolData randomSymbol = GetRandomSymbol();
            SetTargetSymbol(randomSymbol);
        }
    }

    /// <summary>
    /// Update the visual symbol that will be the result
    /// </summary>
    private void UpdateResultSymbolVisual()
    {
        if (currentTargetSymbol == null || symbolObjects.Count == 0)
            return;

        // Update the symbol at index 2 (third symbol - the result position)
        if (symbolObjects.Count > 2)
        {
            GameObject targetSymbolObject = symbolObjects[2];
            UpdateSymbolObjectVisual(targetSymbolObject, currentTargetSymbol);
        }
    }

    /// <summary>
    /// Update a specific symbol GameObject to show new symbol data
    /// </summary>
    private void UpdateSymbolObjectVisual(GameObject symbolObj, MaskReelSymbolData newSymbolData)
    {
        if (symbolObj == null || newSymbolData == null) return;

        MaskReelSymbol symbolComponent = symbolObj.GetComponent<MaskReelSymbol>();
        if (symbolComponent != null)
        {
            symbolComponent.Initialize(newSymbolData);
        }
    }

    public void ClearTargetSymbol()
    {
        SetTargetSymbol(null);
    }

    // ==========================================
    // ANIMATION EVENTS - For calling from Animator
    // ==========================================

    /// <summary>
    /// Animation Event: Set target to a random symbol
    /// </summary>
    public void AnimationEvent_SetRandomTarget()
    {
        if (maskSymbolDatabase.Length > 0)
        {
            MaskReelSymbolData randomSymbol = maskSymbolDatabase[Random.Range(0, maskSymbolDatabase.Length)];
            SetTargetSymbolImmediate(randomSymbol);
        }
    }

    /// <summary>
    /// Animation Event: Set target to specific multiplier value
    /// </summary>
    public void AnimationEvent_SetTargetByMultiplier(int multiplierValue)
    {
        MaskReelSymbolData targetSymbol = GetSymbolByMultiplier(multiplierValue);
        if (targetSymbol != null)
        {
            SetTargetSymbolImmediate(targetSymbol);
        }
    }

    /// <summary>
    /// Animation Event: Set target to symbol by index in database
    /// </summary>
    public void AnimationEvent_SetTargetByIndex(int symbolIndex)
    {
        if (symbolIndex >= 0 && symbolIndex < maskSymbolDatabase.Length)
        {
            SetTargetSymbolImmediate(maskSymbolDatabase[symbolIndex]);
        }
    }

    /// <summary>
    /// Animation Event: Set target to highest value symbol
    /// </summary>
    public void AnimationEvent_SetHighValueTarget()
    {
        MaskReelSymbolData highestSymbol = GetHighestMultiplierSymbol();
        if (highestSymbol != null)
        {
            SetTargetSymbolImmediate(highestSymbol);
        }
    }

    /// <summary>
    /// Animation Event: Clear target (use random)
    /// </summary>
    public void AnimationEvent_ClearTarget()
    {
        SetTargetSymbolImmediate(null);
    }

    // Helper methods for animation events
    private MaskReelSymbolData GetSymbolByMultiplier(int multiplier)
    {
        foreach (MaskReelSymbolData symbol in maskSymbolDatabase)
        {
            if (symbol.multiplierValue == multiplier)
                return symbol;
        }
        return null;
    }

    private MaskReelSymbolData GetHighestMultiplierSymbol()
    {
        MaskReelSymbolData highest = null;
        int maxMultiplier = 0;

        foreach (MaskReelSymbolData symbol in maskSymbolDatabase)
        {
            if (symbol.multiplierValue > maxMultiplier)
            {
                maxMultiplier = symbol.multiplierValue;
                highest = symbol;
            }
        }

        return highest;
    }

    // ==========================================
    // REEL STRIP MANAGEMENT
    // ==========================================

    private void CreateInitialReelStrip()
    {
        reelStrip.Clear();

        for (int i = 0; i < totalSymbols; i++)
        {
            MaskReelSymbolData randomSymbol = GetRandomSymbol();
            if (randomSymbol != null)
            {
                reelStrip.Add(randomSymbol);
            }
        }

    }

    private MaskReelSymbolData GetRandomSymbol()
    {
        if (maskSymbolDatabase.Length == 0) return null;

        float totalWeight = maskSymbolDatabase.Sum(s => s.spawnWeight);
        float random = Random.Range(0, totalWeight);
        float current = 0;

        foreach (MaskReelSymbolData symbol in maskSymbolDatabase)
        {
            current += symbol.spawnWeight;
            if (random <= current)
                return symbol;
        }

        return maskSymbolDatabase[0];
    }

    // ==========================================
    // VISUAL SYMBOL MANAGEMENT
    // ==========================================

    private void CreateSymbolObjects()
    {
        // Clear existing objects
        foreach (GameObject obj in symbolObjects)
        {
            if (obj != null) Destroy(obj);
        }
        symbolObjects.Clear();

        // Create visual symbols
        for (int i = 0; i < totalSymbols; i++)
        {
            CreateSymbolObject(i);
        }
    }

    private void CreateSymbolObject(int index)
    {
        // GameObject symbolObj = Instantiate(maskSymbolPrefab, symbolsContainer);
        //
        // // Position the symbol
        // RectTransform symbolRect = symbolObj.GetComponent<RectTransform>();
        // symbolRect.anchoredPosition = new Vector2(0, index * symbolSpacing);
        //
        // // Set up the symbol data
        // MaskReelSymbolData symbolData = reelStrip[index % reelStrip.Count];
        // MaskReelSymbol symbolComponent = symbolObj.GetComponent<MaskReelSymbol>();
        //
        // if (symbolComponent == null)
        //     symbolComponent = symbolObj.AddComponent<MaskReelSymbol>();
        //
        // symbolComponent.Initialize(symbolData);
        // symbolObjects.Add(symbolObj);
    }
    public void SetTargetFromAPI(int apiMultiplierValue)
    {
        if (maskSymbolDatabase == null || maskSymbolDatabase.Length == 0)
        {
            return;
        }

        // Find symbol with matching multiplierValue
        MaskReelSymbolData targetSymbol = System.Array.Find(maskSymbolDatabase,
            symbol => symbol.multiplierValue == apiMultiplierValue);

        if (targetSymbol != null)
        {
            // Set as current target immediately
            SetTargetSymbol(targetSymbol);

            // Also update inspector field for consistency
            inspectorTargetSymbol = targetSymbol;

            // Override useTargetSymbol to ensure API target is used
            useTargetSymbol = true;

        }
        else
        {
            // Fallback to random if API symbol not found
            MaskReelSymbolData randomSymbol = GetRandomSymbol();
            SetTargetSymbol(randomSymbol);
        }
    }
    public bool IsAPIMode()
    {
        return WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode;
    }
    private void UpdateSymbolVisual(int index)
    {
        if (index < 0 || index >= symbolObjects.Count || index >= reelStrip.Count)
            return;

        GameObject symbolObj = symbolObjects[index];
        UpdateSymbolObjectVisual(symbolObj, reelStrip[index]);
    }

    // ==========================================
    // SPINNING SYSTEM
    // ==========================================

    public void StartSpin()
    {
        if (isSpinning)
        {
            return;
        }

        // Reset target change tracking
        targetChangeCount = 0;
        lastTargetChangeTime = 0f;

        // Apply Inspector target settings before spinning
        ApplyTargetFromInspector();

        StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SpinCoroutine()
    {
        isSpinning = true;
        waitingForAnimationComplete = true;

        // Fire spin start event
        OnSpinStart?.Invoke();

        // Start spinning sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMaskSpinning();
        }

        // Play spin animation
        if (reelAnimator != null)
        {
            reelAnimator.Play(idleAnimationName);
            yield return null;
            reelAnimator.Play(spinStopAnimationName);
        }

        float startTime = Time.time;

        if (useAnimationEvents)
        {
            // Wait for animation event or timeout
            while (waitingForAnimationComplete)
            {
                float elapsed = Time.time - startTime;
                if (elapsed >= maxWaitTime)
                {
                    waitingForAnimationComplete = false;
                    break;
                }
                yield return null;
            }
        }
        else
        {
            // Use fallback timer
            yield return new WaitForSeconds(fallbackSpinDuration);
        }

        // The result is always the third symbol (index 2)
        GetResult();
        CompleteSpin();
    }

    private void GetResult()
    {
        if (reelStrip.Count >= 3)
        {
            resultSymbol = reelStrip[2]; // Third symbol is always the result
            selectedMultiplier = resultSymbol.multiplierValue;

            string changeInfo = targetChangeCount > 0 ? $" (changed {targetChangeCount} times mid-spin)" : "";

            // Verify target symbol worked
            if (currentTargetSymbol != null)
            {
                bool success = resultSymbol.name == currentTargetSymbol.name;
            }
        }
        else
        {
            resultSymbol = null;
            selectedMultiplier = 1;
        }
    }

    private void CompleteSpin()
    {
        isSpinning = false;
        waitingForAnimationComplete = false;

        // Stop spinning sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMaskSpinning();
        }

        // Fire completion event - needed for MaskReelClickHandler
        OnSpinComplete?.Invoke(selectedMultiplier, resultSymbol);
    }

    // ==========================================
    // ANIMATION EVENTS
    // ==========================================

    public void OnReelStoppedAnimationEvent()
    {
        if (!useAnimationEvents) return;

        waitingForAnimationComplete = false;
    }

    // ==========================================
    // VISIBLE SYMBOL DETECTION
    // ==========================================

    /// <summary>
    /// Get the symbol that is currently visible in the center
    /// </summary>
    public MaskReelSymbolData GetVisibleSymbol()
    {
        if (!isSpinning && reelStrip.Count >= 3)
        {
            return reelStrip[2]; // Third symbol is always the visible/result symbol
        }

        return null;
    }

    /// <summary>
    /// Get multiple visible symbols
    /// </summary>
    public List<MaskReelSymbolData> GetVisibleSymbols(int count = 1)
    {
        List<MaskReelSymbolData> visible = new List<MaskReelSymbolData>();

        if (!isSpinning && reelStrip.Count >= 3)
        {
            int startIndex = 2 - (count / 2);

            for (int i = 0; i < count; i++)
            {
                int symbolIndex = startIndex + i;
                if (symbolIndex >= 0 && symbolIndex < reelStrip.Count)
                {
                    visible.Add(reelStrip[symbolIndex]);
                }
            }
        }

        return visible;
    }

    // ==========================================
    // UTILITY METHODS
    // ==========================================

    private void ValidateComponents()
    {
        if (symbolsContainer == null || maskSymbolPrefab == null)
        {
            enabled = false;
        }

        if (reelAnimator == null)
            reelAnimator = GetComponent<Animator>();
    }

    private void SetIdleState()
    {
        if (reelAnimator != null)
        {
            reelAnimator.Play(idleAnimationName);
        }
        waitingForAnimationComplete = false;
    }

    public void ResetReel()
    {
        if (isSpinning)
        {
            StopAllCoroutines();
            isSpinning = false;
            waitingForAnimationComplete = false;
        }

        SetIdleState();
        currentTargetSymbol = null;
        selectedMultiplier = 1;
        resultSymbol = null;
    }

    /// <summary>
    /// Force reset if the reel gets stuck - needed for MaskReelClickHandler
    /// </summary>
    public void ForceReset()
    {
        StopAllCoroutines();
        isSpinning = false;
        waitingForAnimationComplete = false;

        // Stop any sounds
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMaskSpinning();
        }

        SetIdleState();
    }

    private void LogCurrentReelState()
    {
        for (int i = 0; i < Mathf.Min(5, reelStrip.Count); i++)
        {
            string marker = i == 2 ? " <- RESULT POSITION" : "";
        }
    }

    // ==========================================
    // PUBLIC GETTERS
    // ==========================================

    public bool IsSpinning() => isSpinning;
    public int GetSelectedMultiplier() => selectedMultiplier;
    public MaskReelSymbolData GetResultSymbol() => resultSymbol;
    public bool HasTargetSymbol() => currentTargetSymbol != null;
    public MaskReelSymbolData GetTargetSymbol() => currentTargetSymbol;
    public bool IsUsingInspectorTarget() => useTargetSymbol;
    public MaskReelSymbolData GetInspectorTargetSymbol() => inspectorTargetSymbol;
    public int GetTargetChangeCount() => targetChangeCount;

    // ==========================================
    // DEBUG METHODS
    // ==========================================

    [ContextMenu("Test Mid-Spin Target Change")]
    public void TestMidSpinChange()
    {
        if (isSpinning && maskSymbolDatabase.Length > 0)
        {
            MaskReelSymbolData randomSymbol = maskSymbolDatabase[Random.Range(0, maskSymbolDatabase.Length)];
            SetTargetSymbolImmediate(randomSymbol);
        }
    }

    [ContextMenu("Set Random Target")]
    public void SetRandomTarget()
    {
        if (maskSymbolDatabase.Length > 0)
        {
            MaskReelSymbolData randomTarget = maskSymbolDatabase[Random.Range(0, maskSymbolDatabase.Length)];
            SetTargetSymbol(randomTarget);
        }
    }

    [ContextMenu("Apply Inspector Target")]
    public void ApplyInspectorTarget()
    {
        ApplyTargetFromInspector();
        LogReelState();
    }

    [ContextMenu("Test Spin")]
    public void TestSpin()
    {
        if (!isSpinning)
        {
            StartSpin();
        }
    }

    [ContextMenu("Log Reel State")]
    public void LogReelState()
    {
        LogCurrentReelState();
    }
}