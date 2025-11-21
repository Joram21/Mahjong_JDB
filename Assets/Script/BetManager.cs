using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;

[System.Serializable]
public class BetButtonPair
{
    public GameObject landscapeButton;
    public GameObject portraitButton;
}

public class BetManager : MonoBehaviour
{
    [SerializeField] private float[] betAmounts = { 0.25f, 0.5f, 1.25f, 2.5f, 6.25f };
    [SerializeField] private float[] betMultipliers = { 1f, 2f, 3f, 5f, 10f }; // NEW: Bet multipliers array
    [SerializeField] private UIElementManager uiElements;
    public event Action OnBalanceChanged;
    public event Action<float> OnWinAdded;
    [SerializeField] private FinalizeClickBlocker clickBlocker;
    public event Action<float> OnWinAnimationComplete;

    public static int CurrentBetIndex => currentBetIndex;
    private static int currentBetIndex = 0;
    private double currentBalance = 2000f;
    private float lastWinAmount = 0f;

    // ADDED: Public property to access lastWinAmount
    public float LastWinAmount => lastWinAmount;

    public float CurrentBet => betAmounts[currentBetIndex];
    public float CurrentBetMultiplier => betMultipliers[currentBetIndex]; // NEW: Property for current bet multiplier
    public int MaxBetIndex => betAmounts.Length - 1;
    public Coroutine winAnimationCoroutine;

    public bool IsAnimatingWin => winAnimationCoroutine != null;

    public bool IsDemoMode = true;  // Set this flag externally (e.g., from WebManAPI)
    public float targetWinAmount = 0f;
    public event Action OnBetChanged;
    [Header("Animation Settings")]
    [SerializeField] private bool isFastMode = false;
    [SerializeField] private float normalWinAnimationMultiplier = 1.0f;
    [SerializeField] private float fastWinAnimationMultiplier = 0.3f; // Much faster!
    [SerializeField] private List<BetButtonPair> betButtonPairs;
    private bool pendingBalanceUpdate = false;
    private double pendingBalance = 0;
    private bool shouldDelayBalanceUpdate = false;

    // UPDATED: Start method with language subscription
    private void Awake()
    {
        // Don't initialize balance here - wait for Start to coordinate with API
    }
    private IEnumerator InitializeBalance()
    {
        // Wait for ConfigMan to be ready
        while (ConfigMan.Instance == null || !ConfigMan.Instance.ReceivedConfigs)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (ConfigMan.Instance.IsDemo)
        {
            currentBalance = 2000f;
            IsDemoMode = true;
            Debug.Log("[BetManager] Demo mode - using default balance: 2000");
        }
        else
        {
            // Wait for balance to be fetched from API
            Debug.Log("[BetManager] Waiting for balance to be fetched from API...");

            while (!FetchPlayerBalance.BalanceFetched)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Read balance from PlayerPrefs (set by FetchPlayerBalance)
            float fetchedBalance = PlayerPrefs.GetFloat("Balance", 2000f);
            currentBalance = fetchedBalance;
            IsDemoMode = false;

            Debug.Log($"[BetManager] Live mode - Balance loaded from API: {currentBalance}");
        }

        UpdateUI();
    }

    private void Start()
    {
        // Start balance initialization coroutine
        StartCoroutine(InitializeBalance());

        if (clickBlocker != null)
        {
            clickBlocker.ActivateForWin(true);
        }
        UpdateBetButtonIndicators();

        // Validate that betAmounts and betMultipliers arrays have the same length
        if (betAmounts.Length != betMultipliers.Length)
        {
            Debug.LogError("BetManager: betAmounts and betMultipliers arrays must have the same length!");
        }

        // Subscribe to language refresh events
        if (LanguageMan.instance != null)
        {
            LanguageMan.instance.onLanguageRefresh.AddListener(RefreshWinText);
        }
    }

    // ADDED: OnDestroy method for cleanup
    private void OnDestroy()
    {
        // Unsubscribe from language refresh events  
        if (LanguageMan.instance != null)
        {
            LanguageMan.instance.onLanguageRefresh.RemoveListener(RefreshWinText);
        }
    }

    // ADDED: Refresh win text when language changes
    private void RefreshWinText()
    {
        // Only refresh win text if currently showing a win
        if (lastWinAmount > 0)
        {
            bool inFreeSpins = FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;
            UpdateWinText(inFreeSpins);
        }
        else
        {
            // If no win is showing, let SlotMachineController handle default text
            // Debug.Log("[BetManager] No win to refresh, letting SlotMachineController handle default text");
        }
    }

    private string FormatBetAmount(float amount)
    {
        // If the amount is a whole number, show without decimals
        if (amount % 1 == 0)
        {
            return amount.ToString("0");
        }
        else
        {
            // Remove trailing zeros from decimals
            return amount.ToString("0.##");
        }
    }


