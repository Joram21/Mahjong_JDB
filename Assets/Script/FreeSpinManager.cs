using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Linq;
using Lean.Gui;
using Random = UnityEngine.Random;

public class FreeSpinManager : MonoBehaviour
{
    // Singleton pattern implementation
    private static FreeSpinManager _instance;
    public static event Action OnSwitchToFreeSpin;
    public static FreeSpinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FreeSpinManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("FreeSpinManager");
                    _instance = go.AddComponent<FreeSpinManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Slot Machine References")]
    [SerializeField] private WinCalculator winCalculator;
    [SerializeField] private FinalizeClickBlocker clickBlocker;
    [SerializeField] private GameObject reels;

    [Header("Settings")]
    [SerializeField] private float delayBetweenFreeSpins = 0.3f;
    [SerializeField] private int additionalSpinsAmount = 10;
    [SerializeField] private float waitForWinAnimationDuration = 1.5f;

    // State management
    public bool freeSpinFeatureActive = false;
    private bool freeSpinHasBegun = false;
    public bool FreeSpinHasBegun
    {
        get => freeSpinHasBegun;
        set
        {
            OnSwitchToFreeSpin?.Invoke();
            freeSpinHasBegun = value;
        }
    }
    public bool IsFreeSpinActive => freeSpinHasBegun;
    public GameObject uppersection;

    [Header("UI References")]
    [SerializeField] private GameObject FreeSpinBG;
    [SerializeField] private GameObject SlotmachineBG;
    [SerializeField] private FreeSpinStarterClickBlocker freeSpinClickBlocker;
    [SerializeField] private UIElementManager uiElements;
    [SerializeField] private WinAnimationController winAnimationController;
    [Header("Multiplier System")]
    [SerializeField] private int currentMultiplier = 1;
    [SerializeField] private int maxMultiplier = 64;
    [SerializeField] private GameObject multiplierDisplay;
    [SerializeField] private Text multiplierText;
    [Header("Modified Free Spin Award System")]
    [SerializeField] private int freeSpinsPerAward = 5;  // Always add 5 at a time
    [SerializeField] private int maxTotalFreeSpins = 25; // Maximum 25 total free spins

    [Header("Background Settings")]
    [SerializeField] private Image slotMachineBackgroundImage;
    [SerializeField] private Sprite freeSpinBackgroundSprite;
    [SerializeField] private Sprite originalBackgroundSprite;

    [Header("Free Spin Award Panel")]
    [SerializeField] private GameObject totalWildsAwardPanel;
    [SerializeField] private GameObject freeSpinAwardPanel;
    [SerializeField] private Button startFreeSpinsButton;
    [SerializeField] private Image freeSpinCountTens;
    [SerializeField] private Image freeSpinCountUnits;
    [SerializeField] private Image totalWildsCountTens;
    [SerializeField] private Image totalWildsCountUnits;
    [SerializeField] private Sprite[] digitSprites;
    [SerializeField] private Sprite[] freeSpinCountDigitSprites;
    [SerializeField] private Sprite[] freeSpinWildCountDigitSprites;

    [Header("Current Spin Display")]
    [SerializeField] private Image currentSpinTens;
    [SerializeField] private Image currentSpinUnits;

    [Header("Total Spins Display")]
    [SerializeField] private Image totalSpinTens;
    [SerializeField] private Image totalSpinUnits;

    [Header("Total Wild Added Display")]
    [SerializeField] private Image[] totalWildAddedTens;
    [SerializeField] private Image[] totalWildAddedUnits;

    [Header("Free Spins During Free Spins")]
    [SerializeField] private GameObject bonusPanel;
    [SerializeField] private Text extraSpinsText;
    [SerializeField] private float extraSpinsPanelDuration = 3f;

    [Header("Mask Transformation")]
    [SerializeField] private float transformationAnimationDuration = 1.5f;
    [SerializeField] private int minimumConsecutiveReels = 3; // Minimum consecutive reels needed
    [SerializeField] private SymbolData[] availableMaskTypes; // Assign your 5 mask types in inspector

    [Header("Woman Reveal Animation (Independent)")]
    [SerializeField] private GameObject womanRevealAnimationObject; // The separate woman reveal animation GameObject
    [SerializeField] private Animator womanRevealAnimator; // Animator for the woman reveal effect
    [SerializeField] private string womanRevealTrigger = "PlayWomanReveal"; // Trigger name for woman reveal
    [SerializeField] private float womanMaskAnimationDuration = 2f;

    [Header("Mask Transformation")]
    private List<Symbol> currentTransformingMasks = new List<Symbol>();
    private SymbolData currentTargetMaskType = null;
    private bool maskTransformationExecuted = false;
    // Properties
    public int CurrentSpinIndex => currentSpin;
    public int RemainingFreeSpins => totalFreeSpins - currentSpin + 1;
    public int TotalFreeSpins => totalFreeSpins;

    // Private variables
    private int totalFreeSpins;
    private int currentSpin;
    private int totalWildsAdded;
    private Coroutine addingSpinsCoroutine;
    private bool isWaitingForNextSpin = false;
    private float accumulatedFreeSpinWins = 0f;
    private float previousDisplayedWin = 0f;

    // Mask Reel variables
    private bool maskReelTriggered = false;
    private int selectedMaskMultiplier = 1;
    private GameObject activeMaskReel;

    [Header("Win Display")]
    [SerializeField] private GameObject accumulatedWinPanel;
    [SerializeField] private Transform winAmountContainer;
    [SerializeField] private Transform initializingLinesContainer;
    [SerializeField] private Transform totalAmountContainer;
    [SerializeField] private GameObject digitPrefab;
    [SerializeField] private GameObject BluedigitPrefab;
    [SerializeField] private Sprite[] winDigitSprites;
    [SerializeField] private Sprite[] winblueDigitSprites;
    [SerializeField] private Sprite decimalPointSprite;
    [SerializeField] private Sprite bluedecimalPointSprite;
    [SerializeField] private Sprite bluecommaPointSprite;
    [SerializeField] private Sprite commaPointSprite;
    [SerializeField] private float digitSpacing = 10f;
    [SerializeField] private float decimalSpacing = 10f;
    [SerializeField] private float decimalScale = 0.4f;
    [SerializeField] private float counterIncrementSpeed = 0.05f;
    [SerializeField] private float finalWinDisplayDuration = 5f;
    [SerializeField] private Animator leftWinAnimator;
    [SerializeField] private Animator rightWinAnimator;
    [SerializeField] private AudioClip counterTickSound;
    [SerializeField] private AudioClip bigWinSound;
    [SerializeField] private float bigWinThreshold = 100f;
    private bool isHandlingRetrigger = false;
    private bool isAddingSpins = false;

    private List<Image> spawnedWinDigits = new List<Image>(); // Win amount
    private List<Image> spawnedLinesDigits = new List<Image>(); // Lines (already existed)
    private List<Image> spawnedTotalDigits = new List<Image>(); // Blue total
    private bool isDisplayingWin = false;

