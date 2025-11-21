using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public class SlotMachineController : MonoBehaviour
{
    // Singleton pattern implementation
    private static SlotMachineController _instance;
    public static SlotMachineController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SlotMachineController>();
            }
            return _instance;
        }
    }

    [Header("References")]
    [SerializeField] private SlotReel[] reels;
    [SerializeField] private Button spinButton;
    [SerializeField] private WinCalculator winCalculator;
    public BetManager betManager;
    public WebManAPI webManAPI;
    [SerializeField] private WinAnimationController winAnimationController;
    [SerializeField] private AutoSpinManager autoSpinManager; // Add reference to AutoSpinManager

    [Header("Spin Timing Settings")]
    [SerializeField] private float[] reelStopDelays = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }; // Normal delays
    [SerializeField] private float[] fastReelStopDelays = { 0.4f, 0.4f, 0.4f, 0.5f, 0.4f }; // Fast delays
    [SerializeField] private float baseSpinDuration = 1.0f; // Minimum time all reels spin
    [SerializeField] private float fastModeMultiplier = 0.75f; // Multiplier for fast mode (60% of normal time)
    [SerializeField] private float fastModeStopDelayMultiplier = 1f;
    public SymbolData[] symbolDatabase;
    private float[] originalReelStopDelays;
    private bool tensionEffectApplied = false;

    private Color disabledButtonColor = new Color(0.78f, 0.78f, 0.78f, 0.7f); // #C8C8C8 in RGB
    private Color normalButtonColor = Color.white;
    private Coroutine spinButtonColorTransition;

    [Header("Stop Button Settings")]
    [SerializeField] private float earlyStopLockThreshold = 1.0f; // Time threshold in seconds
    private float spinStartTime = 0f;
    private bool buttonLocked = false;

    public bool isFastMode = false; // toggled by speed button

    private bool isSpinning;
    public bool isDemoMode;
    private bool[] reelHasBonusSymbol = new bool[5];
    public bool IsSpinning => isSpinning;
    public System.Action OnFreeSpinsEnded;
    public event Action OnSpinStart;
    public event Action OnSpinComplete;

    public UIElementManager uiElements;

    // Track the active spin coroutine so we can stop it
    private Coroutine activeSpinCoroutine;
    private Coroutine activeStoppingManagerCoroutine; // NEW: Track stopping manager
    private bool stopRequested = false;
    private bool freeSpinOnReel1 = false;
    private bool freeSpinOnReel2 = false;
    private string currentTransactionID;

    [Header("Reel 5 Tension Settings")]
    [SerializeField] private float reel5TensionMultiplier = 2.0f; // How much to slow down reel 5
    [SerializeField] private float extendedReel5Delay = 1.5f; // Extra time for reel 5
    [SerializeField] private bool enableReel5TensionEffect = true;
    public string CurrentTransactionID => currentTransactionID;
    private const string TRANSACTION_ID_KEY = "LastTransactionID";
    private bool freeSpinDetectedOnReel1 = false;
    private bool freeSpinDetectedOnReel2 = false;

    // Add new variables in SlotMachineController
    [Header("Reel 3 Tension Settings")]
    [SerializeField] private float reel3TensionMultiplier = 2.0f; // How much to slow down reel 3
    [SerializeField] private float extendedReel3Delay = 0.5f; // Extra time for reel 3
    [SerializeField] private bool enableTensionEffect = false;
    private bool reel5TensionEffectApplied = false;


    [Header("Demo Mode Random Symbol Settings")]
    [SerializeField] public bool useRandomSymbolsInDemo = true;
    [SerializeField] private float[] symbolWeights = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f }; // Adjust weights for different symbol types

    [Header("Cycling Spin Sounds")]
    [SerializeField] private bool enableCyclingSpinSounds = true;
    private int currentSpinSoundIndex = 1; // Tracks which sound to play next (1-10)

    // NEW: Dynamic stopping management
    private float spinStartTimeStamp = 0f;
    private bool[] reelStopScheduled; // Track which reels are scheduled to stop
    private float[] reelStopTimes; // When each reel should stop
    private Dictionary<int, Coroutine> reelStopCoroutines; // Individual stop coroutines
    private bool[] reelsInTensionMode;
    private Dictionary<int, float> tensionCompletionTimes;
    [SerializeField] private float tensionAnimationDuration = 2.0f; // Duration of tension animation
    [Header("API Integration")]
    private bool apiResponseReceived = false;
    private bool waitingForAPIResponse = false;

    // Ensure singleton pattern works correctly with Unity's lifecycle
    private void Awake()
    {
        // Implement singleton pattern
        if (_instance != null && _instance != this)
        {
            // If an instance already exists and it's not this one, destroy this one
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Initialize new stopping management arrays - moved to Start() to ensure reels are assigned

    }

    private void Start()
    {
        // Initialize stopping management arrays based on actual reel count
        int reelCount = reels != null ? reels.Length : 5;
        reelStopScheduled = new bool[reelCount];
        reelStopTimes = new float[reelCount];
        reelStopCoroutines = new Dictionary<int, Coroutine>();
        reelsInTensionMode = new bool[reelCount];
        tensionCompletionTimes = new Dictionary<int, float>();

        uiElements.spinButton.landscape.onClick.AddListener(OnSpinButtonClicked);
        uiElements.spinButton.portrait.onClick.AddListener(OnSpinButtonClicked);

        // Set initial state of button images
        uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(true);
        uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(false);
        uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(true);
        uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(false);

        uiElements.speedButton.landscape.onClick.AddListener(ToggleSpeedMode);
        uiElements.speedButton.portrait.onClick.AddListener(ToggleSpeedMode);
        uiElements.speedButtonImage.landscape.sprite = uiElements.normalSpeedSprite;
        uiElements.speedButtonImage.portrait.sprite = uiElements.normalSpeedSprite;

        // UPDATED: Use translation instead of hard-coded text
        InitializeWinText();

        // Subscribe to language refresh events
        if (LanguageMan.instance != null)
        {
            LanguageMan.instance.onLanguageRefresh.AddListener(RefreshDynamicTexts);
        }

        // Validate reel delays configuration
        if (reelStopDelays.Length != reels.Length)
        {
            ResetReelDelaysToDefault();
        }
        for (int i = 0; i < reels.Length; i++)
        {
            int reelIndex = i; // Capture for lambda
                               // Existing event subscriptions 
            reels[i].OnFreeSpinSymbolDetected += (detectedReelIndex) => HandleEarlyFreeSpinDetection(reelIndex);

        }
        foreach (SlotReel reel in reels)
        {
            // reel.OnSpinStart += () => Debug.Log("Reel started spinning");
            // reel.OnSpinEnd += () => Debug.Log("Reel stopped spinning");
        }
        // Subscribe to each reel's spin end event to check for free spins
        for (int i = 0; i < reels.Length; i++)
        {
            int reelIndex = i; // Capture the index for lambda
            // reels[i].OnSpinStart += () => Debug.Log("Reel started spinning");
            reels[i].OnSpinEnd += () =>
            {
                CheckReelForFreeSpin(reelIndex);
            };
        }

        var resetReelTimingProfile = new SpinTimingProfile(
           "resetdelays",
           1.0f,  // Base spin duration
           0.6f,  // Fast mode multiplier
           new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }  // Five delay values
        );
        // LoadLastTransactionID();
        originalReelStopDelays = new float[reelStopDelays.Length];
        reelStopDelays.CopyTo(originalReelStopDelays, 0);
        ResetButtonColors();
        ValidateReelDelayArrays();
    }
    private void ValidateReelDelayArrays()
    {
        int reelCount = reels.Length;

        if (reelStopDelays.Length != reelCount)
        {
            Debug.LogError($"[SlotMachineController] reelStopDelays array size ({reelStopDelays.Length}) doesn't match reel count ({reelCount}). Resizing array.");
            System.Array.Resize(ref reelStopDelays, reelCount);

            // Fill missing entries with default value
            for (int i = 0; i < reelStopDelays.Length; i++)
            {
                if (reelStopDelays[i] == 0f)
                    reelStopDelays[i] = 0.5f;
            }
        }

        if (fastReelStopDelays.Length != reelCount)
        {
            Debug.LogError($"[SlotMachineController] fastReelStopDelays array size ({fastReelStopDelays.Length}) doesn't match reel count ({reelCount}). Resizing array.");
            System.Array.Resize(ref fastReelStopDelays, reelCount);

            // Fill missing entries with default value
            for (int i = 0; i < fastReelStopDelays.Length; i++)
            {
                if (fastReelStopDelays[i] == 0f)
                    fastReelStopDelays[i] = 0.4f;
            }
        }

        Debug.Log($"[SlotMachineController] Validated delays: {reelCount} reels, {reelStopDelays.Length} normal delays, {fastReelStopDelays.Length} fast delays");
    }

    private void OnDestroy()
    {
        // Unsubscribe from language refresh events
        if (LanguageMan.instance != null)
        {
            LanguageMan.instance.onLanguageRefresh.RemoveListener(RefreshDynamicTexts);
        }
    }

    // ADDED: Initialize win text with proper translation
    private void InitializeWinText()
    {
        // Wait for LanguageMan to initialize if needed
        if (LanguageMan.instance == null || LanguageMan.instance.Data.Length == 0)
        {
            Invoke(nameof(InitializeWinText), 0.1f);
            return;
        }
        SetPlaceBetText();
    }

    // ADDED: Helper method to set "PLACE YOUR BET!" text with translation
    public void SetPlaceBetText()
    {
        string placeBetText = "PLACE YOUR BET!";//LanguageHelper.GetTranslation("L_8");
        uiElements.winText.landscape.text = placeBetText;
        uiElements.winText.portrait.text = placeBetText;
    }

    // ADDED: Helper method to set "GOODLUCK!" text with translation
    private void SetGoodLuckText()
    {
        string goodLuckText = "GOOD LUCK!";//LanguageHelper.GetTranslation("L_7");
        uiElements.winText.landscape.text = goodLuckText;
        uiElements.winText.portrait.text = goodLuckText;
    }

    // ADDED: Force refresh all text
    public void ForceRefreshText()
    {
        // Debug.Log("[SlotMachineController] Force refreshing all text");

        if (isSpinning)
        {
            if (!FreeSpinManager.Instance.IsFreeSpinActive)
            {
                SetGoodLuckText();
            }
        }
        else if (betManager != null && betManager.LastWinAmount > 0)
        {
            bool inFreeSpins = FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;
            betManager.UpdateWinText(inFreeSpins);
        }
        else
        {
            SetPlaceBetText();
        }
    }

    // UPDATED: Refresh dynamic texts when language changes
    private void RefreshDynamicTexts()
    {
        // Debug.Log("[SlotMachineController] RefreshDynamicTexts called");
        ForceRefreshText();
    }

    private void PlayCyclingSpinSound()
    {
        // Only play cycling spin sounds if enabled, not in fast mode, and not in free spins
        if (!enableCyclingSpinSounds || isFastMode || (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive))
        {
            // Debug.Log($"[SlotMachineController] Skipping cycling spin sound. Enabled: {enableCyclingSpinSounds}, FastMode: {isFastMode}, FreeSpinActive: {(FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)}");
            return;
        }

        // Play the current sound
        SoundManager.Instance.PlayCyclingSpinSound(currentSpinSoundIndex);
        // Debug.Log($"[SlotMachineController] Playing cycling spin sound {currentSpinSoundIndex}");

        // Increment and cycle the sound index (1-10)
        currentSpinSoundIndex++;
        if (currentSpinSoundIndex > 10)
        {
            currentSpinSoundIndex = 1; // Reset to first sound after 10th
            // Debug.Log("[SlotMachineController] Cycling back to spin sound 1");
        }
    }

    public void SetCyclingSpinSoundsEnabled(bool enabled)
    {
        enableCyclingSpinSounds = enabled;
        // Debug.Log($"[SlotMachineController] Cycling spin sounds enabled: {enabled}");
    }

    public int GetCurrentSpinSoundIndex()
    {
        return currentSpinSoundIndex;
    }

    public void ResetSpinSoundIndex()
    {
        currentSpinSoundIndex = 1;
        // Debug.Log("[SlotMachineController] Reset spin sound index to 1");
    }

    public void OnReelTriggeredTension(int triggerReelIndex)
    {
        // Debug.Log($"[SlotMachineController] Reel {triggerReelIndex} triggered immediate tension effect");

        if (triggerReelIndex == 1) // Reel 2 triggered tension for reel 3
        {
            ApplyImmediateTensionToReel3();
        }
        else if (triggerReelIndex == 3) // Reel 4 triggered tension for reel 5
        {
            ApplyImmediateTensionToReel5();
        }
    }

    private void ApplyImmediateTensionToReel3()
    {
        if (!enableTensionEffect) return;

        // Mark reel 3 for tension
        if (reels.Length > 2)
        {
            reels[2].useTensionLinearMovement = true;
            reels[2].currentSpeedMultiplier = reel3TensionMultiplier;
        }

        DelaySubsequentReelsForTension(2, extendedReel3Delay);
    }
    private void DelaySubsequentReelsForTension(int tensionReelIndex, float tensionDelay)
    {
        // Debug.Log($"[SlotMachineController] IMMEDIATELY delaying reels after {tensionReelIndex} by {tensionDelay}s");

        // Apply delay to all reels that come after the tension reel
        for (int i = tensionReelIndex + 1; i < reels.Length && i < reelStopTimes.Length; i++)
        {
            float oldStopTime = reelStopTimes[i];
            reelStopTimes[i] += tensionDelay;

            // Debug.Log($"[SlotMachineController] Reel {i} stop time updated: {oldStopTime:F2}s -> {reelStopTimes[i]:F2}s");
        }
    }

    private void DelaySubsequentReelsForUntilWeGetResults(float tensionDelay)
    {
        for (int i = 0; i < reels.Length && i < reelStopTimes.Length; i++)
        {
            float oldStopTime = reelStopTimes[i];
            reelStopTimes[i] += tensionDelay * 0.8f;
        }
    }

    public void OnReelTensionStarted(int reelIndex)
    {
        // Debug.Log($"[SlotMachineController] Reel {reelIndex} tension animation STARTED");
        reelsInTensionMode[reelIndex] = true;

        // Additional delay for subsequent reels to ensure they wait for tension completion
        AddTensionWaitTimeToSubsequentReels(reelIndex);
    }
    public void OnReelTensionCompleted(int reelIndex)
    {
        // Debug.Log($"[SlotMachineController] Reel {reelIndex} tension animation COMPLETED");
        reelsInTensionMode[reelIndex] = false;
        tensionCompletionTimes[reelIndex] = Time.time;

        // Allow subsequent reels to proceed if they were waiting
        AllowSubsequentReelsToProceeed(reelIndex);
    }
    private void AddTensionWaitTimeToSubsequentReels(int tensionReelIndex)
    {
        float extraWaitTime = tensionAnimationDuration; // Use the actual tension animation duration

        for (int i = tensionReelIndex + 1; i < reels.Length && i < reelStopTimes.Length; i++)
        {
            float oldStopTime = reelStopTimes[i];
            reelStopTimes[i] += extraWaitTime;

            // Debug.Log($"[SlotMachineController] Added tension wait time to reel {i}: {oldStopTime:F2}s -> {reelStopTimes[i]:F2}s");
        }
    }
    private void AllowSubsequentReelsToProceeed(int tensionReelIndex)
    {
        // Debug.Log($"[SlotMachineController] Allowing reels after {tensionReelIndex} to proceed");

        // You could implement early release logic here if needed
        // For now, reels will proceed based on their updated stop times
    }
    private void ApplyImmediateTensionToReel5()
    {
        if (!enableReel5TensionEffect) return;

        // Debug.Log("[SlotMachineController] Applying IMMEDIATE tension to reel 5");

        if (reels.Length > 4)
        {
            reels[4].useTensionLinearMovement = true;
            reels[4].currentSpeedMultiplier = reel5TensionMultiplier;
            // Debug.Log("[SlotMachineController] Applied immediate tension effects to reel 5");
        }

        // No subsequent reels for reel 5, so no delay needed
    }

    private void ResetReelDelaysToDefault()
    {
        // Create default delays with increasing time for each reel
        reelStopDelays = new float[reels.Length];
        for (int i = 0; i < reels.Length; i++)
        {
            reelStopDelays[i] = 0.3f + (i * 0.2f);
        }
    }
    public List<Symbol> GetVisibleSymbolsInReel(SlotReel reel)
    {
        List<Symbol> visibleSymbols = new List<Symbol>();
        RectTransform containerRect = reel.symbolsContainer;

        RectTransform reelRect = reel.GetComponent<RectTransform>();
        Vector3[] reelCorners = new Vector3[4];
        reelRect.GetWorldCorners(reelCorners);
        for (int i = 0; i < 4; i++)
        {
            reelCorners[i] = containerRect.InverseTransformPoint(reelCorners[i]);
        }

        foreach (Transform child in containerRect)
        {
            Symbol symbol = child.GetComponent<Symbol>();
            if (symbol == null) continue;

            RectTransform symbolRect = child.GetComponent<RectTransform>();
            Vector3 symbolCenter = symbolRect.localPosition;
            float symbolHalfHeight = symbolRect.rect.height / 2;

            if (SymbolIsVisible(symbolCenter, symbolHalfHeight, reelCorners))
            {
                visibleSymbols.Add(symbol);
            }
        }
        return visibleSymbols;
    }
    private void HandleEarlyFreeSpinDetection(int reelIndex)
    {
        if (reelIndex == 0)
        {
            if (DoesReelHaveFreeSpin(0))
            {
                freeSpinDetectedOnReel1 = true;
                // Debug.Log("[SlotMachineController] Free Spin detected early on Reel 0");
            }
            else
            {
                // No free spin on reel 1, reset delays to default
                ResetReelDelaysOnMissedFreeSpin();
                // Debug.Log("[SlotMachineController] No Free Spin on Reel 0 - resetting delays to default");
            }
        }
        else if (reelIndex == 1 && freeSpinDetectedOnReel1)
        {
            if (DoesReelHaveFreeSpin(1))
            {
                freeSpinDetectedOnReel2 = true;
                // Debug.Log("[SlotMachineController] Free Spin detected early on Reel 1 - Will apply tension for Reel 2! States: R1=" + freeSpinDetectedOnReel1 + ", R2=" + freeSpinDetectedOnReel2);
            }
            else
            {
                // Free spin on reel 1 but not on reel 2, reset delays to default
                ResetReelDelaysOnMissedFreeSpin();
                // Debug.Log("No Free Spin on Reel 2 - resetting delays to default");
            }
        }
        else if (reelIndex == 2 && freeSpinDetectedOnReel1 && freeSpinDetectedOnReel2)
        {
            // Debug.Log("[SlotMachineController] Free Spin detected early on Reel 2 - Free Spins will be triggered!");
        }
    }
    private bool SymbolIsVisible(Vector3 symbolCenter, float symbolHalfHeight, Vector3[] viewportCorners)
    {
        float symbolTop = symbolCenter.y + symbolHalfHeight;
        float symbolBottom = symbolCenter.y - symbolHalfHeight;
        float viewportTop = viewportCorners[1].y;
        float viewportBottom = viewportCorners[0].y;

        // Check if majority of symbol is visible (at least 60% overlap)
        float overlapTop = Mathf.Min(symbolTop, viewportTop);
        float overlapBottom = Mathf.Max(symbolBottom, viewportBottom);
        float overlapHeight = overlapTop - overlapBottom;

        return overlapHeight >= (symbolHalfHeight * 2 * 0.6f); // At least 60% visible
    }
    private void CheckReelForFreeSpin(int reelIndex)
    {
        // Debug.Log($"[SlotMachineController] CheckReelForFreeSpin: Checking Reel {reelIndex} for FreeSpin symbols");

        List<Symbol> visibleSymbols = GetVisibleSymbolsInReel(reels[reelIndex]);

        // Log all symbol types in this reel
        string symbolTypes = string.Join(", ", visibleSymbols.Select(s => s.Data.type.ToString()));
        // Debug.Log($"[SlotMachineController] Symbols in Reel {reelIndex}: {symbolTypes}");

        bool hasFreeSpinSymbol = visibleSymbols.Exists(s => s.Data.type == SymbolData.SymbolType.FreeSpin);

        // Debug.Log($"[SlotMachineController] Reel {reelIndex} has FreeSpin: {hasFreeSpinSymbol}. FreeSpin1: {freeSpinOnReel1}, FreeSpin2: {freeSpinOnReel2}");

        if (reelIndex == 0 && hasFreeSpinSymbol)
        {
            freeSpinOnReel1 = true;
            // Debug.Log("[SlotMachineController] Free Spin symbol found on Reel 0");
        }
        else if (reelIndex == 1 && hasFreeSpinSymbol && freeSpinOnReel1)
        {
            freeSpinOnReel2 = true;
            // Debug.Log("[SlotMachineController] Free Spin symbol found on Reel 1 - Will activate tension effect!");
        }
    }
    public bool DoesReelHaveFreeSpin(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Length) return false;

        var visibleSymbols = reels[reelIndex].GetTopVisibleSymbols(reels[reelIndex].visibleSymbols);
        return visibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
    }
    public void ToggleSpeedMode()
    {
        isFastMode = !isFastMode;

        Sprite currentSprite = isFastMode ? uiElements.fastSpeedSprite : uiElements.normalSpeedSprite;
        uiElements.speedButtonImage.landscape.sprite = currentSprite;
        uiElements.speedButtonImage.portrait.sprite = currentSprite;
        for (int i = 0; i < reels.Length; i++)
        {
            if (reels[i].IsSpinning)
            {
                // Apply speed mode to currently spinning reels
                reels[i].SetFastMode(isFastMode);
            }
        }
        // UPDATE: Sync FreeSpinSlotMachineController
        if (FreeSpinSlotMachineController.Instance != null)
        {
            FreeSpinSlotMachineController.Instance.isFastMode = isFastMode;
        }

        float speedMultiplier = isFastMode ? 1.4f : 1.0f;
        foreach (SlotReel reel in reels)
        {
            reel.SetSpeedMultiplier(speedMultiplier);
        }

        // Debug.Log("Speed Mode: " + (isFastMode ? "FAST" : "NORMAL"));
    }
    // In SlotMachineController.cs - when speed button is pressed



    // ==========================================
    // NEW SEPARATED SPIN/STOP SYSTEM
    // ==========================================

    // MODIFIED: Now only handles starting reels and launching stopping manager
    private IEnumerator SpinAllReels()
    {
        isSpinning = true;
        winAnimationController.StopAnimations(fromButton: true);
        betManager.targetWinAmount = 0f; // Reset the target win amount

        foreach (SlotReel reel in reels)
        {
            reel.currentSpeedMultiplier = 1.0f;
        }

        // Only change button visuals if NOT in auto-spin mode
        if (autoSpinManager == null || !autoSpinManager.IsAutoSpinning)
        {
            uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(true);
        }

        if (!FreeSpinManager.Instance.IsFreeSpinActive)
        {
            // UPDATED: Use translation instead of hard-coded "GOODLUCK!"
            SetGoodLuckText();
        }

        DisableGameButtons();

        // Start all reels spinning - NOW PASS FAST MODE STATE
        foreach (SlotReel reel in reels)
        {
            reel.StartSpinning(isFastMode); // Pass fast mode state
        }

        // Calculate actual spin duration based on speed mode
        float actualSpinDuration = isFastMode ?
            baseSpinDuration * fastModeMultiplier :
            baseSpinDuration;

        // Wait for base spin duration
        yield return new WaitForSeconds(actualSpinDuration);

        // Start the dynamic stopping manager
        activeStoppingManagerCoroutine = StartCoroutine(ManageReelStopping());
    }
    // NEW: Dynamic stopping manager that can respond to tension effects
    private IEnumerator ManageReelStopping()
    {
        // Safety check: ensure arrays are properly initialized
        if (reelStopScheduled == null || reelStopTimes == null ||
            reelStopScheduled.Length != reels.Length || reelStopTimes.Length != reels.Length)
        {
            Debug.LogError("[SlotMachineController] Stopping arrays not properly initialized");
            yield break;
        }
        // Debug.Log("[SlotMachineController] Starting dynamic reel stopping manager");

        spinStartTimeStamp = Time.time;

        // Initialize stop scheduling for all reels with cumulative delays
        for (int i = 0; i < reels.Length; i++)
        {
            reelStopScheduled[i] = false;
            reelStopTimes[i] = CalculateInitialStopTime(i);
            //Debug.Log($"[SlotMachineController] Reel {i} initial stop time: {reelStopTimes[i]:F2}s (delay: {reelStopTimes[i] - spinStartTimeStamp:F2}s)");
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

        // Wait until all reels have fully stopped
        yield return new WaitUntil(() => AllReelsStopped());

        // Debug.Log("[SlotMachineController] All reels stopped, finalizing spin");
        FinalizeSpin();
    }

    // NEW: Calculate when a reel should stop initially (can be modified by tension effects)
    private float CalculateInitialStopTime(int reelIndex)
    {
        float cumulativeDelay = 0f;
        float[] currentDelays = GetCurrentReelStopDelays();

        // Add bounds checking
        if (reelIndex >= currentDelays.Length)
        {
            Debug.LogError($"[SlotMachineController] ReelIndex {reelIndex} exceeds delay array size {currentDelays.Length}. Using default delay.");
            return spinStartTimeStamp + 0.5f * (reelIndex + 1);
        }

        for (int i = 0; i <= reelIndex; i++)
        {
            // Additional safety check inside the loop
            if (i >= currentDelays.Length)
            {
                Debug.LogError($"[SlotMachineController] Loop index {i} exceeds delay array size {currentDelays.Length}");
                break;
            }

            float reelDelay = currentDelays[i];

            // Apply additional multiplier if in fast mode
            if (isFastMode)
            {
                reelDelay *= fastModeStopDelayMultiplier;
            }

            cumulativeDelay += reelDelay;
        }

        return spinStartTimeStamp + baseSpinDuration + cumulativeDelay;
    }


    private float[] GetCurrentReelStopDelays()
    {
        return isFastMode ? fastReelStopDelays : reelStopDelays;
    }

    public bool callOnce = false;
    private IEnumerator HandleIndividualReelStop(int reelIndex)
    {
        // Wait until it's time for this reel to stop
        yield return new WaitUntil(() => ShouldReelStop(reelIndex));

        yield return new WaitForSeconds(0.4f);

        // Mark as scheduled and stop the reel
        // if (!callOnce)
        // {
        //     DelaySubsequentReelsForUntilWeGetResults(WebManAPI.Instance.actionDuration);
        //     callOnce = true;
        // }
        reelStopScheduled[reelIndex] = true;
        reels[reelIndex].BeginStopping();

        // *** ADD THIS: Wait for tension animation to complete if this reel uses tension ***
        if (reels[reelIndex].useTensionLinearMovement)
        {
            yield return new WaitUntil(() => reels[reelIndex].IsTensionAnimationComplete);
        }
    }

    // NEW: Dynamic condition checking for when a reel should stop
    private bool ShouldReelStop(int reelIndex)
    {
        // If stop was manually requested, stop immediately
        if (stopRequested)
            return true;

        // In non-demo mode, wait for API response before allowing any reels to stop
        // if (waitingForAPIResponse && !apiResponseReceived)
        // {
        //     return false;
        // }

        // Check if any preceding reel is in tension mode and should block this reel
        for (int i = 0; i < reelIndex; i++)
        {
            if (reelsInTensionMode[i])
            {
                return false;
            }
        }

        // Check normal timing
        float currentTime = Time.time;
        float scheduledStopTime = reelStopTimes[reelIndex];

        return currentTime >= scheduledStopTime;
    }

    // Add this new method to be called by WebManAPI when response is received:
    public void OnAPIResponseReceived()
    {
        if (!waitingForAPIResponse) return;

        // Wait a short moment to ensure target symbols are fully assigned
        StartCoroutine(DelayedAPIResponseConfirmation());
    }

    private IEnumerator DelayedAPIResponseConfirmation()
    {
        // Small delay to ensure all target symbols are properly assigned
        yield return new WaitForSeconds(0.3f);

        apiResponseReceived = true;
    }

    // NEW: Apply tension effects dynamically to stop times
    private float ApplyDynamicTensionToStopTime(int reelIndex, float originalStopTime)
    {
        float modifiedStopTime = originalStopTime;

        // Reel 3 tension effect
        if (reelIndex == 2 && enableTensionEffect && freeSpinDetectedOnReel1 && freeSpinDetectedOnReel2)
        {
            if (!tensionEffectApplied)
            {
                // Debug.Log("[SlotMachineController] Applying Reel 3 tension effect dynamically!");
                float tensionDelay = isFastMode ? extendedReel3Delay * fastModeStopDelayMultiplier : extendedReel3Delay;
                modifiedStopTime += tensionDelay;
                tensionEffectApplied = true;

                // IMPORTANT: Update stop times for subsequent reels to maintain delays
                DelaySubsequentReelsForTension(reelIndex, tensionDelay); // FIX: Use existing method
            }
        }

        // Reel 5 tension effect - ADD timing logic (flag and immediate effects already applied)
        if (reelIndex == 4 && enableReel5TensionEffect && reel5TensionEffectApplied)
        {
            // Debug.Log("[SlotMachineController] Applying Reel 5 tension timing dynamically!");
            float tensionDelay = isFastMode ? extendedReel5Delay * fastModeStopDelayMultiplier : extendedReel5Delay;
            modifiedStopTime += tensionDelay;

            // No subsequent reels to update for reel 5
        }

        return modifiedStopTime;
    }

    // NEW: Apply any special effects when a reel is about to stop
    private void ApplyTensionEffectsForReel(int reelIndex)
    {
        // Reel 3 tension
        if (reelIndex == 2 && enableTensionEffect && freeSpinDetectedOnReel1 && freeSpinDetectedOnReel2)
        {
            SlotReel reel3 = reels[2];

            // Only apply if not already applied
            if (!reel3.useTensionLinearMovement)
            {
                reel3.currentSpeedMultiplier = reel3TensionMultiplier;
                reel3.useTensionLinearMovement = true;
                // Debug.Log($"[SlotMachineController] Applied fallback tension effects to reel 3");
            }
        }

        // Reel 5 tension - same pattern, different reel
        if (reelIndex == 4 && enableReel5TensionEffect && reel5TensionEffectApplied)
        {
            SlotReel reel5 = reels[4];

            // Only apply if not already applied
            if (!reel5.useTensionLinearMovement)
            {
                reel5.currentSpeedMultiplier = reel5TensionMultiplier;
                reel5.useTensionLinearMovement = true;
                // Debug.Log($"[SlotMachineController] Applied fallback tension effects to reel 5");
            }
        }
    }

    // ==========================================
    // END NEW SEPARATED SYSTEM
    // ==========================================

    public void ResetReelDelaysOnMissedFreeSpin()
    {
        // Create a default timing profile with all delays at 0.5s
        var defaultDelaysProfile = new SpinTimingProfile(
            "default_delays",
            baseSpinDuration,  // Keep the current base spin duration
            fastModeMultiplier,  // Keep the current fast mode multiplier
            new float[] { 0.4f, 0.4f, 0.4f, 0f, 0.4f }  // Reset all reel delays to 0.5s
        );

        // Apply this timing profile
        SetSpinTimingProfile(defaultDelaysProfile);

        // Reset the tension effect flags
        tensionEffectApplied = false;

        // Reset the linear movement flag for all reels
        foreach (SlotReel reel in reels)
        {
            reel.useTensionLinearMovement = false;
            reel.currentSpeedMultiplier = 1.0f;

            // Stop any tension sounds
            if (reel.isTensionSoundPlaying)
            {
                reel.StopTensionSound();
            }
        }

        // Debug.Log("Reset reel delays to default (0.5s) due to no free spin on reel 1");
    }

    // Method to adjust all timing parameters at once
    public void SetSpinTimingProfile(SpinTimingProfile profile)
    {
        baseSpinDuration = profile.baseSpinDuration;
        fastModeMultiplier = profile.fastModeMultiplier;

        // Copy provided delays or resize if needed
        if (profile.reelStopDelays.Length == reels.Length)
        {
            reelStopDelays = profile.reelStopDelays;
        }
        else
        {
            // Debug.LogWarning("Provided reel delays don't match reel count. Resizing array.");
            reelStopDelays = new float[reels.Length];
            for (int i = 0; i < reels.Length; i++)
            {
                // Use provided delays where available, otherwise use defaults
                if (i < profile.reelStopDelays.Length)
                    reelStopDelays[i] = profile.reelStopDelays[i];
                else
                    reelStopDelays[i] = 0.3f + (i * 0.2f);
            }
        }

        // Debug.Log($"Spin timing updated: Base={baseSpinDuration}, Fast multiplier={fastModeMultiplier}");
    }
    private string GenerateTransactionID()
    {
        System.Random random = new System.Random();
        string transactionID = "";

        for (int i = 0; i < 10; i++)
        {
            transactionID += random.Next(0, 10).ToString();
        }

        return transactionID;
    }
    public void GenerateRandomTargetSymbolsForDemo()
    {
        if (!isDemoMode || !useRandomSymbolsInDemo)
            return;

        // Debug.Log("[SlotMachineController] Generating random target symbols for demo mode using spawn weights");

        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> randomSymbols = GenerateWeightedRandomSymbolsForReel(reelIndex);
            SetTargetSymbolsForReel(reelIndex, randomSymbols);

            // Log what symbols were generated for this reel
            string symbolNames = string.Join(", ", randomSymbols.Select(s => s.type.ToString()));
            // Debug.Log($"[Demo] Reel {reelIndex} target symbols: {symbolNames}");
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

    private SymbolData GetWeightedRandomSymbolForReel(int reelIndex, ref bool freeSpinAlreadyAdded)
    {
        // Create list of valid symbols for this reel
        List<SymbolData> validSymbols = new List<SymbolData>();

        // Track mask reel separately
        bool allowMaskReel = false || reelIndex >= 2 && reelIndex <= 4 && !FreeSpinManager.Instance.IsFreeSpinActive;

        foreach (SymbolData symbol in symbolDatabase)
        {
            // Handle MaskReel symbol specially
            // if (symbol.type == SymbolData.SymbolType.FreeSpin)
            // {
            //     if (allowMaskReel)
            //     {
            //         validSymbols.Add(symbol);
            //     }
            //     continue; // Skip to next symbol
            // }

            // Skip wild symbols on first reel
            if (reelIndex == 0 && symbol.type == SymbolData.SymbolType.Wild)
                continue;

            // Skip free spin symbols on reels beyond index 2 (reels 4 and 5)
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
            // Debug.LogError($"No valid symbols available for reel {reelIndex}!");
            return null;
        }

        // Use weighted random selection based on spawnWeight
        SymbolData selectedSymbol = SelectWeightedRandomSymbol(validSymbols);

        // Track if we added a free spin symbol
        if (selectedSymbol != null && selectedSymbol.type == SymbolData.SymbolType.FreeSpin)
        {
            freeSpinAlreadyAdded = true;
        }

        return selectedSymbol;
    }

    private SymbolData SelectWeightedRandomSymbol(List<SymbolData> symbols)
    {
        if (symbols.Count == 0)
            return null;

        // Calculate total weight
        float totalWeight = symbols.Sum(s => s.spawnWeight);

        if (totalWeight <= 0)
        {
            // Debug.LogWarning("Total spawn weight is 0 or negative. Using equal probability.");
            return symbols[UnityEngine.Random.Range(0, symbols.Count)];
        }

        // Get random value within total weight range
        float randomWeight = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // Find the symbol that corresponds to this weight
        foreach (SymbolData symbol in symbols)
        {
            currentWeight += symbol.spawnWeight;
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
        // Return a safe symbol based on reel index
        var safeSymbols = symbolDatabase.Where(s =>
            // Exclude MaskReel during free spins or on reels 1-2
            (s.type != SymbolData.SymbolType.FreeSpin ||
             (!FreeSpinManager.Instance.IsFreeSpinActive && reelIndex >= 2 && reelIndex <= 4)) &&
            (reelIndex == 0 ? s.canAppearInFirstReel && s.type != SymbolData.SymbolType.Wild : true) &&
            (reelIndex > 2 ? s.type != SymbolData.SymbolType.FreeSpin : true)
        ).ToList();

        if (safeSymbols.Count > 0)
        {
            // Prefer lower-value symbols as fallback
            var cardSymbols = safeSymbols.Where(s =>
                s.type == SymbolData.SymbolType.Ten ||
                s.type == SymbolData.SymbolType.Jack ||
                s.type == SymbolData.SymbolType.Queen).ToList();

            if (cardSymbols.Count > 0)
                return cardSymbols.First();
            else
                return safeSymbols.First();
        }

        return null;
    }

    // Add this method to create interesting symbol combinations for demo
    public void GenerateInterestingDemoSymbols()
    {
        if (!isDemoMode)
            return;

        float randomChoice = UnityEngine.Random.Range(0f, 1f);

        if (randomChoice < 0.10f) // 10% chance for free spin scenario
        {
            // Debug.Log("[Demo] Generating EXCLUSIVE Free Spin scenario");
            GenerateExclusiveFreeSpinScenario();
        }
        else if (randomChoice < 0.20f) // 10% chance for mask reel scenario  
        {
            // Debug.Log("[Demo] Generating EXCLUSIVE Mask Reel scenario");
            GenerateExclusiveMaskReelScenario();
        }
        else // 80% chance for weighted random (normal wins allowed)
        {
            // Debug.Log("[Demo] Generating normal weighted random symbols");
            GenerateNormalRandomSymbols();
        }
    }

    // NEW: Generate exclusive freespin scenario (no other special features)
    private void GenerateExclusiveFreeSpinScenario()
    {
        // Debug.Log("[Demo] Generating EXCLUSIVE Free Spin scenario - no mask reels allowed");

        // Generate freespins on first 3 reels
        for (int reelIndex = 0; reelIndex < 3 && reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            var freeSpinSymbol = symbolDatabase.FirstOrDefault(s => s.type == SymbolData.SymbolType.FreeSpin);
            bool freeSpinAdded = false;

            for (int i = 0; i < reels[reelIndex].visibleSymbols; i++)
            {
                // 70% chance to add freespin if we haven't added one yet
                if (!freeSpinAdded && UnityEngine.Random.Range(0f, 1f) < 0.7f && freeSpinSymbol != null)
                {
                    symbols.Add(freeSpinSymbol);
                    freeSpinAdded = true;
                }
                else
                {
                    // Use safe symbols (NO mask reels allowed)
                    symbols.Add(GetSafeNonSpecialSymbolForReel(reelIndex));
                }
            }

            SetTargetSymbolsForReel(reelIndex, symbols);
        }

        // Fill remaining reels with safe symbols (NO mask reels)
        for (int reelIndex = 3; reelIndex < reels.Length; reelIndex++)
        {
            SetTargetSymbolsForReel(reelIndex, GenerateSafeSymbolsForReel(reelIndex));
        }
    }

    // NEW: Generate exclusive mask reel scenario (no other special features)
    private void GenerateExclusiveMaskReelScenario()
    {
        // Debug.Log("[Demo] Generating EXCLUSIVE Mask Reel scenario - no freespins allowed");

        // Fill first 2 reels with safe symbols (NO freespins)
        for (int reelIndex = 0; reelIndex < 2 && reelIndex < reels.Length; reelIndex++)
        {
            SetTargetSymbolsForReel(reelIndex, GenerateSafeSymbolsForReel(reelIndex));
        }

        // Generate mask reels on reels 3, 4, 5
        for (int reelIndex = 2; reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            var maskReelSymbol = symbolDatabase.FirstOrDefault(s => s.type == SymbolData.SymbolType.FreeSpin);
            bool maskReelAdded = false;

            for (int i = 0; i < reels[reelIndex].visibleSymbols; i++)
            {
                // 70% chance to add mask reel if we haven't added one yet
                if (!maskReelAdded && UnityEngine.Random.Range(0f, 1f) < 0.7f && maskReelSymbol != null)
                {
                    symbols.Add(maskReelSymbol);
                    maskReelAdded = true;
                }
                else
                {
                    // Use safe symbols (NO freespins allowed)
                    symbols.Add(GetSafeNonSpecialSymbolForReel(reelIndex));
                }
            }

            SetTargetSymbolsForReel(reelIndex, symbols);
        }
    }

    // NEW: Generate normal symbols with potential regular wins
    private void GenerateNormalRandomSymbols()
    {
        // Debug.Log("[Demo] Generating normal random symbols - regular wins allowed");

        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            List<SymbolData> randomSymbols = GenerateWeightedRandomSymbolsForReel(reelIndex);
            SetTargetSymbolsForReel(reelIndex, randomSymbols);

            // Log what symbols were generated for this reel
            string symbolNames = string.Join(", ", randomSymbols.Select(s => s.type.ToString()));
            // Debug.Log($"[Demo] Reel {reelIndex} target symbols: {symbolNames}");
        }
    }

    // NEW: Get safe symbols that exclude special features
    private SymbolData GetSafeNonSpecialSymbolForReel(int reelIndex)
    {
        // Get symbols that are NOT FreeSpin or MaskReel
        var safeSymbols = symbolDatabase.Where(s =>
            s.type != SymbolData.SymbolType.FreeSpin &&
            s.type != SymbolData.SymbolType.FreeSpin &&
            (reelIndex == 0 ? s.canAppearInFirstReel && s.type != SymbolData.SymbolType.Wild : true)
        ).ToList();

        if (safeSymbols.Count > 0)
        {
            return SelectWeightedRandomSymbol(safeSymbols);
        }

        // Fallback to any basic symbol
        return GetSafeSymbolForReel(reelIndex);
    }

    // NEW: Generate multiple safe symbols for a reel
    private List<SymbolData> GenerateSafeSymbolsForReel(int reelIndex)
    {
        List<SymbolData> targetSymbols = new List<SymbolData>();
        int symbolsNeeded = reels[reelIndex].visibleSymbols;

        for (int i = 0; i < symbolsNeeded; i++)
        {
            SymbolData safeSymbol = GetSafeNonSpecialSymbolForReel(reelIndex);
            if (safeSymbol != null)
            {
                targetSymbols.Add(safeSymbol);
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

    private void GenerateFreeSpinScenario()
    {
        // Debug.Log("[Demo] Generating Free Spin scenario (LEGACY - use GenerateExclusiveFreeSpinScenario instead)");
        GenerateExclusiveFreeSpinScenario();
    }

    private void UpdateTransactionIDDisplay()
    {
        if (uiElements.TransactionID != null)
        {
            uiElements.TransactionID.landscape.text = currentTransactionID;
            uiElements.TransactionID.portrait.text = currentTransactionID;
        }
    }

    // MODIFIED: These tension effect methods now work with the dynamic system
    public void ApplyReel3TensionEffect()
    {
        if (!enableTensionEffect)
        {
            // Debug.Log("[SlotMachineController] Tension effect disabled, not applying");
            return;
        }

        // Debug.Log("[SlotMachineController] Applying Reel 3 tension effect...");

        // The dynamic system will handle this when reel 3 is about to stop
        // We just need to ensure the flags are set for detection
        tensionEffectApplied = true;
    }

    private void LoadLastTransactionID()
    {
        if (PlayerPrefs.HasKey(TRANSACTION_ID_KEY))
        {
            currentTransactionID = PlayerPrefs.GetString(TRANSACTION_ID_KEY);
        }
        else
        {
            currentTransactionID = GenerateTransactionID();
        }
        UpdateTransactionIDDisplay();
    }
    public void ResetFreeSpinDetectionFlags()
    {
        bool prevR1 = freeSpinDetectedOnReel1;
        bool prevR2 = freeSpinDetectedOnReel2;
        bool prevFSR1 = freeSpinOnReel1;
        bool prevFSR2 = freeSpinOnReel2;

        freeSpinDetectedOnReel1 = false;
        freeSpinDetectedOnReel2 = false;
        freeSpinOnReel1 = false;
        freeSpinOnReel2 = false;


        // Reset tension flags
        tensionEffectApplied = false;
        reel5TensionEffectApplied = false;

        ResetBonusDetectionFlags();

        // Debug.Log($"[SlotMachineController] Reset all detection flags. Previous: R1={prevR1}, R2={prevR2}, FSR1={prevFSR1}, FSR2={prevFSR2}");
    }

    // Helper method to calculate total spin duration
    public float CalculateTotalSpinDuration(bool fastMode = false)
    {
        float multiplier = fastMode ? fastModeMultiplier : 1.0f;
        float baseTime = baseSpinDuration * multiplier;

        // Find the maximum reel delay
        float maxReelDelay = 0f;
        foreach (float delay in reelStopDelays)
        {
            maxReelDelay = Mathf.Max(maxReelDelay, delay);
        }

        // Approximate the reel stopping animation time
        float reelStoppingTime = 0.5f; // Adjust based on your actual reel stopping animations

        return baseTime + (maxReelDelay * multiplier) + reelStoppingTime;
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

    // MODIFIED: FinalizeSpin now cleans up the new stopping system
    private void FinalizeSpin()
    {
        float basePayout = winCalculator.CalculateWin(reels);
        float winAmount = basePayout;

        if (isDemoMode)
        {
            if (winAmount > 0)
            {
                if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
                {
                    FreeSpinManager.Instance.AccumulateWin(winAmount);
                }
                else
                {
                    betManager.StartWinAnimation(winAmount);
                }
            }
            else
            {
                if (!FreeSpinManager.Instance.IsFreeSpinActive)
                {
                    // UPDATED: Use translation instead of hard-coded "PLACE YOUR BET!"
                    SetPlaceBetText();
                }
            }
        }
        else
        {
            if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
            {
                if (betManager.targetWinAmount > 0)
                {
                    FreeSpinManager.Instance.AccumulateWin(betManager.targetWinAmount);
                }
            }
            else
            {
                betManager.ShowWinAfterSpin();
            }

            if (betManager.targetWinAmount <= 0 && (FreeSpinManager.Instance == null || !FreeSpinManager.Instance.IsFreeSpinActive))
            {
                SetPlaceBetText();
            }
            else
            {
                FreeSpinManager.Instance.SetInteractable();
            }
        }

        // Only change button visuals if NOT in auto-spin mode
        if (autoSpinManager == null || !autoSpinManager.IsAutoSpinning)
        {
            uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(false);
            uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(true);
            uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(false);
        }

        if (FreeSpinManager.Instance != null && !FreeSpinManager.Instance.IsFreeSpinActive && !MaskReelManager.Instance.IsMaskReelActive)
        {
            // Re-enable the spin button regardless of whether it was locked
            uiElements.spinButton.landscape.interactable = true;
            uiElements.spinButton.portrait.interactable = true;

            // Enable other buttons
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
            TransitionSpinButtonColor(false);
        }

        // Reset spin-related flags
        isSpinning = false;
        stopRequested = false;
        buttonLocked = false;
        RestoreOriginalDelays();
        ResetFreeSpinDetectionFlags();

        // NEW: Clean up stopping system
        CleanupStoppingSystem();

        foreach (SlotReel reel in reels)
        {
            reel.currentSpeedMultiplier = 1.0f;
            reel.useTensionLinearMovement = false;

            // Make sure tension sounds are stopped
            if (reel.isTensionSoundPlaying)
            {
                reel.StopTensionSound();
            }
        }

        OnSpinComplete?.Invoke();
    }

    // NEW: Clean up the stopping system
    private void CleanupStoppingSystem()
    {
        // Stop all individual reel stop coroutines
        foreach (var kvp in reelStopCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        reelStopCoroutines.Clear();

        // Stop the stopping manager if it's still running
        if (activeStoppingManagerCoroutine != null)
        {
            StopCoroutine(activeStoppingManagerCoroutine);
            activeStoppingManagerCoroutine = null;
        }

        // Reset stopping arrays
        for (int i = 0; i < reelStopScheduled.Length; i++)
        {
            reelStopScheduled[i] = false;
            reelStopTimes[i] = 0f;
        }

        // Debug.Log("[SlotMachineController] Stopping system cleaned up");
    }

    // NEW: Debug method to visualize current stopping schedule
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogStoppingSchedule()
    {
        if (isSpinning)
        {
            float currentTime = Time.time;
            string schedule = "[SlotMachineController] Current stopping schedule:\n";

            for (int i = 0; i < reels.Length && i < reelStopTimes.Length; i++)
            {
                float timeUntilStop = reelStopTimes[i] - currentTime;
                string status = reelStopScheduled[i] ? "STOPPING" : (timeUntilStop <= 0 ? "READY" : $"WAITING ({timeUntilStop:F1}s)");
                schedule += $"  Reel {i}: {status} (scheduled: {reelStopTimes[i]:F2}s)\n";
            }

            // Debug.Log(schedule);
        }
    }
    public SlotReel[] GetReels()
    {
        return reels;
    }
    private void RestoreOriginalDelays()
    {
        if (tensionEffectApplied || reel5TensionEffectApplied)
        {
            // Debug.Log("[SlotMachineController] Restoring original reel delays...");
            // Debug.Log($"[SlotMachineController] Current delays: {string.Join(", ", reelStopDelays)}");

            originalReelStopDelays.CopyTo(reelStopDelays, 0);
            // Debug.Log($"[SlotMachineController] Restored delays: {string.Join(", ", reelStopDelays)}");

            // Reset the linear movement flag for all reels and stop any tension sounds
            foreach (SlotReel reel in reels)
            {
                reel.useTensionLinearMovement = false;
                // Debug.Log($"[SlotMachineController] Reset linear movement for reel {Array.IndexOf(reels, reel)}");
            }

            tensionEffectApplied = false;
            reel5TensionEffectApplied = false;
            // Debug.Log("[SlotMachineController] Tension effect fully reset");
        }
    }
    public void StartAllReels()
    {
        RestoreOriginalDelays();

        // Reset API response tracking
        apiResponseReceived = false;
        waitingForAPIResponse = true;

        if (!FreeSpinManager.Instance.IsFreeSpinActive)
        {
            if (!betManager.CanPlaceBet())
            {
                GameManager.Instance.PromptMan.DisplayPrompt(PromptType.InsufficientFunds);
                return;
            }
            else
            {
                betManager.DeductBet();
            }
        }

        foreach (SlotReel reel in reels)
        {
            reel.ResetReelPosition();
        }

        ResetAllAnimations();
        currentTransactionID = GenerateTransactionID();
        UpdateTransactionIDDisplay();

        ResetBonusDetectionFlags();

        OnSpinStart?.Invoke();

        PlayCyclingSpinSound();

        // Record the time when spin started
        spinStartTime = Time.time;
        buttonLocked = false;
        stopRequested = false;
        isSpinning = true;

        activeSpinCoroutine = StartCoroutine(SpinAllReels());
    }
    private IEnumerator WaitForOscillationsAndFinalize()
    {
        // Give a short delay for the reels to start their oscillation effect
        yield return new WaitForSeconds(0.1f);

        // Check if any reels have free spin symbols to ensure proper detection
        CheckForFreeSpinSymbols();

        // Wait until all reels have completed their oscillation
        yield return new WaitUntil(() => AllReelsStopped());

        // Finalize the spin
        FinalizeSpin();
    }

    // Add this helper method to ensure free spin symbols are detected in immediate stop
    private void CheckForFreeSpinSymbols()
    {
        freeSpinDetectedOnReel1 = false;
        freeSpinDetectedOnReel2 = false;

        // Check reels 1 and 2 for free spin symbols
        if (reels.Length > 0 && DoesReelHaveFreeSpin(0))
        {
            freeSpinDetectedOnReel1 = true;
            // Debug.Log("Free Spin detected on Reel 1 during immediate stop");
        }

        if (reels.Length > 1 && DoesReelHaveFreeSpin(1) && freeSpinDetectedOnReel1)
        {
            freeSpinDetectedOnReel2 = true;
            // Debug.Log("Free Spin detected on Reel 2 during immediate stop - Will apply tension for Reel 3!");

            // Apply tension effect if Reel 3 is still spinning
            if (reels.Length > 2 && reels[2].IsSpinning && enableTensionEffect)
            {
                ApplyReel3TensionEffect();
            }
        }
    }

    // MODIFIED: StopAllReels now works with the new system
    private void StopAllReels()
    {
        // Debug.Log("[SlotMachineController] Stopping all reels IMMEDIATELY");

        for (int i = 0; i < reels.Length; i++)
        {
            if (reels[i].IsSpinning)
            {
                reels[i].PlayImmediateStop(); // ? Simple call
            }
        }

        // Set controller state
        stopRequested = true;
    }

    private void ResetAllAnimations()
    {
        foreach (SlotReel reel in reels)
        {
            foreach (Transform child in reel.symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null)
                {
                    symbol.ResetAnimation(); // Reset animations on all symbols
                    symbol.DeactivateWinHighlight();
                }
            }
        }
    }

    public void SetTargetSymbolsForReel(int reelIndex, List<SymbolData> symbols)
    {
        if (reelIndex >= 0 && reelIndex < reels.Length)
        {
            // Clear previous targets
            reels[reelIndex].targetSymbols.Clear();

            // Add new targets and ensure count matches visible symbols
            if (symbols != null && symbols.Count > 0)
            {
                reels[reelIndex].targetSymbols.AddRange(symbols);
                // Debug.Log($"Set {symbols.Count} target symbols for reel {reelIndex}");
            }
        }
    }

    public void TransitionSpinButtonColor(bool toDisabled)
    {
        // Stop any ongoing color transition
        if (spinButtonColorTransition != null)
        {
            StopCoroutine(spinButtonColorTransition);
        }

        // Start new transition
        spinButtonColorTransition = StartCoroutine(TransitionButtonColorCoroutine(toDisabled));
    }

    private IEnumerator TransitionButtonColorCoroutine(bool toDisabled)
    {
        float duration = 0.2f; // 0.2 second transition
        float elapsedTime = 0f;

        // Determine start and target colors
        Color startColor, targetColor;
        if (toDisabled)
        {
            startColor = normalButtonColor;
            targetColor = disabledButtonColor;
        }
        else
        {
            startColor = disabledButtonColor;
            targetColor = normalButtonColor;
        }

        // Images to transition (add circular images when they're properly set up)
        Image[] images = {
            uiElements.spinButtonVisuals.normalLandscape,
            uiElements.spinButtonVisuals.spinningLandscape,
            uiElements.spinButtonVisuals.normalPortrait,
            uiElements.spinButtonVisuals.spinningPortrait,
            uiElements.spinButtonVisuals.circularLandscape,
            uiElements.spinButtonVisuals.circularPortrait
        };

        // Transition loop
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration; // normalized time (0 to 1)
            Color currentColor = Color.Lerp(startColor, targetColor, t);

            // Apply color to all button images
            foreach (Image image in images)
            {
                if (image != null) // Make sure the image exists
                {
                    image.color = currentColor;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end on the exact target color
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = targetColor;
            }
        }

        spinButtonColorTransition = null;
    }
    public bool DoesReelHaveMaskReel(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Length) return false;

        var visibleSymbols = reels[reelIndex].GetTopVisibleSymbols(reels[reelIndex].visibleSymbols);
        return visibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
    }

    public void StartFreeSpin()
    {
        if (isSpinning)
        {
            return;
        }

        StartCoroutine(SpinAllReels());
        WebManAPI.Instance.InitiateFreeSpinRequest();
    }

    // Add this method to SlotMachineController.cs
    public void SetUseRandomSymbolsInDemo(bool useRandom)
    {
        useRandomSymbolsInDemo = useRandom;
        // Debug.Log($"[SlotMachineController] useRandomSymbolsInDemo set to: {useRandom}");
    }

    private void OnSpinButtonClicked()
    {
        SoundManager.Instance.PlaySound("button");

        if (betManager.IsAnimatingWin)
        {
            betManager.FinalizeWinAnimation(FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive);
            return;
        }

        SoundManager.Instance.StopSound("freespinscoring");

        if (!isSpinning)
        {
            StartAllReels();
        }
        else if (!stopRequested && !buttonLocked)
        {
            // Check if this is an early stop attempt
            float timeElapsed = Time.time - spinStartTime;

            if (timeElapsed < earlyStopLockThreshold)
            {
                // Early stop attempt - lock the button until spin completes
                // Debug.Log("Early stop attempt detected - locking button until spin completes");
                buttonLocked = true;

                // Disable the spin button completely
                uiElements.spinButton.landscape.interactable = false;
                uiElements.spinButton.portrait.interactable = false;
                TransitionSpinButtonColor(true);
                // Optional: Play a "denied" sound
                // SoundManager.Instance.PlaySound("denied");
            }
            else
            {
                // Normal stop request - proceed with stopping
                stopRequested = true;
                StopAllReels();
            }
        }
    }

    // MODIFIED: Tension effects now work with the dynamic system
    public void ApplyReel5TensionEffect()
    {
        if (!enableReel5TensionEffect)
        {
            // Debug.Log("[SlotMachineController] Reel 5 tension effect disabled, not applying");
            return;
        }

        // Debug.Log("[SlotMachineController] Applying Reel 5 tension effect...");

        // The dynamic system will handle this when reel 5 is about to stop
        // We just need to ensure the flags are set for detection
        reel5TensionEffectApplied = true;
    }

    // Add this method to check for woman and wild symbols on reels


    public bool DoesReelHaveWildSymbol(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Length) return false;

        var visibleSymbols = reels[reelIndex].GetTopVisibleSymbols(reels[reelIndex].visibleSymbols);
        return visibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.Wild);
    }

    // Add this handler method for early detection of woman/wild symbols

    public bool DoesReelHaveBonusSymbol(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reelHasBonusSymbol.Length)
            return false;
        return reelHasBonusSymbol[reelIndex];
    }


    public void SetReelBonusSymbol(int reelIndex, bool hasBonus)
    {
        if (reelIndex >= 0 && reelIndex < reelHasBonusSymbol.Length)
        {
            reelHasBonusSymbol[reelIndex] = hasBonus;
            // Debug.Log($"[SlotMachineController] Reel {reelIndex} bonus symbol: {hasBonus}");
        }
    }
    public void ResetBonusDetectionFlags()
    {
        for (int i = 0; i < reelHasBonusSymbol.Length; i++)
        {
            reelHasBonusSymbol[i] = false;
        }
        // Debug.Log("[SlotMachineController] Reset bonus detection flags");
    }
    private void ResetButtonColors()
    {
        // Reset all spin button images to normal color
        Image[] spinButtonImages = {
            uiElements.spinButtonVisuals.normalLandscape,
            uiElements.spinButtonVisuals.spinningLandscape,
            uiElements.spinButtonVisuals.normalPortrait,
            uiElements.spinButtonVisuals.spinningPortrait,
            uiElements.spinButtonVisuals.circularLandscape,
            uiElements.spinButtonVisuals.circularPortrait
        };

        foreach (Image image in spinButtonImages)
        {
            if (image != null)
            {
                image.color = normalButtonColor;
            }
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
}

// Helper class for storing timing profiles
[System.Serializable]
public class SpinTimingProfile
{
    public string profileName;
    public float baseSpinDuration = 1.0f;
    public float fastModeMultiplier = 0.6f;
    public float[] reelStopDelays;

    public SpinTimingProfile(string name, float baseDuration, float fastMultiplier, float[] delays)
    {
        profileName = name;
        baseSpinDuration = baseDuration;
        fastModeMultiplier = fastMultiplier;
        reelStopDelays = delays;
    }
}