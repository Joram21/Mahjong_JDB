using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

[System.Serializable]
public class ButtonSet
{
    public Button landscape; // Used for both landscape and tablet
    public Button portrait;
}

[System.Serializable]
public class TextSet
{
    public TextMeshProUGUI landscape; // Used for both landscape and tablet
    public TextMeshProUGUI portrait;
}

[System.Serializable]
public class PanelSet
{
    public GameObject landscape;
    public GameObject portrait;
}

[System.Serializable]
public class ClickBlockerSet
{
    public Image landscape;
    public Image portrait;
}

public class AutoSpinManager : MonoBehaviour
{
    #region Singleton
    public static AutoSpinManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    #endregion

    [Header("References")]
    [SerializeField] private BetManager betManager;
    [SerializeField] private SlotMachineController slotMachine;
    [SerializeField] private PanelSet autoSpinPanels;
    [SerializeField] private UIElementManager uiElements;
    [SerializeField] private Button resumeButton;
    [SerializeField] private TextMeshProUGUI resumeButtonText;
    [SerializeField] private ClickBlockerSet clickBlockers;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private ViewMan viewManager;

    private int remainingSpins;
    private Coroutine autoSpinRoutine;
    private bool wasInterrupted = false;
    private ScreenCategory currentScreenCategory;

    public bool IsAutoSpinning { get; private set; }
    private int lastSelectedAutoSpinIndex = -1;

    [SerializeField] private AutoSpinPanel autoSpinPanelLandscape;
    [SerializeField] private AutoSpinPanel autoSpinPanelPortrait;
    [SerializeField] private SpinButtonNumberAnimator landscapeSpinAnimator;
    [SerializeField] private SpinButtonNumberAnimator portraitSpinAnimator;
    [SerializeField] private GameObject mainFeatureButton;
    private bool buttonsLockedForLastSpin = false; // NEW: Track button lock state

    public int LastSelectedAutoSpinIndex => lastSelectedAutoSpinIndex;

    private void Start()
    {
        // Initialize listeners for both button sets
        uiElements.autoSpinButton.landscape.onClick.AddListener(() => ToggleAutoSpinPanel());
        uiElements.autoSpinButton.portrait.onClick.AddListener(() => ToggleAutoSpinPanel());

        uiElements.spinButton.landscape.onClick.AddListener(OnSpinButtonClicked);
        uiElements.spinButton.portrait.onClick.AddListener(OnSpinButtonClicked);

        resumeButton.onClick.AddListener(OnResumeButtonClicked);

        // Initial state
        SetAllPanelsInactive();
        resumeButton.gameObject.SetActive(false);
        DisableAllClickBlockers();

        // Add click away listeners to all click blockers
        AddTriggerListener(clickBlockers.landscape.gameObject, EventTriggerType.PointerClick, OnClickAway);
        AddTriggerListener(clickBlockers.portrait.gameObject, EventTriggerType.PointerClick, OnClickAway);
        AddTriggerListener(clickBlockers.landscape.gameObject, EventTriggerType.PointerClick, OnClickAway);

        // Listen for orientation changes
        if (viewManager != null)
        {
            currentScreenCategory = viewManager.CurrentScreenCategory;
        }
    }

    private void Update()
    {
        if (viewManager != null && viewManager.CurrentScreenCategory != currentScreenCategory)
        {
            currentScreenCategory = viewManager.CurrentScreenCategory;
            UpdatePanelsBasedOnOrientation();
        }
    }

    private void UpdatePanelsBasedOnOrientation()
    {
        // If any panel is active, show only the one for the current orientation
        bool anyPanelActive = autoSpinPanels.landscape.activeSelf ||
                              autoSpinPanels.portrait.activeSelf;

        if (anyPanelActive)
        {
            SetAllPanelsInactive();
            EnableClickBlockerForCurrentOrientation();

            switch (currentScreenCategory)
            {
                case ScreenCategory.Landscape:
                    autoSpinPanels.landscape.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
                case ScreenCategory.Portrait:
                    autoSpinPanels.portrait.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
                case ScreenCategory.Tablet:
                    autoSpinPanels.landscape.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
            }
        }
    }