    // Events
    public event System.Action<int, int, int> OnFreeSpin;
    public System.Action OnFreeSpinsEnded;
    private List<Symbol> originalNonMaskSymbols = new List<Symbol>();
    private bool hasWinsDuringFreeSpins = false;
    [Header("Animation Timing")]
    [SerializeField] private float fanAnimationStartDelay = 2.767f; // 2 seconds 23 frames at 30fps
    [SerializeField] private bool enableOverlappingAnimations = true;
    Symbol[,] VisibleSymbolsGrid = new Symbol[5, 3];
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        InitializeBackgroundSprites();
        InitializeUI();
    }

    private void InitializeBackgroundSprites()
    {
        if (slotMachineBackgroundImage != null && originalBackgroundSprite == null)
        {
            originalBackgroundSprite = slotMachineBackgroundImage.sprite;
        }
    }

    private void InitializeUI()
    {
        // Hide all panels initially
        if (freeSpinAwardPanel != null) freeSpinAwardPanel.SetActive(false);
        if (bonusPanel != null) bonusPanel.SetActive(false);
        if (FreeSpinBG != null) FreeSpinBG.SetActive(false);
        if (uppersection != null) uppersection.SetActive(false);
        if (reels != null) reels.SetActive(false);

        freeSpinFeatureActive = false;

        if (clickBlocker != null)
        {
            clickBlocker.ActivateForFreeSpin(true);
        }
    }

    private void Start()
    {
        if (winCalculator == null)
            winCalculator = FindAnyObjectByType<WinCalculator>();

        // Subscribe to normal slot machine events
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnSpinStart += ResetDetectionFlags;
            SlotMachineController.Instance.OnSpinComplete += ResetDetectionFlags;
        }
        // Hide the woman reveal animation after completion
        if (womanRevealAnimationObject != null)
        {
            womanRevealAnimationObject.SetActive(false);  // ← SET TO INACTIVE
        }
        // Subscribe to free spin slot machine events
        if (FreeSpinSlotMachineController.Instance != null)
        {
            FreeSpinSlotMachineController.Instance.OnSpinComplete += HandleFreeSpinComplete;
        }
    }

    // ==========================================
    // FREE SPIN FEATURE - MODIFIED METHODS
    // ==========================================

    // Called when free spins are triggered from the NORMAL slot machine
    public IEnumerator HandleFreeSpinSequence()
    {
        yield return new WaitForSeconds(3.5f);
        SoundManager.Instance.StopSound("freespin");

        SetInteractable();

        // Calculate how many free spins to award
        int awardedFreeSpins = CalculateFreeSpinsAwarded();
        if (winAnimationController != null)
        {
            winAnimationController.CleanupPayoutDisplays();
        }
        // 1. Switch background sprite first
        if (slotMachineBackgroundImage != null && freeSpinBackgroundSprite != null)
        {
            slotMachineBackgroundImage.sprite = freeSpinBackgroundSprite;
        }

        // 2 & 3. Activate free spin background and reels, deactivate normal background
        LineManager.instance.ResetLines();
        LineManager.instance.AssignBg(true);
        WebManAPI.Instance.tempWin = SlotMachineController.Instance.betManager.targetWinAmount;
        VisibleSymbolsGrid = LineManager.instance.CopyVisibleSymbolsGrid();
        if (FreeSpinBG != null) FreeSpinBG.SetActive(true);
        if (SlotmachineBG != null) SlotmachineBG.SetActive(false);
        if (reels != null) reels.SetActive(true);

        // IMPORTANT: Switch ALL symbols to free spin mode ONCE when entering free spins
        SwitchAllSymbolsToFreeSpinMode(true);

        // 4. Show panel with number of free spins awarded
        ShowFreeSpinAwardPanel(awardedFreeSpins);

        // Play sound
        SoundManager.Instance.PlayFreeSpinMusicLoop();

        // 5. Wait for 1 seconds
        yield return new WaitForSeconds(2f);

        // 6. Hide the award panel
        if (freeSpinAwardPanel != null)
        {
            freeSpinAwardPanel.SetActive(false);
        }

        // 7. SIMPLE: Just activate the GameObject directly like MaskReelManager
        if (freeSpinClickBlocker != null)
        {
            freeSpinClickBlocker.gameObject.SetActive(true); // Direct activation
        }
    }

    public void StartFreeSpinsFromClick()
    {
        StartCoroutine(BeginFreeSpinSequence());
    }

    public IEnumerator BeginFreeSpinsFromClickBlocker()
    {
        if (freeSpinClickBlocker != null)
        {
            freeSpinClickBlocker.gameObject.SetActive(true);
        }
        yield return StartCoroutine(BeginFreeSpinSequence());
    }

    private void ShowFreeSpinAwardPanel(int spinsAwarded)
    {
        totalFreeSpins = spinsAwarded;
        UpdateFreeSpinCountDisplay(spinsAwarded);

        if (freeSpinAwardPanel != null)
        {
            freeSpinAwardPanel.SetActive(true);
        }

        // Activate the upper section with spin counts
        if (uppersection != null) uppersection.SetActive(true);

        // NEW: Update displays immediately when showing award panel
        currentSpin = 0; // Start at 0
        totalWildsAdded = 1; // Start at 0
        UpdateCurrentSpinDisplay(currentSpin);
        UpdateTotalWildAddedDisplay(totalWildsAdded);
        UpdateTotalSpinDisplay(totalFreeSpins);

    }

    public IEnumerator BeginFreeSpinSequence()
    {
        // initialize multiplier
        InitializeMultiplier();

        Time.timeScale = 1.4f;
        WebManAPI.Instance.tempPayLines = WebManAPI.Instance.payLines;
        freeSpinFeatureActive = true;
        hasWinsDuringFreeSpins = false;
        SetInteractable();

        FreeSpinHasBegun = true;
        OnFreeSpin?.Invoke(0, totalFreeSpins, totalFreeSpins);

        yield return new WaitForSeconds(0.5f);

        // Set current spin to 1 when actually starting the first spin
        currentSpin = 1;
        UpdateCurrentSpinDisplay(currentSpin);
        UpdateTotalWildAwardedDisplay(4);
        totalWildsAwardPanel.GetComponent<LeanWindow>().TurnOn();
        yield return new WaitForSeconds(1.0f);
        totalWildsAdded = Mathf.Min(currentMultiplier * 2, maxMultiplier); ;
        UpdateTotalWildAddedDisplay(totalWildsAdded);
        totalWildsAwardPanel.GetComponent<LeanWindow>().TurnOff();

        // Start the first free spin
        if (FreeSpinSlotMachineController.Instance != null)
        {
            FreeSpinSlotMachineController.Instance.StartFreeSpin();
        }
    }
    private void InitializeMultiplier()
    {
        currentMultiplier = 1;
        UpdateMultiplierDisplay();
    }

    private void DoubleMultiplier()
    {
        if (currentMultiplier < maxMultiplier)
        {
            int oldMultiplier = currentMultiplier;
            currentMultiplier = Mathf.Min(currentMultiplier * 2, maxMultiplier);
            Debug.Log($"[FreeSpinManager] Multiplier changed: {oldMultiplier}x → {currentMultiplier}x");

            UpdateMultiplierDisplay();
        }
    }

    private void UpdateMultiplierDisplay()
    {
        if (multiplierText != null)
        {
            multiplierText.text = $"{currentMultiplier}x";
        }
    }

    public float ApplyMultiplier(float baseWin)
    {
        return baseWin * currentMultiplier;
    }

    private int CalculateFreeSpinsAwarded()
    {
        if (WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode)
        {
            return freeSpinsPerAward; // Always add 5 in API mode
        }

        // Demo mode: count free spin symbols on first 3 reels
        int freeSpinCount = 0;
        if (SlotMachineController.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (SlotMachineController.Instance.DoesReelHaveFreeSpin(i))
                {
                    freeSpinCount++;
                }
            }
        }

        // Award spins based on count - CONSISTENT WITH API
        switch (freeSpinCount)
        {
            case 3: return 5;  // Always 5, matching API
            case 4: return 5;
            case 5: return 5;
            default: return 5;
        }
    }

    // ==========================================
    // SIMPLIFIED FREE SPIN MANAGEMENT
    // ==========================================

    private void HandleFreeSpinComplete()
    {
        if (!freeSpinFeatureActive || isDisplayingWin) return;

        if (!isWaitingForNextSpin)
        {
            isWaitingForNextSpin = true;
            StartCoroutine(HandleFreeSpinCompleteWithNewFlow());
        }
    }

    private IEnumerator HandleFreeSpinCompleteWithNewFlow()
    {
        CaptureOriginalNonMaskSymbols();

        // Calculate original wins
        float originalWins = winCalculator.CalculateWin(GetActiveReels());
        if (originalWins > 0)
        {
            // Apply current multiplier to the win
            float multipliedWins = ApplyMultiplier(originalWins);
            AccumulateWin(multipliedWins);
        }

        // Wait for animations and continue to next spin
        yield return new WaitUntil(() => !winAnimationController.IsAnimatingWin);
        yield return new WaitForSeconds(delayBetweenFreeSpins);

        // Double the multiplier AFTER the current spin is complete, for the NEXT spin
        DoubleMultiplier();

        StartCoroutine(StartNextFreeSpin());
    }
    // NEW METHOD: Handle overlapping woman and fan animations
    private IEnumerator PlayOverlappingWomanAndFanAnimations(List<Symbol> maskSymbols)
    {
        // Start woman animation
        StartCoroutine(PlayWomanMaskRevealAnimationNonBlocking(maskSymbols));

        // Wait for the specific timing (2 seconds 23 frames)
        yield return new WaitForSeconds(fanAnimationStartDelay);

        // Start fan animation while woman is still playing
        StartCoroutine(HandleMaskTransformationNonBlocking(maskSymbols));

        // Wait for both animations to complete
        // Woman animation total duration
        float remainingWomanTime = Mathf.Max(0, womanMaskAnimationDuration - fanAnimationStartDelay);

        // Fan animation duration
        float fanDuration = transformationAnimationDuration;

        // Wait for whichever is longer
        float maxDuration = Mathf.Max(remainingWomanTime, fanDuration);
        yield return new WaitForSeconds(maxDuration);
    }

    // NON-BLOCKING woman animation (doesn't yield return, just starts)
    private IEnumerator PlayWomanMaskRevealAnimationNonBlocking(List<Symbol> maskSymbols)
    {
        // Activate and play the separate woman reveal animation
        if (womanRevealAnimationObject != null)
        {
            womanRevealAnimationObject.SetActive(true);
        }

        if (womanRevealAnimator != null)
        {
            womanRevealAnimator.Play("Woman");
        }

        // Play reveal sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("maskreveal");
        }

        // Wait for woman mask animation to complete
        yield return new WaitForSeconds(womanMaskAnimationDuration);

        // Hide the woman reveal animation after completion
        if (womanRevealAnimationObject != null)
        {
            womanRevealAnimationObject.SetActive(false);
        }
    }

    // NON-BLOCKING mask transformation (doesn't yield return at the start)
    private IEnumerator HandleMaskTransformationNonBlocking(List<Symbol> allMaskSymbols)
    {
        if (allMaskSymbols == null || allMaskSymbols.Count == 0)
        {
            yield break;
        }

        // STEP 1: Select target mask type
        SymbolData targetMaskType = SelectRandomMaskType();
        if (targetMaskType == null)
        {
            yield break;
        }

        // STEP 2: Find qualifying masks using EvaluateBranches logic
        List<Symbol> qualifyingMasks = FindQualifyingMasksForTransformation(allMaskSymbols, targetMaskType);

        if (qualifyingMasks.Count == 0)
        {
            yield break;
        }

        // Store for centralized transformation
        currentTransformingMasks = new List<Symbol>(qualifyingMasks);
        currentTargetMaskType = targetMaskType;

        // STEP 3: Play ONLY mask transformation animation
        yield return StartCoroutine(PlayMaskTransformationAnimation());

        // Clear stored references
        currentTransformingMasks.Clear();
        currentTargetMaskType = null;
    }

    private void CaptureOriginalNonMaskSymbols()
    {
        originalNonMaskSymbols.Clear();

        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null) return;

        // Get all currently visible symbols
        for (int i = 0; i < activeReels.Length; i++)
        {
            List<Symbol> visibleSymbols = winCalculator.GetVisibleSymbolsInReel(activeReels[i]);
            foreach (Symbol symbol in visibleSymbols)
            {
                // Capture non-mask symbols (excluding wilds since they should always be considered)
                if (symbol.Data != null &&
                    !symbol.Data.IsMaskSymbol() &&
                    symbol.Data.type != SymbolData.SymbolType.Wild &&
                    symbol.Data.type != SymbolData.SymbolType.FreeSpin)
                {
                    originalNonMaskSymbols.Add(symbol);
                }
            }
        }
    }
    public void ResetSymbolsAndClearPayouts()
    {
        if (winAnimationController != null)
        {
            winAnimationController.StopAnimations();
        }
    }

    // NEW: Method to check if we should play woman animation
    private bool ShouldPlayWomanAnimation(List<Symbol> maskSymbols)
    {
        if (maskSymbols == null || maskSymbols.Count == 0) return false;

        // Get current visible symbols for analysis
        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null) return false;

        List<Symbol>[] allVisibleSymbols = new List<Symbol>[activeReels.Length];
        for (int i = 0; i < activeReels.Length; i++)
        {
            allVisibleSymbols[i] = winCalculator.GetVisibleSymbolsInReel(activeReels[i]);
        }

        // Check for woman animation triggers:
        // 1. Different masks across all 5 reels
        // 2. At least one reel has 2+ masks OR mask + wild OR 2+ wilds
        bool hasDifferentMasksAcross5Reels = CheckDifferentMasksAcross5Reels(allVisibleSymbols);
        bool hasQualifyingReelComposition = CheckQualifyingReelComposition(allVisibleSymbols);

        bool shouldPlay = hasDifferentMasksAcross5Reels && hasQualifyingReelComposition;

        return shouldPlay;
    }

    private bool CheckDifferentMasksAcross5Reels(List<Symbol>[] allVisibleSymbols)
    {
        HashSet<SymbolData.SymbolType> uniqueMaskTypes = new HashSet<SymbolData.SymbolType>();
        bool[] reelHasMaskOrWild = new bool[5];

        for (int reel = 0; reel < 5 && reel < allVisibleSymbols.Length; reel++)
        {
            foreach (Symbol symbol in allVisibleSymbols[reel])
            {
                if (symbol.Data.IsMaskSymbol())
                {
                    uniqueMaskTypes.Add(symbol.Data.type);
                    reelHasMaskOrWild[reel] = true;
                }
                else if (symbol.Data.type == SymbolData.SymbolType.Wild)
                {
                    reelHasMaskOrWild[reel] = true;
                }
            }
        }

        // Check if all 5 reels have masks or wilds, and we have different mask types
        bool all5ReelsHaveMaskOrWild = reelHasMaskOrWild.All(x => x);
        bool hasDifferentMaskTypes = uniqueMaskTypes.Count > 1;

        return all5ReelsHaveMaskOrWild && hasDifferentMaskTypes;
    }

    private bool CheckQualifyingReelComposition(List<Symbol>[] allVisibleSymbols)
    {
        for (int reel = 0; reel < allVisibleSymbols.Length; reel++)
        {
            int maskCount = 0;
            int wildCount = 0;

            foreach (Symbol symbol in allVisibleSymbols[reel])
            {
                if (symbol.Data.IsMaskSymbol())
                    maskCount++;
                else if (symbol.Data.type == SymbolData.SymbolType.Wild)
                    wildCount++;
            }

            // Check qualifying conditions:
            // 1. 2+ masks in a reel
            // 2. mask + wild in a reel
            // 3. 2+ wilds in a reel
            if (maskCount >= 2 || (maskCount >= 1 && wildCount >= 1) || wildCount >= 2)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // CONSECUTIVE MASK DETECTION SYSTEM
    // ==========================================

    public struct ConsecutiveMaskResult
    {
        public bool hasConsecutiveMasks;
        public int startReel;
        public int endReel;
        public int consecutiveCount;
        public List<Symbol> allMaskSymbols;
    }

    /// <summary>
    /// Check for masks in 3+ consecutive reels starting from reel 1
    /// </summary>
    private ConsecutiveMaskResult CheckForConsecutiveMasks()
    {
        ConsecutiveMaskResult result = new ConsecutiveMaskResult
        {
            hasConsecutiveMasks = false,
            startReel = -1,
            endReel = -1,
            consecutiveCount = 0,
            allMaskSymbols = new List<Symbol>()
        };

        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null)
        {
            return result;
        }

        // Check consecutive reels starting from reel 1 (index 0)
        List<int> consecutiveReelsWithMasks = new List<int>();
        List<Symbol> consecutiveMaskSymbols = new List<Symbol>();

        // Start from reel 0 and check consecutive reels
        for (int currentReelIndex = 0; currentReelIndex < activeReels.Length; currentReelIndex++)
        {
            List<Symbol> reelMasks = GetMaskSymbolsFromReel(activeReels[currentReelIndex]);
            List<Symbol> reelWilds = GetWildSymbolsFromReel(activeReels[currentReelIndex]);

            // ENHANCED: Check if reel has masks OR wilds (wilds can bridge)
            if (reelMasks.Count > 0 || reelWilds.Count > 0)
            {
                // This reel has masks or wilds - add to consecutive count
                consecutiveReelsWithMasks.Add(currentReelIndex);
                consecutiveMaskSymbols.AddRange(reelMasks);
            }
            else
            {
                // No masks or wilds in this reel - break the consecutive chain
                break;
            }
        }

        // ENHANCED: Additional validation - ensure we have actual mask symbols, not just wilds
        bool hasActualMaskSymbols = consecutiveMaskSymbols.Count > 0;

        // Check if we found enough consecutive reels with masks/wilds starting from reel 1
        if (consecutiveReelsWithMasks.Count >= minimumConsecutiveReels &&
            consecutiveReelsWithMasks[0] == 0 &&
            hasActualMaskSymbols)
        {
            result.hasConsecutiveMasks = true;
            result.startReel = consecutiveReelsWithMasks[0];
            result.endReel = consecutiveReelsWithMasks[consecutiveReelsWithMasks.Count - 1];
            result.consecutiveCount = consecutiveReelsWithMasks.Count;
            result.allMaskSymbols = consecutiveMaskSymbols;

            string reelList = string.Join(", ", consecutiveReelsWithMasks);
        }

        return result;
    }

    // ADD THIS HELPER METHOD to get wild symbols from a reel
    private List<Symbol> GetWildSymbolsFromReel(SlotReel reel)
    {
        List<Symbol> wildSymbols = new List<Symbol>();

        if (reel == null) return wildSymbols;

        List<Symbol> visibleSymbols = reel.GetTopVisibleSymbols(reel.visibleSymbols);
        foreach (Symbol symbol in visibleSymbols)
        {
            if (symbol.Data != null && symbol.Data.type == SymbolData.SymbolType.Wild)
            {
                wildSymbols.Add(symbol);
            }
        }

        return wildSymbols;
    }


    /// <summary>
    /// Get all mask symbols from a specific reel
    /// </summary>
    private List<Symbol> GetMaskSymbolsFromReel(SlotReel reel)
    {
        List<Symbol> maskSymbols = new List<Symbol>();

        if (reel == null) return maskSymbols;

        List<Symbol> visibleSymbols = reel.GetTopVisibleSymbols(reel.visibleSymbols);
        foreach (Symbol symbol in visibleSymbols)
        {
            if (symbol.Data != null && symbol.Data.IsMaskSymbol())
            {
                maskSymbols.Add(symbol);
            }
        }

        return maskSymbols;
    }

    // ==========================================
    // MASK TRANSFORMATION SYSTEM (SIMPLIFIED)
    // ==========================================

    /// <summary>
    /// Handle the complete mask transformation sequence (simplified)
    /// </summary>
    private IEnumerator HandleMaskTransformation(List<Symbol> allMaskSymbols)
    {
        if (allMaskSymbols == null || allMaskSymbols.Count == 0)
        {
            yield break;
        }

        // STEP 1: Select target mask type
        SymbolData targetMaskType = SelectRandomMaskType();
        if (targetMaskType == null)
        {
            yield break;
        }

        // STEP 2: Find qualifying masks using EvaluateBranches logic
        List<Symbol> qualifyingMasks = FindQualifyingMasksForTransformation(allMaskSymbols, targetMaskType);

        if (qualifyingMasks.Count == 0)
        {
            yield break;
        }

        // Store for centralized transformation
        currentTransformingMasks = new List<Symbol>(qualifyingMasks);
        currentTargetMaskType = targetMaskType;


        // STEP 3: Play ONLY mask transformation animation (no weird highlighting)
        yield return StartCoroutine(PlayMaskTransformationAnimation());

        // Clear stored references
        currentTransformingMasks.Clear();
        currentTargetMaskType = null;
    }

    /// <summary>
    /// Find qualifying masks using EvaluateBranches logic (simplified)
    /// </summary>
    private List<Symbol> FindQualifyingMasksForTransformation(List<Symbol> allMaskSymbols, SymbolData targetMaskType)
    {
        List<Symbol> qualifyingMasks = new List<Symbol>();

        // Get current visible symbols
        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null) return qualifyingMasks;

        List<Symbol>[] allVisibleSymbols = new List<Symbol>[activeReels.Length];
        for (int i = 0; i < activeReels.Length; i++)
        {
            allVisibleSymbols[i] = winCalculator.GetVisibleSymbolsInReel(activeReels[i]);
        }

        // For each mask symbol, check if transforming it would create 3+ consecutive matching symbols
        foreach (Symbol maskSymbol in allMaskSymbols)
        {
            if (WouldCreateThreeConsecutiveAfterTransformation(maskSymbol, targetMaskType, allVisibleSymbols, allMaskSymbols))
            {
                qualifyingMasks.Add(maskSymbol);
            }
        }

        return qualifyingMasks;
    }

    /// <summary>
    /// Check if transforming a mask would create 3+ consecutive matching symbols (using EvaluateBranches logic)
    /// </summary>
    private bool WouldCreateThreeConsecutiveAfterTransformation(Symbol maskSymbol, SymbolData targetMaskType,
        List<Symbol>[] allVisibleSymbols, List<Symbol> allMaskSymbols)
    {
        // Find which reel and position this mask is in
        int maskReelIndex = -1;
        int maskPositionIndex = -1;

        for (int reel = 0; reel < allVisibleSymbols.Length; reel++)
        {
            for (int pos = 0; pos < allVisibleSymbols[reel].Count; pos++)
            {
                if (allVisibleSymbols[reel][pos] == maskSymbol)
                {
                    maskReelIndex = reel;
                    maskPositionIndex = pos;
                    break;
                }
            }
            if (maskReelIndex != -1) break;
        }

        if (maskReelIndex == -1) return false;

        // Simulate transformation: count consecutive matching symbols from reel 0
        int consecutiveCount = 0;

        for (int reel = 0; reel < allVisibleSymbols.Length; reel++)
        {
            bool reelHasMatchingSymbol = false;

            for (int pos = 0; pos < allVisibleSymbols[reel].Count; pos++)
            {
                Symbol currentSymbol = allVisibleSymbols[reel][pos];

                // Check what this symbol would be after transformation
                SymbolData.SymbolType symbolTypeAfterTransform;

                if (allMaskSymbols.Contains(currentSymbol))
                {
                    // This is a mask that would be transformed
                    symbolTypeAfterTransform = targetMaskType.type;
                }
                else
                {
                    // This symbol stays the same
                    symbolTypeAfterTransform = currentSymbol.Data.type;
                }

                // Check if it matches our target type (or is wild)
                if (symbolTypeAfterTransform == targetMaskType.type ||
                    symbolTypeAfterTransform == SymbolData.SymbolType.Wild)
                {
                    reelHasMatchingSymbol = true;
                    break;
                }
            }

            if (reelHasMatchingSymbol)
            {
                consecutiveCount++;
            }
            else
            {
                // Break consecutive chain
                break;
            }
        }

        return consecutiveCount >= 3;
    }

    private IEnumerator PlayMaskTransformationAnimation()
    {
        ResetTransformationFlag();
        if (currentTransformingMasks == null || currentTransformingMasks.Count == 0)
        {
            yield break;
        }

        // STEP 1: Reset all mask symbols
        foreach (Symbol maskSymbol in currentTransformingMasks)
        {
            if (maskSymbol != null)
            {
                maskSymbol.ResetAnimation();
            }
        }

        yield return new WaitForSeconds(0.1f);

        // STEP 2: Trigger animation (animation event will handle transformation)
        foreach (Symbol maskSymbol in currentTransformingMasks)
        {
            if (maskSymbol != null)
            {
                maskSymbol.PlayMaskTransformAnimation();
            }
        }

        // STEP 3: Play transformation sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("masktransform");
        }

        // STEP 4: Wait for FULL animation duration (no midpoint execution)
        yield return new WaitForSeconds(transformationAnimationDuration);

        // STEP 5: Reset animation states
        foreach (Symbol maskSymbol in currentTransformingMasks)
        {
            if (maskSymbol != null)
            {
                maskSymbol.ResetAnimation();
            }
        }
    }


    /// <summary>
    /// Centralized mask transformation - transforms all qualifying masks to target type
    /// </summary>
    public void ExecuteCentralizedMaskTransformationOnce()
    {
        // Prevent multiple calls during the same transformation sequence
        if (maskTransformationExecuted)
        {
            return;
        }

        maskTransformationExecuted = true;
        ExecuteCentralizedMaskTransformation();
    }

    // Reset the flag when starting a new transformation sequence
    private void ResetTransformationFlag()
    {
        maskTransformationExecuted = false;
    }
    public void ExecuteCentralizedMaskTransformation()
    {
        if (currentTransformingMasks == null || currentTransformingMasks.Count == 0)
        {
            return;
        }

        if (currentTargetMaskType == null)
        {
            return;
        }


        // Transform all qualifying masks to the target type
        for (int i = 0; i < currentTransformingMasks.Count; i++)
        {
            Symbol maskSymbol = currentTransformingMasks[i];
            if (maskSymbol != null)
            {
                // Transform mask to target symbol type
                maskSymbol.SetSymbolData(currentTargetMaskType);
                maskSymbol.UpdateVisualForFreeSpins(true); // Ensure free spin visuals are applied
            }
            else
            {
                // Debug.LogWarning($"[FreeSpinManager] Mask symbol {i + 1} is null!");
            }
        }
    }

    // Woman mask reveal animation (independent UI effect)
    private IEnumerator PlayWomanMaskRevealAnimation(List<Symbol> maskSymbols)
    {
        // Activate and play the separate woman reveal animation
        if (womanRevealAnimationObject != null)
        {
            womanRevealAnimationObject.SetActive(true);
        }

        if (womanRevealAnimator != null)
        {
            womanRevealAnimator.Play("Woman");
        }

        // Play reveal sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("maskreveal");
        }

        // Wait for woman mask animation to complete
        yield return new WaitForSeconds(womanMaskAnimationDuration);

        // Hide the woman reveal animation after completion
        if (womanRevealAnimationObject != null)
        {
            womanRevealAnimationObject.SetActive(false);
        }
    }

    /// <summary>
    /// Randomly select which mask type all masks will become
    /// </summary>
    // In FreeSpinManager - MODIFIED SelectRandomMaskType()
    private SymbolData SelectRandomMaskType()
    {
        // Check if we're in API mode and have a selected mask type from API
        if (WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode)
        {
            string apiSelectedMaskType = " ";// WebManAPI.Instance.GetSelectedMaskType();

            if (!string.IsNullOrEmpty(apiSelectedMaskType))
            {
                // Convert API mask type to your SymbolData.SymbolType
                SymbolData.SymbolType targetType = ConvertAPIMaskTypeToSymbolType(apiSelectedMaskType);

                // Find the mask in your available types
                SymbolData apiSelectedMask = System.Array.Find(availableMaskTypes,
                    mask => mask.type == targetType);

                if (apiSelectedMask != null)
                {
                    return apiSelectedMask;
                }
            }
        }

        // Fallback to random selection (demo mode or API failure)
        if (availableMaskTypes == null || availableMaskTypes.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, availableMaskTypes.Length);
        SymbolData selectedMask = availableMaskTypes[randomIndex];

        return selectedMask;
    }

    // Helper method to convert API mask names to your symbol types
    private SymbolData.SymbolType ConvertAPIMaskTypeToSymbolType(string apiMaskType)
    {
        switch (apiMaskType)
        {
            case "PurpleMask": return SymbolData.SymbolType.WhiteDragon;
            case "OrangeMask": return SymbolData.SymbolType.BlackDragon;
            case "GreenMask": return SymbolData.SymbolType.GreenDragon;
            case "YellowMask": return SymbolData.SymbolType.RedDragon;
            default:
                return SymbolData.SymbolType.RedDragon; // Default fallback
        }
    }

    // Consolidated next spin logic
    private IEnumerator StartNextFreeSpin()
    {
        originalNonMaskSymbols.Clear();

        if (currentSpin >= totalFreeSpins)
        {
            EndFreeSpins();
            isWaitingForNextSpin = false;
            yield break;
        }

        currentSpin++;
        UpdateCurrentSpinDisplay(currentSpin);
        totalWildsAdded = Mathf.Min(currentMultiplier * 2, maxMultiplier); ;
        if (totalWildsAdded <= 64)
        {
            UpdateTotalWildAwardedDisplay(totalWildsAdded);
            UpdateTotalWildAddedDisplay(totalWildsAdded);
            totalWildsAwardPanel.GetComponent<LeanWindow>().TurnOn();
            yield return new WaitForSeconds(1.7f);
            totalWildsAwardPanel.GetComponent<LeanWindow>().TurnOff();
        }

        ResetAllAnimations();
        OnFreeSpin?.Invoke(currentSpin - 1, RemainingFreeSpins, totalFreeSpins);

        StartCoroutine(StartNextFreeSpinWithDelay());
    }

    private IEnumerator StartNextFreeSpinWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        // Start next free spin
        if (FreeSpinSlotMachineController.Instance != null)
        {
            FreeSpinSlotMachineController.Instance.StartFreeSpin();
        }

        isWaitingForNextSpin = false;
    }

    private void EndFreeSpins()
    {
        StartCoroutine(EndFreeSpinsSequence());
    }

    // ==========================================
    // UTILITY METHODS
    // ==========================================

    /// <summary>
    /// Switch ALL symbols in both normal and free spin reels to free spin mode
    /// </summary>
    private void SwitchAllSymbolsToFreeSpinMode(bool freeSpinMode)
    {
        int symbolsUpdated = 0;

        // Update symbols in free spin reels
        if (FreeSpinSlotMachineController.Instance != null)
        {
            SlotReel[] freeSpinReels = FreeSpinSlotMachineController.Instance.GetReels();
            if (freeSpinReels != null)
            {
                symbolsUpdated += UpdateSymbolsInReels(freeSpinReels, freeSpinMode, "FreeSpinReels");
            }
        }

        // Update symbols in normal reels (if they exist)
        if (SlotMachineController.Instance != null)
        {
            SlotReel[] normalReels = SlotMachineController.Instance.GetReels();
            if (normalReels != null)
            {
                symbolsUpdated += UpdateSymbolsInReels(normalReels, freeSpinMode, "NormalReels");
            }
        }
    }

    private int UpdateSymbolsInReels(SlotReel[] reels, bool freeSpinMode, string reelSetName)
    {
        int count = 0;
        foreach (SlotReel reel in reels)
        {
            if (reel == null || reel.symbolsContainer == null) continue;

            foreach (Transform child in reel.symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null)
                {
                    symbol.UpdateVisualForFreeSpins(freeSpinMode);
                    count++;
                }
            }
        }
        return count;
    }

    public void AccumulateWin(float amount)
    {
        previousDisplayedWin = accumulatedFreeSpinWins;
        hasWinsDuringFreeSpins = true;
        accumulatedFreeSpinWins += amount;

        if (SlotMachineController.Instance != null && SlotMachineController.Instance.betManager != null)
        {
            SlotMachineController.Instance.betManager.AnimateIncrementalWin(amount, accumulatedFreeSpinWins);
        }
    }
    public bool HasWinsDuringFreeSpins
    {
        get { return hasWinsDuringFreeSpins; }
    }
    public void SetInteractable()
    {
        if (FreeSpinHasBegun)
        {
            uiElements.spinButton.landscape.interactable = false;
            uiElements.spinButton.portrait.interactable = false;

            if (SlotMachineController.Instance != null)
                SlotMachineController.Instance.TransitionSpinButtonColor(true);

            uiElements.autoSpinButton.landscape.interactable = false;
            uiElements.autoSpinButton.portrait.interactable = false;
            uiElements.increaseButton.landscape.interactable = false;
            uiElements.increaseButton.portrait.interactable = false;
            uiElements.decreaseButton.landscape.interactable = false;
            uiElements.decreaseButton.portrait.interactable = false;
        }
        else
        {
            uiElements.spinButton.landscape.interactable = true;
            uiElements.spinButton.portrait.interactable = true;

            if (SlotMachineController.Instance != null)
                SlotMachineController.Instance.TransitionSpinButtonColor(false);

            uiElements.autoSpinButton.landscape.interactable = true;
            uiElements.autoSpinButton.portrait.interactable = true;
            uiElements.decreaseButton.landscape.interactable = true;
            uiElements.decreaseButton.portrait.interactable = true;
            uiElements.increaseButton.landscape.interactable = true;
            uiElements.increaseButton.portrait.interactable = true;
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
    }

    private void UpdateFreeSpinCountDisplay(int count)
    {
        int tens = count / 10;
        int units = count % 10;

        if (freeSpinCountTens != null)
        {
            freeSpinCountTens.enabled = (tens > 0);
            if (tens > 0 && tens < freeSpinCountDigitSprites.Length)
                freeSpinCountTens.sprite = freeSpinCountDigitSprites[tens];
        }

        if (freeSpinCountUnits != null)
        {
            freeSpinCountUnits.enabled = true;
            if (units >= 0 && units < freeSpinCountDigitSprites.Length)
                freeSpinCountUnits.sprite = freeSpinCountDigitSprites[units];
        }
    }

    private void UpdateCurrentSpinDisplay(int spinNumber)
    {
        int tens = spinNumber / 10;
        int units = spinNumber % 10;

        if (currentSpinTens != null)
        {
            currentSpinTens.enabled = (tens > 0);
            if (tens > 0 && tens < digitSprites.Length)
                currentSpinTens.sprite = digitSprites[tens];
        }

        if (currentSpinUnits != null)
        {
            currentSpinUnits.enabled = true;
            if (units >= 0 && units < digitSprites.Length)
                currentSpinUnits.sprite = digitSprites[units];
        }
    }

    private void UpdateTotalWildAddedDisplay(int totalWildAddedNumber)
    {
        if (totalWildAddedNumber > 60)
            return;
        int tens = totalWildAddedNumber / 10;
        int units = totalWildAddedNumber % 10;

        if (totalWildAddedTens != null)
        {
            totalWildAddedTens[0].enabled = totalWildAddedNumber == 0 ? (tens >= 0) : (tens > 0);
            totalWildAddedTens[1].enabled = totalWildAddedNumber == 0 ? (tens >= 0) : (tens > 0);
            if (tens >= 0 && tens < digitSprites.Length)
            {
                totalWildAddedTens[0].sprite = digitSprites[tens];
                totalWildAddedTens[1].sprite = digitSprites[tens];
            }
        }

        if (totalWildAddedUnits != null)
        {
            totalWildAddedUnits[0].enabled = true;
            totalWildAddedUnits[1].enabled = true;
            if (units >= 0 && units < digitSprites.Length)
            {
                totalWildAddedUnits[0].sprite = digitSprites[units];
                totalWildAddedUnits[1].sprite = digitSprites[units];
            }
        }
    }

    private void UpdateTotalWildAwardedDisplay(int totalWildAddedNumber)
    {
        if (totalWildAddedNumber > 60)
            return;

        int tens = totalWildAddedNumber / 10;
        int units = totalWildAddedNumber % 10;

        if (totalWildsCountTens != null)
        {
            totalWildsCountTens.gameObject.SetActive(tens > 0);
            if (tens > 0 && tens < freeSpinWildCountDigitSprites.Length)
                totalWildsCountTens.sprite = freeSpinWildCountDigitSprites[tens];
        }

        if (totalWildsCountUnits != null)
        {
            totalWildsCountUnits.enabled = true;
            if (units >= 0 && units < freeSpinWildCountDigitSprites.Length)
                totalWildsCountUnits.sprite = freeSpinWildCountDigitSprites[units];
        }
    }

    private void UpdateTotalSpinDisplay(int totalNumber)
    {
        int tens = totalNumber / 10;
        int units = totalNumber % 10;

        if (totalSpinTens != null)
        {
            totalSpinTens.enabled = (tens > 0);
            if (tens > 0 && tens < digitSprites.Length)
                totalSpinTens.sprite = digitSprites[tens];
        }

        if (totalSpinUnits != null)
        {
            totalSpinUnits.enabled = true;
            if (units >= 0 && units < digitSprites.Length)
                totalSpinUnits.sprite = digitSprites[units];
        }
    }

    private bool CheckForActiveWinAnimations()
    {
        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null) return false;

        foreach (SlotReel reel in activeReels)
        {
            foreach (Transform child in reel.symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null && symbol.IsAnimating())
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void ResetAllAnimations()
    {
        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null) return;

        foreach (SlotReel reel in activeReels)
        {
            foreach (Transform child in reel.symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null)
                {
                    symbol.ResetAnimation();
                    symbol.DeactivateWinHighlight();
                }
            }
        }
    }

    private SlotReel[] GetActiveReels()
    {
        if (freeSpinFeatureActive && FreeSpinSlotMachineController.Instance != null)
        {
            return FreeSpinSlotMachineController.Instance.GetReels();
        }
        else if (SlotMachineController.Instance != null)
        {
            return SlotMachineController.Instance.GetReels();
        }

        return null;
    }

    private void ResetDetectionFlags()
    {
        // Reset any detection flags when starting a new spin
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.ResetFreeSpinDetectionFlags();
        }
    }

    public void ResetAfterFreeSpins()
    {
        // Reset multiplier
        currentMultiplier = 1;
        UpdateMultiplierDisplay();
        // Reset all state
        isWaitingForNextSpin = false;
        isDisplayingWin = false;
        maskReelTriggered = false;

        // Reset UI elements
        if (FreeSpinBG != null) FreeSpinBG.SetActive(false);
        if (uppersection != null) uppersection.SetActive(false);
        if (freeSpinAwardPanel != null) freeSpinAwardPanel.SetActive(false);

        // MODIFIED: Close the accumulated win panel here (with all displays still visible on it)
        if (accumulatedWinPanel != null)
        {
            accumulatedWinPanel.SetActive(false);
            Debug.Log("[FreeSpinManager] Closing accumulated win panel (all displays destroyed with panel)");
        }

        // MODIFIED: Now that panel is closed, show the normal reels window
        if (SlotmachineBG != null)
        {
            SlotmachineBG.SetActive(true);
            Debug.Log("[FreeSpinManager] Enabled normal reels window");
        }

        if (freeSpinClickBlocker != null)
        {
            freeSpinClickBlocker.gameObject.SetActive(false);
            freeSpinClickBlocker.GetComponent<FreeSpinStarterClickBlocker>().ResetForNewSession();
        }

        // Reset state flags
        freeSpinFeatureActive = false;
        FreeSpinHasBegun = false;
        currentSpin = 0;
        totalFreeSpins = 0;
        accumulatedFreeSpinWins = 0f;
        previousDisplayedWin = 0f;

        // Clear displays (they're already destroyed with the panel, but clear the references)
        ClearWinDisplay();
        SetInteractable();

        SoundManager.Instance.StopPlayFreeSpinMusicLoop();
        SoundManager.Instance.StopPlayFreeWinsLoop();
    }

    // ==========================================
    // HANDLE FREE SPINS DURING FREE SPINS
    // ==========================================

    public bool retriggerDoneOnce = false;
    public IEnumerator HandleFreeSpinsDuringFreeSpins(float freespinPayout = 0f)
    {
        // Check if we can add more spins (max 25 total)
        int spinsToAdd = freeSpinsPerAward;
        if (totalFreeSpins + spinsToAdd > maxTotalFreeSpins)
        {
            spinsToAdd = maxTotalFreeSpins - totalFreeSpins;
        }

        if (spinsToAdd <= 0)
        {
            // Already at maximum, don't add more spins
            yield break;
        }
        if (retriggerDoneOnce)
        {
            yield break;
        }
        // Prevent multiple retrigger calls during the same sequence
        if (isHandlingRetrigger)
        {
            yield break;
        }
        if (bonusPanel != null)
        {
            bonusPanel.SetActive(true);
            SoundManager.Instance.PlaySound("freespin");
            if (extraSpinsText != null)
                extraSpinsText.text = $"+{spinsToAdd} FREE SPINS";

            yield return StartCoroutine(AddSpinsWithAnimationAndWait(spinsToAdd));
            bonusPanel.SetActive(false);
        }

        isHandlingRetrigger = true;
        retriggerDoneOnce = true;

        isWaitingForNextSpin = true;
        yield return new WaitForSeconds(4.0f);

        if (bonusPanel != null)
        {
            bonusPanel.SetActive(true);
            SoundManager.Instance.PlaySound("freespin");
            if (extraSpinsText != null)
                extraSpinsText.text = $"+{additionalSpinsAmount} FREE SPINS";

            // SoundManager.Instance.PlaySound("extraspins");

            yield return StartCoroutine(AddSpinsWithAnimationAndWait(additionalSpinsAmount));

            bonusPanel.SetActive(false);

            if (winAnimationController != null)
            {
                winAnimationController.PlayWinAnimations();
            }
        }
        else
        {
            yield return StartCoroutine(AddSpinsWithAnimationAndWait(additionalSpinsAmount));
        }

        isWaitingForNextSpin = false;
        isHandlingRetrigger = false;

        if (currentSpin <= totalFreeSpins)
        {
            StartCoroutine(StartNextFreeSpinWithDelay());
        }
    }

    private IEnumerator AddSpinsWithAnimationAndWait(int spinsToAdd)
    {
        // Prevent multiple spin addition calls
        if (isAddingSpins)
        {
            yield break;
        }

        isAddingSpins = true;
        addingSpinsCoroutine = StartCoroutine(AddSpinsCoroutine(spinsToAdd));
        yield return addingSpinsCoroutine;
        isAddingSpins = false;
    }

    private IEnumerator AddSpinsCoroutine(int spinsToAdd)
    {
        // FIXED: Store the original value at the very beginning and never change the reference
        int storedOriginalTotal = totalFreeSpins;
        int targetTotal = storedOriginalTotal + spinsToAdd;

        // Animate the addition one by one using the stored original value
        for (int i = 1; i <= spinsToAdd; i++)
        {
            totalFreeSpins = storedOriginalTotal + i;
            UpdateTotalSpinDisplay(totalFreeSpins);
            yield return new WaitForSeconds(0.5f);
        }

        // Ensure final value is set correctly using the stored original value
        totalFreeSpins = storedOriginalTotal + spinsToAdd;
        UpdateTotalSpinDisplay(totalFreeSpins);
    }


    // ==========================================
    // WIN DISPLAY METHODS (MODIFIED TO SHOW FINAL FREESPIN PAYOUT)
    // ==========================================

    private IEnumerator EndFreeSpinsSequence()
    {
        if (SlotMachineController.Instance.betManager.IsAnimatingWin)
        {
            yield return new WaitUntil(() => !SlotMachineController.Instance.betManager.IsAnimatingWin);
        }

        SoundManager.Instance.StopPlayFreeSpinMusicLoop();
        SoundManager.Instance.StopPlayFreeWinsLoop();

        if (accumulatedFreeSpinWins > 0)
        {
            const int initializingLines = 2;
            float totalAccumulatedWin = accumulatedFreeSpinWins * initializingLines;

            yield return StartCoroutine(DisplayAccumulatedWin());

            yield return new WaitUntil(() => !isDisplayingWin);

            bool isBigWin = totalAccumulatedWin >= bigWinThreshold;

            if (isBigWin)
            {
                winCalculator.StartCelebration();
                SoundManager.Instance.PlayBigWinLoop();
            }

            SoundManager.Instance.PlaySound("freespinscoring");
            SlotMachineController.Instance.betManager.AddWin(totalAccumulatedWin);

            if (WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode)
            {
                WebManAPI.Instance.RecordFreeSpinTotalWin(totalAccumulatedWin);
            }

            accumulatedFreeSpinWins = 0f;

            yield return new WaitUntil(() => !SlotMachineController.Instance.betManager.IsAnimatingWin);

            if (isBigWin)
            {
                yield return new WaitForSeconds(2f);
                SoundManager.Instance.StopBigWinLoop();
            }
        }

        CompleteEndingFreeSpins();
    }
    private void CompleteEndingFreeSpins()
    {
        if (SlotMachineController.Instance != null)
            SlotMachineController.Instance.OnFreeSpinsEnded?.Invoke();

        FreeSpinHasBegun = false;
        SetInteractable();
        ResetAfterFreeSpins();
    }

    private IEnumerator DisplayAccumulatedWin()
    {
        if (isDisplayingWin) yield break;
        isDisplayingWin = true;

        ClearWinDisplay();
        SoundManager.Instance.PlaySound("freespinresult");

        if (accumulatedWinPanel != null)
            accumulatedWinPanel.SetActive(true);

        if (leftWinAnimator != null)
            leftWinAnimator.SetTrigger("ShowWin");
        if (rightWinAnimator != null)
            rightWinAnimator.SetTrigger("ShowWin");

        // Stop all ongoing win animations
        if (winAnimationController != null)
        {
            winAnimationController.StopAnimations();
        }

        // ============================================
        // STEP 1: Display Accumulated Win Amount (Regular Digits)
        // ============================================
        Debug.Log("[FreeSpinManager] STEP 1: Displaying Accumulated Win Amount...");

        float displayAmount = 0f;
        CreateDigitDisplay(displayAmount); // Show in winAmountContainer
        yield return new WaitForSeconds(0.5f);

        float incrementAmount = Mathf.Max(0.1f, accumulatedFreeSpinWins / 100f);

        // Animate counting up for accumulated wins
        while (displayAmount < accumulatedFreeSpinWins)
        {
            displayAmount += incrementAmount;
            if (displayAmount > accumulatedFreeSpinWins)
                displayAmount = accumulatedFreeSpinWins;

            UpdateDigitDisplay(displayAmount);

            if (counterTickSound != null)
                SoundManager.Instance.PlaySound(counterTickSound.name);

            yield return new WaitForSeconds(counterIncrementSpeed);
        }

        UpdateDigitDisplay(accumulatedFreeSpinWins);
        Debug.Log($"[FreeSpinManager] Accumulated Win displayed: ${accumulatedFreeSpinWins:F2}");

        // Hold for a moment before showing next value
        yield return new WaitForSeconds(1.0f);

        // ============================================
        // STEP 2: Display Initializing Lines (Constant = 2)
        // ============================================
        Debug.Log("[FreeSpinManager] STEP 2: Displaying Initializing Lines...");

        const int initializingLines = 2; // CONSTANT VALUE
        CreateLinesDisplay(initializingLines); // Show in initializingLinesContainer

        // Play sound for lines appearing
        if (counterTickSound != null)
            SoundManager.Instance.PlaySound(counterTickSound.name);

        Debug.Log($"[FreeSpinManager] Initializing Lines displayed: {initializingLines}");

        // Hold for a moment before showing total
        yield return new WaitForSeconds(1.0f);

        // ============================================
        // STEP 3: Display Total Accumulated Win (Blue Digits)
        // Total = Accumulated Win × Initializing Lines
        // ============================================
        Debug.Log("[FreeSpinManager] STEP 3: Displaying Total Accumulated Win...");

        float totalAccumulatedWin = accumulatedFreeSpinWins * initializingLines;

        float displayTotal = 0f;
        CreateBlueDigitDisplay(displayTotal); // Show in totalAmountContainer
        yield return new WaitForSeconds(0.5f);

        float totalIncrementAmount = Mathf.Max(0.1f, totalAccumulatedWin / 100f);

        // Animate counting up for total
        while (displayTotal < totalAccumulatedWin)
        {
            displayTotal += totalIncrementAmount;
            if (displayTotal > totalAccumulatedWin)
                displayTotal = totalAccumulatedWin;

            UpdateBlueDigitDisplay(displayTotal);

            if (counterTickSound != null)
                SoundManager.Instance.PlaySound(counterTickSound.name);

            yield return new WaitForSeconds(counterIncrementSpeed);
        }

        UpdateBlueDigitDisplay(totalAccumulatedWin);
        Debug.Log($"[FreeSpinManager] Total Accumulated Win displayed: ${totalAccumulatedWin:F2}");

        // ============================================
        // STEP 4: Hold Final Display
        // ============================================
        yield return new WaitForSeconds(finalWinDisplayDuration);

        // ============================================
        // CLEANUP & TRANSITION BACK
        // ============================================
        foreach (var symbol in LineManager.instance.VisibleSymbolsGrid)
        {
            symbol.DeactivateWinHighlight();
        }

        if (accumulatedWinPanel != null)
            accumulatedWinPanel.SetActive(false);

        freeSpinFeatureActive = false;
        Time.timeScale = 1f;
        winCalculator.winAnimationController.loopAnimations = true;

        if (winAnimationController != null)
        {
            winAnimationController.CleanupPayoutDisplays();
        }

        // Switch back to normal slot machine
        if (reels != null) reels.SetActive(false);

        // Update backgrounds
        if (SlotmachineBG != null) SlotmachineBG.SetActive(true);
        if (FreeSpinBG != null) FreeSpinBG.SetActive(false);
        if (slotMachineBackgroundImage != null && originalBackgroundSprite != null)
        {
            slotMachineBackgroundImage.sprite = originalBackgroundSprite;
        }

        if (uppersection != null) uppersection.SetActive(false);

        SwitchAllSymbolsToFreeSpinMode(false);
        LineManager.instance.AssignBg(false);
        retriggerDoneOnce = false;

        // Use the TOTAL accumulated win (with multiplier) for final payout
        if (totalAccumulatedWin > 0)
        {
            WinCalculator.Instance.DisplayFreeSpinTotalPayout(totalAccumulatedWin);
        }

        FreeSpinSlotMachineController.Instance.EnableGameButtons();

        // Restore original symbols
        for (int j = 0; j < 5; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (VisibleSymbolsGrid[j, i].Data.type == SymbolData.SymbolType.FreeSpin)
                    continue;

                Symbol existingSymbol = VisibleSymbolsGrid[j, i];
                SymbolData newSymbolData = VisibleSymbolsGrid[j, i].Data;

                LineManager.instance.InitializeGrid(j, i, existingSymbol, newSymbolData);
            }
        }

        if (winAnimationController != null && WebManAPI.Instance.tempPayLines.Count > 0)
        {
            winAnimationController.PlayWinAnimations(WebManAPI.Instance.tempPayLines);
            SlotMachineController.Instance.betManager.InvokeWin();
        }

        isDisplayingWin = false;
    }

    private IEnumerator DisplayAccumulatedTotalWin()
    {
        if (isDisplayingWin) yield break;
        isDisplayingWin = true;

        ClearWinDisplay();
        SoundManager.Instance.PlaySound("freespinresult");

        if (accumulatedWinPanel != null)
            accumulatedWinPanel.SetActive(true);

        if (leftWinAnimator != null)
            leftWinAnimator.SetTrigger("ShowWin");
        if (rightWinAnimator != null)
            rightWinAnimator.SetTrigger("ShowWin");

        // FIXED: Stop all ongoing win animations before showing accumulated total
        if (winAnimationController != null)
        {
            winAnimationController.StopAnimations();
        }

        // Rest of the method stays the same...
        float displayAmount = 0f;
        CreateBlueDigitDisplay(displayAmount);
        yield return new WaitForSeconds(0.5f);

        float incrementAmount = Mathf.Max(0.1f, accumulatedFreeSpinWins / 100f);

        while (displayAmount < accumulatedFreeSpinWins)
        {
            displayAmount += incrementAmount;
            if (displayAmount > accumulatedFreeSpinWins)
                displayAmount = accumulatedFreeSpinWins;

            UpdateBlueDigitDisplay(displayAmount);

            if (counterTickSound != null)
                SoundManager.Instance.PlaySound(counterTickSound.name);

            yield return new WaitForSeconds(counterIncrementSpeed);
        }

        UpdateBlueDigitDisplay(accumulatedFreeSpinWins);

        yield return new WaitForSeconds(finalWinDisplayDuration);

        foreach (var symbol in LineManager.instance.VisibleSymbolsGrid)
        {
            symbol.DeactivateWinHighlight();
        }

        if (accumulatedWinPanel != null)
            accumulatedWinPanel.SetActive(false);

        freeSpinFeatureActive = false;
        Time.timeScale = 1f;
        winCalculator.winAnimationController.loopAnimations = true;
        if (winAnimationController != null)
        {
            winAnimationController.CleanupPayoutDisplays();
        }

        // Switch back to normal slot machine
        if (reels != null) reels.SetActive(false);

        // Update backgrounds
        if (SlotmachineBG != null) SlotmachineBG.SetActive(true);
        if (FreeSpinBG != null) FreeSpinBG.SetActive(false);
        if (slotMachineBackgroundImage != null && originalBackgroundSprite != null)
        {
            slotMachineBackgroundImage.sprite = originalBackgroundSprite;
        }

        if (uppersection != null) uppersection.SetActive(false);

        SwitchAllSymbolsToFreeSpinMode(false);
        LineManager.instance.AssignBg(false);
        retriggerDoneOnce = false;

        if (accumulatedFreeSpinWins > 0)
        {
            WinCalculator.Instance.DisplayFreeSpinTotalPayout(accumulatedFreeSpinWins);
        }

        FreeSpinSlotMachineController.Instance.EnableGameButtons();

        for (int j = 0; j < 5; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (VisibleSymbolsGrid[j, i].Data.type == SymbolData.SymbolType.FreeSpin)
                    continue;

                Symbol existingSymbol = VisibleSymbolsGrid[j, i];
                SymbolData newSymbolData = VisibleSymbolsGrid[j, i].Data;

                LineManager.instance.InitializeGrid(j, i, existingSymbol, newSymbolData);
            }
        }


        if (winAnimationController != null && WebManAPI.Instance.tempPayLines.Count > 0)
        {
            winAnimationController.PlayWinAnimations(WebManAPI.Instance.tempPayLines);
            SlotMachineController.Instance.betManager.InvokeWin();
        }

        isDisplayingWin = false;
    }


    private void CreateDigitDisplay(float amount)
    {
        if (winAmountContainer == null || digitPrefab == null)
            return;

        ClearWinDigitsOnly();
        string formattedAmount = amount.ToString("F2");

        float totalWidth = 0f;
        foreach (char c in formattedAmount)
        {
            if (c == '.' || c == ',')
            {
                totalWidth += (decimalPointSprite.rect.width * decimalScale) + decimalSpacing;
            }
            else
            {
                int d = c - '0';
                totalWidth += winDigitSprites[d].rect.width + digitSpacing;
            }
        }

        float currentX = -totalWidth / 2f;

        foreach (char c in formattedAmount)
        {
            var go = Instantiate(digitPrefab, winAmountContainer);
            var img = go.GetComponent<Image>();
            var rt = go.GetComponent<RectTransform>();

            if (c == '.' || c == ',')
            {
                img.sprite = c is '.' ? decimalPointSprite : commaPointSprite;
                rt.localScale = Vector3.one * decimalScale;
                float w = decimalPointSprite.rect.width * decimalScale;
                currentX += w / 2f;
                rt.anchoredPosition = new Vector2(currentX, 0);
                currentX += w / 2f + decimalSpacing;
            }
            else
            {
                int d = c - '0';
                img.sprite = winDigitSprites[d];
                rt.localScale = Vector3.one;
                float w = winDigitSprites[d].rect.width;
                currentX += w / 2f;
                rt.anchoredPosition = new Vector2(currentX, 0);
                currentX += w / 2f + digitSpacing;
            }

            spawnedWinDigits.Add(img);
        }
    }
    private void CreateBlueDigitDisplay(float amount)
    {
        if (totalAmountContainer == null || BluedigitPrefab == null)
            return;

        ClearTotalDigitsOnly();
        string formattedAmount = amount.ToString("F2");

        float totalWidth = 0f;
        foreach (char c in formattedAmount)
        {
            if (c == '.' || c == ',')
            {
                totalWidth += (bluedecimalPointSprite.rect.width * decimalScale) + decimalSpacing;
            }
            else
            {
                int d = c - '0';
                totalWidth += winblueDigitSprites[d].rect.width + digitSpacing;
            }
        }

        float currentX = -totalWidth / 2f;

        foreach (char c in formattedAmount)
        {
            var go = Instantiate(BluedigitPrefab, totalAmountContainer);
            var img = go.GetComponent<Image>();
            var rt = go.GetComponent<RectTransform>();

            if (c == '.' || c == ',')
            {
                img.sprite = c is '.' ? bluedecimalPointSprite : bluecommaPointSprite;
                rt.localScale = Vector3.one * decimalScale;
                float w = bluedecimalPointSprite.rect.width * decimalScale;
                currentX += w / 2f;
                rt.anchoredPosition = new Vector2(currentX, 0);
                currentX += w / 2f + decimalSpacing;
            }
            else
            {
                int d = c - '0';
                img.sprite = winblueDigitSprites[d];
                rt.localScale = Vector3.one;
                float w = winblueDigitSprites[d].rect.width;
                currentX += w / 2f;
                rt.anchoredPosition = new Vector2(currentX, 0);
                currentX += w / 2f + digitSpacing;
            }

            spawnedTotalDigits.Add(img);
        }
    }

    private void UpdateDigitDisplay(float amount)
    {
        if (spawnedWinDigits.Count == 0)
        {
            CreateDigitDisplay(amount);
            return;
        }

        string formattedAmount = amount.ToString("F2");

        if (formattedAmount.Length != spawnedWinDigits.Count)
        {
            ClearWinDigitsOnly();
            CreateDigitDisplay(amount);
            return;
        }

        for (int i = 0; i < formattedAmount.Length; i++)
        {
            char c = formattedAmount[i];

            if (i < spawnedWinDigits.Count)
            {
                var digitImage = spawnedWinDigits[i];

                if (c == '.' || c == ',')
                {
                    digitImage.sprite = c is '.' ? decimalPointSprite : commaPointSprite;
                    RectTransform rt = digitImage.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -5f);
                }
                else if (char.IsDigit(c))
                {
                    int digit = int.Parse(c.ToString());
                    if (digit >= 0 && digit < winDigitSprites.Length)
                    {
                        digitImage.sprite = winDigitSprites[digit];
                        RectTransform rt = digitImage.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
                    }
                }
            }
        }
    }
    private void UpdateBlueDigitDisplay(float amount)
    {
        if (spawnedTotalDigits.Count == 0)
        {
            CreateBlueDigitDisplay(amount);
            return;
        }

        string formattedAmount = amount.ToString("F2");

        if (formattedAmount.Length != spawnedTotalDigits.Count)
        {
            ClearTotalDigitsOnly();
            CreateBlueDigitDisplay(amount);
            return;
        }

        for (int i = 0; i < formattedAmount.Length; i++)
        {
            char c = formattedAmount[i];

            if (i < spawnedTotalDigits.Count)
            {
                var digitImage = spawnedTotalDigits[i];

                if (c == '.' || c == ',')
                {
                    digitImage.sprite = c is '.' ? bluedecimalPointSprite : bluecommaPointSprite;
                    RectTransform rt = digitImage.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -5f);
                }
                else if (char.IsDigit(c))
                {
                    int digit = int.Parse(c.ToString());
                    if (digit >= 0 && digit < winblueDigitSprites.Length)
                    {
                        digitImage.sprite = winblueDigitSprites[digit];
                        RectTransform rt = digitImage.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
                    }
                }
            }
        }
    }

    private void ClearWinDigitsOnly()
    {
        foreach (var digit in spawnedWinDigits)
        {
            if (digit != null)
                Destroy(digit.gameObject);
        }
        spawnedWinDigits.Clear();
    }

    private void ClearLinesDisplay()
    {
        foreach (var digit in spawnedLinesDigits)
        {
            if (digit != null)
                Destroy(digit.gameObject);
        }
        spawnedLinesDigits.Clear();
    }

    private void ClearTotalDigitsOnly()
    {
        foreach (var digit in spawnedTotalDigits)
        {
            if (digit != null)
                Destroy(digit.gameObject);
        }
        spawnedTotalDigits.Clear();
    }

    private void ClearWinDisplay()
    {
        ClearWinDigitsOnly();
        ClearLinesDisplay();
        ClearTotalDigitsOnly();
    }

    private void CreateLinesDisplay(int linesCount)
    {
        if (initializingLinesContainer == null || digitPrefab == null)
        {
            Debug.LogError("[FreeSpinManager] initializingLinesContainer or digitPrefab is null!");
            return;
        }

        ClearLinesDisplay();

        string linesStr = linesCount.ToString();

        float totalWidth = 0f;

        foreach (char c in linesStr)
        {
            int d = c - '0';
            if (d >= 0 && d < winDigitSprites.Length)
            {
                totalWidth += winDigitSprites[d].rect.width + digitSpacing;
            }
        }

        if (totalWidth > digitSpacing)
        {
            totalWidth -= digitSpacing;
        }

        float currentX = -totalWidth / 2f;

        foreach (char c in linesStr)
        {
            if (!char.IsDigit(c)) continue;

            int digit = c - '0';
            if (digit < 0 || digit >= winDigitSprites.Length) continue;

            GameObject go = Instantiate(digitPrefab, initializingLinesContainer);
            Image img = go.GetComponent<Image>();
            RectTransform rt = go.GetComponent<RectTransform>();

            img.sprite = winDigitSprites[digit];
            rt.localScale = Vector3.one;

            float w = winDigitSprites[digit].rect.width;
            currentX += w / 2f;
            rt.anchoredPosition = new Vector2(currentX, 0);
            currentX += w / 2f + digitSpacing;

            spawnedLinesDigits.Add(img);
        }

        Debug.Log($"[FreeSpinManager] Created lines display with {spawnedLinesDigits.Count} digits");
    }

    [ContextMenu("Test Accumulated Win Display")]
    private void TestAccumulatedWinDisplay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to test this!");
            return;
        }

        accumulatedFreeSpinWins = 125.50f;

        StartCoroutine(DisplayAccumulatedWin());
    }

    [ContextMenu("Validate Container References")]
    private void ValidateContainerReferences()
    {
        Debug.Log("=== CONTAINER VALIDATION ===");

        Debug.Log($"Win Amount Container: {(winAmountContainer != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Initializing Lines Container: {(initializingLinesContainer != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Total Amount Container: {(totalAmountContainer != null ? "✓" : "✗ MISSING")}");

        Debug.Log($"Digit Prefab: {(digitPrefab != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Blue Digit Prefab: {(BluedigitPrefab != null ? "✓" : "✗ MISSING")}");

        Debug.Log($"Win Digit Sprites: {(winDigitSprites != null && winDigitSprites.Length == 10 ? "✓" : "✗ MISSING")}");
        Debug.Log($"Blue Digit Sprites: {(winblueDigitSprites != null && winblueDigitSprites.Length == 10 ? "✓" : "✗ MISSING")}");

        Debug.Log("=== END VALIDATION ===");
    }

    private void OnDestroy()
    {
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnSpinStart -= ResetDetectionFlags;
            SlotMachineController.Instance.OnSpinComplete -= ResetDetectionFlags;
        }

        if (FreeSpinSlotMachineController.Instance != null)
        {
            FreeSpinSlotMachineController.Instance.OnSpinComplete -= HandleFreeSpinComplete;
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }
}