    public void AddWin(float amount)
    {
        if (winAnimationCoroutine != null) StopCoroutine(winAnimationCoroutine);

        if (amount > 0)
        {
            targetWinAmount = amount;

            bool isBigWin = amount >= 100f;
            bool isFromFreeSpins = FreeSpinManager.Instance != null &&
                                  !FreeSpinManager.Instance.freeSpinFeatureActive &&
                                  FreeSpinManager.Instance.FreeSpinHasBegun == false;

            winAnimationCoroutine = StartCoroutine(AnimateWinAmount());
            PlayWinAnimation();
        }
        else
        {
            lastWinAmount = 0;
            UpdateUI();
        }
    }
    public bool IsFastMode
    {
        get => isFastMode;
        set => isFastMode = value;
    }

    // UPDATED: FinalizeWinAnimation method
    // UPDATED: FinalizeWinAnimation method in BetManager.cs
    public void FinalizeWinAnimation(bool inFreeSpins = false)
    {
        if (winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }

        // Apply any pending balance updates when animation is finalized
        shouldDelayBalanceUpdate = false;
        if (pendingBalanceUpdate)
        {
            currentBalance = pendingBalance;
            pendingBalanceUpdate = false;
            UpdateUI();
            // Debug.Log($"[BetManager] Applied delayed balance update on finalize: {pendingBalance}");
        }

        if (inFreeSpins)
        {
            lastWinAmount = targetWinAmount;
            UpdateWinText(true);
        }
        else
        {
            // if (IsDemoMode)
            // {
            //     double remainingAmount = targetWinAmount - lastWinAmount;
            //     if (remainingAmount > 0)
            //     {
            //         currentBalance += remainingAmount;
            //         lastWinAmount = targetWinAmount;
            //         UpdateWinText(false);
            //         UpdateUI();
            //     }
            // }
            // else
            {
                lastWinAmount = targetWinAmount;
                UpdateWinText(false);
                OnWinAnimationComplete?.Invoke(targetWinAmount);
            }
        }

        SoundManager.Instance.StopBigWinLoop();
        SoundManager.Instance.StopSmallWinLoop();
        if (inFreeSpins)
        {
            SoundManager.Instance.StopSound("freespinswins");
        }
        SoundManager.Instance.PlaySound("finalscore");
    }
    // UPDATED: AnimateWinAmount method
    // UPDATED: AnimateWinAmount method in BetManager.cs
    private IEnumerator AnimateWinAmount()
    {
        float startAmount = 0f;
        float _targetWinAmount = targetWinAmount;
        shouldDelayBalanceUpdate = true; // Flag to delay balance updates

        // Calculate duration...
        float baseDuration = targetWinAmount;
        float durationMultiplier = isFastMode ? fastWinAnimationMultiplier : normalWinAnimationMultiplier;
        float duration = baseDuration * durationMultiplier;
        float minDuration = isFastMode ? 0.2f : 0.5f;
        var pause = WinCalculator.Instance.winAnimationController.pauseBetweenSymbolTypes;
        var paylineCount = WebManAPI.Instance.payLines.Count;
        var incrementDuration = FreeSpinManager.Instance.freeSpinFeatureActive ?
            (paylineCount > 10 ? 6 * pause : 4 * pause) : (paylineCount * pause);
        float maxDuration = isFastMode ? 1.5f : incrementDuration;
        duration = Mathf.Clamp(duration, minDuration, maxDuration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            lastWinAmount = Mathf.Lerp(startAmount, _targetWinAmount, elapsed / duration);
            UpdateWinText();

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final amount is exact
        lastWinAmount = _targetWinAmount;
        UpdateWinText();

        // Apply any pending balance updates
        shouldDelayBalanceUpdate = false;
        if (pendingBalanceUpdate)
        {
            currentBalance = pendingBalance;
            pendingBalanceUpdate = false;
            UpdateUI();
        }

        if (IsDemoMode)
        {
            currentBalance += targetWinAmount;
            UpdateUI();
        }
        else
        {
            // FIXED: In live mode, invoke the event AND apply the balance update
            OnWinAnimationComplete?.Invoke(targetWinAmount);
            
            // Wait a frame to ensure API response is received
            yield return new WaitForEndOfFrame();
            
            // If balance still hasn't updated, force UI refresh
            if (!pendingBalanceUpdate)
            {
                UpdateUI();
            }
        }

        SoundManager.Instance.PlaySound("finalscore");

        winAnimationCoroutine = null;
    }

    // UPDATED: StartWinAnimation method
    public void StartWinAnimation(float amount)
    {
        if (winAnimationCoroutine != null) StopCoroutine(winAnimationCoroutine);

        if (amount > 0)
        {
            targetWinAmount = amount;
            winAnimationCoroutine = StartCoroutine(AnimateWinAmount());
            PlayWinAnimation();
            OnWinAdded?.Invoke(amount);
        }
        else
        {
            lastWinAmount = 0;
            UpdateUI();
            UpdateWinText(); // Use translation
        }
    }

    // UPDATED: ShowWinAfterSpin method
    public void ShowWinAfterSpin()
    {
        if (targetWinAmount > 0)
        {
            StartWinAnimation(targetWinAmount);
        }
        else
        {
            lastWinAmount = 0;
            UpdateUI();
            UpdateWinText(); // Use translation
        }
    }

    public void InvokeWin()
    {
        WebManAPI.Instance.BetUpdate(WebManAPI.Instance.tempWin);
        WebManAPI.Instance.tempWin = 0;
    }

    // UPDATED: UpdateWinText method with translation support
    public void UpdateWinText(bool inFreeSpins = false)
    {
        string winString;

        if (inFreeSpins)
        {
            // Always show win during free spins, even if it's zero
            winString = LanguageHelper.GetWinText(lastWinAmount);
        }
        else
        {
            // Normal behavior outside free spins
            winString = lastWinAmount > 0 ? LanguageHelper.GetWinText(lastWinAmount) : LanguageHelper.GetAmountText(lastWinAmount);
        }

        uiElements.winText.landscape.text = winString;
        uiElements.winText.portrait.text = winString;
    }

    public void PlayWinAnimation()
    {
        StartCoroutine(AnimateWinTextScale());
    }

    private IEnumerator AnimateWinTextScale()
    {
        float duration = 0.2f;
        float targetScale = 1.2f;
        Vector3 originalScaleLandscape = uiElements.winText.landscape.transform.localScale;
        Vector3 originalScalePortrait = uiElements.winText.portrait.transform.localScale;

        // Scale up
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, targetScale, t);
            uiElements.winText.landscape.transform.localScale = new Vector3(scale, scale, scale);
            uiElements.winText.portrait.transform.localScale = new Vector3(scale, scale, scale);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(targetScale, 1f, t);
            uiElements.winText.landscape.transform.localScale = new Vector3(scale, scale, scale);
            uiElements.winText.portrait.transform.localScale = new Vector3(scale, scale, scale);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset scales
        uiElements.winText.landscape.transform.localScale = originalScaleLandscape;
        uiElements.winText.portrait.transform.localScale = originalScalePortrait;
    }

