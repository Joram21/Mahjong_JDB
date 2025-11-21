using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityTimer;
using static SymbolData;

public class SlotReel : MonoBehaviour
{
    [Header("References")]
    public RectTransform symbolsContainer;
    [SerializeField] private GameObject symbolPrefab;
    [SerializeField] private ParticleSystem stopParticles;
    [SerializeField] private bool isFirstReel;
    [SerializeField] private Animator reelAnimator; // Animator component for reel movement

    [Header("Animation Settings")]
    [SerializeField] private string spinAnimationName = "Spinning";
    [SerializeField] private string speedAnimationName = "Speed";
    [SerializeField] private string stopAnimationName = "Stop";
    [SerializeField] private string tensionAnimationName = "TensionAnimation";
    [SerializeField] private string immediateStopAnimationName = "ImmediateStop";
    [SerializeField] private float normalSpinSpeed = 1.0f;
    [SerializeField] private float tensionSpinSpeed = 0.5f; // Slower for tension effect

    [Header("Animation Timing")]
    [SerializeField] private float stopAnimationDuration = 1.0f; // Duration of stop animation with oscillation
    [SerializeField] private float tensionAnimationDuration = 2.0f; // Duration for tension effect

    [Header("Settings")]
    [SerializeField] public int visibleSymbols = 3;
    [SerializeField] private float symbolSpacing = 5f;
    public SymbolData[] symbolDatabase;
    public bool isTensionSoundPlaying = false;
    private bool isInFastMode = false;

    [Header("Free Spin Settings")]
    [SerializeField] private bool canShowFreeSpin = true;
    [SerializeField] private int reelIndex;

    // NEW: Bonus symbol settings
    [Header("Bonus Settings")]
    [SerializeField] private bool canShowBonus = false; // Only reel 3 should have this set to true

    // Keep all existing properties for game logic
    private int nextSymbolIndex = 0;
    private bool isSpinning;
    private float symbolHeight;
    private Vector2 initialPosition;
    private List<SymbolData> reelStrip = new List<SymbolData>();
    private bool isDecelerating = false;

    public bool IsInDecelerationPhase => isDecelerating;
    public System.Action<int> OnWomanSymbolDetected;
    public System.Action<int> OnWildSymbolDetected;
    public System.Action OnSpinStart;
    public System.Action OnSpinEnd;
    public bool IsSpinning => isSpinning;

    [Header("Visibility")]
    [SerializeField] private RectTransform reelViewport;
    public RectTransform ReelViewport => reelViewport;

    [Header("Deterministic Settings")]
    public List<SymbolData> targetSymbols = new List<SymbolData>();
    private bool isInitializing = true;
    private float speedMultiplier = 1.0f;
    private bool hasInjectedSymbols = false;
    private bool isStoppingRequested = false;
    public float currentSpeedMultiplier = 1.0f;
    public System.Action<int> OnFreeSpinSymbolDetected;
    public System.Action<int> OnMaskReelSymbolDetected;

    // Animation-specific properties
    public bool useTensionLinearMovement = false;
    private bool isInTensionMode = false;
    private bool tensionAnimationCompleted = false;

    // Cache for symbol GameObjects to avoid repeated GetComponent calls
    private List<Symbol> cachedSymbols = new List<Symbol>();

    // Track if immediate detection has been done to avoid duplicates
    private bool hasTriggeredImmediateDetection = false;

    // Property for SlotMachineController to check if tension is complete
    public bool IsTensionAnimationComplete => tensionAnimationCompleted && !isInTensionMode;
    [Header("Fast Mode Settings")]
    [SerializeField] private string fastStopAnimationName = "FastStop"; // NEW: Fast stop animation
    private void Awake()
    {
        ValidateComponents();
        isInitializing = true;

        // NEW: Set bonus capability based on reel index
        // Only reel 3 (index 2) can show bonus symbols
        canShowBonus = (reelIndex == 2);

        GenerateReelStrip();
        InitializeReel();
        isInitializing = false;
    }

    private void ValidateComponents()
    {
        if (symbolPrefab == null || symbolsContainer == null)
        {
            enabled = false;
        }

        if (reelAnimator == null)
        {
            reelAnimator = GetComponent<Animator>();
            if (reelAnimator == null)
            {
                enabled = false;
            }
        }
    }

    public void StartSpinning(bool fastMode = false)
    {
        if (isSpinning) return;

        StopAllCoroutines();
        isSpinning = true;
        isStoppingRequested = false;
        isInTensionMode = false;
        tensionAnimationCompleted = false;
        hasTriggeredImmediateDetection = false;
        isInFastMode = fastMode; // Store fast mode state

        OnSpinStart?.Invoke();

        // Start spinning animation (will now choose based on fast mode)
        PlaySpinAnimation();

        // Start continuous spinning coroutine for symbol management
        StartCoroutine(ContinuousSpinRoutine());
    }
    private void PlaySpinAnimation()
    {
        if (reelAnimator != null)
        {
            // Set animation speed based on current multipliers
            float animationSpeed = normalSpinSpeed * speedMultiplier / currentSpeedMultiplier;
            reelAnimator.speed = animationSpeed;

            // Choose animation based on fast mode
            string animationToPlay = isInFastMode ? speedAnimationName : spinAnimationName;

            // Play appropriate animation
            reelAnimator.Play(animationToPlay);
        }
    }
    private void PlayStopAnimation()
    {
        if (reelAnimator != null)
        {
            // Reset animation speed for stop animation
            reelAnimator.speed = 1.0f;

            // Choose animation based on speed mode
            string animationToPlay = isInFastMode ? fastStopAnimationName : stopAnimationName;

            // Play appropriate stop animation
            reelAnimator.Play(animationToPlay);
        }
    }