    private void SetAllPanelsInactive()
    {
        autoSpinPanels.landscape.SetActive(false);
        autoSpinPanels.portrait.SetActive(false);

        // Check if we're in API/Server mode before activating main feature button
        bool isApiMode = WebManAPI.Instance != null && !WebManAPI.Instance.isDemoMode;
        mainFeatureButton.SetActive(!isApiMode);
    }
    private void DisableAllClickBlockers()
    {
        clickBlockers.landscape.raycastTarget = false;
        clickBlockers.landscape.color = Color.clear;

        clickBlockers.portrait.raycastTarget = false;
        clickBlockers.portrait.color = Color.clear;
    }

    private void EnableClickBlockerForCurrentOrientation()
    {
        DisableAllClickBlockers();

        Image targetBlocker = GetCurrentBlocker();
        if (targetBlocker != null)
        {
            targetBlocker.raycastTarget = true;
            StartCoroutine(FadeIn(targetBlocker));
        }
    }

    private Image GetCurrentBlocker()
    {
        switch (currentScreenCategory)
        {
            case ScreenCategory.Landscape:
                return clickBlockers.landscape;
            case ScreenCategory.Portrait:
                return clickBlockers.portrait;
            case ScreenCategory.Tablet:
                return clickBlockers.landscape;
            default:
                return null;
        }
    }

    private GameObject GetCurrentPanel()
    {
        switch (currentScreenCategory)
        {
            case ScreenCategory.Landscape:
                return autoSpinPanels.landscape;
            case ScreenCategory.Portrait:
                return autoSpinPanels.portrait;
            case ScreenCategory.Tablet:
                return autoSpinPanels.landscape;
            default:
                return null;
        }
    }

    public void ToggleAutoSpinPanel()
    {
        GameObject currentPanel = GetCurrentPanel();
        if (currentPanel == null) return;

        bool newState = !currentPanel.activeSelf;

        // Set all panels to the new state
        SetAllPanelsInactive();

        if (newState) // Opening the panel
        {
            SoundManager.Instance.PlaySound("Button"); // Move sound here
            currentPanel.SetActive(true);
            mainFeatureButton.SetActive(false);
            EnableClickBlockerForCurrentOrientation();
        }
        else // Closing the panel
        {
            Image currentBlocker = GetCurrentBlocker();
            if (currentBlocker != null)
            {
                StartCoroutine(FadeOut(currentBlocker));
            }
        }
    }

