using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class FreeSpinSlotMachineController : MonoBehaviour
{
    // Singleton pattern implementation
    private static FreeSpinSlotMachineController _instance;
    public static FreeSpinSlotMachineController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FreeSpinSlotMachineController>();
            }
            return _instance;
        }
    }

    [Header("References")]
    [SerializeField] private SlotReel[] reels;
    [SerializeField] private WinCalculator winCalculator;
    public BetManager betManager;
    [SerializeField] private WinAnimationController winAnimationController;
    [SerializeField] private AutoSpinManager autoSpinManager;
    [SerializeField] private GameObject freeSpinReels;

    [Header("Spin Timing Settings")]
    [SerializeField] private float[] reelStopDelays = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }; // Normal delays
    [SerializeField] private float[] fastReelStopDelays = { 0.4f, 0.4f, 0.4f, 0.4f, 0.4f }; // Fast delays
    [SerializeField] private float baseSpinDuration = 1.0f; // Minimum time all reels spin
    [SerializeField] private float fastModeMultiplier = 0.75f; // Multiplier for fast mode (60% of normal time)
    [SerializeField] private float fastModeStopDelayMultiplier = 1f;
    [SerializeField] private float normalSpinDuration = 2.0f;
    [SerializeField] private float fastSpinDuration = 1.0f;
    public SymbolData[] symbolDatabase;

    [Header("Free Spin Specific Settings")]
    [SerializeField] private bool enableFreeSpinRetrigger = true;
    [SerializeField] private float freeSpinDelayBetweenSpins = 0.3f;
    [SerializeField] private bool enableMaskTransformation = true; // NEW: Enable mask transformation
    [SerializeField] private float maskTransformationDelay = 1.5f; // NEW: Delay before showing transformation UI

    [Header("Demo Mode Symbol Settings")]
    [SerializeField] private bool useRandomSymbolsInDemo = true;

    [Header("API Response Validation")]
    [SerializeField] private float maxSpinTimeBeforeTimeout = 60f; // Maximum time to wait for API response
    [SerializeField] private bool debugAPIResponse = true;

    // State management
    private bool isSpinning;
    public bool isFastMode = false;
    private bool stopRequested = false;
    private string currentTransactionID;

    // Dynamic stopping management (copied from original)
    private float spinStartTimeStamp = 0f;
    private bool[] reelStopScheduled;
    private float[] reelStopTimes;
    private Dictionary<int, Coroutine> reelStopCoroutines;
    private bool[] reelsInTensionMode;
    private Dictionary<int, float> tensionCompletionTimes;
    private Coroutine activeSpinCoroutine;
    private Coroutine activeStoppingManagerCoroutine;

    // Free spin detection for retriggering
    private bool freeSpinDetectedOnReel3 = false;
    private bool freeSpinDetectedOnReel4 = false;
    private bool freeSpinDetectedOnReel5 = false;

    // Properties
    public bool IsSpinning => isSpinning;
    public string CurrentTransactionID => currentTransactionID;

    // Events
    public event Action OnSpinStart;
    public event Action OnSpinComplete;

    [Header("API Integration - ENHANCED")]
    private bool apiResponseReceived = false;
    private bool waitingForAPIResponse = false;
    private float spinStartTime = 0f;
    private Coroutine apiTimeoutCoroutine;

    [Header("UI References")]
    public UIElementManager uiElements;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Initialize stopping management arrays
        reelStopScheduled = new bool[5];
        reelStopTimes = new float[5];
        reelStopCoroutines = new Dictionary<int, Coroutine>();
        reelsInTensionMode = new bool[5];
        tensionCompletionTimes = new Dictionary<int, float>();
    }

    private void Start()
    {
        // Subscribe to reel events for free spin detection
        for (int i = 0; i < reels.Length; i++)
        {
            int reelIndex = i;
            reels[i].OnFreeSpinSymbolDetected += (detectedReelIndex) => HandleEarlyFreeSpinDetection(reelIndex);
            reels[i].OnSpinEnd += () => CheckReelForFreeSpin(reelIndex);
        }

        UpdateAllSymbolsForFreeSpinMode(true);
    }

    // Method to update all symbols for free spin mode - ENHANCED
    private void UpdateAllSymbolsForFreeSpinMode(bool freeSpinMode)
    {
        // Debug.Log($"[FreeSpinSlotMachineController] Updating all symbols for free spin mode: {freeSpinMode}");

        int symbolsUpdated = 0;
        foreach (SlotReel reel in reels)
        {
            if (reel == null || reel.symbolsContainer == null) continue;

            foreach (Transform child in reel.symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null)
                {
                    symbol.UpdateVisualForFreeSpins(freeSpinMode);
                    symbolsUpdated++;
                }
            }
        }
    }

    // Start a free spin
    public void StartFreeSpin()
    {
        if (isSpinning)
        {
            return;
        }

        ResetAPIResponseState();

        ResetFreeSpinDetectionFlags();

        StartCoroutine(SpinAllReels());

        if (WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode)
        {
            WebManAPI.Instance.InitiateFreeSpinRequest();
        }

        if (WebManAPI.Instance.isDemoMode)
        {
            WebManAPI.Instance.InitiateFreeSpinRequest();
        }
    }

    // RESTORED: Original method name for backward compatibility
    public void StartAllReels()
    {
        StartFreeSpin();
    }

    /// <summary>
    /// ENHANCED: Reset API response state for new spin
    /// </summary>
    private void ResetAPIResponseState()
    {
        apiResponseReceived = false;
        waitingForAPIResponse = (WebManAPI.Instance != null);
        spinStartTime = Time.time;

        if (apiTimeoutCoroutine != null)
        {
            StopCoroutine(apiTimeoutCoroutine);
            apiTimeoutCoroutine = null;
        }
    }

    // WEIGHTED RANDOM SYMBOL GENERATION FOR FREE SPINS
    // ==========================================

    public void GenerateRandomTargetSymbolsForFreeSpins()
    {
        ResetFreeSpinDetectionFlags();
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> randomSymbols = GenerateWeightedRandomSymbolsForReel(reelIndex);
            SetTargetSymbolsForReel(reelIndex, randomSymbols);

            string symbolNames = string.Join(", ", randomSymbols.Select(s => s.type.ToString()));
        }
    }

    private List<SymbolData> GenerateWeightedRandomSymbolsForReel(int reelIndex)
    {
        List<SymbolData> targetSymbols = new List<SymbolData>();
        int symbolsNeeded = reels[reelIndex].visibleSymbols;

        // Track special symbols to avoid duplicates per reel
        bool freeSpinAdded = false;

        for (int i = 0; i < symbolsNeeded; i++)
        {
            SymbolData randomSymbol = GetWeightedRandomSymbolForReel(reelIndex, ref freeSpinAdded);
            if (randomSymbol != null)
            {
                targetSymbols.Add(randomSymbol);
            }
            else
            {
                // Fallback to a safe symbol if something goes wrong
                var fallbackSymbol = GetSafeSymbolForReel(reelIndex);
                if (fallbackSymbol != null)
                    targetSymbols.Add(fallbackSymbol);
            }
        }

        return targetSymbols;
    }
    private float[] GetCurrentReelStopDelays()
    {
        return isFastMode ? fastReelStopDelays : reelStopDelays;
    }

    private float GetCurrentSpinDuration()
    {
        return isFastMode ? fastSpinDuration : normalSpinDuration;
    }

    private SymbolData GetWeightedRandomSymbolForReel(int reelIndex, ref bool freeSpinAlreadyAdded)
    {
        // Create list of valid symbols for this reel during FREE SPINS
        List<SymbolData> validSymbols = new List<SymbolData>();

        foreach (SymbolData symbol in symbolDatabase)
        {
            // Use the enhanced exclusion logic
            // if (symbol.ShouldExcludeFromFreeSpins())
            //     continue;

            // Skip wild symbols on first reel
            // if (reelIndex == 0 && symbol.type == SymbolData.SymbolType.Wild)
            //     continue;

            // Skip free spin symbols on reels before index 2 (reels 1 and 2)
            if (reelIndex > 2 && symbol.type == SymbolData.SymbolType.FreeSpin)
                continue;

            // Skip free spin symbols if we already have one on this reel
            if (freeSpinAlreadyAdded && symbol.type == SymbolData.SymbolType.FreeSpin)
                continue;

            // Check canAppearInFirstReel property
            if (reelIndex == 0 && !symbol.canAppearInFirstReel)
                continue;

            validSymbols.Add(symbol);
        }

        if (validSymbols.Count == 0)
        {
            // Debug.LogError($"No valid symbols available for free spin reel {reelIndex}!");
            return null;
        }

        // Use free spin spawn weights
        SymbolData selectedSymbol = SelectWeightedRandomSymbolWithFreeSpinWeights(validSymbols);

        // Track if we added a free spin symbol
        if (selectedSymbol != null && selectedSymbol.type == SymbolData.SymbolType.FreeSpin)
        {
            freeSpinAlreadyAdded = true;
        }

        return selectedSymbol;
    }

    // Modified to use free spin spawn weights
    private SymbolData SelectWeightedRandomSymbolWithFreeSpinWeights(List<SymbolData> symbols)
    {
        if (symbols.Count == 0)
            return null;

        // Calculate total weight using free spin weights
        float totalWeight = symbols.Sum(s => s.GetFreeSpinSpawnWeight());

        if (totalWeight <= 0)
        {
            // Debug.LogWarning("Total free spin spawn weight is 0 or negative. Using equal probability.");
            return symbols[UnityEngine.Random.Range(0, symbols.Count)];
        }

        // Get random value within total weight range
        float randomWeight = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // Find the symbol that corresponds to this weight
        foreach (SymbolData symbol in symbols)
        {
            currentWeight += symbol.GetFreeSpinSpawnWeight();
            if (randomWeight <= currentWeight)
            {
                return symbol;
            }
        }

        // Fallback to last symbol
        return symbols[symbols.Count - 1];
    }

    private SymbolData GetSafeSymbolForReel(int reelIndex)
    {
        // Return a safe symbol based on reel index during FREE SPINS
        var safeSymbols = symbolDatabase.Where(s =>
            // !s.ShouldExcludeFromFreeSpins() &&
            // Standard reel restrictions
            // (reelIndex == 0 ? s.canAppearInFirstReel && s.type != SymbolData.SymbolType.Wild : true)
            reelIndex > 2 ? s.type != SymbolData.SymbolType.FreeSpin : true
        ).ToList();

        if (safeSymbols.Count > 0)
        {
            // Prefer card symbols as fallback during free spins
            var cardSymbols = safeSymbols.Where(s =>
                s.type == SymbolData.SymbolType.Ten ||
                s.type == SymbolData.SymbolType.Jack ||
                s.type == SymbolData.SymbolType.Queen ||
                s.type == SymbolData.SymbolType.King).ToList();

            if (cardSymbols.Count > 0)
                return cardSymbols.First();
            else
                return safeSymbols.First();
        }

        return null;
    }

    public void GenerateInterestingFreeSpinSymbols()
    {
        if (!WebManAPI.Instance.isDemoMode)
            return;

        float randomChoice = UnityEngine.Random.Range(0f, 1f);

        if (randomChoice < 0.3f) // 30% chance for mask-heavy scenario
        {
            GenerateMaskHeavyScenario();
        }
        else if (randomChoice < 0.45f) // 15% chance for free spin retrigger
        {
            GenerateFreeSpinRetriggerScenario();
        }
        else // 55% chance for weighted random
        {
            GenerateRandomTargetSymbolsForFreeSpins();
        }
    }

    private void GenerateMaskHeavyScenario()
    {
        // Debug.Log("[FreeSpinSlotMachineController] Generating mask-heavy scenario");

        // Get all mask symbols (excluding MaskReel)
        var maskSymbols = symbolDatabase.Where(s => s.IsMaskSymbol()).ToList();

        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> symbols = new List<SymbolData>();

            for (int i = 0; i < reels[reelIndex].visibleSymbols; i++)
            {
                // 70% chance for mask symbol, 30% chance for card symbol
                if (UnityEngine.Random.Range(0f, 1f) < 0.7f && maskSymbols.Count > 0)
                {
                    symbols.Add(maskSymbols[UnityEngine.Random.Range(0, maskSymbols.Count)]);
                }
                else
                {
                    bool dummyFreeSpin = false;
                    symbols.Add(GetWeightedRandomSymbolForReel(reelIndex, ref dummyFreeSpin));
                }
            }

            SetTargetSymbolsForReel(reelIndex, symbols);
        }
    }

    private void GenerateFreeSpinRetriggerScenario()
    {
        // Debug.Log("[FreeSpinSlotMachineController] Generating free spin retrigger scenario");

        // Give last 3 reels a chance to have free spin symbols for retriggering
        for (int reelIndex = 2; reelIndex <= 4 && reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            var freeSpinSymbol = symbolDatabase.FirstOrDefault(s => s.type == SymbolData.SymbolType.FreeSpin);
            bool freeSpinAdded = false;

            for (int i = 0; i < reels[reelIndex].visibleSymbols; i++)
            {
                // 60% chance to add freespin if we haven't added one yet
                if (!freeSpinAdded && UnityEngine.Random.Range(0f, 1f) < 0.6f && freeSpinSymbol != null)
                {
                    symbols.Add(freeSpinSymbol);
                    freeSpinAdded = true;
                }
                else
                {
                    // Use weighted random for other positions
                    symbols.Add(GetWeightedRandomSymbolForReel(reelIndex, ref freeSpinAdded));
                }
            }

            SetTargetSymbolsForReel(reelIndex, symbols);
        }

        // Fill remaining reels with weighted random symbols
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            SetTargetSymbolsForReel(reelIndex, GenerateWeightedRandomSymbolsForReel(reelIndex));
        }
    }

    // ==========================================
    // EXISTING METHODS WITH ENHANCED API VALIDATION
    // ==========================================

    private IEnumerator SpinAllReels()
    {
        isSpinning = true;
        winAnimationController.StopAnimations();
        betManager.targetWinAmount = 0f; // Reset the target win amount
        // Reset reel multipliers
        foreach (SlotReel reel in reels)
        {
            reel.currentSpeedMultiplier = 1.0f;
        }

        // Update UI - but only if not in auto-spin mode
        if (autoSpinManager == null || !autoSpinManager.IsAutoSpinning)
        {
            uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(true);
        }

        DisableGameButtons();

        foreach (SlotReel reel in reels)
        {
            reel.SetFastMode(isFastMode); // Set fast mode
            reel.StartSpinning();
        }

        // Use speed-aware spin duration
        float actualSpinDuration = GetCurrentSpinDuration();
        yield return new WaitForSeconds(actualSpinDuration);

        // Start the dynamic stopping manager
        activeStoppingManagerCoroutine = StartCoroutine(ManageReelStopping());
    }

    // Copy the stopping management system from original SlotMachineController
    private IEnumerator ManageReelStopping()
    {
        // Debug.Log("[FreeSpinSlotMachineController] Starting dynamic reel stopping manager");

        spinStartTimeStamp = Time.time;

        // Initialize stop scheduling for all reels
        for (int i = 0; i < reels.Length; i++)
        {
            reelStopScheduled[i] = false;
            reelStopTimes[i] = CalculateInitialStopTime(i);
        }

        // Start individual stop coroutines for each reel
        for (int i = 0; i < reels.Length; i++)
        {
            if (reelStopCoroutines.ContainsKey(i))
            {
                StopCoroutine(reelStopCoroutines[i]);
            }
            reelStopCoroutines[i] = StartCoroutine(HandleIndividualReelStop(i));
        }

        // Wait until all reels have stopped
        yield return new WaitUntil(() => AllReelsStopped());

        // Debug.Log("[FreeSpinSlotMachineController] All reels stopped, finalizing free spin");
        FinalizeSpin();
    }

    private float CalculateInitialStopTime(int reelIndex)
    {
        float cumulativeDelay = 0f;
        float[] currentDelays = GetCurrentReelStopDelays(); // Use speed-aware delays

        for (int i = 0; i <= reelIndex; i++)
        {
            float reelDelay = currentDelays[i];

            // Apply additional multiplier if in fast mode
            if (isFastMode)
            {
                reelDelay *= fastModeStopDelayMultiplier;
            }

            cumulativeDelay += reelDelay;
        }

        return spinStartTimeStamp + cumulativeDelay;
    }

    public bool callOnce = false;
    private IEnumerator HandleIndividualReelStop(int reelIndex)
    {
        // Wait until it's time for this reel to stop
        yield return new WaitUntil(() => ShouldReelStop(reelIndex));

        // Mark as scheduled and stop the reel
        reelStopScheduled[reelIndex] = true;
        // if (!callOnce)
        // {
        //     DelaySubsequentReelsForUntilWeGetResults(WebManAPI.Instance.actionDuration);
        //     callOnce = true;
        // }
        reels[reelIndex].BeginStopping();

        // Wait for tension animation if applicable
        if (reels[reelIndex].useTensionLinearMovement)
        {
            yield return new WaitUntil(() => reels[reelIndex].IsTensionAnimationComplete);
        }
    }

    private void DelaySubsequentReelsForUntilWeGetResults(float tensionDelay)
    {
        // Apply delay to all reels that come after the tension reel
        for (int i = 0; i < reels.Length && i < reelStopTimes.Length; i++)
        {
            reelStopTimes[i] += tensionDelay;
        }
    }

    /// <summary>
    /// ENHANCED: Should reel stop - with proper API validation
    /// </summary>
    private bool ShouldReelStop(int reelIndex)
    {
        if (stopRequested) return true;

        // CRITICAL: In non-demo mode, wait for VALID API response before allowing any reels to stop
        if (waitingForAPIResponse)
        {
            // Check if we have received a VALID API response
            // bool hasValidResponse = WebManAPI.Instance != null && WebManAPI.Instance.HasValidSpinResponse();

            /* if (!hasValidResponse)
             {
                 if (debugAPIResponse)
                 {
                     // Debug.Log($"[FreeSpinSlotMachineController] Reel {reelIndex} waiting for VALID API response... " +
                              $"(Elapsed: {Time.time - spinStartTime:F1}s)");
                 }
                 return false; // Keep spinning until we get a valid response
             }
             else
             {
                 // We have a valid response, mark as received and continue with normal stopping logic
                 apiResponseReceived = true;
                 waitingForAPIResponse = false;

                 if (debugAPIResponse)
                 {
                     // Debug.Log($"[FreeSpinSlotMachineController] VALID API response received after {Time.time - spinStartTime:F1}s, " +
                              $"allowing reel {reelIndex} to proceed with stopping logic");
                 }
             }*/
        }

        // Check if any preceding reel is in tension mode
        for (int i = 0; i < reelIndex; i++)
        {
            if (reelsInTensionMode[i])
                return false;
        }

        float currentTime = Time.time;
        float scheduledStopTime = reelStopTimes[reelIndex];
        return currentTime >= scheduledStopTime;
    }

    /// <summary>
    /// ENHANCED: Called when API response is received - with validation
    /// </summary>
    public void OnAPIResponseReceived()
    {
        if (!waitingForAPIResponse)
        {
            if (debugAPIResponse)
            {
                // Debug.Log("[FreeSpinSlotMachineController] OnAPIResponseReceived called but not waiting for response");
            }
            return;
        }

        // Double-check that we actually have a valid response
        /* if (WebManAPI.Instance == null || !WebManAPI.Instance.HasValidSpinResponse())
          {
              // Debug.LogWarning("[FreeSpinSlotMachineController] OnAPIResponseReceived called but no valid response available!");
              return;
          }
        */
        // Debug.Log("[FreeSpinSlotMachineController] VALID API response received, allowing reels to stop after delay");

        // Wait a short moment to ensure target symbols are fully assigned
        StartCoroutine(DelayedAPIResponseConfirmation());
    }

    private IEnumerator DelayedAPIResponseConfirmation()
    {
        // Small delay to ensure all target symbols are properly assigned
        yield return new WaitForSeconds(0.2f);

        // Final validation before proceeding
        /*if (WebManAPI.Instance != null && WebManAPI.Instance.HasValidSpinResponse())
        {
            apiResponseReceived = true;
            waitingForAPIResponse = false;

            // Stop the timeout coroutine since we got a valid response
            if (apiTimeoutCoroutine != null)
            {
                StopCoroutine(apiTimeoutCoroutine);
                apiTimeoutCoroutine = null;
            }

            // Debug.Log("[FreeSpinSlotMachineController] API response processing complete, reels can now stop");
        }
        else
        {
            // Debug.LogError("[FreeSpinSlotMachineController] API response validation failed during delayed confirmation!");
        }*/
    }

    private void FinalizeSpin()
    {
        // Note: We don't update symbols for free spin mode here anymore
        // Symbols should maintain free spin mode throughout the entire free spin session

        float basePayout = winCalculator.CalculateWin(reels);
        float winAmount = basePayout;

        // Check for free spin retrigger
        bool freeSpinRetriggered = CheckForFreeSpinRetrigger();

        if (freeSpinRetriggered && enableFreeSpinRetrigger)
        {
            // Debug.Log("[FreeSpinSlotMachineController] Free spins retriggered!");
            // FreeSpinManager will handle the retrigger
            StartCoroutine(HandleFreeSpinRetrigger());
        }

        // Update UI
        if (autoSpinManager == null || !autoSpinManager.IsAutoSpinning)
        {
            uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(false);
        }

        // Reset state
        isSpinning = false;
        stopRequested = false;
        ResetFreeSpinDetectionFlags();
        CleanupStoppingSystem();

        // Reset API state for next spin
        ResetAPIResponseState();

        // Reset reels
        foreach (SlotReel reel in reels)
        {
            reel.currentSpeedMultiplier = 1.0f;
            reel.useTensionLinearMovement = false;

            if (reel.isTensionSoundPlaying)
            {
                reel.StopTensionSound();
            }
        }

        OnSpinComplete?.Invoke();
    }

    private bool CheckForFreeSpinRetrigger()
    {
        // Check if we have free spin symbols on first 3 reels
        bool reel3HasFreeSpin = DoesReelHaveFreeSpin(0);
        bool reel4HasFreeSpin = DoesReelHaveFreeSpin(1);
        bool reel5HasFreeSpin = DoesReelHaveFreeSpin(2);

        return reel3HasFreeSpin && reel4HasFreeSpin && reel5HasFreeSpin;
    }

    private IEnumerator HandleFreeSpinRetrigger()
    {
        // Wait for current spin to complete
        yield return new WaitUntil(() => !isSpinning);

        // Call FreeSpinManager to handle the retrigger
        if (FreeSpinManager.Instance != null)
        {
            yield return StartCoroutine(FreeSpinManager.Instance.HandleFreeSpinsDuringFreeSpins());
        }
    }

    // Free spin detection methods (similar to original)
    private void HandleEarlyFreeSpinDetection(int reelIndex)
    {
        if (reelIndex == 2)
        {
            if (DoesReelHaveFreeSpin(0))
            {
                freeSpinDetectedOnReel3 = true;
                // Debug.Log("[FreeSpinSlotMachineController] Free Spin detected on Reel 3");
            }
        }
        else if (reelIndex == 3 && freeSpinDetectedOnReel3)
        {
            if (DoesReelHaveFreeSpin(1))
            {
                freeSpinDetectedOnReel4 = true;
                // Debug.Log("[FreeSpinSlotMachineController] Free Spin detected on Reel 4");
            }
        }
        else if (reelIndex == 4 && freeSpinDetectedOnReel4)
        {
            if (DoesReelHaveFreeSpin(2))
            {
                freeSpinDetectedOnReel5 = true;
                // Debug.Log("[FreeSpinSlotMachineController] Free Spin detected on Reel 5");
            }
        }
    }

    private void CheckReelForFreeSpin(int reelIndex)
    {
        var visibleSymbols = GetVisibleSymbolsInReel(reels[reelIndex]);
        bool hasFreeSpinSymbol = visibleSymbols.Exists(s => s.Data.type == SymbolData.SymbolType.FreeSpin);

        if (reelIndex == 2 && hasFreeSpinSymbol)
        {
            freeSpinDetectedOnReel3 = true;
        }
        else if (reelIndex == 3 && hasFreeSpinSymbol && freeSpinDetectedOnReel3)
        {
            freeSpinDetectedOnReel4 = true;
        }
        else if (reelIndex == 4 && hasFreeSpinSymbol && freeSpinDetectedOnReel4)
        {
            freeSpinDetectedOnReel5 = true;
        }
    }

    public bool DoesReelHaveFreeSpin(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Length) return false;

        var visibleSymbols = reels[reelIndex].GetTopVisibleSymbols(reels[reelIndex].visibleSymbols);
        return visibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
    }

    private List<Symbol> GetVisibleSymbolsInReel(SlotReel reel)
    {
        return winCalculator.GetVisibleSymbolsInReel(reel);
    }

    public void ResetFreeSpinDetectionFlags()
    {
        freeSpinDetectedOnReel3 = false;
        freeSpinDetectedOnReel4 = false;
        freeSpinDetectedOnReel5 = false;
    }

    public void ToggleSpeedMode()
    {
        isFastMode = !isFastMode;

        // Update UI sprites - FIXED: Use main UI elements if FreeSpinSlotMachineController doesn't have its own
        if (uiElements.speedButtonImage.landscape != null && uiElements.speedButtonImage.portrait != null)
        {
            Sprite currentSprite = isFastMode ? uiElements.fastSpeedSprite : uiElements.normalSpeedSprite;
            uiElements.speedButtonImage.landscape.sprite = currentSprite;
            uiElements.speedButtonImage.portrait.sprite = currentSprite;
        }
        else if (SlotMachineController.Instance != null)
        {
            // Fallback: Use main controller's UI elements
            Sprite currentSprite = isFastMode ? SlotMachineController.Instance.uiElements.fastSpeedSprite : SlotMachineController.Instance.uiElements.normalSpeedSprite;
            SlotMachineController.Instance.uiElements.speedButtonImage.landscape.sprite = currentSprite;
            SlotMachineController.Instance.uiElements.speedButtonImage.portrait.sprite = currentSprite;
        }

        // Apply speed to currently spinning reels immediately
        float speedMultiplier = isFastMode ? 1.4f : 1.0f;
        for (int i = 0; i < reels.Length; i++)
        {
            if (reels[i].IsSpinning)
            {
                reels[i].SetFastMode(isFastMode);
            }
            reels[i].SetSpeedMultiplier(speedMultiplier);
        }

        // IMPORTANT: Keep both controllers in sync
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.isFastMode = isFastMode;

            // Also sync the main controller's reels if they exist
            var mainReels = SlotMachineController.Instance.GetReels();
            foreach (SlotReel reel in mainReels)
            {
                if (reel.IsSpinning)
                {
                    reel.SetFastMode(isFastMode);
                }
                reel.SetSpeedMultiplier(speedMultiplier);
            }
        }

        // Update BetManager
        if (betManager != null)
        {
            betManager.IsFastMode = isFastMode;
        }

        // Debug.Log($"[FreeSpinSlotMachineController] Speed Mode: {(isFastMode ? "FAST" : "NORMAL")}");
    }

    public void DisableGameButtons()
    {
        uiElements.autoSpinButton.landscape.interactable = false;
        uiElements.autoSpinButton.portrait.interactable = false;
        uiElements.increaseButton.landscape.interactable = false;
        uiElements.increaseButton.portrait.interactable = false;
        uiElements.decreaseButton.landscape.interactable = false;
        uiElements.decreaseButton.portrait.interactable = false;
        uiElements.mainBetButton.landscape.interactable = false;
        uiElements.mainBetButton.portrait.interactable = false;
    }

    public void EnableGameButtons()
    {
        uiElements.autoSpinButton.landscape.interactable = true;
        uiElements.autoSpinButton.portrait.interactable = true;
        uiElements.decreaseButton.landscape.interactable = true;
        uiElements.decreaseButton.portrait.interactable = true;
        uiElements.mainBetButton.landscape.interactable = true;
        uiElements.mainBetButton.portrait.interactable = true;
        if (BetManager.CurrentBetIndex == 0)
        {
            uiElements.decreaseButton.landscape.interactable = false;
            uiElements.decreaseButton.portrait.interactable = false;
        }
        else
        {
            uiElements.increaseButton.landscape.interactable = true;
            uiElements.increaseButton.portrait.interactable = true;
        }
    }

    // RESTORED: Original method names for backward compatibility
    public void StopAllReels()
    {
        stopRequested = true;

        // Force immediate stop for all reels
        for (int i = 0; i < reels.Length; i++)
        {
            if (reels[i].IsSpinning)
            {
                reels[i].StopSpinImmediately();
            }
        }
    }

    public void StopAllReelsImmediately()
    {
        StopAllReels();
    }

    // Method to manually trigger free spin (for UI buttons)
    public void OnSpinButtonPressed()
    {
        if (!isSpinning)
        {
            StartFreeSpin();
        }
    }

    private bool AllReelsStopped()
    {
        foreach (SlotReel reel in reels)
        {
            if (reel.IsSpinning) return false;

        }
        // NEW: Highlight winning positions
        // if (LineManager.instance != null)
        // {
        //     LineManager.instance.HighlightWinningPositions();
        // }
        return true;

    }

    private void CleanupStoppingSystem()
    {
        foreach (var kvp in reelStopCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        reelStopCoroutines.Clear();

        if (activeStoppingManagerCoroutine != null)
        {
            StopCoroutine(activeStoppingManagerCoroutine);
            activeStoppingManagerCoroutine = null;
        }

        // Stop API timeout coroutine if active
        if (apiTimeoutCoroutine != null)
        {
            StopCoroutine(apiTimeoutCoroutine);
            apiTimeoutCoroutine = null;
        }

        for (int i = 0; i < reelStopScheduled.Length; i++)
        {
            reelStopScheduled[i] = false;
            reelStopTimes[i] = 0f;
        }
    }

    // Set target symbols for reels (for API integration) - ENHANCED
    public void SetTargetSymbolsForReel(int reelIndex, List<SymbolData> symbols)
    {
        if (reelIndex >= 0 && reelIndex < reels.Length)
        {
            reels[reelIndex].targetSymbols.Clear();
            if (symbols != null && symbols.Count > 0)
            {
                reels[reelIndex].targetSymbols.AddRange(symbols);
                // Debug.Log($"[FreeSpinSlotMachineController] Set {symbols.Count} target symbols for reel {reelIndex}");

                // Log mask symbols specifically
                var maskSymbols = symbols.Where(s => s.IsMaskSymbol()).ToList();
                if (maskSymbols.Count > 0)
                {
                    // Debug.Log($"[FreeSpinSlotMachineController] Reel {reelIndex} has {maskSymbols.Count} mask symbols: {string.Join(", ", maskSymbols.Select(s => s.type))}");
                }
            }
        }
    }

    // Get reels array (for WebManAPI access)
    public SlotReel[] GetReels()
    {
        return reels;
    }

    /// <summary>
    /// Public method to check if free spin controller is waiting for API response
    /// </summary>
    public bool IsWaitingForAPIResponse()
    {
        return waitingForAPIResponse;
    }

    /// <summary>
    /// Public method to force stop waiting for API (for error recovery)
    /// </summary>
    public void ForceStopWaitingForAPI()
    {
        waitingForAPIResponse = false;
        apiResponseReceived = false;

        if (apiTimeoutCoroutine != null)
        {
            StopCoroutine(apiTimeoutCoroutine);
            apiTimeoutCoroutine = null;
        }

        // Debug.Log("[FreeSpinSlotMachineController] Forced stop waiting for API response");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        // Cleanup any running coroutines
        CleanupStoppingSystem();
    }
}