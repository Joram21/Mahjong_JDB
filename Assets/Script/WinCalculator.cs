using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityTimer;

public class WinCalculator : MonoBehaviour
{
    // Singleton Instance
    public static WinCalculator Instance { get; private set; }

    [SerializeField] private Color winHighlightColor = Color.yellow;
    [SerializeField] private float highlightDuration = 1f;

    // Celebration fields
    [SerializeField] private ConfettiSpawner confettiSpawner;
    [SerializeField] private float bigWinThreshold = 50f;
    [SerializeField] private float confettiBurstInterval = 0.3f;
    [SerializeField] private BetManager betManager;
    [SerializeField] private UIElementManager uiElements;

    // Win Animation Controller
    [SerializeField] public WinAnimationController winAnimationController;

    // Celebration tracking
    private Coroutine celebrationRoutine;
    public bool isCelebrating;
    private float celebrationDuration = 10f;

    // Track if there's a wild in the winning combinations
    private bool hasWildInWinningCombination = false;
    private Coroutine wildSoundCoroutine;

    // Store FreeSpin and MaskReel combinations for later display
    private List<Symbol> storedFreeSpinSymbols = new List<Symbol>();
    private List<Symbol> storedMaskReelSymbols = new List<Symbol>();
    private WinAnimationController.WinPosition storedFreeSpinPosition;
    private WinAnimationController.WinPosition storedMaskReelPosition;

    public event Action OnBalanceChanged;

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the next line if you want this to persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Debug.LogWarning("[WinCalculator] Multiple WinCalculator instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // If there's no Win Animation Controller, try to find one
        if (winAnimationController == null)
        {
            winAnimationController = FindAnyObjectByType<WinAnimationController>();

            // If still null, create one
            if (winAnimationController == null)
            {
                GameObject controllerObj = new GameObject("WinAnimationController");
                winAnimationController = controllerObj.AddComponent<WinAnimationController>();
            }
        }
    }