    private IEnumerator FadeIn(Image target)
    {
        float alpha = 0;
        target.color = new Color(0, 0, 0, alpha);

        while (alpha < 0.5f)
        {
            alpha += Time.deltaTime / fadeDuration;
            target.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }

    private IEnumerator FadeOut(Image target)
    {
        float alpha = target.color.a;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeDuration;
            target.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        target.raycastTarget = false;
    }

    private void OnClickAway(BaseEventData data)
    {
        GameObject currentPanel = GetCurrentPanel();
        if (currentPanel != null && currentPanel.activeSelf)
        {
            ToggleAutoSpinPanel();
        }
    }

    private void ResetAllIndicators()
    {
        if (autoSpinPanelLandscape != null)
            autoSpinPanelLandscape.ResetIndicators();

        if (autoSpinPanelPortrait != null)
            autoSpinPanelPortrait.ResetIndicators();
    }

    // Set spin button to spinning state
    private void SetSpinButtonToSpinningState()
    {
        // Show spinning button visuals for both orientations
        uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(false);
        uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(true);
        uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(false);
        uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(true);
    }

    // Set spin button to normal state
    private void SetSpinButtonToNormalState()
    {
        // Show normal button visuals for both orientations
        uiElements.spinButtonVisuals.normalLandscape.gameObject.SetActive(true);
        uiElements.spinButtonVisuals.spinningLandscape.gameObject.SetActive(false);
        uiElements.spinButtonVisuals.normalPortrait.gameObject.SetActive(true);
        uiElements.spinButtonVisuals.spinningPortrait.gameObject.SetActive(false);
    }

    public void StartAutoSpin(int spins)
    {
        if (IsAutoSpinning) return;

        remainingSpins = spins - 1;
        SetAllPanelsInactive();
        DisableAllClickBlockers();
        resumeButton.gameObject.SetActive(false);

        // Set spin button to spinning state at the start of auto-spin
        SetSpinButtonToSpinningState();

        if (autoSpinRoutine != null) StopCoroutine(autoSpinRoutine);
        autoSpinRoutine = StartCoroutine(AutoSpinRoutine());
    }

    // In AutoSpinManager.cs, modify the AutoSpinRoutine method:

    private IEnumerator AutoSpinRoutine()
    {
        IsAutoSpinning = true;
        wasInterrupted = false;

        if (landscapeSpinAnimator != null)
            landscapeSpinAnimator.AnimateTo(remainingSpins);
        if (portraitSpinAnimator != null)
            portraitSpinAnimator.AnimateTo(remainingSpins);

        // Always maintain the spinning state throughout auto-spin
        SetSpinButtonToSpinningState();

        while (remainingSpins >= 0 && !wasInterrupted)
        {
            if (!betManager.CanPlaceBet())
            {
                GameManager.Instance.PromptMan.DisplayPrompt(PromptType.InsufficientFunds);
                break;
            }
            
            // NEW: Check if this is the last spin and lock buttons
            if (remainingSpins == 1)
            {
                LockButtonsForLastSpin();
            }

            // Keep spin button in spinning state before starting reels
            SetSpinButtonToSpinningState();

            slotMachine.StartAllReels();

            // Wait for regular spin to complete
            yield return new WaitUntil(() => !slotMachine.IsSpinning && !betManager.IsAnimatingWin);

            // Continue showing spinning animation during the delay between spins
            SetSpinButtonToSpinningState();

            yield return new WaitForSeconds(1f);

            // Wait for free spins to complete if triggered
            if (FreeSpinManager.Instance)
            {
                yield return new WaitWhile(() => FreeSpinManager.Instance.IsFreeSpinActive);
            }

            // NEW: Wait for mask reel to complete if triggered
            if (MaskReelManager.Instance)
            {
                yield return new WaitWhile(() => MaskReelManager.Instance.IsMaskReelActive);
            }

            if (!wasInterrupted)
            {
                remainingSpins--;
                if (landscapeSpinAnimator != null)
                    landscapeSpinAnimator.AnimateTo(remainingSpins);
                if (portraitSpinAnimator != null)
                    portraitSpinAnimator.AnimateTo(remainingSpins);

                // Keep spin button in spinning state even after updating the count
                SetSpinButtonToSpinningState();
            }
        }

        IsAutoSpinning = false;

        // Only set back to normal when auto-spin fully completes or is interrupted
        SetSpinButtonToNormalState();
        ResetSpinButtonTexts();

        if (wasInterrupted)
        {
            resumeButtonText.text = remainingSpins.ToString();
            resumeButton.gameObject.SetActive(true);
        }
        else
        {
            UnlockButtons();

            // Re-enable the autospin panel after completion (silently, without sound)
            yield return new WaitForSeconds(0.3f); // Small delay for visual clarity
            EnableAutoSpinPanelSilently(); // NEW: Silent activation without sound
        }
    }
    
    // NEW: Enable autospin panel without sound (for automatic activation after completion)
    private void EnableAutoSpinPanelSilently()
    {
        GameObject currentPanel = GetCurrentPanel();
        if (currentPanel == null) return;

        // Set all panels inactive first
        SetAllPanelsInactive();

        // Enable the current panel silently
        currentPanel.SetActive(true);
        mainFeatureButton.SetActive(false);
        EnableClickBlockerForCurrentOrientation();
    }
    
    // NEW: Lock buttons during last spin
    private void LockButtonsForLastSpin()
    {
        if (buttonsLockedForLastSpin) return;

        buttonsLockedForLastSpin = true;
        Debug.Log("[AutoSpinManager] Locking buttons for last spin");

        // Lock spin buttons
        uiElements.spinButton.landscape.interactable = false;
        uiElements.spinButton.portrait.interactable = false;

        // Lock bet buttons
        uiElements.increaseButton.landscape.interactable = false;
        uiElements.increaseButton.portrait.interactable = false;
        uiElements.decreaseButton.landscape.interactable = false;
        uiElements.decreaseButton.portrait.interactable = false;

        // Lock main bet button
        uiElements.mainBetButton.landscape.interactable = false;
        uiElements.mainBetButton.portrait.interactable = false;
    }
    
    // NEW: Unlock buttons after autospin completion
    private void UnlockButtons()
    {
        if (!buttonsLockedForLastSpin) return;

        buttonsLockedForLastSpin = false;
        Debug.Log("[AutoSpinManager] Unlocking buttons after autospin completion");

        // Unlock spin buttons
        uiElements.spinButton.landscape.interactable = true;
        uiElements.spinButton.portrait.interactable = true;

        // Unlock bet buttons
        uiElements.increaseButton.landscape.interactable = true;
        uiElements.increaseButton.portrait.interactable = true;
        uiElements.decreaseButton.landscape.interactable = true;
        uiElements.decreaseButton.portrait.interactable = true;
        
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

        // Unlock main bet button
        uiElements.mainBetButton.landscape.interactable = true;
        uiElements.mainBetButton.portrait.interactable = true;
    }
    
    private void ResetSpinButtonTexts()
    {
        
        if (landscapeSpinAnimator.oldText != null)
            landscapeSpinAnimator.oldText.text = "";
        if (landscapeSpinAnimator.newText != null)
            landscapeSpinAnimator.newText.text = "";
        if (portraitSpinAnimator.oldText != null)
            portraitSpinAnimator.oldText.text = "";
        if (portraitSpinAnimator.newText != null)
            portraitSpinAnimator.newText.text = "";
        ResetAllIndicators(); // clear highlights
    }

    private void OnSpinButtonClicked()
    {
        if (IsAutoSpinning && (!betManager.IsAnimatingWin && slotMachine.IsSpinning))
        {
            SoundManager.Instance.PlaySound("button");
            InterruptAutoSpin();
        }
    }

    private void InterruptAutoSpin()
    {
        // Only interrupt if we're actually auto-spinning and have remaining spins
        if (IsAutoSpinning && remainingSpins >= 0)
        {
            wasInterrupted = true;
            IsAutoSpinning = false;

            // Safely stop the coroutine if it exists
            if (autoSpinRoutine != null)
            {
                StopCoroutine(autoSpinRoutine);
                autoSpinRoutine = null;
            }

            // Set back to normal state when interrupted
            SetSpinButtonToNormalState();
            ResetSpinButtonTexts();
        }
    }

    private void OnResumeButtonClicked()
    {
        if (remainingSpins >= 0)
        {
            resumeButton.gameObject.SetActive(false);
            StartAutoSpin(remainingSpins);
        }
    }

    public void RecordAutoSpinSelection(int index)
    {
        lastSelectedAutoSpinIndex = index;
    }

    public void ClearAutoSpin()
    {
        if (IsAutoSpinning)
        {
            InterruptAutoSpin();
        }
        resumeButton.gameObject.SetActive(false);
    }

    private void AddTriggerListener(GameObject obj, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}