    public void IncreaseBet()
    {
        if (currentBetIndex < MaxBetIndex)
        {
            SoundManager.Instance.PlaySound("button");
            currentBetIndex++;
            UpdateUI(); // Call full UI update instead of just UpdateBetTexts()
            OnBetChanged?.Invoke();
        }
    }

    public void DecreaseBet()
    {
        if (currentBetIndex > 0)
        {
            SoundManager.Instance.PlaySound("button");
            currentBetIndex--;
            UpdateUI(); // Call full UI update instead of just UpdateBetTexts()
            OnBetChanged?.Invoke();
        }
    }

    public void SetBetIndex(int index)
    {
        currentBetIndex = Mathf.Clamp(index, 0, betAmounts.Length - 1);
        UpdateUI();
        OnBetChanged?.Invoke();
    }

    private void UpdateBetTexts()
    {
        string betText = FormatBetAmount(CurrentBet);

        // Update main bet texts
        uiElements.betText.landscape.text = betText;
        uiElements.betText.portrait.text = betText;

        // Update all other bet amount texts
        uiElements.autospinbetamount.landscape.text = betText;
        uiElements.autospinbetamount.portrait.text = betText;
        uiElements.betbetamount.landscape.text = betText;
        uiElements.betbetamount.portrait.text = betText;
        uiElements.instructionsbetamount.landscape.text = betText;
        uiElements.instructionsbetamount.portrait.text = betText;
    }

    private void UpdateBalanceTexts()
    {
        string balanceText = $"{currentBalance:N2}";

        // Update main balance texts
        uiElements.balanceText.landscape.text = balanceText;
        uiElements.balanceText.portrait.text = balanceText;

        // Update all other balance amount texts
        uiElements.autospinbalanceamount.landscape.text = balanceText;
        uiElements.autospinbalanceamount.portrait.text = balanceText;
        uiElements.betbalanceamount.landscape.text = balanceText;
        uiElements.betbalanceamount.portrait.text = balanceText;
        uiElements.instructionsbalanceamount.landscape.text = balanceText;
        uiElements.instructionsbalanceamount.portrait.text = balanceText;
    }

