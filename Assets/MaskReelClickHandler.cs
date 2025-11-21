using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MaskReelClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private MaskReelController maskReelController;
    [SerializeField] private Image clickBlockerImage;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private bool showClickFeedback = true;

    [Header("Click Settings")]
    [SerializeField] private bool preventClicksDuringSpin = true;

    [Header("?? Debug & Recovery")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool autoResetIfStuck = true;
    [SerializeField] private float stuckTimeoutSeconds = 10f;

    private bool canClick = true;
    private float lastClickTime = 0f;

    private void Awake()
    {
        ValidateComponents();
        SetupClickBlocker();
    }

    private void Start()
    {
        // Subscribe to mask reel events
        if (maskReelController != null)
        {
            maskReelController.OnSpinStart += OnSpinStarted;
            maskReelController.OnSpinComplete += OnSpinCompleted;
        }

        UpdateVisualState();

        // ? SAFETY: Reset controller state on start
        if (autoResetIfStuck && maskReelController != null)
        {
            maskReelController.ForceReset();
            // Debug.Log("?? [MaskReelClickHandler] Auto-reset controller on start");
        }
    }

    private void Update()
    {
        // ? AUTO-RECOVERY: Check for stuck spins
        if (autoResetIfStuck && maskReelController != null && maskReelController.IsSpinning())
        {
            if (Time.time - lastClickTime > stuckTimeoutSeconds)
            {
                // Debug.LogWarning($"?? [Auto-Recovery] Spin stuck for {stuckTimeoutSeconds}s - auto-resetting!");
                maskReelController.ForceReset();
                canClick = true;
                UpdateVisualState();
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (maskReelController != null)
        {
            maskReelController.OnSpinStart -= OnSpinStarted;
            maskReelController.OnSpinComplete -= OnSpinCompleted;
        }
    }

    private void ValidateComponents()
    {
        if (maskReelController == null)
        {
            maskReelController = GetComponentInParent<MaskReelController>();
            // if (maskReelController == null)
            // Debug.LogWarning("[MaskReelClickHandler] No MaskReelController found!");
        }

        if (clickBlockerImage == null)
        {
            clickBlockerImage = GetComponent<Image>();
            // if (clickBlockerImage == null)
            // Debug.LogWarning("[MaskReelClickHandler] No Image component found!");
        }
    }

    private void SetupClickBlocker()
    {
        // Ensure we have an Image component for click detection
        if (clickBlockerImage == null)
        {
            clickBlockerImage = gameObject.AddComponent<Image>();
        }

        // Make it transparent by default (invisible but clickable)
        if (clickBlockerImage.sprite == null)
        {
            clickBlockerImage.color = new Color(1, 1, 1, 0.01f); // Almost transparent
        }

        // Ensure raycast target is enabled for clicking
        clickBlockerImage.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        lastClickTime = Time.time;

        // ? ENHANCED DEBUG
        if (enableDebugMode)
        {
            LogDetailedState();
        }

        if (CanStartSpin())
        {
            StartMaskReelSpin();
        }
        else
        {
            // Debug.Log($"?? [MaskReelClickHandler] Cannot start spin - Use manual reset if needed");

            // ? MANUAL RECOVERY OPTION
            if (enableDebugMode && maskReelController != null && maskReelController.IsSpinning())
            {
                // Debug.Log("?? [Manual Recovery] Double-click within 2 seconds to force reset");
                StartCoroutine(CheckForDoubleClick());
            }
        }
    }

    private System.Collections.IEnumerator CheckForDoubleClick()
    {
        float startTime = Time.time;
        yield return new WaitForSeconds(2f);

        // Check if user clicked again within 2 seconds
        if (Time.time - lastClickTime < 2f && maskReelController != null && maskReelController.IsSpinning())
        {
            // Debug.LogWarning("?? [Manual Recovery] Double-click detected - forcing reset!");
            maskReelController.ForceReset();
            canClick = true;
            UpdateVisualState();
        }
    }

    private void LogDetailedState()
    {
        // Debug.Log("?? === CLICK HANDLER DEBUG STATE ===");
        // Debug.Log($"?? canClick: {canClick}");
        // Debug.Log($"?? maskReelController: {(maskReelController != null ? "Found" : "NULL")}");

        if (maskReelController != null)
        {
            // Debug.Log($"?? controller.IsSpinning(): {maskReelController.IsSpinning()}");
        }

        // Debug.Log($"?? preventClicksDuringSpin: {preventClicksDuringSpin}");
        // Debug.Log($"?? gameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
        // Debug.Log($"?? Time since last click: {Time.time - lastClickTime:F1}s");
    }

    private bool CanStartSpin()
    {
        // ? DETAILED CHECKS with logging
        if (!canClick)
        {
            // if (enableDebugMode) Debug.Log("?? [CanStartSpin] Blocked: canClick = false");
            return false;
        }

        if (maskReelController == null)
        {
            // if (enableDebugMode) Debug.Log("?? [CanStartSpin] Blocked: maskReelController = null");
            return false;
        }

        if (preventClicksDuringSpin && maskReelController.IsSpinning())
        {
            // if (enableDebugMode) Debug.Log("?? [CanStartSpin] Blocked: reel is spinning");
            return false;
        }

        // if (enableDebugMode) Debug.Log("?? [CanStartSpin] ? All checks passed!");
        return true;
    }

    private void StartMaskReelSpin()
    {
        // Debug.Log("[MaskReelClickHandler] Starting mask reel spin...");

        // Deactivate the click blocker immediately when clicked
        DeactivateClickBlocker();

        if (showClickFeedback)
        {
            StartCoroutine(ShowClickFeedback());
        }

        maskReelController.StartSpin();
    }

    private void DeactivateClickBlocker()
    {
        // Deactivate the click blocker GameObject immediately
        if (gameObject != null)
        {
            gameObject.SetActive(false);
            // Debug.Log("[MaskReelClickHandler] Click blocker deactivated after being clicked");
        }
    }

    private System.Collections.IEnumerator ShowClickFeedback()
    {
        if (clickBlockerImage == null) yield break;

        Color originalColor = clickBlockerImage.color;

        // Flash effect
        clickBlockerImage.color = new Color(1, 1, 1, 0.3f);
        yield return new WaitForSeconds(0.1f);
        clickBlockerImage.color = originalColor;
    }

    private void OnSpinStarted()
    {
        // Debug.Log("[MaskReelClickHandler] Spin started - disabling clicks");
        UpdateVisualState();
    }

    private void OnSpinCompleted(int multiplier, MaskReelSymbolData symbolData)
    {
        // Debug.Log($"[MaskReelClickHandler] Spin completed with {multiplier}x multiplier and symbol: {symbolData?.name}");

        // QUICK FIX: Force enable clicks after spin completion
        canClick = true;

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (clickBlockerImage == null) return;

        bool isSpinning = maskReelController != null && maskReelController.IsSpinning();

        if (isSpinning && preventClicksDuringSpin)
        {
            clickBlockerImage.color = disabledColor;
        }
        else
        {
            clickBlockerImage.color = normalColor;
        }
    }

    // ? NEW: Public methods for manual control
    public void ManualReset()
    {
        // Debug.LogWarning("?? [ManualReset] Manually resetting click handler and controller");

        if (maskReelController != null)
        {
            maskReelController.ForceReset();
        }

        canClick = true;
        UpdateVisualState();

        // Debug.Log("?? [ManualReset] Reset complete");
    }

    public void EnableClicks()
    {
        canClick = true;
        UpdateVisualState();
        // Debug.Log("?? [EnableClicks] Clicks enabled");
    }

    public void DisableClicks()
    {
        canClick = false;
        UpdateVisualState();
        // Debug.Log("?? [DisableClicks] Clicks disabled");
    }

    public void SetMaskReelController(MaskReelController controller)
    {
        if (maskReelController != null)
        {
            maskReelController.OnSpinStart -= OnSpinStarted;
            maskReelController.OnSpinComplete -= OnSpinCompleted;
        }

        maskReelController = controller;

        if (maskReelController != null)
        {
            maskReelController.OnSpinStart += OnSpinStarted;
            maskReelController.OnSpinComplete += OnSpinCompleted;
        }

        // Debug.Log($"?? [SetMaskReelController] Controller set: {(controller != null ? "Success" : "Null")}");
    }

    // ? NEW: Context menu for easy testing in editor
    [ContextMenu("Force Reset Everything")]
    private void ContextMenuReset()
    {
        ManualReset();
    }

    [ContextMenu("Debug Current State")]
    private void ContextMenuDebug()
    {
        LogDetailedState();
    }
}