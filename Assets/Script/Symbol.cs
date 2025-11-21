using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Symbol : MonoBehaviour
{
    private static readonly int PlayWin = Animator.StringToHash("PlayWin");
    public SymbolData Data { get; private set; }

    [Header("Grid Position (editable in Inspector)")]
    public int col;
    public int row;

    // References to both animators
    [SerializeField] private Animator baseAnimator;
    [SerializeField] private Animator specialAnimator;

    // References to image components
    [SerializeField] private Image baseImage;
    [SerializeField] private Image winHighlightImage;
    [SerializeField] private Image animImage;

    // Size configurations
    [Header("Symbol Sizes")]
    [SerializeField] private Vector2 normalSize = new Vector2(230f, 230f); // Default size for card symbols
    [SerializeField] private Vector2 freeSpinMaskReelSize = new Vector2(230f, 230f); // FreeSpin and MaskReel size
    [SerializeField] private Vector2 wildSize = new Vector2(230f, 230f); // Wild symbol size
    [SerializeField] private Vector2 maskSize = new Vector2(230f, 230f); // Unique size for mask symbols (customize as needed)
    [SerializeField] private Vector2 winHighlightSize = new Vector2(230f, 230f);
    // [SerializeField] private Vector2 frogSize = new Vector2(230f, 230f);

    // Free spin mode tracking
    private bool isFreeSpinMode = false;

    // Animation state tracking
    private bool isAnimating = false;

    [Header("Mask Reveal Animations")]
    [SerializeField] private string maskTransformTrigger = "PlayMaskTransform";

    private bool isInRevealSequence = false;

    [Header("Rendering Order")]

    // Events
    public System.Action<Symbol> OnSymbolTransformed;
    public System.Action<Symbol> OnWinAnimationStarted;
    public System.Action<Symbol> OnWinAnimationEnded;

    public (int col, int row) GetPositions()
    {
        return new(row, col);
    }

    public void Initialize(SymbolData data)
    {
        Data = data;

        // Get references to images
        baseImage = transform.Find("DefaultImage").GetComponent<Image>();
        winHighlightImage = transform.Find("WinHighlightImage").GetComponent<Image>();
        animImage = transform.Find("AnimationImage").GetComponent<Image>();

        // Get references to animators
        baseAnimator = baseImage.GetComponent<Animator>();
        specialAnimator = animImage.GetComponent<Animator>();

        // Set up base image animator for standard symbols
        if (data.animationOverride != null && baseAnimator != null)
        {
            AnimatorOverrideController baseOverrideController =
                new AnimatorOverrideController(baseAnimator.runtimeAnimatorController);
            baseOverrideController["Idle"] = data.idleAnimation;
            baseOverrideController["Win"] = data.winAnimation;
            baseAnimator.runtimeAnimatorController = baseOverrideController;
        }

        // Set up special animation animator for special symbols
        if (data.animationOverride != null && specialAnimator != null)
        {
            AnimatorOverrideController specialOverrideController =
                new AnimatorOverrideController(specialAnimator.runtimeAnimatorController);

            switch (data.type)
            {
                case SymbolData.SymbolType.Wild:
                    specialOverrideController["Special"] = data.wildAnimation;
                    break;
                case SymbolData.SymbolType.FreeSpin:
                    specialOverrideController["Special"] = data.freeSpinAnimation;
                    break;

                case SymbolData.SymbolType.GreenDragon:
                case SymbolData.SymbolType.WhiteDragon:
                case SymbolData.SymbolType.BlackDragon:
                case SymbolData.SymbolType.RedDragon:
                    break;
            }

            specialAnimator.runtimeAnimatorController = specialOverrideController;
        }

        // Initial visual setup - use appropriate sprite based on mode
        UpdateVisualSprites();

        baseImage.enabled = true;
        winHighlightImage.enabled = false;
        animImage.enabled = false;

        // Check if we're in free spin mode at start
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            UpdateVisualForFreeSpins(true);
        }

        SetupSizing();
    }

    private void UpdateVisualSprites()
    {
        if (Data == null) return;

        // Get the correct sprite for current mode
        Sprite correctSprite = Data.GetCurrentSprite(isFreeSpinMode);
        Sprite correctHighlightSprite = Data.GetCurrentWinHighlightSprite(isFreeSpinMode);

        // Update base image
        if (baseImage != null && correctSprite != null)
        {
            baseImage.sprite = correctSprite;
        }

        // Update win highlight image
        // if (winHighlightImage != null && correctHighlightSprite != null)
        // {
        //     winHighlightImage.sprite = correctHighlightSprite;
        // }

        // Update animation image to match
        if (animImage != null && correctSprite != null)
        {
            animImage.sprite = correctSprite;
        }

        // Debug.Log($"[Symbol] Updated sprites - Type: {Data.type}, FreeSpinMode: {isFreeSpinMode}, Sprite: {correctSprite?.name}");
    }
    private void SetupSizing()
    {
        // Get all rect transforms at start
        RectTransform baseRect = baseImage.rectTransform;
        RectTransform winRect = winHighlightImage.rectTransform;
        RectTransform animRect = animImage.rectTransform;

        // Store normal size from the prefab if not already set
        if (normalSize == Vector2.zero)
        {
            normalSize = baseRect.sizeDelta;
        }

        // Set initial sizes
        Vector2 targetSize = GetTargetSize();

        baseRect.sizeDelta = targetSize;
        winRect.sizeDelta = winHighlightSize; // Use dedicated win highlight size
        animRect.sizeDelta = targetSize;
    }

    private Vector2 GetTargetSize()
    {
        switch (Data.type)
        {
            case SymbolData.SymbolType.Wild:
                return wildSize;
            case SymbolData.SymbolType.FreeSpin:
                // case SymbolData.SymbolType.PearlRed: // MaskReel uses same size as FreeSpin
                return freeSpinMaskReelSize;
            case SymbolData.SymbolType.RedDragon:
            case SymbolData.SymbolType.WhiteDragon:
            case SymbolData.SymbolType.BlackDragon:
            case SymbolData.SymbolType.GreenDragon:
                return maskSize; // Unique size for mask symbols
            default:
                return normalSize;
        }
    }

    public void ValidateSymbolSize()
    {
        Vector2 correctSize = GetTargetSize();

        if (baseImage.rectTransform.sizeDelta != correctSize)
            baseImage.rectTransform.sizeDelta = correctSize;

        if (winHighlightImage.rectTransform.sizeDelta != winHighlightSize)
            winHighlightImage.rectTransform.sizeDelta = winHighlightSize; // Use dedicated win highlight size

        if (animImage.rectTransform.sizeDelta != correctSize)
            animImage.rectTransform.sizeDelta = correctSize;
    }
    // Update visual for free spin mode
    public void UpdateVisualForFreeSpins(bool freeSpinMode)
    {
        bool wasInFreeSpinMode = isFreeSpinMode;
        isFreeSpinMode = freeSpinMode;

        // ALWAYS update sprite variants when this method is called
        UpdateVisualSprites();

        // Validate sizing for the current symbol type
        ValidateSymbolSize();

        // Debug.Log($"[Symbol] Updated visual mode: {wasInFreeSpinMode} -> {freeSpinMode} (Symbol: {Data?.type})");
    }

    // Add this validation method
    private void ValidateSymbolState()
    {
        if (Data == null) return;

        // Ensure correct sprite is applied
        Sprite correctSprite = Data.GetCurrentSprite(isFreeSpinMode);
        if (baseImage != null && baseImage.sprite != correctSprite)
        {
            // Debug.LogWarning($"[Symbol] Sprite mismatch detected, correcting: {baseImage.sprite?.name} -> {correctSprite?.name}");
            baseImage.sprite = correctSprite;
        }
    }
    // Set symbol data (for transformations) - ENHANCED for centralized transformation
    public void SetSymbolData(SymbolData newData)
    {
        if (Data == newData) return; // No change needed

        SymbolData oldData = Data;
        Data = newData;

        // Debug.Log($"[Symbol] === SYMBOL TRANSFORMATION === {gameObject.name}: {oldData?.type} -> {newData.type}");

        // Update sprites for new symbol type
        UpdateVisualSprites();

        // Update size for new symbol type
        ValidateSymbolSize();

        // Trigger transformation event
        OnSymbolTransformed?.Invoke(this);
    }
    public void ActivateWinHighlight()
    {
        if (winHighlightImage != null)
            winHighlightImage.enabled = true;
    }

    public void DeactivateWinHighlight()
    {
        if (winHighlightImage != null)
            winHighlightImage.gameObject.SetActive(true);
        
    }

    // Call this method to trigger the win animation on the symbol.
    public void PlayWinAnimation()
    {
        isAnimating = true;
        OnWinAnimationStarted?.Invoke(this);

        switch (Data.type)
        {
            case SymbolData.SymbolType.Wild:
                Debug.LogError("calling");
                PlaySpecialAnimation("Wild");
                break;
            case SymbolData.SymbolType.FreeSpin:
                winHighlightImage.gameObject.SetActive(true);
                winHighlightImage.enabled = true;
                winHighlightImage.sprite = Data.winHighlightSprite;
                PlaySpecialAnimation("BonusWin_Mahjong");
                break;
            case SymbolData.SymbolType.RedDragon:
            case SymbolData.SymbolType.WhiteDragon:
            case SymbolData.SymbolType.BlackDragon:
            case SymbolData.SymbolType.Queen:
            case SymbolData.SymbolType.King:
            case SymbolData.SymbolType.Jack:
            case SymbolData.SymbolType.Ten:
            case SymbolData.SymbolType.GreenDragon:
                PlayBaseAnimation();
                break;
        }
    }

    private void PlaySpecialAnimation(string symbolType)
    {
        // Debug.Log($"[Symbol] === PLAYING SPECIAL ANIMATION === {symbolType} on {gameObject.name}");

        // For other animations, use the original behavior
        baseImage.enabled = false;
        animImage.enabled = true;

        // Ensure the animation image has the correct sprite from Data
        if (Data != null)
        {
            Sprite correctSprite = Data.GetCurrentSprite(isFreeSpinMode);
            if (animImage != null && correctSprite != null)
            {
                animImage.sprite = correctSprite;
            }
        }

        // Reset and stop base animator
        if (baseAnimator != null)
        {
            // Debug.Log($"[Symbol] Resetting base animator for {gameObject.name}");
            baseAnimator.ResetTrigger("PlayWin");
            baseAnimator.Play("Idle", 0, 0);
        }
        else
        {
            // Debug.LogWarning($"[Symbol] Base animator is null for {gameObject.name}");
        }

        // Play the appropriate special animation
        if (specialAnimator != null)
        {
            // Debug.Log($"[Symbol] Special animator found for {gameObject.name}");

            switch (symbolType)
            {

                case "Wild":
                    // Debug.Log($"[Symbol] Playing {symbolType} animation on {gameObject.name}");
                    specialAnimator.SetTrigger("PlayWild");
                    break;
                case "BonusWin_Mahjong":
                    Debug.LogError("calling2");
                    specialAnimator.SetTrigger("PlayFreeSpin");
                    // Debug.Log($"[Symbol] Playing {symbolType} animation on {gameObject.name}");
                    break;
            }
        }
        else
        {
            // Debug.LogError($"[Symbol] Special animator is NULL for {gameObject.name}! Cannot play {symbolType} animation.");
        }
    }

    private IEnumerator CheckAnimatorTriggerNextFrame()
    {
        yield return null; // Wait one frame

        if (specialAnimator != null)
        {
            AnimatorStateInfo afterState = specialAnimator.GetCurrentAnimatorStateInfo(0);
            // Debug.Log($"[Symbol] Animator state after trigger (next frame): {afterState.shortNameHash} (normalized time: {afterState.normalizedTime})");

            // Check if animation is playing
            if (afterState.normalizedTime > 0 || !afterState.IsName("Idle"))
            {
                // Debug.Log($"[Symbol] Animation successfully started on {gameObject.name}");
            }
            else
            {
                // Debug.LogWarning($"[Symbol] Animation may not have started on {gameObject.name}");
            }
        }
    }

    // Add this method for compatibility
    public void PlaySpecialAnimation(SymbolData.SymbolType animationType)
    {
        switch (animationType)
        {
            case SymbolData.SymbolType.FreeSpin:
                PlaySpecialAnimation("BonusWin_Mahjong");
                break;
            case SymbolData.SymbolType.Wild:
                PlaySpecialAnimation("Wild");
                break;
            default:
                if (Data.IsMaskSymbol() && isFreeSpinMode)
                    specialAnimator.Play("GreenDragonSwitch");
                break;
        }
    }

    // PUBLIC: Method for FreeSpinManager to trigger mask transformation animation
    public void PlayMaskTransformAnimation()
    {
        if (Data == null || !Data.IsMaskSymbol() || !isFreeSpinMode)
        {
            // Debug.LogWarning($"[Symbol] Cannot play mask transform - not a mask or not in free spin mode");
            return;
        }

        // Debug.Log($"[Symbol] Playing mask transformation animation on {gameObject.name} ({Data.type})");

        // Ensure we're in clean state
        ResetAnimation();

        // Play special transformation animation
        PlaySpecialAnimation("MaskTransform");
    }

    private void PlayBaseAnimation()
    {
        // Use the base image
        baseImage.enabled = true;
        animImage.enabled = false;

        // Reset and stop special animator
        if (specialAnimator != null)
        {
            specialAnimator.ResetTrigger("PlayWild");
            specialAnimator.ResetTrigger("PlayFreeSpin");
            specialAnimator.ResetTrigger("PlayMaskReel");
            specialAnimator.Play("GreenDragonSwitch");
            specialAnimator.Play("Idle", 0, 0);
        }

        // Play the standard win animation
        if (baseAnimator != null)
        {
            // Debug.Log($"Playing standard win animation for {Data.type}");
            baseAnimator.SetTrigger(PlayWin);
        }
    }

    // Reset the symbol back to normal state - ENHANCED to prevent stuck animations
    public void ResetAnimation()
    {
        isAnimating = false;
        isInRevealSequence = false;

        // Reset base animator
        if (baseAnimator != null && baseAnimator.gameObject.activeInHierarchy)
        {
            baseAnimator.ResetTrigger("PlayWin");
            baseAnimator.Play("Idle", 0, 0f);
        }

        // Reset special animator
        if (specialAnimator != null && specialAnimator.gameObject.activeInHierarchy)
        {
            specialAnimator.ResetTrigger("PlayWild");
            specialAnimator.ResetTrigger("PlayFreeSpin");
            // specialAnimator.ResetTrigger("PlayMaskReel");
            // specialAnimator.Play("GreenDragonSwitch");
            specialAnimator.Play("Idle", 0, 0f);
        }

        // Set correct image visibility
        if (baseImage != null) baseImage.enabled = true;
        if (animImage != null) animImage.enabled = false;

        // Turn off win highlight
        DeactivateWinHighlight();

        OnWinAnimationEnded?.Invoke(this);
    }

    // This method can be called from animation events when the animation completes
    public void OnAnimationComplete()
    {
        ResetAnimation();
    }

    // Check if the symbol is currently animating
    public bool IsAnimating()
    {
        // If we set the flag manually, use that
        if (isAnimating || isInRevealSequence) return true;

        // Check if base animator is playing a non-idle animation
        if (baseAnimator != null && baseAnimator.gameObject.activeSelf)
        {
            AnimatorStateInfo stateInfo = baseAnimator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Idle") && stateInfo.normalizedTime < 1.0f)
            {
                return true;
            }
        }

        // Check if special animator is playing a non-idle animation
        if (specialAnimator != null && specialAnimator.gameObject.activeSelf)
        {
            AnimatorStateInfo stateInfo = specialAnimator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Idle") && stateInfo.normalizedTime < 1.0f)
            {
                return true;
            }
        }

        // Check if win highlight is active
        if (winHighlightImage != null && winHighlightImage.enabled)
        {
            return true;
        }

        // Check if animation image is active (which would indicate special animation)
        if (animImage != null && animImage.enabled)
        {
            return true;
        }

        return false;
    }

    // ==========================================
    // ANIMATION EVENT METHODS (called from Animator)
    // ==========================================

    // Called when target mask preview animation completes  
    public void OnTargetMaskPreviewComplete()
    {
        // Debug.Log("[Symbol] Target mask preview animation completed");
        // Ready for final transformation
        // FreeSpinManager coordinates the final transformation
    }

    // Called when final transformation animation completes
    public void OnMaskTransformationComplete()
    {
        // Debug.Log($"[Symbol] === ANIMATION EVENT === OnMaskTransformationComplete called on {gameObject.name} at {Time.time}");
        // NOTE: Transformation is now handled centrally by FreeSpinManager, not via animation events
    }

    // ==========================================
    // UTILITY METHODS
    // ==========================================

    // Utility methods
    public bool IsMaskSymbol()
    {
        return Data != null && Data.IsMaskSymbol();
    }

    public bool CanTransform()
    {
        return Data != null &&
               Data.IsMaskSymbol() &&
               Data.canTransformInFreeSpins &&
               isFreeSpinMode;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public void SetAlpha(float alpha)
    {
        if (baseImage != null)
        {
            Color color = baseImage.color;
            color.a = alpha;
            baseImage.color = color;
        }
    }

    public bool IsFreeSpinMode => isFreeSpinMode;
    public bool IsInRevealSequence => isInRevealSequence;

    // Method to customize mask symbol size
    public void SetMaskSymbolSize(Vector2 newMaskSize)
    {
        maskSize = newMaskSize;
        if (Data != null && Data.IsMaskSymbol())
        {
            ValidateSymbolSize(); // Update size immediately if this is a mask symbol
        }
    }

    // Get current mask size
    public Vector2 GetMaskSymbolSize()
    {
        return maskSize;
    }

    // Context menu for testing
    [ContextMenu("Test Win Animation")]
    public void TestWinAnimation()
    {
        PlayWinAnimation();
    }

    [ContextMenu("Test Free Spin Mode Toggle")]
    public void TestFreSpinModeToggle()
    {
        UpdateVisualForFreeSpins(!isFreeSpinMode);
    }

    [ContextMenu("Validate Symbol Size")]
    public void TestValidateSize()
    {
        ValidateSymbolSize();
        // Debug.Log($"[Symbol] Current size: {baseImage.rectTransform.sizeDelta}, Target size: {GetTargetSize()}");
    }

    [ContextMenu("Force Update Visual Sprites")]
    public void TestForceUpdateVisualSprites()
    {
        UpdateVisualSprites();
        // Debug.Log($"[Symbol] Forced visual sprite update - Free spin mode: {isFreeSpinMode}, Symbol: {Data?.type}");
    }
}