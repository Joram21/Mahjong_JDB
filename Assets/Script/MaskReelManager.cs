using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MaskReelManager : MonoBehaviour
{
    // Singleton pattern
    private static MaskReelManager _instance;
    public static MaskReelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<MaskReelManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MaskReelManager");
                    _instance = go.AddComponent<MaskReelManager>();
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject maskReelPanel;
    [SerializeField] private Button startMaskReelButton;
    [SerializeField] private Text startButtonText;

    [SerializeField] private Image multiplierSymbol; // For 'x' symbol
    [SerializeField] private Sprite multiplierXSprite;

    [Header("Mask Reel Controller")]
    [SerializeField] private MaskReelController maskReelController;
    [SerializeField] private GameObject maskReelContainer;

    [Header("References")]
    [SerializeField] private BetManager betManager;
    [SerializeField] private UIElementManager uiElements;

    [Header("Settings")]
    [SerializeField] private float panelFadeDuration = 0.5f;
    [SerializeField] private float multiplierDisplayDuration = 3f;

    [Header("Pink Highlight & Click Blocker")]
    [SerializeField] private GameObject pinkHighlightGameObject;
    [SerializeField] private GameObject clickBlockerGameObject; // This should have MaskReelClickHandler

    [Header("Canvas Fade Animation")]
    [SerializeField] private Animator canvasFadeAnimator;
    [SerializeField] private float canvasFadeAnimationDuration = 1f;

    [Header("Win Display System")]
    [SerializeField] private GameObject winDisplayPanel;

    [Header("Panel Display Panels (Assign Parent Objects)")]
    [SerializeField] private GameObject multiplierPanel; // Parent of multiplier digits
    [SerializeField] private GameObject betPanel;        // Parent of bet digits
    [SerializeField] private GameObject totalWinPanel;   // Parent of total win digits
    [SerializeField] private GameObject wholeMaskReelSection; // Parent of entire mask reel UI

    [Header("Panel Display Settings")]
    [SerializeField] private float panelDisplayDelay = 0.8f; // Delay between panels

    [Header("Multiplier Display (3 digits max)")]
    [SerializeField] private Image multiplierHundreds;
    [SerializeField] private Image multiplierTens;
    [SerializeField] private Image multiplierUnits;

    [Header("Current Bet Display (always 3 digits)")]
    [SerializeField] private Image betHundreds;
    [SerializeField] private Image betTens;
    [SerializeField] private Image betUnits;

    [Header("Total Win Display (up to 6 digits)")]
    [SerializeField] private Image totalWinHundredThousands;  // 6th digit
    [SerializeField] private Image totalWinTenThousands;
    [SerializeField] private Image totalWinThousands;
    [SerializeField] private Image totalWinHundreds;
    [SerializeField] private Image totalWinTens;
    [SerializeField] private Image totalWinUnits;

    [Header("Digit Sprites")]
    [SerializeField] private Sprite[] digitSprites; // 0-9 sprites

    [Header("Display Settings")]
    [SerializeField] private float winDisplayDuration = 3f;

    // State
    public bool maskReelActive = false;
    private int selectedMultiplier = 1;
    private float maskReelWin = 0f;
    private CanvasGroup panelCanvasGroup;
    [Header("API Integration")]
    private float apiWinAmount = 0f;
    private bool hasAPIWinAmount = false;
    // Events
    public System.Action OnMaskReelStart;
    public System.Action<int, float> OnMaskReelComplete;
    [Header("Win Celebration")]
    private Coroutine maskReelCelebrationRoutine;
    private bool isMaskReelCelebrating = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Setup canvas group for fading
        if (maskReelPanel != null)
        {
            panelCanvasGroup = maskReelPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = maskReelPanel.AddComponent<CanvasGroup>();

            maskReelPanel.SetActive(false);
        }
        if (uiElements == null)
            uiElements = FindAnyObjectByType<UIElementManager>();
        // Setup button listener
        if (startMaskReelButton != null)
        {
            startMaskReelButton.onClick.AddListener(OnStartMaskReelClicked);
        }

        // Find references if not assigned
        if (betManager == null)
            betManager = FindAnyObjectByType<BetManager>();

        // Hide all displays initially
        HideAllResultDisplays();
    }

    // ==========================================
    // PANEL-BASED DISPLAY SYSTEM
    // ==========================================

    private IEnumerator DisplayMaskReelResults()
    {
        // Debug.Log("[MaskReelManager] Displaying mask reel results...");

        // Get the values - use API data in non-demo mode
        int multiplier = maskReelController.GetSelectedMultiplier();
        float currentBet = betManager.CurrentBet;
        float totalWin;

        if (IsAPIMode() && hasAPIWinAmount)
        {
            // Non-demo mode: Use API win amount directly
            totalWin = apiWinAmount;
            // Debug.Log($"[MaskReelManager] Using API values - Multiplier: {multiplier}x, Bet: {currentBet:F2}, API Win: {totalWin:F2}");
        }
        else
        {
            // Demo mode: Calculate win amount
            totalWin = multiplier * currentBet;
            // Debug.Log($"[MaskReelManager] Using calculated values - Multiplier: {multiplier}x, Bet: {currentBet:F2}, Calculated Win: {totalWin:F2}");
        }

        // Update maskReelWin for later use
        maskReelWin = totalWin;

        // Show the whole mask reel section first
        if (wholeMaskReelSection != null)
        {
            wholeMaskReelSection.SetActive(true);
            // Debug.Log("Panel Display] Whole mask reel section shown");
        }

        // Show main win display panel
        if (winDisplayPanel != null)
        {
            winDisplayPanel.SetActive(true);
        }

        // Hide all panels initially
        HideAllPanels();

        // Set up all digit sprites first (but panels remain hidden)
        SetupMultiplierDigits(multiplier);
        SetupBetDigits(currentBet);
        SetupTotalWinDigits(totalWin);

        // Show panels one by one with delays
        yield return StartCoroutine(ShowPanelSequence());

        // Debug.Log("[MaskReelManager] Results display complete - panels will stay visible until sequence ends");
    }
    /// <summary>
    /// Manage button interactability during mask reel sessions (same as free spins)
    /// </summary>
    private void SetInteractable()
    {
        if (maskReelActive)
        {
            // Debug.Log("[MaskReelManager] Deactivating all buttons during mask reel session");

            // Deactivate spin buttons
            uiElements.spinButton.landscape.interactable = false;
            uiElements.spinButton.portrait.interactable = false;

            // Change spin button color to disabled state
            if (SlotMachineController.Instance != null)
                SlotMachineController.Instance.TransitionSpinButtonColor(true);

            // Deactivate auto spin buttons
            uiElements.autoSpinButton.landscape.interactable = false;
            uiElements.autoSpinButton.portrait.interactable = false;

            // Deactivate bet adjustment buttons
            uiElements.increaseButton.landscape.interactable = false;
            uiElements.increaseButton.portrait.interactable = false;
            uiElements.decreaseButton.landscape.interactable = false;
            uiElements.decreaseButton.portrait.interactable = false;
        }
        else
        {
            // Debug.Log("[MaskReelManager] Reactivating all buttons after mask reel session");

            // Reactivate spin buttons
            uiElements.spinButton.landscape.interactable = true;
            uiElements.spinButton.portrait.interactable = true;

            // Reset spin button color to normal state
            if (SlotMachineController.Instance != null)
                SlotMachineController.Instance.TransitionSpinButtonColor(false);

            // Reactivate auto spin buttons
            uiElements.autoSpinButton.landscape.interactable = true;
            uiElements.autoSpinButton.portrait.interactable = true;

            // Reactivate bet adjustment buttons
            uiElements.increaseButton.landscape.interactable = true;
            uiElements.increaseButton.portrait.interactable = true;
            uiElements.decreaseButton.landscape.interactable = true;
            uiElements.decreaseButton.portrait.interactable = true;
        }
    }

    public void SetAPIWinAmount(float winAmount)
    {
        apiWinAmount = winAmount;
        hasAPIWinAmount = true;
        // Debug.Log($"[MaskReelManager-API] API win amount set: {winAmount:F2}");
    }

    /// <summary>
    /// Clear API win amount (for demo mode or reset)
    /// </summary>
    public void ClearAPIWinAmount()
    {
        apiWinAmount = 0f;
        hasAPIWinAmount = false;
        // Debug.Log("[MaskReelManager-API] API win amount cleared");
    }

    /// <summary>
    /// Check if operating in API mode
    /// </summary>
    private bool IsAPIMode()
    {
        return WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode;
    }
    private IEnumerator ShowPanelSequence()
    {
        // Debug.Log("Panel Display] Starting panel sequence...");

        // 1. Show multiplier panel
        if (multiplierPanel != null)
        {
            // Debug.Log("Panel Display] Showing multiplier panel");
            multiplierPanel.SetActive(true);

            // NEW: Play multiplier panel sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundOneShot("multiplierpanel");
                // Debug.Log("[MaskReelManager] Playing multiplier panel sound");
            }

            yield return new WaitForSeconds(panelDisplayDelay);
        }

        // 2. Show bet panel
        if (betPanel != null)
        {
            // Debug.Log("Panel Display] Showing bet panel");
            betPanel.SetActive(true);

            // NEW: Play bet panel sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundOneShot("betpanel");
                // Debug.Log("[MaskReelManager] Playing bet panel sound");
            }

            yield return new WaitForSeconds(panelDisplayDelay);
        }

        // 3. Show total win panel
        if (totalWinPanel != null)
        {
            // Debug.Log("Panel Display] Showing total win panel");
            totalWinPanel.SetActive(true);

            // NEW: Play total win panel sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundOneShot("totalwinpanel");
                // Debug.Log("[MaskReelManager] Playing total win panel sound");
            }

            yield return new WaitForSeconds(panelDisplayDelay);
        }

        // All panels shown - they stay visible until mask reel sequence completes
        // Debug.Log("Panel Display] All panels shown - will stay visible until mask reel sequence ends");
    }

    private void SetupMultiplierDigits(int multiplier)
    {
        // Debug.Log($"[MaskReelManager] Setting up multiplier: {multiplier}");

        // Convert to digits
        int hundreds = multiplier / 100;
        int tens = (multiplier % 100) / 10;
        int units = multiplier % 10;

        // Handle hundreds digit
        if (multiplierHundreds != null)
        {
            if (hundreds > 0)
            {
                multiplierHundreds.sprite = GetDigitSprite(hundreds);
                multiplierHundreds.gameObject.SetActive(true);
            }
            else
            {
                multiplierHundreds.gameObject.SetActive(false);
            }
        }

        // Handle tens digit (show if hundreds exist OR if tens > 0)
        if (multiplierTens != null)
        {
            if (hundreds > 0 || tens > 0)
            {
                multiplierTens.sprite = GetDigitSprite(tens);
                multiplierTens.gameObject.SetActive(true);
            }
            else
            {
                multiplierTens.gameObject.SetActive(false);
            }
        }

        // Handle units digit (always show)
        if (multiplierUnits != null)
        {
            multiplierUnits.sprite = GetDigitSprite(units);
            multiplierUnits.gameObject.SetActive(true);
        }

        // Debug.Log($"[MaskReelManager] Multiplier digits setup: {hundreds}-{tens}-{units}");
    }

    private void SetupBetDigits(float bet)
    {
        // Debug.Log($"[MaskReelManager] Setting up bet: {bet:F2}");

        // Convert bet to cents to work with integers (e.g., 1.25 -> 125 cents)
        int betCents = Mathf.RoundToInt(bet * 100);

        int hundreds = betCents / 100;
        int tens = (betCents % 100) / 10;
        int units = betCents % 10;

        // Always show all 3 bet digits
        if (betHundreds != null)
        {
            betHundreds.sprite = GetDigitSprite(hundreds);
            betHundreds.gameObject.SetActive(true);
        }

        if (betTens != null)
        {
            betTens.sprite = GetDigitSprite(tens);
            betTens.gameObject.SetActive(true);
        }

        if (betUnits != null)
        {
            betUnits.sprite = GetDigitSprite(units);
            betUnits.gameObject.SetActive(true);
        }

        // Debug.Log($"[MaskReelManager] Bet digits setup: {hundreds}-{tens}-{units}");
    }

    private void SetupTotalWinDigits(float totalWin)
    {
        // Debug.Log($"[MaskReelManager] Setting up total win: {totalWin:F2}");

        // Convert to cents for integer math (e.g., 1000.00 -> 100000 cents)
        int winCents = Mathf.RoundToInt(totalWin * 100);

        // Extract 6 digits
        int hundredThousands = winCents / 100000;
        int tenThousands = (winCents % 100000) / 10000;
        int thousands = (winCents % 10000) / 1000;
        int hundreds = (winCents % 1000) / 100;
        int tens = (winCents % 100) / 10;
        int units = winCents % 10;

        // Handle hundred thousands digit (6th digit)
        if (totalWinHundredThousands != null)
        {
            if (hundredThousands > 0)
            {
                totalWinHundredThousands.sprite = GetDigitSprite(hundredThousands);
                totalWinHundredThousands.gameObject.SetActive(true);
            }
            else
            {
                totalWinHundredThousands.gameObject.SetActive(false);
            }
        }

        // Handle ten thousands digit
        if (totalWinTenThousands != null)
        {
            if (hundredThousands > 0 || tenThousands > 0)
            {
                totalWinTenThousands.sprite = GetDigitSprite(tenThousands);
                totalWinTenThousands.gameObject.SetActive(true);
            }
            else
            {
                totalWinTenThousands.gameObject.SetActive(false);
            }
        }

        // Handle thousands digit
        if (totalWinThousands != null)
        {
            if (hundredThousands > 0 || tenThousands > 0 || thousands > 0)
            {
                totalWinThousands.sprite = GetDigitSprite(thousands);
                totalWinThousands.gameObject.SetActive(true);
            }
            else
            {
                totalWinThousands.gameObject.SetActive(false);
            }
        }

        // Handle hundreds digit
        if (totalWinHundreds != null)
        {
            if (hundredThousands > 0 || tenThousands > 0 || thousands > 0 || hundreds > 0)
            {
                totalWinHundreds.sprite = GetDigitSprite(hundreds);
                totalWinHundreds.gameObject.SetActive(true);
            }
            else
            {
                totalWinHundreds.gameObject.SetActive(false);
            }
        }

        // Handle tens digit (show if any higher digit exists OR if tens > 0)
        if (totalWinTens != null)
        {
            if (hundredThousands > 0 || tenThousands > 0 || thousands > 0 || hundreds > 0 || tens > 0)
            {
                totalWinTens.sprite = GetDigitSprite(tens);
                totalWinTens.gameObject.SetActive(true);
            }
            else
            {
                totalWinTens.gameObject.SetActive(false);
            }
        }

        // Handle units digit (always show)
        if (totalWinUnits != null)
        {
            totalWinUnits.sprite = GetDigitSprite(units);
            totalWinUnits.gameObject.SetActive(true);
        }

        // Debug.Log($"[MaskReelManager] Total win digits setup: {hundredThousands}-{tenThousands}-{thousands}-{hundreds}-{tens}-{units}");
    }

    private Sprite GetDigitSprite(int digit)
    {
        if (digitSprites != null && digit >= 0 && digit < digitSprites.Length)
        {
            return digitSprites[digit];
        }
        else
        {
            // Debug.LogError($"[MaskReelManager] Invalid digit {digit} or missing digit sprites!");
            return null;
        }
    }

    private void HideAllPanels()
    {
        if (multiplierPanel != null) multiplierPanel.SetActive(false);
        if (betPanel != null) betPanel.SetActive(false);
        if (totalWinPanel != null) totalWinPanel.SetActive(false);

        // Debug.Log("Panel Display] All panels hidden");
    }

    private void HideAllResultDisplays()
    {
        // Hide the entire mask reel section (if assigned)
        if (wholeMaskReelSection != null)
        {
            wholeMaskReelSection.SetActive(false);
            // Debug.Log("Panel Display] Whole mask reel section hidden");
        }

        // Hide the individual panels
        HideAllPanels();

        // Hide individual digit objects (fallback)
        if (multiplierHundreds != null) multiplierHundreds.gameObject.SetActive(false);
        if (multiplierTens != null) multiplierTens.gameObject.SetActive(false);
        if (multiplierUnits != null) multiplierUnits.gameObject.SetActive(false);

        if (betHundreds != null) betHundreds.gameObject.SetActive(false);
        if (betTens != null) betTens.gameObject.SetActive(false);
        if (betUnits != null) betUnits.gameObject.SetActive(false);

        if (totalWinHundredThousands != null) totalWinHundredThousands.gameObject.SetActive(false);
        if (totalWinTenThousands != null) totalWinTenThousands.gameObject.SetActive(false);
        if (totalWinThousands != null) totalWinThousands.gameObject.SetActive(false);
        if (totalWinHundreds != null) totalWinHundreds.gameObject.SetActive(false);
        if (totalWinTens != null) totalWinTens.gameObject.SetActive(false);
        if (totalWinUnits != null) totalWinUnits.gameObject.SetActive(false);

        // Hide main win display panel
        if (winDisplayPanel != null) winDisplayPanel.SetActive(false);

        // Debug.Log("Panel Display] All displays hidden");
    }

    // ==========================================
    // MAIN MASK REEL SEQUENCE
    // ==========================================

    public IEnumerator HandleMaskReelSequence()
    {
        // Debug.Log("[MaskReelManager] Starting Mask Reel sequence");

        maskReelActive = true;
        SetInteractable();
        OnMaskReelStart?.Invoke();

        // Disable random symbol generation in SlotController
        SlotMachineController.Instance.SetUseRandomSymbolsInDemo(false);
        wholeMaskReelSection.SetActive(true);

        // 1. Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // 2. Activate pink highlight and play children animations immediately
        if (pinkHighlightGameObject != null)
        {
            pinkHighlightGameObject.SetActive(true);

            // NEW: Start pink highlight music
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPinkHighlightMusic();
                // Debug.Log("[MaskReelManager] Pink highlight music started");
            }

            // Play children animations immediately
            Animator[] childAnimators = pinkHighlightGameObject.GetComponentsInChildren<Animator>();
            foreach (Animator animator in childAnimators)
            {
                if (animator != null)
                {
                    animator.Play(0); // Play the first animation state
                }
            }
            // Debug.Log("[MaskReelManager] Pink highlight activated with children animations");
        }

        // 3. Play canvas fading animation
        yield return StartCoroutine(PlayCanvasFadeAnimation());

        // 4. After 1 second: switch off pink highlight and activate click blocker
        yield return new WaitForSeconds(1f);

        if (pinkHighlightGameObject != null)
        {
            pinkHighlightGameObject.SetActive(false);

            // NEW: Stop pink highlight music and start mask reel BG music
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopPinkHighlightMusic();
                SoundManager.Instance.PlayMaskReelBGMusic();
                // Debug.Log("[MaskReelManager] Switched from pink highlight music to mask reel BG music");
            }

            // Debug.Log("[MaskReelManager] Pink highlight deactivated");
        }

        if (clickBlockerGameObject != null)
        {
            clickBlockerGameObject.SetActive(true);
            // Debug.Log("[MaskReelManager] Click blocker activated");
        }

        // 5. Continue with existing fade in panel logic
        yield return StartCoroutine(FadeInMaskReelPanel());

        // 6. Show start button
        ShowStartButton();
    }

    private IEnumerator PlayCanvasFadeAnimation()
    {
        if (canvasFadeAnimator != null)
        {
            canvasFadeAnimator.Play("FadeIn");
            yield return new WaitForSeconds(canvasFadeAnimationDuration);
        }
        else
        {
            // Debug.LogWarning("[MaskReelManager] Canvas fade animator not assigned");
        }
    }

    private IEnumerator FadeInMaskReelPanel()
    {
        if (maskReelPanel == null || panelCanvasGroup == null) yield break;

        maskReelPanel.SetActive(true);
        panelCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / panelFadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        // Debug.Log("[MaskReelManager] Panel faded in");
    }

    private void ShowStartButton()
    {
        if (startMaskReelButton != null)
        {
            startMaskReelButton.gameObject.SetActive(true);
            startMaskReelButton.interactable = true;
        }

        if (startButtonText != null)
        {
            startButtonText.text = "PRESS TO START";
        }

        // Debug.Log("[MaskReelManager] Start button shown");
    }

    private void OnStartMaskReelClicked()
    {
        // Debug.Log("[MaskReelManager] Start button clicked");

        // Hide start button
        if (startMaskReelButton != null)
        {
            startMaskReelButton.gameObject.SetActive(false);
        }

        StartCoroutine(ExecuteMaskReelSpin());
    }

    // SIMPLIFIED: No event subscriptions - direct call system
    private IEnumerator ExecuteMaskReelSpin()
    {
        // Debug.Log("[MaskReelManager] Starting mask reel spin sequence...");
        

        // Simple: Just tell controller to start spinning
        // No event subscriptions needed - controller will call us directly!
        if (maskReelController != null)
        {
            maskReelController.StartSpin();
            // Debug.Log("[MaskReelManager] Spin started - controller will call back directly when done");
        }
        else
        {
            // Debug.LogError("[MaskReelManager] MaskReelController not found!");
        }

        yield break; // Exit immediately - controller handles everything
    }

    // PUBLIC: Now controller can call this directly (no events needed)
    public void OnMaskReelSpinComplete(int multiplier, MaskReelSymbolData symbolData)
    {
        selectedMultiplier = multiplier;
        // Debug.Log($"[MaskReelManager] CONTROLLER FINISHED! Multiplier: {selectedMultiplier}x");

        // NEW: Stop mask spinning sound when spinning completes
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMaskSpinning();
            // Debug.Log("[MaskReelManager] Mask spinning sound stopped");
        }

        // No event unsubscription needed with direct calls!

        // Now handle the win sequence
        StartCoroutine(HandlePostSpinSequence());
    }

    private IEnumerator HandlePostSpinSequence()
    {
        // Now MaskReelManager handles the win display and processing
        yield return StartCoroutine(CalculateAndShowWin());

        // Complete the entire sequence
        CompleteMaskReelSequence();
    }

    // UPDATED: Show mask reel results, then clear for normal win animation
    private IEnumerator CalculateAndShowWin()
    {
        if (betManager != null)
        {
            maskReelWin = selectedMultiplier * betManager.CurrentBet;
            // Debug.Log($"[MaskReelManager] Win calculated: {selectedMultiplier} x {betManager.CurrentBet} = {maskReelWin}");

            // 1. Display the mask reel specific UI (panels show sequentially)
            yield return StartCoroutine(DisplayMaskReelResults());

            // 2. Show final result for a moment
            // Debug.Log("[MaskReelManager] Showing final mask reel result...");
            yield return new WaitForSeconds(2f);

            // 3. Hide mask reel displays BEFORE calling AddWin
            // Debug.Log("[MaskReelManager] Hiding mask reel displays to make way for normal win animation");
            HideAllResultDisplays();

            // 4. HANDLE ALL WIN PRESENTATION IN MASK REEL MANAGER
            HandleMaskReelWinPresentation();

            // 5. Now call AddWin WITHOUT sound conflicts (BetManager only handles visual animation)
            // Debug.Log("[MaskReelManager] Calling betManager.AddWin - visual animation only");
            betManager.AddWin(maskReelWin);
            WinCalculator.Instance.DisplayMaskReelTotalPayout(maskReelWin);

            // 6. Brief wait to let BetManager's win animation start
            yield return new WaitForSeconds(1f);

            // Debug.Log("[MaskReelManager] Win sequence complete - BetManager now handling win display");
        }
    }

    /// <summary>
    /// Handle all mask reel win presentation (sounds, big win, confetti)
    /// </summary>
    private void HandleMaskReelWinPresentation()
    {
        // Debug.Log($"[MaskReelManager] Handling win presentation for {maskReelWin:F2}");

        // Use the same big win threshold from WinCalculator
        float bigWinThreshold = 100f; // Same as WinCalculator

        if (maskReelWin >= bigWinThreshold)
        {
            // BIG WIN: Start celebration and confetti
            // Debug.Log("[MaskReelManager] BIG WIN! Starting celebration and confetti");

            // Play big win sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBigWinLoop();
            }

            // Start confetti celebration
            StartMaskReelCelebration();
        }
        else if (maskReelWin > 0f)
        {
            // SMALL WIN: Play small win sound
            // Debug.Log("[MaskReelManager] Small win - playing win sound");

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySmallWinLoop();
            }
        }
    }

    /// <summary>
    /// Start celebration for big mask reel wins (same as WinCalculator)
    /// </summary>
    private void StartMaskReelCelebration()
    {
        if (maskReelCelebrationRoutine != null)
        {
            StopCoroutine(maskReelCelebrationRoutine);
        }
        maskReelCelebrationRoutine = StartCoroutine(MaskReelCelebrationRoutine());
    }

    private IEnumerator MaskReelCelebrationRoutine()
    {
        isMaskReelCelebrating = true;

        // Find confetti spawner (same as WinCalculator)
        ConfettiSpawner confettiSpawner = FindAnyObjectByType<ConfettiSpawner>();

        if (confettiSpawner != null)
        {
            confettiSpawner.LaunchConfetti();
            // Debug.Log("[MaskReelManager] Confetti launched for big mask reel win");
        }
        else
        {
            // Debug.LogWarning("[MaskReelManager] ConfettiSpawner not found - no confetti for big win");
        }

        // Wait until the BetManager animation has started
        float timeout = 1f;
        float elapsed = 0f;
        while (betManager != null && !betManager.IsAnimatingWin && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Now wait until the animation ends
        while (betManager != null && betManager.IsAnimatingWin)
        {
            yield return null;
        }

        // Stop confetti when win animation completes
        if (confettiSpawner != null)
        {
            // confettiSpawner.StopConfetti();
            // Debug.Log("[MaskReelManager] Confetti stopped - win animation complete");
        }

        isMaskReelCelebrating = false;
        maskReelCelebrationRoutine = null;
    }

    public void DeactivateClickBlocker()
    {
        if (clickBlockerGameObject != null)
        {
            clickBlockerGameObject.SetActive(false);
            // Debug.Log("[MaskReelManager] Click blocker deactivated by click handler");
        }
    }

    public void OnClickBlockerUsed()
    {
        // Debug.Log("[MaskReelManager] Click blocker was clicked - spin initiated");
    }

    private void CompleteMaskReelSequence()
    {
        if (IsAPIMode() && hasAPIWinAmount && apiWinAmount > 0)
        {
            WebManAPI.Instance.RecordFreeSpinTotalWin(apiWinAmount);
        }

        // NEW: Stop all mask reel sounds when sequence completes
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMaskReelBGMusic();
        }

        // Hide the panel
        if (maskReelPanel != null)
        {
            maskReelPanel.SetActive(false);
        }

        // // Re-enable random symbols if in demo mode
        // if (SlotMachineController.Instance.isDemoMode)
        // {
        //     SlotMachineController.Instance.SetUseRandomSymbolsInDemo(true);
        // }

        // Clear API data for next round
        ClearAPIWinAmount();

        // Reset state
        maskReelActive = false;
        SetInteractable();
        selectedMultiplier = 1;

        // Fire completion event
        OnMaskReelComplete?.Invoke(selectedMultiplier, maskReelWin);

        // Reset mask reel controller
        if (maskReelController != null)
        {
            maskReelController.ResetReel();
        }

        maskReelWin = 0f;

        // Debug.Log("[MaskReelManager] Mask reel sequence completed");
    }
    // ==========================================
    // PUBLIC GETTERS
    // ==========================================

    public bool IsMaskReelActive => maskReelActive;
    public int GetSelectedMultiplier() => selectedMultiplier;
    public float GetMaskReelWin() => maskReelWin;
    public bool HasAPIWinAmount() => hasAPIWinAmount;
    public float GetAPIWinAmount() => apiWinAmount;

    // ==========================================
    // CLEANUP
    // ==========================================
    private void OnDisable()
    {
        // Stop celebration if component is disabled
        if (maskReelCelebrationRoutine != null)
        {
            StopCoroutine(maskReelCelebrationRoutine);
            isMaskReelCelebrating = false;
            maskReelCelebrationRoutine = null;
        }
    }

    private void OnDestroy()
    {
        // NEW: Stop all mask reel sounds when object is destroyed
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopPinkHighlightMusic();
            SoundManager.Instance.StopMaskReelBGMusic();
            SoundManager.Instance.StopMaskSpinning();
        }

        // No event cleanup needed with direct calls!
        if (_instance == this)
        {
            _instance = null;
        }
    }
}