    private void UpdateBetButtonIndicators()
    {
        for (int i = 0; i < betButtonPairs.Count; i++)
        {
            bool isSelected = (i == currentBetIndex);

            // Enable/disable indicator on both layouts
            SetIndicatorActive(betButtonPairs[i].landscapeButton, isSelected);
            SetIndicatorActive(betButtonPairs[i].portraitButton, isSelected);
        }
    }

    private void SetIndicatorActive(GameObject button, bool active)
    {
        // Assuming the indicator is the first child. Adjust index if needed.
        if (button.transform.childCount > 0)
        {
            button.transform.GetChild(0).gameObject.SetActive(active);
        }
    }

    public bool CanPlaceBet()
    {
        return currentBalance >= CurrentBet; // Still uses bet amount for deduction check
    }

    // UPDATED: DeductBet method in BetManager.cs  
    public void DeductBet()
    {
        if (IsDemoMode)
        {
            currentBalance -= CurrentBet;
            // Debug.Log($"[DEMO] Deducted bet of {CurrentBet}, new balance: {currentBalance}");
            UpdateUI();
        }
        else
        {
            // Debug.Log($"[NON-DEMO] Bet deduction handled by API - current balance: {currentBalance}");
            // In non-demo mode, balance deduction is handled by the API
            // We just update the UI to show current state
            UpdateUI();
        }
    }


    // New method to set balance from API
    public void SetBalance(double newBalance)
    {
        if (!IsDemoMode)
        {
            // If we're currently animating a win, store the balance for later
            if (IsAnimatingWin && shouldDelayBalanceUpdate)
            {
                pendingBalanceUpdate = true;
                pendingBalance = newBalance;
                // Debug.Log($"[BetManager] Balance update delayed until animation completes: {newBalance}");
                return;
            }

            currentBalance = newBalance;
        }
        UpdateUI();
    }


    public void ResetDemoBalance()
    {
        if (IsDemoMode)
        {
            currentBalance = 2000f;
        }
        UpdateUI();
    }

    public void AnimateIncrementalWin(float incrementAmount, float finalTotalAmount, bool inFreeSpins = false)
    {
        // Store the total for UI
        targetWinAmount = finalTotalAmount;

        if (winAnimationCoroutine != null) StopCoroutine(winAnimationCoroutine);

        // Start animation for just the increment, but display the total
        if (incrementAmount > 0 && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            SoundManager.Instance.PlayFreeWinsLoop();
            winAnimationCoroutine = StartCoroutine(AnimateIncrementalWinAmount(incrementAmount, finalTotalAmount, inFreeSpins));
            PlayWinAnimation();
            OnWinAdded?.Invoke(incrementAmount);
        }
        else if (inFreeSpins)
        {
            // Even if increment is 0, still show the win display in free spins mode
            lastWinAmount = finalTotalAmount;
            UpdateWinText(true);
        }
    }

    public void SetFreeSpinWinDisplay(float amount)
    {
        lastWinAmount = amount;
        UpdateWinText(true); // Always use free spins format
    }

    public void PreserveWinDisplay()
    {
        // Ensure the win text doesn't get reset between spins
        if (winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }

        // Make sure the win text is visible with the current value
        UpdateWinText();
    }

    private IEnumerator AnimateIncrementalWinAmount(float incrementAmount, float totalAmount, bool inFreeSpins = false)
    {
        float startAmount = lastWinAmount;
        float duration = incrementAmount * 1f;// Start from whatever is currently shown
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            var pause = WinCalculator.Instance.winAnimationController.pauseBetweenSymbolTypes;
            var paylineCount = WebManAPI.Instance.payLines.Count;
            duration = paylineCount > 15 ? 6 * pause : 3 * pause;
        }
        // Scale duration with size of increment
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Animate from current to current+increment
            lastWinAmount = Mathf.Lerp(startAmount, totalAmount, elapsed / duration);

            // Update the win text with free spins formatting if needed
            UpdateWinText(inFreeSpins);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final amount is exact
        lastWinAmount = totalAmount;
        UpdateWinText(inFreeSpins);

        SoundManager.Instance.StopBigWinLoop();
        SoundManager.Instance.StopSmallWinLoop();
        SoundManager.Instance.StopPlayFreeWinsLoop();
        SoundManager.Instance.PlaySound("finalscore");

        winAnimationCoroutine = null;
    }

    // UPDATED: ResetWinState method
    public void ResetWinState()
    {
        if (winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }

        lastWinAmount = 0f;
        targetWinAmount = 0f;

        // Reset UI with translation support
        UpdateWinText(false);
    }

    private void UpdateUI()
    {
        UpdateBalanceTexts();
        UpdateBetTexts();
        UpdateBetButtonIndicators(); // Ensure button indicators are always updated

        OnBalanceChanged?.Invoke();
    }
}