    void OnDestroy()
    {
        // Clear the instance when this object is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowLines()
    {
        StartCoroutine(ShowLinesCor(new List<(int, int, float)>() { (0, 2, 0.05f), (5, 3, 0.05f), (7, 4, 0.05f), (9, 3, 0.05f) }));
    }

    IEnumerator ShowLinesCor(List<(int, int, float)> winningLines)
    {
        if (winningLines.Count <= 0)
        {
            yield break;
        }

        LineManager.instance.linesParent.SetActive(true);
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1].gameObject.SetActive(true);
            yield return new WaitForSeconds(0.03f);
        }
        yield return new WaitForSeconds(0.8f);
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1].gameObject.SetActive(false);
        }
        LineManager.instance.linesParent.SetActive(false);
        StartCoroutine(ShowLinesWithSquares(winningLines));
    }


    IEnumerator ShowLinesWithSquares(List<(int, int, float)> winningLines)
    {
        if (winningLines.Count <= 0)
        {
            yield break;
        }
        LineManager.instance.linesParent.SetActive(true);
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1].EnableSquaresThenLine(t.Item2, t.Item3);
            yield return new WaitForSeconds(5.0f);
            LineManager.instance.lines[t.Item1].DisableSquaresThenLine();
        }
        yield return new WaitForSeconds(0.1f);
        LineManager.instance.linesParent.SetActive(false);
    }

    // Main win calculation method
    public float CalculateWin(SlotReel[] reels)
    {
        float totalPayout = 0f;
        List<Symbol>[] visibleSymbols = GetVisibleSymbols(reels);
        bool hasFreeSpinWin = false;
        // bool hasMaskReelWin = false;

        // Debug.Log($"[WinCalculator] Starting CalculateWin - CurrentBet: {betManager.CurrentBet:F2}, CurrentMultiplier: {betManager.CurrentBetMultiplier:F2}");

        // Reset the wild flag
        hasWildInWinningCombination = false;

        // Clear any previous winning combinations
        if (winAnimationController != null)
        {
            winAnimationController.ClearWinningCombinations();
        }

        // Clear stored special symbol combinations
        storedFreeSpinSymbols.Clear();
        storedMaskReelSymbols.Clear();

        // Check for FreeSpin combinations (NO DISPLAY)
        CheckFreeSpinCombinations(visibleSymbols, ref hasFreeSpinWin);

        foreach (var payLine in WebManAPI.Instance.payLines)
        {
            totalPayout += payLine.Item3;
        }

        // Calculate final win
        float totalWin = totalPayout;


        // Start animations if there are any winning combinations
        if (winAnimationController != null && totalWin > 0 && !hasFreeSpinWin)

        {
            winAnimationController.PlayWinAnimations();
        }

        // Handle sounds and animations based on win type
        if (hasWildInWinningCombination && totalWin > 0 && !FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            PlayWildSoundThenWinSound(totalWin);
        }
        else if (totalWin >= bigWinThreshold && !FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            StartCelebration();
            SoundManager.Instance.PlayBigWinLoop();
        }
        else if (totalWin > 0 && totalWin < bigWinThreshold && !hasFreeSpinWin && !FreeSpinManager.Instance.IsFreeSpinActive)
        {
            SoundManager.Instance.PlaySmallWinLoop();
        }

        // Handle free spin wins
        if (hasFreeSpinWin && FreeSpinManager.Instance != null)
        {
            if (FreeSpinManager.Instance.freeSpinFeatureActive)
            {
                float freeSpinPayout = CalculateFreeSpinPayout();
                StartCoroutine(FreeSpinManager.Instance.HandleFreeSpinsDuringFreeSpins(freeSpinPayout));
            }
            else
            {
                if (winAnimationController != null && hasFreeSpinWin)
                {
                    SlotMachineController.Instance.SetPlaceBetText();
                    winAnimationController.PlayWinAnimations();
                    StartCoroutine(FreeSpinManager.Instance.HandleFreeSpinSequence());
                    SoundManager.Instance.PlaySound("freespin");
                    FreeSpinManager.Instance.FreeSpinHasBegun = true;
                    FreeSpinManager.Instance.SetInteractable();
                }
            }
        }

        return totalPayout; // Return the payout before multiplier application
    }


    // MODIFIED: Check for FreeSpin combinations (NO IMMEDIATE DISPLAY)
    private void CheckFreeSpinCombinations(List<Symbol>[] visibleSymbols, ref bool hasFreeSpinWin)
    {
        bool reel3HasFreeSpin = visibleSymbols[2].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
        bool reel4HasFreeSpin = visibleSymbols[3].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
        bool reel5HasFreeSpin = visibleSymbols[4].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);

        // Regular Free Spin: 3 FreeSpin symbols on reels 3, 4, 5
        if (reel3HasFreeSpin && reel4HasFreeSpin && reel5HasFreeSpin)
        {
            // Debug.Log("[WinCalculator] Free Spin combination detected: 3 FreeSpin symbols");
            hasFreeSpinWin = true;

            // STORE FreeSpin symbols for later display (NO IMMEDIATE DISPLAY)
            List<Symbol> freeSpinCombo = new List<Symbol>();
            freeSpinCombo.AddRange(visibleSymbols[2].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));
            freeSpinCombo.AddRange(visibleSymbols[3].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));
            freeSpinCombo.AddRange(visibleSymbols[4].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));

            // Store for later display
            storedFreeSpinSymbols = new List<Symbol>(freeSpinCombo);

            // CHANGE: Use any FreeSpin symbol from reel 0 (like normal wins)
            Symbol firstReelFreeSpin = visibleSymbols[2].FirstOrDefault(s => s.Data.type == SymbolData.SymbolType.FreeSpin);

            if (!FreeSpinManager.Instance.retriggerDoneOnce)
            {
                if (firstReelFreeSpin != null && winAnimationController != null)
                {
                    storedFreeSpinPosition = winAnimationController.DetermineWinPositionPublic(firstReelFreeSpin, 2);
                }

                foreach (Symbol symbol in freeSpinCombo)
                {
                    symbol.ActivateWinHighlight();
                    Debug.LogError("calling when checking combinations");
                    symbol.PlayWinAnimation();
                }
            }
        }
    }

    // MODIFIED: Check for MaskReel combinations (NO IMMEDIATE DISPLAY)
    private void CheckMaskReelCombinations(List<Symbol>[] visibleSymbols, ref bool hasMaskReelWin)
    {
        bool reel3HasMaskReel = visibleSymbols[2].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
        bool reel4HasMaskReel = visibleSymbols[3].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
        bool reel5HasMaskReel = visibleSymbols[4].Any(s => s.Data.type == SymbolData.SymbolType.FreeSpin);

        // MaskReel combination: 3 MaskReel symbols on reels 3, 4, 5
        if (reel3HasMaskReel && reel4HasMaskReel && reel5HasMaskReel)
        {
            // Debug.Log("[WinCalculator] MaskReel combination detected: 3 MaskReel symbols");
            hasMaskReelWin = true;

            // STORE MaskReel symbols for later display (NO IMMEDIATE DISPLAY)
            List<Symbol> maskReelCombo = new List<Symbol>();
            maskReelCombo.AddRange(visibleSymbols[2].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));
            maskReelCombo.AddRange(visibleSymbols[3].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));
            maskReelCombo.AddRange(visibleSymbols[4].Where(s => s.Data.type == SymbolData.SymbolType.FreeSpin));

            // Store for later display
            storedMaskReelSymbols = new List<Symbol>(maskReelCombo);

            // Determine position based on first MaskReel symbol (reel 3)
            if (maskReelCombo.Count > 0 && winAnimationController != null)
            {
                storedMaskReelPosition = winAnimationController.DetermineWinPositionPublic(maskReelCombo[0], 2);
            }

            // Activate win highlights (but NO payout display)
            foreach (Symbol symbol in maskReelCombo)
            {
                symbol.ActivateWinHighlight();
                Debug.LogError("calling on mask reel combination");
                symbol.PlayWinAnimation();
            }

            // Debug.Log("[WinCalculator] MaskReel symbols stored for later display - NO immediate payout shown");
        }
    }

    // Calculate FreeSpin payout (called during freespin sequence)
    private float CalculateFreeSpinPayout()
    {
        float freeSpinPayout = 0f;

        if (storedFreeSpinSymbols.Count >= 3)
        {
            int matchCount = storedFreeSpinSymbols.Count;
            int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
            Symbol freeSpinSymbol = storedFreeSpinSymbols.Find(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
            if (freeSpinSymbol != null)
            {
                freeSpinPayout = freeSpinSymbol.Data.payouts[payoutIndex] * betManager.CurrentBet;
                // Debug.Log($"[WinCalculator] Free Spin payout: {freeSpinSymbol.Data.payouts[payoutIndex]:F2} * CurrentMultiplier: {betManager.CurrentBetMultiplier:F2} = {freeSpinPayout:F2}");
            }
        }

        return freeSpinPayout;
    }

    // Calculate MaskReel payout (called during mask reel sequence)
    private float CalculateMaskReelPayout()
    {
        float maskReelPayout = 0f;

        if (storedMaskReelSymbols.Count >= 3)
        {
            int matchCount = storedMaskReelSymbols.Count;
            int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
            Symbol maskReelSymbol = storedMaskReelSymbols.Find(s => s.Data.type == SymbolData.SymbolType.FreeSpin);
            if (maskReelSymbol != null)
            {
                maskReelPayout = maskReelSymbol.Data.payouts[payoutIndex] * betManager.CurrentBetMultiplier;
                // Debug.Log($"[WinCalculator] MaskReel payout: {maskReelSymbol.Data.payouts[payoutIndex]:F2} * CurrentMultiplier: {betManager.CurrentBetMultiplier:F2} = {maskReelPayout:F2}");
            }
        }

        return maskReelPayout;
    }

    // NEW: Method to display FreeSpin total payout (called after freespin sequence completes)
    // SIMPLIFIED: Use existing FreeSpin position just like normal wins
    // CORRECTED: Use WinPosition enum directly (not a custom struct)
    public void DisplayFreeSpinTotalPayout(float totalPayout)
    {
        winAnimationController.ShowFreeSpinPayout(totalPayout, storedFreeSpinPosition);
    }



    // NEW: Method to display MaskReel total payout (called after mask reel sequence completes)
    public void DisplayMaskReelTotalPayout(float totalPayout)
    {
        // Debug.Log($"[WinCalculator] DisplayMaskReelTotalPayout called with: {totalPayout:F2}");

        if (winAnimationController != null && storedMaskReelSymbols.Count > 0)
        {
            // Create a temporary winning combination for display positioning
            winAnimationController.RegisterWinningCombination(storedMaskReelSymbols, totalPayout, 2);

            // Show the payout display at the stored position (reel 3 area)
            winAnimationController.ShowMaskReelPayout(totalPayout, storedMaskReelPosition);

            // Debug.Log($"[WinCalculator] MaskReel payout display shown: {totalPayout:F2} at position {storedMaskReelPosition}");
        }
        else
        {
            // Debug.LogWarning("[WinCalculator] Cannot display MaskReel payout - missing controller or symbols");
        }
    }

    // Method to stop mask reel animations (called when pink highlight starts)
    public void StopMaskReelAnimations()
    {
        if (winAnimationController != null)
        {
            winAnimationController.StopMaskReelAnimations();
        }
    }

    private void PlayWildSoundThenWinSound(float totalWin)
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            return;
        }

        // Stop any existing coroutine
        if (wildSoundCoroutine != null)
        {
            StopCoroutine(wildSoundCoroutine);
        }

        wildSoundCoroutine = StartCoroutine(PlayWildSoundSequence(totalWin));
    }

    private IEnumerator PlayWildSoundSequence(float totalWin)
    {
        // Play wild sound first
        SoundManager.Instance.PlaySound("wild");
        // Debug.Log("Playing Wild sound first");

        // Get the duration of the wild sound
        float wildSoundDuration = SoundManager.Instance.GetSoundDuration("wild");

        // Wait for the wild sound to finish
        yield return new WaitForSeconds(wildSoundDuration);

        // Now check if win animation is still ongoing
        if (betManager != null && betManager.IsAnimatingWin)
        {
            // If it's a big win, start big win celebration
            if (totalWin >= bigWinThreshold)
            {
                StartCelebration();
                SoundManager.Instance.PlayBigWinLoop();
                // Debug.Log("Start Big Win Celebration after Wild sound");
            }
            else
            {
                SoundManager.Instance.PlaySmallWinLoop();
                // Debug.Log("Start Small Win Sound after Wild sound");
            }
        }

        wildSoundCoroutine = null;
    }

    public void StartCelebration()
{
    Debug.Log("[WinCalculator] StartCelebration called!");
    
    if (confettiSpawner == null)
    {
        Debug.LogError("[WinCalculator] Confetti spawner is NULL!");
        return;
    }
    
    if (celebrationRoutine != null)
    {
        StopCoroutine(celebrationRoutine);
    }
    celebrationRoutine = StartCoroutine(CelebrationRoutine());
}


    public void StopCelebration()
    {
        if (celebrationRoutine != null)
        {
            if (confettiSpawner != null)
                confettiSpawner.StopConfetti();
            isCelebrating = false;
            StopCoroutine(celebrationRoutine);
        }
        if (confettiSpawner != null)
            confettiSpawner.StopConfetti();
        isCelebrating = false;
    }

    public bool IsCelebrationNull()
    {
        return celebrationRoutine == null;
    }

    private IEnumerator CelebrationRoutine()
    {
        Debug.Log("[WinCalculator] celebration routine called!");
        isCelebrating = true;

        if (confettiSpawner != null)
        
            confettiSpawner.LaunchConfetti();

        // Wait until the animation has started
        // float timeout = 1f;
        float elapsed = 0f;
        while (isCelebrating)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Now wait until the animation ends
        while (betManager != null && winAnimationController.IsAnimatingWin)
        {
            yield return null;
        }

        if (confettiSpawner != null)
            confettiSpawner.StopConfetti();
        isCelebrating = false;
    }

    private void OnDisable()
    {
        if (celebrationRoutine != null)
        {
            StopCoroutine(celebrationRoutine);
            isCelebrating = false;
        }

        if (wildSoundCoroutine != null)
        {
            StopCoroutine(wildSoundCoroutine);
            wildSoundCoroutine = null;
        }
    }

    #region EvaluateBranches for reference

    // Modified EvaluateBranches to handle win calculations with multipliers
    // private float EvaluateBranches(Symbol baseCandidate, List<Symbol>[] visibleSymbols, int reelIndex,
    //                      int matchCount, int wildCount, List<Symbol> branch,
    //                      ref bool hasFreeSpinWin, ref bool hasMaskReelWin)
    // {
    //     if (reelIndex >= visibleSymbols.Length)
    //     {
    //         if (matchCount >= 3)
    //         {
    //             int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
    //             float winPayout = baseCandidate.Data.payouts[payoutIndex];
    //             bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //             bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //
    //             Debug.Log($"[WinCalculator] Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2} = {winPayout:F2}");
    //
    //             // Check if win contains wild symbols
    //             if (wildCount > 0)
    //             {
    //                 hasWildInWinningCombination = true;
    //                 Debug.Log($"[WinCalculator] Win contains wild symbols (count: {wildCount})");
    //             }
    //
    //             if (isFreeSpinWin)
    //             {
    //                 hasFreeSpinWin = true;
    //                 float finalWin = winPayout * betManager.CurrentBet;
    //                 Debug.Log($"[WinCalculator] Free Spin win: Base {winPayout:F2} * CurrentBet: {betManager.CurrentBet:F2} = {finalWin:F2}");
    //                 HandleFreeSpinWin();
    //                 return finalWin; // ? Return calculated amount (1.00) not raw payout (2.00)
    //             }
    //             else if (isMaskReelWin)
    //             {
    //                 hasMaskReelWin = true;
    //                 float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //                 Debug.Log($"[WinCalculator] MaskReel win: Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //                 // DON'T register for immediate display
    //                 return winPayout;
    //             }
    //             else
    //             {
    //                 // Register this winning combination with the animation controller
    //                 float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //                 Debug.Log($"[WinCalculator] Regular win: Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //
    //                 if (winAnimationController != null)
    //                 {
    //                     RegisterWinningCombination(branch, finalWin);
    //                 }
    //                 else
    //                 {
    //                     LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
    //                 }
    //                 return winPayout;
    //             }
    //         }
    //         return 0f;
    //     }
    //
    //     // Continue with branch evaluation...
    //     List<Symbol> candidates = visibleSymbols[reelIndex].FindAll(s =>
    //         s.Data == baseCandidate.Data ||
    //         (s.Data.type == SymbolData.SymbolType.Wild && baseCandidate.Data.wildCanSubstitute)
    //     );
    //
    //     if (candidates.Count == 0)
    //     {
    //         if (matchCount >= 3)
    //         {
    //             int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
    //             bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //             bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //
    //             float winPayout = baseCandidate.Data.payouts[payoutIndex];
    //
    //             Debug.Log($"[WinCalculator] End of candidates - Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2}  = {winPayout:F2}");
    //
    //             if (wildCount > 0)
    //             {
    //                 hasWildInWinningCombination = true;
    //             }
    //
    //             if (isFreeSpinWin)
    //             {
    //                 hasFreeSpinWin = true;
    //                 float finalWin = winPayout * betManager.CurrentBet;
    //                 Debug.Log($"[WinCalculator] Free Spin win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBet:F2} = {finalWin:F2}");
    //                 // DON'T register for immediate display
    //                 HandleFreeSpinWin();
    //                 return finalWin;
    //             }
    //             else if (isMaskReelWin)
    //             {
    //                 hasMaskReelWin = true;
    //                 float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //                 Debug.Log($"[WinCalculator] MaskReel win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //                 // DON'T register for immediate display
    //                 return winPayout;
    //             }
    //             else
    //             {
    //                 float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //                 Debug.Log($"[WinCalculator] Regular win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //
    //                 if (winAnimationController != null)
    //                 {
    //                     RegisterWinningCombination(branch, finalWin);
    //                 }
    //                 else
    //                 {
    //                     LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
    //                 }
    //                 return winPayout;
    //             }
    //         }
    //         return 0f;
    //     }
    //
    //     float totalBranchPayout = 0f;
    //     foreach (Symbol candidate in candidates)
    //     {
    //         int additionalWild = (candidate.Data.type == SymbolData.SymbolType.Wild) ? 1 : 0;
    //         List<Symbol> newBranch = new List<Symbol>(branch) { candidate };
    //         totalBranchPayout += EvaluateBranches(baseCandidate, visibleSymbols, reelIndex + 1,
    //                                            matchCount + 1, wildCount + additionalWild,
    //                                            newBranch, ref hasFreeSpinWin, ref hasMaskReelWin);
    //     }
    //
    //     if (totalBranchPayout == 0 && matchCount >= 3)
    //     {
    //         int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
    //         bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //         bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
    //
    //         float winPayout = baseCandidate.Data.payouts[payoutIndex];
    //
    //         Debug.Log($"[WinCalculator] Branch end (totalBranchPayout=0) - Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2} = {winPayout:F2}");
    //
    //         if (wildCount > 0)
    //         {
    //             hasWildInWinningCombination = true;
    //         }
    //
    //         if (isFreeSpinWin)
    //         {
    //             hasFreeSpinWin = true;
    //             float finalWin = winPayout * betManager.CurrentBet;
    //             Debug.Log($"[WinCalculator] Free Spin win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //             // DON'T register for immediate display
    //             HandleFreeSpinWin();
    //             return finalWin;
    //         }
    //         else if (isMaskReelWin)
    //         {
    //             hasMaskReelWin = true;
    //             float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //             Debug.Log($"[WinCalculator] MaskReel win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //             // DON'T register for immediate display
    //             return winPayout;
    //         }
    //         else
    //         {
    //             float finalWin = winPayout * betManager.CurrentBetMultiplier;
    //             Debug.Log($"[WinCalculator] Regular win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
    //
    //             if (winAnimationController != null)
    //             {
    //                 RegisterWinningCombination(branch, finalWin);
    //             }
    //             else
    //             {
    //                 LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
    //             }
    //             return winPayout;
    //         }
    //     }
    //
    //     return totalBranchPayout;
    // }

    #endregion

    // Modified EvaluateBranches to handle win calculations with multipliers
    private float EvaluateBranches(Symbol baseCandidate, List<Symbol>[] visibleSymbols, int reelIndex,
                         int matchCount, int wildCount, List<Symbol> branch,
                         ref bool hasFreeSpinWin, ref bool hasMaskReelWin)
    {
        if (reelIndex >= visibleSymbols.Length)
        {
            if (matchCount >= 3)
            {
                int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
                float winPayout = baseCandidate.Data.payouts[payoutIndex];
                bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
                bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);

                // Debug.Log($"[WinCalculator] Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2} = {winPayout:F2}");

                // Check if win contains wild symbols
                if (wildCount > 0)
                {
                    hasWildInWinningCombination = true;
                    Debug.Log($"[WinCalculator] Win contains wild symbols (count: {wildCount})");
                }

                // Also ensure wild tracking continues properly in the recursive calls:
                List<Symbol> innerCandidates = visibleSymbols[reelIndex].FindAll(s =>
                    s.Data == baseCandidate.Data ||
                    (s.Data.type == SymbolData.SymbolType.Wild && baseCandidate.Data.wildCanSubstitute)
                );

                float innerTotalBranchPayout = 0f;
                foreach (Symbol candidate in innerCandidates)
                {
                    List<Symbol> newBranch = new List<Symbol>(branch) { candidate };
                    int newWildCount = wildCount + (candidate.Data.type == SymbolData.SymbolType.Wild ? 1 : 0);

                    float candidatePayout = EvaluateBranches(baseCandidate, visibleSymbols, reelIndex + 1,
                                                           matchCount + 1, newWildCount, newBranch,
                                                           ref hasFreeSpinWin, ref hasMaskReelWin);
                    innerTotalBranchPayout += candidatePayout;
                }

                if (isFreeSpinWin)
                {
                    hasFreeSpinWin = true;
                    float finalWin = winPayout * betManager.CurrentBet;
                    // Debug.Log($"[WinCalculator] Free Spin win: Base {winPayout:F2} * CurrentBet: {betManager.CurrentBet:F2} = {finalWin:F2}");
                    HandleFreeSpinWin();
                    return finalWin; // ? Return calculated amount (1.00) not raw payout (2.00)
                }
                else if (isMaskReelWin)
                {
                    hasMaskReelWin = true;
                    float finalWin = winPayout * betManager.CurrentBetMultiplier;
                    // Debug.Log($"[WinCalculator] MaskReel win: Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
                    // DON'T register for immediate display
                    return winPayout;
                }
                else
                {
                    // Register this winning combination with the animation controller
                    float finalWin = winPayout * betManager.CurrentBetMultiplier;
                    // Debug.Log($"[WinCalculator] Regular win: Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");

                    if (winAnimationController != null)
                    {
                        RegisterWinningCombination(branch, finalWin);
                    }
                    else
                    {
                        LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
                    }
                    return winPayout;
                }
            }
            return 0f;
        }

        // Continue with branch evaluation...
        List<Symbol> candidates = visibleSymbols[reelIndex].FindAll(s =>
            s.Data == baseCandidate.Data ||
            (s.Data.type == SymbolData.SymbolType.Wild && baseCandidate.Data.wildCanSubstitute)
        );

        if (candidates.Count == 0)
        {
            if (matchCount >= 3)
            {
                int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
                bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
                bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);

                float winPayout = baseCandidate.Data.payouts[payoutIndex];

                // Debug.Log($"[WinCalculator] End of candidates - Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2}  = {winPayout:F2}");

                if (wildCount > 0)
                {
                    hasWildInWinningCombination = true;
                }

                if (isFreeSpinWin)
                {
                    hasFreeSpinWin = true;
                    float finalWin = winPayout * betManager.CurrentBet;
                    // Debug.Log($"[WinCalculator] Free Spin win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBet:F2} = {finalWin:F2}");
                    // DON'T register for immediate display
                    HandleFreeSpinWin();
                    return finalWin;
                }
                else if (isMaskReelWin)
                {
                    hasMaskReelWin = true;
                    float finalWin = winPayout * betManager.CurrentBetMultiplier;
                    // Debug.Log($"[WinCalculator] MaskReel win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
                    // DON'T register for immediate display
                    return winPayout;
                }
                else
                {
                    float finalWin = winPayout * betManager.CurrentBetMultiplier;
                    // Debug.Log($"[WinCalculator] Regular win (no candidates): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");

                    if (winAnimationController != null)
                    {
                        RegisterWinningCombination(branch, finalWin);
                    }
                    else
                    {
                        LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
                    }
                    return winPayout;
                }
            }
            return 0f;
        }

        float totalBranchPayout = 0f;
        foreach (Symbol candidate in candidates)
        {
            int additionalWild = (candidate.Data.type == SymbolData.SymbolType.Wild) ? 1 : 0;
            List<Symbol> newBranch = new List<Symbol>(branch) { candidate };
            totalBranchPayout += EvaluateBranches(baseCandidate, visibleSymbols, reelIndex + 1,
                                               matchCount + 1, wildCount + additionalWild,
                                               newBranch, ref hasFreeSpinWin, ref hasMaskReelWin);
        }

        if (totalBranchPayout == 0 && matchCount >= 3)
        {
            int payoutIndex = Mathf.Clamp(matchCount - 3, 0, 2);
            bool isFreeSpinWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);
            bool isMaskReelWin = (baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin);

            float winPayout = baseCandidate.Data.payouts[payoutIndex];

            // Debug.Log($"[WinCalculator] Branch end (totalBranchPayout=0) - Symbol {baseCandidate.Data.type} match {matchCount}: Base payout {baseCandidate.Data.payouts[payoutIndex]:F2} = {winPayout:F2}");

            if (wildCount > 0)
            {
                hasWildInWinningCombination = true;
            }

            if (isFreeSpinWin)
            {
                hasFreeSpinWin = true;
                float finalWin = winPayout * betManager.CurrentBet;
                // Debug.Log($"[WinCalculator] Free Spin win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
                // DON'T register for immediate display
                HandleFreeSpinWin();
                return finalWin;
            }
            else if (isMaskReelWin)
            {
                hasMaskReelWin = true;
                float finalWin = winPayout * betManager.CurrentBetMultiplier;
                // Debug.Log($"[WinCalculator] MaskReel win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");
                // DON'T register for immediate display
                return winPayout;
            }
            else
            {
                float finalWin = winPayout * betManager.CurrentBetMultiplier;
                // Debug.Log($"[WinCalculator] Regular win (branch end): Base {winPayout:F2} * CurrentMultiplier {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}");

                if (winAnimationController != null)
                {
                    RegisterWinningCombination(branch, finalWin);
                }
                else
                {
                    LogAndAnimateBranch(branch, winPayout, matchCount, wildCount);
                }
                return winPayout;
            }
        }

        return totalBranchPayout;
    }

    // Helper method to handle free spin wins
    private void HandleFreeSpinWin()
    {
        if (FreeSpinManager.Instance != null)
        {
            // Always play FreeSpin sound
            SoundManager.Instance.PlaySound("freespin");

            if (!FreeSpinManager.Instance.IsFreeSpinActive)
            {
                // First time triggering free spins
                if (uiElements != null)
                {
                    uiElements.spinButton.landscape.interactable = false;
                    uiElements.spinButton.portrait.interactable = false;
                }
                SoundManager.Instance.PlaySound("freespinmusic");
            }
        }
    }

    // Method to register winning combinations with the animation controller
    private void RegisterWinningCombination(List<Symbol> symbols, float payout, int reelIndex = -1)
    {
        if (winAnimationController != null)
        {
            winAnimationController.RegisterWinningCombination(symbols, payout, reelIndex);

            // Check if any symbols in the combination are wild
            foreach (Symbol symbol in symbols)
            {
                if (symbol.Data.type == SymbolData.SymbolType.Wild)
                {
                    hasWildInWinningCombination = true;
                    break;
                }
            }
        }
    }

    // Helper method to log the winning branch and trigger animations
    private void LogAndAnimateBranch(List<Symbol> branch, float payout, int matchCount, int wildCount)
    {
        string log = "Win Combination: ";
        bool hasWild = false;

        foreach (Symbol s in branch)
        {
            log += s.Data.type + " ";
            s.ActivateWinHighlight();
            if (s.Data.type == SymbolData.SymbolType.Wild)
                hasWild = true;
        }

        float finalWin = payout * betManager.CurrentBetMultiplier;
        log += $"| Matched Reels: {matchCount}, Wilds: {wildCount}, Payout: {payout:F2} * CurrentMultiplier: {betManager.CurrentBetMultiplier:F2} = {finalWin:F2}";
        // Debug.Log($"[WinCalculator] {log}");

        foreach (Symbol s in branch)
        {
            s.PlayWinAnimation();
            Debug.LogError("calling");
            s.ActivateWinHighlight();
        }
    }

    // Returns visible symbols for all reels
    private List<Symbol>[] GetVisibleSymbols(SlotReel[] reels)
    {
        List<Symbol>[] result = new List<Symbol>[reels.Length];
        for (int i = 0; i < reels.Length; i++)
        {
            result[i] = GetVisibleSymbolsInReel(reels[i]);
        }
        return result;
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

    public float CalculateCurrentWins()
    {
        SlotReel[] activeReels = GetActiveReels();
        if (activeReels == null)
        {
            // Debug.LogWarning("[WinCalculator] No active reels found for win calculation");
            return 0f;
        }

        // Debug.Log("[WinCalculator] Calculating current wins for active reels");

        // Clear previous winning combinations for this calculation
        if (winAnimationController != null)
        {
            winAnimationController.ClearWinningCombinations();
        }

        // Use the existing CalculateWin method but don't trigger special features
        return CalculateWinInternal(activeReels, false);
    }
    /// <summary>
    /// Check for pure wild combinations (2-5 wilds in consecutive reels starting from reel 0)
    /// </summary>
    private float CheckWildOnlyCombinations(List<Symbol>[] visibleSymbols)
    {
        float totalWildPayout = 0f;
        List<Symbol> firstReelWilds = visibleSymbols[0].Where(s => s.Data.type == SymbolData.SymbolType.Wild).ToList();

        foreach (Symbol wildSymbol in firstReelWilds)
        {
            List<Symbol> wildBranch = new List<Symbol> { wildSymbol };
            int consecutiveWilds = 1;

            // Check consecutive reels for more wilds
            for (int reelIndex = 1; reelIndex < visibleSymbols.Length; reelIndex++)
            {
                List<Symbol> wildCandidates = visibleSymbols[reelIndex]
                    .Where(s => s.Data.type == SymbolData.SymbolType.Wild).ToList();

                if (wildCandidates.Count > 0)
                {
                    wildBranch.Add(wildCandidates[0]);
                    consecutiveWilds++;
                }
                else
                {
                    break; // No more consecutive wilds
                }
            }

            // Check if we have a valid wild combination (2-5 wilds)
            if (consecutiveWilds >= 2)
            {
                hasWildInWinningCombination = true;

                // Calculate wild payout - use a base wild payout table
                float wildPayout = GetWildOnlyPayout(consecutiveWilds);

                // Register the winning wild combination
                if (winAnimationController != null)
                {
                    RegisterWinningCombination(wildBranch, wildPayout);
                }

                totalWildPayout += wildPayout;
                Debug.Log($"[WinCalculator] Wild-only combination: {consecutiveWilds} wilds, payout: {wildPayout:F2}");
            }
        }

        return totalWildPayout;
    }

    /// <summary>
    /// Get payout for wild-only combinations
    /// </summary>
    private float GetWildOnlyPayout(int wildCount)
    {
        // Define payout values for 2-5 wild combinations
        // You can adjust these values based on your game design
        switch (wildCount)
        {
            case 2: return 10f;   // 2 wilds
            case 3: return 500f;  // 3 wilds
            case 4: return 3000f;  // 4 wilds
            case 5: return 10000f; // 5 wilds (jackpot!)
            default: return 0f;
        }
    }


    /// <summary>
    /// Internal win calculation method that can skip special feature triggers
    /// </summary>
    private float CalculateWinInternal(SlotReel[] reels, bool triggerSpecialFeatures = true)
    {
        Debug.Log($"[WinCalculator] Starting win calculation, hasWildInWinningCombination: {hasWildInWinningCombination}");
        float totalPayout = 0f;
        List<Symbol>[] visibleSymbols = GetVisibleSymbols(reels);
        bool hasFreeSpinWin = false;
        bool hasMaskReelWin = false;

        // Reset the wild flag
        hasWildInWinningCombination = false;

        // Only check for special combinations if we're triggering special features
        if (triggerSpecialFeatures)
        {
            // Check for FreeSpin combinations
            CheckFreeSpinCombinations(visibleSymbols, ref hasFreeSpinWin);
        }

        // NEW: Check for wild-only combinations first
        float wildOnlyPayout = CheckWildOnlyCombinations(visibleSymbols);
        totalPayout += wildOnlyPayout;

        // Calculate base payout for regular symbols (consecutive starting from first reel)
        List<Symbol> baseSymbols = visibleSymbols[0];
        foreach (Symbol baseCandidate in baseSymbols)
        {
            // Skip special symbols - they're handled separately
            if (baseCandidate.Data.type == SymbolData.SymbolType.Wild ||
                baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin)
                continue;

            List<Symbol> branch = new List<Symbol> { baseCandidate };
            float branchPayout = EvaluateBranches(baseCandidate, visibleSymbols, 1, 1, 0, branch,
                                                 ref hasFreeSpinWin, ref hasMaskReelWin);
            totalPayout += branchPayout;
        }

        // Handle wild symbols as substitutes for regular symbols
        foreach (Symbol wildCandidate in baseSymbols)
        {
            if (wildCandidate.Data.type != SymbolData.SymbolType.Wild)
                continue;

            foreach (Symbol matchingSymbol in baseSymbols)
            {
                // Skip special symbols and symbols that can't be substituted
                if (matchingSymbol.Data.type == SymbolData.SymbolType.Wild ||
                    matchingSymbol.Data.type == SymbolData.SymbolType.FreeSpin ||
                    !matchingSymbol.Data.wildCanSubstitute)
                    continue;

                List<Symbol> branch = new List<Symbol> { wildCandidate };
                float branchPayout = EvaluateBranches(matchingSymbol, visibleSymbols, 1, 1, 1, branch,
                                                     ref hasFreeSpinWin, ref hasMaskReelWin);
                totalPayout += branchPayout;
            }
        }

        // Calculate final win
        float totalWin = totalPayout * betManager.CurrentBetMultiplier;

        // Only trigger animations and sounds if we're in trigger mode and have wins
        if (triggerSpecialFeatures)
        {
            // Start animations if there are any winning combinations
            if (winAnimationController != null && (totalWin > 0 || hasFreeSpinWin || hasMaskReelWin))
            {
                winAnimationController.PlayWinAnimations();
            }

            // Handle wild sounds and animations
            if (hasWildInWinningCombination && totalWin > 0 && !FreeSpinManager.Instance.freeSpinFeatureActive)
            {
                PlayWildSoundThenWinSound(totalWin);
            }

            // Handle other win sounds...
        }

        return totalWin;
    }

    /// <summary>
    /// Get the active reels based on current game state
    /// </summary>
    private SlotReel[] GetActiveReels()
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            if (FreeSpinSlotMachineController.Instance != null)
            {
                return FreeSpinSlotMachineController.Instance.GetReels();
            }
        }
        else if (SlotMachineController.Instance != null)
        {
            return SlotMachineController.Instance.GetReels();
        }

        // Debug.LogWarning("[WinCalculator] No active slot machine controller found");
        return null;
    }

    // Add this to WinCalculator class
    public float CalculateWinExcludingSymbols(SlotReel[] reels, List<Symbol> excludedSymbols)
    {
        float totalPayout = 0f;
        List<Symbol>[] visibleSymbols = GetVisibleSymbols(reels);
        bool hasFreeSpinWin = false;
        bool hasMaskReelWin = false;

        // Debug.Log($"[WinCalculator] Starting CalculateWinExcludingSymbols - Excluding {excludedSymbols.Count} symbols");

        // Reset the wild flag
        hasWildInWinningCombination = false;

        // Clear any previous winning combinations
        if (winAnimationController != null)
        {
            winAnimationController.ClearWinningCombinations();
        }

        // Filter out excluded symbols AND special symbols from visible symbols
        List<Symbol>[] filteredVisibleSymbols = new List<Symbol>[visibleSymbols.Length];
        for (int i = 0; i < visibleSymbols.Length; i++)
        {
            filteredVisibleSymbols[i] = new List<Symbol>();
            foreach (Symbol symbol in visibleSymbols[i])
            {
                // FIXED: Exclude original non-mask symbols AND special symbols (FreeSpin, MaskReel)
                if (!excludedSymbols.Contains(symbol) &&
                    symbol.Data.type != SymbolData.SymbolType.FreeSpin &&
                    symbol.Data.type != SymbolData.SymbolType.FreeSpin)
                {
                    filteredVisibleSymbols[i].Add(symbol);
                }
                else
                {
                    if (excludedSymbols.Contains(symbol))
                    {
                        // Debug.Log($"[WinCalculator] Excluding original symbol {symbol.Data.type} from calculation");
                    }
                    else if (symbol.Data.type == SymbolData.SymbolType.FreeSpin || symbol.Data.type == SymbolData.SymbolType.FreeSpin)
                    {
                        // Debug.Log($"[WinCalculator] Excluding special symbol {symbol.Data.type} from transformation calculation");
                    }
                }
            }
        }

        // Use filtered symbols for the rest of the calculation
        // Calculate base payout for regular symbols (consecutive starting from first reel)
        List<Symbol> baseSymbols = filteredVisibleSymbols[0];
        foreach (Symbol baseCandidate in baseSymbols)
        {
            // Skip special symbols - they're handled separately (but should already be filtered out)
            if (baseCandidate.Data.type == SymbolData.SymbolType.Wild ||
                baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin ||
                baseCandidate.Data.type == SymbolData.SymbolType.FreeSpin)
                continue;

            List<Symbol> branch = new List<Symbol> { baseCandidate };
            float branchPayout = EvaluateBranches(baseCandidate, filteredVisibleSymbols, 1, 1, 0, branch,
                                                 ref hasFreeSpinWin, ref hasMaskReelWin);
            totalPayout += branchPayout;
            // Debug.Log($"[WinCalculator] Regular symbol {baseCandidate.Data.type} branch payout: {branchPayout:F2}");
        }

        // Handle wild symbols as base symbols (wilds are always allowed)
        foreach (Symbol wildCandidate in baseSymbols)
        {
            if (wildCandidate.Data.type != SymbolData.SymbolType.Wild)
                continue;

            foreach (Symbol matchingSymbol in baseSymbols)
            {
                // Skip special symbols - wilds can't substitute for these
                if (matchingSymbol.Data.type == SymbolData.SymbolType.Wild ||
                    matchingSymbol.Data.type == SymbolData.SymbolType.FreeSpin ||
                    matchingSymbol.Data.type == SymbolData.SymbolType.FreeSpin ||
                    !matchingSymbol.Data.wildCanSubstitute)
                    continue;

                List<Symbol> branch = new List<Symbol> { wildCandidate };
                float branchPayout = EvaluateBranches(matchingSymbol, filteredVisibleSymbols, 1, 1, 1, branch,
                                                     ref hasFreeSpinWin, ref hasMaskReelWin);
                totalPayout += branchPayout;
                // Debug.Log($"[WinCalculator] Wild substituting for {matchingSymbol.Data.type} branch payout: {branchPayout:F2}");
            }
        }

        // Calculate final win
        float totalWin = totalPayout * betManager.CurrentBetMultiplier;
        // Debug.Log($"[WinCalculator] Filtered calculation - Raw totalPayout: {totalPayout:F2} * CurrentMultiplier: {betManager.CurrentBetMultiplier:F2} = Final Win: {totalWin:F2}");

        // Start animations if there are any winning combinations
        if (winAnimationController != null && totalWin > 0)
        {
            winAnimationController.PlayWinAnimations();
        }

        return totalPayout;
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
}