    private void PlayTensionAnimation()
    {
        if (reelAnimator != null && !isInTensionMode)
        {
            isInTensionMode = true;

            // Play tension animation - let the animation handle all speed/timing
            if (HasAnimationClip(tensionAnimationName))
            {
                reelAnimator.Play(tensionAnimationName);
            }
            else
            {
                isInTensionMode = false; // Reset if animation doesn't exist
            }
        }
    }

    private bool HasAnimationClip(string clipName)
    {
        if (reelAnimator == null || reelAnimator.runtimeAnimatorController == null)
            return false;

        foreach (var clip in reelAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return true;
        }
        return false;
    }

    private IEnumerator ContinuousSpinRoutine()
    {
        // This coroutine handles symbol assignment and game logic
        // while the Animator handles the visual movement

        while (!isStoppingRequested)
        {
            // Continue updating symbols and checking for wrapping
            // but don't move the container - let the animator handle that
            UpdateSymbolsForAnimation();
            yield return null;
        }

        // *** CRITICAL FIX: Only call HandleStopSequence if NOT using tension ***
        if (!useTensionLinearMovement)
        {
            // Once stopping is requested, begin deceleration process
            yield return StartCoroutine(HandleStopSequence());
        }
    }

    private void UpdateSymbolsForAnimation()
    {
        // This replaces the old MoveSymbols method
        // We still need to manage symbol creation/destruction
        // but position is handled by animator

        // Check if we need to create new symbols at the top
        // or remove symbols from the bottom based on animation position

        // This logic will depend on how your animator moves the reel
        // You might need to trigger symbol updates based on animation events
        // or position thresholds
    }

    public void BeginStopping()
    {
        if (!isSpinning || isStoppingRequested) return;

        StartCoroutine(BeginStoppingCor());
    }

    IEnumerator BeginStoppingCor()
    {
        if (targetSymbols != null && targetSymbols.Count >= visibleSymbols)
        {
            if (targetSymbols.Count > visibleSymbols)
            {
                targetSymbols = targetSymbols.Take(visibleSymbols).ToList();
            }
            InjectTargetSymbolsOnTopOptimized();

            DetectSymbolsForLogic();
        }
        else
        {
            if (targetSymbols?.Count > visibleSymbols)
            {
                targetSymbols = targetSymbols.Take(visibleSymbols).ToList();
            }
            InjectTargetSymbolsOnTopOptimized();

            DetectSymbolsForLogic();
            // Debug.LogError("Failed to inject symbols");
        }

        yield return new WaitForSeconds(0.1f);

        isStoppingRequested = true;

        if (useTensionLinearMovement)
        {
            PlayTensionAnimation();
        }
    }

    // NEW: Detect symbols for game logic (no sounds yet)
    private void DetectSymbolsForLogic()
    {
        if (hasTriggeredImmediateDetection) return;
        hasTriggeredImmediateDetection = true;

        bool hasFreeSpinSymbol = targetSymbols.Any(s => s.type == SymbolData.SymbolType.FreeSpin);
        bool hasWildSymbol = targetSymbols.Any(s => s.type == SymbolData.SymbolType.Wild);

        string symbolTypes = string.Join(", ", targetSymbols.Select(s => s.type.ToString()));

        // *** FREE SPIN TENSION: Reel 2 triggers tension on Reel 3 ***
        if (reelIndex == 1 && hasFreeSpinSymbol) // Reel 2 (index 1)
        {
            bool reel3HasFreeSpin = SlotMachineController.Instance?.DoesReelHaveFreeSpin(0) ?? false;

            if (reel3HasFreeSpin)
            {
                OnTensionTriggerDetected(); // This applies tension to reel 3
            }
        }

        //TODO stopped tension on reel 5
        // *** MASKREEL TENSION: Reel 4 triggers tension on Reel 5 ***
        // if (reelIndex == 3 && hasMaskReelSymbol) // Reel 4 (index 3)
        // {
        //     bool reel3HasMaskReel = SlotMachineController.Instance?.DoesReelHaveMaskReel(2) ?? false;
        //
        //     if (reel3HasMaskReel)
        //     {
        //         Debug.Log("[SlotReel:3] IMMEDIATE TENSION TRIGGER: Reel 4 has MaskReel and reel 3 has MaskReel!");
        //         OnTensionTriggerDetected(); // This applies tension to reel 5
        //     }
        // }

        // Fire events
        if (hasFreeSpinSymbol)
        {
            OnFreeSpinSymbolDetected?.Invoke(reelIndex);
        }

        if (hasWildSymbol)
        {
            OnWildSymbolDetected?.Invoke(reelIndex);
        }

        // Fire mask reel symbol event
        {
            OnMaskReelSymbolDetected?.Invoke(reelIndex);
        }
    }
    private IEnumerator HandleStopSequence()
    {
        // This should only run for NON-tension reels
        // Tension reels use their animation as the complete stop sequence

        // Play stop animation (includes oscillation in keyframes)
        PlayStopAnimation();

        // Wait for stop animation to complete (oscillation included)
        yield return new WaitForSeconds(stopAnimationDuration);

        // Final check (no sound triggers - those already happened when visible)
        CheckFinalSymbolsAndTriggerEvents();

        // Finalize the spin
        FinalizeSpin();
    }
    public void SetFastMode(bool fastMode)
    {
        isInFastMode = fastMode;
    }
    private bool IsStopAnimationComplete()
    {
        if (reelAnimator == null) return true;

        AnimatorStateInfo stateInfo = reelAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stopAnimationName) && stateInfo.normalizedTime >= 1.0f;
    }

    private void CheckFinalSymbolsAndTriggerEvents()
    {
        // This now only does final validation - sounds already played when visible
        bool hasFreeSpinSymbol = false;
        bool hasWildSymbol = false;
        // Get the FINAL symbols that are visible now
        List<Symbol> finalVisibleSymbols = GetTopVisibleSymbols(visibleSymbols);
        hasFreeSpinSymbol = finalVisibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
        hasWildSymbol = finalVisibleSymbols.Any(s => s.Data.type == SymbolData.SymbolType.Wild);

        // Log for debugging
        string symbolTypes = string.Join(", ", finalVisibleSymbols.Select(s => s.Data.type.ToString()));

        // NO SOUND TRIGGERS HERE - they already played when symbols became visible!
        // Just fire events for any systems that need final confirmation
        if (hasFreeSpinSymbol)
        {
            // Event already fired, but fire again for any late listeners
            OnFreeSpinSymbolDetected?.Invoke(reelIndex);
        }

        if (hasWildSymbol)
        {
            // Event already fired, but fire again for any late listeners
            OnWildSymbolDetected?.Invoke(reelIndex);
        }
    }

    public void StopSpinImmediately()
    {
        if (!isSpinning) return;

        StopAllCoroutines();
        isStoppingRequested = true;

        // Inject target symbols if needed - NOW OPTIMIZED
        if (targetSymbols != null && targetSymbols.Count >= visibleSymbols)
        {
            if (targetSymbols.Count > visibleSymbols)
            {
                targetSymbols = targetSymbols.Take(visibleSymbols).ToList();
            }

            if (!hasInjectedSymbols)
            {
                InjectTargetSymbolsOnTopOptimized();
                // Logic detection for immediate stop as well
                DetectSymbolsForLogic();
            }
        }
        else
        {
            if (targetSymbols?.Count > visibleSymbols)
            {
                targetSymbols = targetSymbols.Take(visibleSymbols).ToList();
            }

            if (!hasInjectedSymbols)
            {
                InjectTargetSymbolsOnTopOptimized();
                // Logic detection for immediate stop as well
                DetectSymbolsForLogic();
            }
            Debug.LogError("failed to inject new symbols");
        }

        // Play stop animation immediately
        PlayStopAnimation();

        // Start immediate stop coroutine
        StartCoroutine(ImmediateStopSequence());
    }

    private IEnumerator ImmediateStopSequence()
    {
        // Play stop animation immediately (includes oscillation)
        PlayStopAnimation();

        // Wait for stop animation to complete
        yield return new WaitForSeconds(stopAnimationDuration);

        // Final validation (no sounds - already played)
        CheckFinalSymbolsAndTriggerEvents();

        // Finalize
        FinalizeSpin();
    }

    // Animation Event Methods (called from Animation Events)
    public void OnSpinAnimationStart()
    {
    }

    public void OnStopAnimationStart()
    {
    }

    // Called when symbols are actually visible during stop animation
    public void OnSymbolsVisible()
    {
        // Check for different symbol types and play appropriate sounds
        bool hasFreeSpinSymbol = targetSymbols.Any(s => s.type == SymbolData.SymbolType.FreeSpin);
        bool hasMaskSymbols = targetSymbols.Any(s =>
            s.type == SymbolData.SymbolType.GreenDragon ||
            s.type == SymbolData.SymbolType.WhiteDragon ||
            s.type == SymbolData.SymbolType.BlackDragon ||
            s.type == SymbolData.SymbolType.RedDragon);

        if (hasFreeSpinSymbol)
        {
            PlayFreeSpinSoundWhenVisible();
        }

        if (hasMaskSymbols)
        {
            PlayMaskSymbolSoundWhenVisible();
        }
    }
    private void PlayMaskReelSoundWhenVisible()
    {
        if (SlotMachineController.Instance == null) return;

        bool reel3HasMaskReel = SlotMachineController.Instance.DoesReelHaveMaskReel(2); // Reel 3 (index 2)
        bool reel4HasMaskReel = SlotMachineController.Instance.DoesReelHaveMaskReel(3); // Reel 4 (index 3)
        bool reel5HasMaskReel = SlotMachineController.Instance.DoesReelHaveMaskReel(4); // Reel 5 (index 4)

        // MaskReel sound sequence logic
        if (reelIndex == 2 && reel3HasMaskReel) // Reel 3 (index 2)
        {
            SoundManager.Instance.PlaySound("maskreel1");
        }
        else if (reelIndex == 3 && reel3HasMaskReel && reel4HasMaskReel) // Reel 4 (index 3)
        {
            SoundManager.Instance.PlaySound("maskreel2"); ;
        }
        else if (reelIndex == 4 && reel3HasMaskReel && reel4HasMaskReel && reel5HasMaskReel) // Reel 5 (index 4)
        {
            SoundManager.Instance.PlaySound("maskreel3");
        }
    }


    private void PlayMaskSymbolSoundWhenVisible()
    {
        // Only play mask sounds during free spins
        if (FreeSpinManager.Instance == null || !FreeSpinManager.Instance.IsFreeSpinActive)
            return;

        // You can add specific sounds for mask symbols if needed
        // For now, just log that mask symbols appeared
        string maskTypes = string.Join(", ", targetSymbols
            .Where(s => s.type == SymbolData.SymbolType.WhiteDragon ||
                       s.type == SymbolData.SymbolType.BlackDragon ||
                       s.type == SymbolData.SymbolType.GreenDragon ||
                       s.type == SymbolData.SymbolType.RedDragon)
            .Select(s => s.type.ToString()));
    }

    // NEW: Animation Event - Start tension sound loop
    public void OnTensionSoundStart()
    {
        if (!isTensionSoundPlaying)
        {
            SoundManager.Instance.PlayTensionLoop();
            isTensionSoundPlaying = true;
        }
    }

    // Animation Event - Stop tension sound loop  
    IEnumerator OnTensionSoundStop()
    {
        float tensionSoundDuration = 1.8f;

        if (isTensionSoundPlaying)
        {
            // Wait for the wild sound to finish
            yield return new WaitForSeconds(tensionSoundDuration);

            SoundManager.Instance.StopTensionLoop();
            isTensionSoundPlaying = false;
        }
    }

    // Animation Event - Called when tension animation completes
    public void OnTensionAnimationComplete()
    {
        isInTensionMode = false;
        tensionAnimationCompleted = true;

        // Ensure tension sound is stopped
        if (isTensionSoundPlaying)
        {
            SoundManager.Instance.StopTensionLoop();
            isTensionSoundPlaying = false;
        }

        // Notify controller that tension is complete and subsequent reels can proceed
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnReelTensionCompleted(reelIndex);
        }

        // Final checks and finalization
        CheckFinalSymbolsAndTriggerEvents();
        FinalizeSpin();
    }

    public void OnStopAnimationComplete()
    {
        CheckFinalSymbolsAndTriggerEvents();
        FinalizeSpin();
    }

    // Play freespin sounds when symbols are actually visible (restored original logic)
    private void PlayFreeSpinSoundWhenVisible()
    {
        if (SlotMachineController.Instance == null) return;

        bool reel3HasFreeSpin = SlotMachineController.Instance.DoesReelHaveFreeSpin(0);
        bool reel4HasFreeSpin = SlotMachineController.Instance.DoesReelHaveFreeSpin(1);
        bool reel5HasFreeSpin = SlotMachineController.Instance.DoesReelHaveFreeSpin(2);

        // Apply tension effect for reel 5 if reels 3 and 4 have freespins
        if (reel3HasFreeSpin && reel4HasFreeSpin)
        {
            SlotMachineController.Instance.ApplyReel3TensionEffect();
        }

        // Original freespin sound logic restored:
        if (reelIndex == 2 && reel3HasFreeSpin)
        {
            SoundManager.Instance.PlaySound("freespin1");
        }
        else if (reelIndex == 3)
        {
            // Play freespin2 only if reel 1 also has a free spin
            if (reel3HasFreeSpin && reel4HasFreeSpin)
            {
                SoundManager.Instance.PlaySound("freespin2");

                // Trigger tension effect for reel 3 if it's still spinning
                SlotMachineController.Instance.ApplyReel3TensionEffect();
            }
        }
        else if (reelIndex == 4)
        {
            // Play freespin3 only if reels 1 and 2 also have free spins
            if (reel3HasFreeSpin && reel4HasFreeSpin && reel3HasFreeSpin)
            {
                // Set freeSpinHasBegun to true in FreeSpinManager
                if (FreeSpinManager.Instance != null)
                {
                    FreeSpinManager.Instance.FreeSpinHasBegun = true;
                }

                Timer.Register(0.8f, () =>
                {
                    SoundManager.Instance.PlaySound("freespin3");
                });
            }
        }
    }

    // NEW: Play bonus sound when symbols are visible

    // OPTIMIZED METHOD: Reuses existing GameObjects instead of creating new ones
    private void InjectTargetSymbolsOnTopOptimized()
    {
        if (targetSymbols == null)
        {
            return;
        }

        // Verify all symbols are valid
        foreach (var symbol in targetSymbols)
        {
            if (symbol == null)
            {
                return;
            }
        }

        // Get all existing symbol components (update cache if needed)
        UpdateSymbolCache();

        // Sort symbols by Y position (top to bottom)
        cachedSymbols.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        // Update the top visible symbols with new data
        for (int i = 0; i < visibleSymbols && i < cachedSymbols.Count && i < targetSymbols.Count; i++)
        {
            Symbol existingSymbol = cachedSymbols[i];
            SymbolData newSymbolData = targetSymbols[i];

            // Update the existing symbol with new data
            UpdateExistingSymbol(existingSymbol, newSymbolData, i, reelIndex);
            LineManager.instance.InitializeGrid(reelIndex, i, existingSymbol, newSymbolData);
        }

        // If we need more symbols than we have, create them (rare case)
        if (visibleSymbols > cachedSymbols.Count)
        {
            for (int i = cachedSymbols.Count; i < visibleSymbols; i++)
            {
                CreateAdditionalSymbol(targetSymbols[i]);
            }
        }

        nextSymbolIndex = (nextSymbolIndex - targetSymbols.Count + reelStrip.Count) % reelStrip.Count;
        hasInjectedSymbols = true;
    }

    // Update symbol cache to avoid repeated GetComponent calls
    private void UpdateSymbolCache()
    {
        cachedSymbols.Clear();
        foreach (Transform child in symbolsContainer)
        {
            Symbol symbol = child.GetComponent<Symbol>();
            if (symbol != null)
            {
                cachedSymbols.Add(symbol);
            }
        }
    }

    // Update an existing symbol with new data (no GameObject creation/destruction)
    private void UpdateExistingSymbol(Symbol existingSymbol, SymbolData newSymbolData, int row = 0, int col = 0)
    {
        // Update the symbol's data
        existingSymbol.Initialize(newSymbolData);
        existingSymbol.row = row;
        existingSymbol.col = col;

        // Update the sprite on the DefaultImage
        Transform defaultImageTransform = existingSymbol.transform.Find("DefaultImage");
        Image baseImage = defaultImageTransform?.GetComponent<Image>();

        if (baseImage != null)
        {
            //  SMART FIX: Choose correct sprite based on game mode
            bool isInFreeSpinMode = (FreeSpinManager.Instance != null &&
                                    FreeSpinManager.Instance.freeSpinFeatureActive);

            if (isInFreeSpinMode && newSymbolData.freeSpinSprite != null)
            {
                baseImage.sprite = newSymbolData.freeSpinSprite; //  Free spin sprite!
            }
            else
            {
                baseImage.sprite = newSymbolData.sprite; // Normal sprite
            }

            baseImage.preserveAspect = true;
        }

        // Reset any win highlights or animations
        existingSymbol.ResetAnimation();
        existingSymbol.DeactivateWinHighlight();

        // Validate the symbol size
        existingSymbol.ValidateSymbolSize();
    }

    // BETTER FIX: CreateAdditionalSymbol() - Smart sprite selection
    private void CreateAdditionalSymbol(SymbolData symbolData)
    {
        GameObject symbolObj = Instantiate(symbolPrefab, symbolsContainer);
        symbolObj.transform.SetAsFirstSibling();

        Symbol symbol = symbolObj.GetComponent<Symbol>();
        if (symbol == null)
        {
            symbol = symbolObj.AddComponent<Symbol>();
        }

        symbol.Initialize(symbolData);

        Image baseImage = symbolObj.transform.Find("DefaultImage")?.GetComponent<Image>();
        if (baseImage != null)
        {
            // SMART FIX: Choose correct sprite based on game mode
            bool isInFreeSpinMode = (FreeSpinManager.Instance != null &&
                                    FreeSpinManager.Instance.freeSpinFeatureActive);

            if (isInFreeSpinMode && symbolData.freeSpinSprite != null)
            {
                baseImage.sprite = symbolData.freeSpinSprite; // Free spin sprite!
            }
            else
            {
                baseImage.sprite = symbolData.sprite; // Normal sprite
            }

            baseImage.preserveAspect = true;
        }

        // Add to cache
        cachedSymbols.Add(symbol);
    }
    // Keep all existing symbol management methods unchanged
    private void InitializeReel()
    {
        symbolHeight = symbolPrefab.GetComponent<RectTransform>().rect.height + symbolSpacing;
        initialPosition = symbolsContainer.anchoredPosition;
        CreateSymbols();
    }

    // private void GenerateReelStrip()
    // {
    //     reelStrip.Clear();

    //     if (!isInitializing && targetSymbols != null && targetSymbols.Count == visibleSymbols)
    //     {
    //         reelStrip.AddRange(targetSymbols);
    //         int remaining = 20 - targetSymbols.Count;
    //         int safetyCounter = 0;
    //         while (reelStrip.Count < 20 && safetyCounter < 40)
    //         {
    //             SymbolData symbol = GetWeightedSymbol();
    //             if (symbol != null)
    //                 reelStrip.Add(symbol);
    //             safetyCounter++;
    //         }
    //     }
    //     else
    //     {
    //         int reelLength = 20;
    //         int safetyCounter = 0;
    //         while (reelStrip.Count < reelLength && safetyCounter < 40)
    //         {
    //             SymbolData symbol = GetWeightedSymbol();
    //             if (symbol != null)
    //                 reelStrip.Add(symbol);
    //             safetyCounter++;
    //         }
    //     }
    // }

    private SymbolData GetWeightedSymbol()
    {
        List<SymbolData> validSymbols = new List<SymbolData>(symbolDatabase);

        // Remove wild symbols from first reel
        if (isFirstReel)
            validSymbols.RemoveAll(s => s.type == SymbolData.SymbolType.Wild);

        // Remove free spin symbols from reels beyond index 2 (reels 4 and 5)
        if (!canShowFreeSpin || reelIndex > 2)
            validSymbols.RemoveAll(s => s.type == SymbolData.SymbolType.FreeSpin);

        // NEW: Handle MaskReel symbols - only appear on reels 3, 4, 5 (indices 2, 3, 4)
        if (reelIndex < 2)
            validSymbols.RemoveAll(s => s.type == SymbolData.SymbolType.FreeSpin);

        // NEW: During free spins, filter symbols differently
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            validSymbols = FilterSymbolsForFreeSpins(validSymbols);
        }

        if (validSymbols.Count == 0)
        {
            return null;
        }

        float totalWeight = validSymbols.Sum(s => s.spawnWeight);
        float random = Random.Range(0, totalWeight);
        float current = 0;

        foreach (SymbolData symbol in validSymbols)
        {
            current += symbol.spawnWeight;
            if (random <= current)
                return symbol;
        }

        return validSymbols[0];
    }

    // NEW: Filter symbols specifically for free spins
    private List<SymbolData> FilterSymbolsForFreeSpins(List<SymbolData> allSymbols)
    {
        List<SymbolData> freeSpinValidSymbols = new List<SymbolData>();

        foreach (SymbolData symbol in allSymbols)
        {
            switch (symbol.type)
            {
                case SymbolData.SymbolType.Jack:
                case SymbolData.SymbolType.King:
                case SymbolData.SymbolType.Queen:
                case SymbolData.SymbolType.Ten:
                case SymbolData.SymbolType.FreeSpin:
                case SymbolData.SymbolType.Wild:
                case SymbolData.SymbolType.WhiteDragon:
                case SymbolData.SymbolType.GreenDragon:
                case SymbolData.SymbolType.RedDragon:
                case SymbolData.SymbolType.BlackDragon:
                    freeSpinValidSymbols.Add(symbol);
                    break;


            }
        }

        return freeSpinValidSymbols;
    }

    private void CreateSymbols()
    {
        foreach (Transform child in symbolsContainer)
            Destroy(child.gameObject);

        cachedSymbols.Clear(); // Clear cache when recreating symbols

        int symbolsNeeded = visibleSymbols + 27;
        for (int i = 0; i < symbolsNeeded; i++)
        {
            CreateSymbol(i);
        }

        // Update cache after creating symbols
        UpdateSymbolCache();
    }

    private void CreateSymbol(int index)
    {
        if (reelStrip.Count == 0)
        {
            return;
        }

        GameObject symbolObj = Instantiate(symbolPrefab, symbolsContainer);
        SymbolData symbolData = reelStrip[index % reelStrip.Count];

        Transform defaultImageTransform = symbolObj.transform.Find("DefaultImage");
        Image baseImage = defaultImageTransform?.GetComponent<Image>();

        Transform AnimationImageTransform = symbolObj.transform.Find("AnimationImage");
        Image animImage = AnimationImageTransform?.GetComponent<Image>();
        Transform highlightTransform = symbolObj.transform.Find("WinHighlightImage");
        Image highlightImage = highlightTransform?.GetComponent<Image>();

        if (baseImage == null)
        {
            return;
        }

        if (animImage == null)
        {
            return;
        }

        baseImage.sprite = symbolData.sprite;
        Symbol symbol = symbolObj.AddComponent<Symbol>();
        Animator animator = symbolObj.GetComponentInChildren<Animator>();

        symbol.Initialize(symbolData);
        symbol.ValidateSymbolSize();
    }
    // Add this simple method to SlotReel.cs
    public void PlayImmediateStop()
    {
        if (!isSpinning) return;

        // Stop current processes
        StopAllCoroutines();
        isStoppingRequested = true;

        // Check if we already have injected symbols
        if (hasInjectedSymbols)
        {
            Debug.Log($"[SlotReel] Symbols already injected on {gameObject.name}");
            return;
        }

        // Ensure targetSymbols list exists
        if (targetSymbols == null)
            targetSymbols = new List<SymbolData>();

        // Generate target symbols if we don't have enough
        if (targetSymbols.Count < visibleSymbols)
        {
            Debug.Log($"[SlotReel] Generating {visibleSymbols - targetSymbols.Count} missing target symbols");

            while (targetSymbols.Count < visibleSymbols)
            {
                SymbolData randomSymbol = GetWeightedSymbol();
                if (randomSymbol != null)
                {
                    targetSymbols.Add(randomSymbol);
                }
                else
                {
                    Debug.LogError($"[SlotReel] Failed to generate symbol {targetSymbols.Count + 1} for {gameObject.name}");
                    break;
                }
            }
        }

        // Only inject if we have enough target symbols
        if (targetSymbols.Count >= visibleSymbols)
        {
            InjectTargetSymbolsOnTopOptimized();
            hasInjectedSymbols = true;
            Debug.Log($"[SlotReel] Successfully injected {targetSymbols.Count} symbols on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[SlotReel] Cannot inject symbols: only have {targetSymbols.Count} target symbols but need {visibleSymbols} on {gameObject.name}");

            // Fallback: try to work with current symbols
            hasInjectedSymbols = true; // Mark as done to prevent infinite loops
        }

        // Play immediate stop animation
        if (reelAnimator != null)
        {
            reelAnimator.Play("StopImmediately");
        }

        // Start simple completion sequence
        StartCoroutine(SimpleImmediateStopSequence());
    }


    private IEnumerator SimpleImmediateStopSequence()
    {
        // Wait for immediate stop animation (short duration)
        yield return new WaitForSeconds(0.3f); // Adjust as needed

        // Finalize
        FinalizeSpin();
    }

    public List<Symbol> GetTopVisibleSymbols(int count)
    {
        List<Symbol> visibleSymbols = new List<Symbol>();

        // Use cached symbols if available, otherwise get fresh ones
        List<Symbol> allSymbols = cachedSymbols.Count > 0 ? new List<Symbol>(cachedSymbols) : new List<Symbol>();

        if (allSymbols.Count == 0)
        {
            // Fallback to manual search if cache is empty
            foreach (Transform child in symbolsContainer)
            {
                Symbol symbol = child.GetComponent<Symbol>();
                if (symbol != null) allSymbols.Add(symbol);
            }
        }

        allSymbols.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        for (int i = 0; i < Mathf.Min(count, allSymbols.Count); i++)
        {
            visibleSymbols.Add(allSymbols[i]);
        }

        if (reelIndex <= 2)
        {
            string symbolTypes = string.Join(", ", visibleSymbols.Select(s => s.Data.type.ToString()));
        }
        return visibleSymbols;
    }

    public void OnStopSoundTiming()
    {
        if (SlotMachineController.Instance == null) return;

        bool reel3HasFreeSpin = SlotMachineController.Instance.DoesReelHaveFreeSpin(0);

        if (!FreeSpinManager.Instance.freeSpinFeatureActive && !reel3HasFreeSpin && (reelIndex == 0 || reelIndex == 1 || reelIndex == 2))
            SoundManager.Instance.PlaySound("reelstop");
        if (!FreeSpinManager.Instance.freeSpinFeatureActive && (reelIndex == 3 || reelIndex == 4))
            SoundManager.Instance.PlaySound("reelstop");
    }

    public void StartTensionSound()
    {
        // This method can be called directly or via animation event
        OnTensionSoundStart();
    }

    public void StopTensionSound()
    {
        // This method can be called directly or via animation event
        StartCoroutine(OnTensionSoundStop());
    }

    public void OnTensionTriggerDetected()
    {
        // Immediately notify controller to apply tension effects
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnReelTriggeredTension(reelIndex);
        }
    }

    public void OnTensionAnimationStarted()
    {
        isInTensionMode = true;
        tensionAnimationCompleted = false;

        // Notify controller that this reel is now in tension and should block others
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnReelTensionStarted(reelIndex);
        }
    }

    // public void ResetReelPosition()
    // {
    //     symbolsContainer.anchoredPosition = initialPosition;
    //     hasInjectedSymbols = false;
    //     hasTriggeredImmediateDetection = false; // Reset for next spin
    //     tensionAnimationCompleted = false; // Reset tension state

    //     // Reset animator to idle state
    //     if (reelAnimator != null)
    //     {
    //         reelAnimator.Play("Idle", 0, 0f);
    //         reelAnimator.speed = 1.0f;
    //     }
    // }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;

        // Update animation speed if currently spinning
        if (isSpinning && reelAnimator != null)
        {
            float animationSpeed = normalSpinSpeed * speedMultiplier / currentSpeedMultiplier;
            reelAnimator.speed = animationSpeed;
        }
    }

    private void FinalizeSpin()
    {
        isSpinning = false;
        isInTensionMode = false;
        tensionAnimationCompleted = false; // Reset for next spin

        // Ensure tension sound is stopped
        if (isTensionSoundPlaying)
        {
            SoundManager.Instance.StopTensionLoop();
            isTensionSoundPlaying = false;
        }

        if (stopParticles != null) stopParticles.Play();

        OnSpinEnd?.Invoke();

        hasInjectedSymbols = false;
        hasTriggeredImmediateDetection = false; // Reset for next spin
        GenerateReelStrip();

        // Reset animator
        if (reelAnimator != null)
        {
            reelAnimator.speed = 1.0f;
        }
    }

    private SymbolData GetNextReelSymbol()
    {
        SymbolData nextSymbol = reelStrip[nextSymbolIndex];
        nextSymbolIndex = (nextSymbolIndex + 1) % reelStrip.Count;
        return nextSymbol;
    }
    // ADD THESE FIELDS at the top of your SlotReel class (around line 70):

    [Header("Target Symbol Mode")]
    private bool useTargetSymbolMode = false; // NEW: Flag to enable target symbol mode
    private bool targetSymbolsPreInjected = false; // NEW: Track if symbols were pre-set

    // ADD THESE PUBLIC METHODS (add near line 100):

    /// <summary>
    /// Enable target symbol mode - call this BEFORE StartSpinning()
    /// </summary>
    public void EnableTargetSymbolMode(bool enable)
    {
        useTargetSymbolMode = enable;
        targetSymbolsPreInjected = false;

        Debug.Log($"[SlotReel] {gameObject.name} - Target symbol mode: {(enable ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Pre-inject target symbols before spinning starts
    /// </summary>
    public void PreInjectTargetSymbols()
    {
        if (!useTargetSymbolMode || targetSymbols == null || targetSymbols.Count == 0)
        {
            Debug.LogWarning($"[SlotReel] {gameObject.name} - Cannot pre-inject: mode={useTargetSymbolMode}, count={targetSymbols?.Count ?? 0}");
            return;
        }

        Debug.Log($"[SlotReel] {gameObject.name} - PRE-INJECTING {targetSymbols.Count} target symbols");

        // Verify all target symbols are valid
        for (int i = 0; i < targetSymbols.Count; i++)
        {
            if (targetSymbols[i] == null)
            {
                Debug.LogError($"[SlotReel] Target symbol {i} is NULL on {gameObject.name}!");
                return;
            }
            Debug.Log($"[SlotReel]   [{i}] {targetSymbols[i].name}");
        }

        // Mark as pre-injected so we know to use these during stopping
        targetSymbolsPreInjected = true;
    }

    // MODIFY GenerateReelStrip() method (around line 650):
    // Replace the entire method with this:

    private void GenerateReelStrip()
    {
        reelStrip.Clear();

        // NEW: Check if we should use target symbols
        if (useTargetSymbolMode && targetSymbolsPreInjected && targetSymbols != null && targetSymbols.Count > 0)
        {
            Debug.Log($"[SlotReel] {gameObject.name} - Using TARGET symbols for reel strip");

            // Add target symbols first
            reelStrip.AddRange(targetSymbols);

            // Fill remaining with random symbols
            int remaining = 20 - targetSymbols.Count;
            int safetyCounter = 0;
            while (reelStrip.Count < 20 && safetyCounter < 40)
            {
                SymbolData symbol = GetWeightedSymbol();
                if (symbol != null)
                    reelStrip.Add(symbol);
                safetyCounter++;
            }

            Debug.Log($"[SlotReel] {gameObject.name} - Reel strip generated with {targetSymbols.Count} target symbols + {remaining} random");
            return;
        }

        // ORIGINAL: Random generation
        if (!isInitializing && targetSymbols != null && targetSymbols.Count == visibleSymbols)
        {
            reelStrip.AddRange(targetSymbols);
            int remaining = 20 - targetSymbols.Count;
            int safetyCounter = 0;
            while (reelStrip.Count < 20 && safetyCounter < 40)
            {
                SymbolData symbol = GetWeightedSymbol();
                if (symbol != null)
                    reelStrip.Add(symbol);
                safetyCounter++;
            }
        }
        else
        {
            int reelLength = 20;
            int safetyCounter = 0;
            while (reelStrip.Count < reelLength && safetyCounter < 40)
            {
                SymbolData symbol = GetWeightedSymbol();
                if (symbol != null)
                    reelStrip.Add(symbol);
                safetyCounter++;
            }
        }
    }

    // ADD THIS METHOD to verify target symbols (add anywhere):

    public void VerifyTargetSymbols()
    {
        Debug.Log($"[SlotReel] {gameObject.name} VERIFICATION:");
        Debug.Log($"  - useTargetSymbolMode: {useTargetSymbolMode}");
        Debug.Log($"  - targetSymbolsPreInjected: {targetSymbolsPreInjected}");
        Debug.Log($"  - targetSymbols count: {targetSymbols?.Count ?? 0}");

        if (targetSymbols != null && targetSymbols.Count > 0)
        {
            for (int i = 0; i < targetSymbols.Count; i++)
            {
                Debug.Log($"  - Symbol {i}: {(targetSymbols[i] != null ? targetSymbols[i].name : "NULL")}");
            }
        }
    }

    // MODIFY ResetReelPosition() method (around line 900):
    // Add these lines at the END of the method:

    public void ResetReelPosition()
    {
        symbolsContainer.anchoredPosition = initialPosition;
        hasInjectedSymbols = false;
        hasTriggeredImmediateDetection = false;
        tensionAnimationCompleted = false;

        // Reset animator to idle state
        if (reelAnimator != null)
        {
            reelAnimator.Play("Idle", 0, 0f);
            reelAnimator.speed = 1.0f;
        }

        // NEW: Reset target symbol mode flags
        useTargetSymbolMode = false;
        targetSymbolsPreInjected = false;

        Debug.Log($"[SlotReel] {gameObject.name} - Reset complete, target mode disabled");
    }
}