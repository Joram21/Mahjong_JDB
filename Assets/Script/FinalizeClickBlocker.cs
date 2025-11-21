using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

// This component turns any UI image into a clickblocker that can finalize animations
public class FinalizeClickBlocker : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private BetManager betManager;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private Color activeColor = new Color(0, 0, 0, 0.0f); // Semi-transparent black

    private Image blockImage;
    private bool isActive = false;
    private bool inFreeSpinMode = false;

    public event Action OnAnimationFinalized;

    private void Awake()
    {
        // Get or add Image component
        blockImage = GetComponent<Image>();
        if (blockImage == null)
        {
            blockImage = gameObject.AddComponent<Image>();
        }

        // Initialize as invisible
        Color initialColor = activeColor;
        initialColor.a = 0;
        blockImage.color = initialColor;
        blockImage.raycastTarget = false; // Disable interaction initially
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // Find references if not assigned
        if (betManager == null)
            betManager = FindAnyObjectByType<BetManager>();

        // Subscribe to relevant events
        if (betManager != null)
            betManager.OnWinAdded += CheckActivateForWin;
    }

    // Activate the clickblocker for win animation
    public void ActivateForWin(bool activate)
    {
        inFreeSpinMode = false;
        if (activate)
        {
            gameObject.SetActive(true);
            StartFadeIn();
        }
        else
        {
            StartFadeOut();
        }
    }

    // Activate the clickblocker for freespin animation
    public void ActivateForFreeSpin(bool activate)
    {
        inFreeSpinMode = true;
        if (activate)
        {
            gameObject.SetActive(true);
            StartFadeIn();
        }
        else
        {
            StartFadeOut();
        }
    }

    private void CheckActivateForWin(float amount)
    {
        // Only activate for significant wins
        if (amount > 0)
        {
            ActivateForWin(true);
        }
    }

    private void StartFadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeImage(0, activeColor.a, fadeInDuration, () => {
            isActive = true;
            blockImage.raycastTarget = true; // Enable interaction
        }));
    }

    private void StartFadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeImage(blockImage.color.a, 0, fadeOutDuration, () => {
            isActive = false;
            blockImage.raycastTarget = false; // Disable interaction
            gameObject.SetActive(false);
        }));
    }

    private System.Collections.IEnumerator FadeImage(float startAlpha, float endAlpha, float duration, Action onComplete = null)
    {
        float elapsed = 0;
        Color color = blockImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            blockImage.color = color;
            yield return null;
        }

        // Ensure final alpha is exact
        color.a = endAlpha;
        blockImage.color = color;

        onComplete?.Invoke();
    }

    // Handle click/tap events
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActive) return;

        if (inFreeSpinMode && FreeSpinManager.Instance != null)
        {
            FinalizeFreespinAnimation();
        }
        // Stop celebrations
        WinCalculator.Instance.StopCelebration();
        if (betManager != null && (betManager.IsAnimatingWin || !WinCalculator.Instance.IsCelebrationNull()))
        {
            FinalizeWinAnimation();
        }
        
        SoundManager.Instance.StopBigWinLoop();
        SoundManager.Instance.StopSmallWinLoop();
        SoundManager.Instance.StopPlayFreeWinsLoop();
        
        // Notify subscribers that animation was finalized
        OnAnimationFinalized?.Invoke();

        // Start fade out
        StartFadeOut();
    }

    private void FinalizeWinAnimation()
    {
        // Check if we're in free spin mode
        bool inFreeSpins = FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive;

        // Call the finalize method in BetManager
        betManager.FinalizeWinAnimation(inFreeSpins);
        
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
            FreeSpinManager.Instance.ResetSymbolsAndClearPayouts();
    }

    private void FinalizeFreespinAnimation()
    {
        // Reset the accumulated win display if needed
        if (FreeSpinManager.Instance != null)
        {
            // Store this value so we can reference it later
            float accumulatedWinAmount = 0f;

            // Instead of calling FinalizeWinAnimation which adds to balance,
            // just stop the animations without adding to balance
            if (betManager != null && betManager.IsAnimatingWin)
            {
                // Stop any ongoing win animations without adding to balance
                if (betManager.winAnimationCoroutine != null)
                {
                    betManager.StopCoroutine(betManager.winAnimationCoroutine);
                    betManager.winAnimationCoroutine = null;
                }

                // Update the UI to show the final amount without adding to balance
                betManager.SetFreeSpinWinDisplay(betManager.targetWinAmount);
            }

            // Resume the free spin sequence if it was paused
            if (SlotMachineController.Instance != null && FreeSpinManager.Instance.RemainingFreeSpins > 0)
            {
                SlotMachineController.Instance.StartFreeSpin();
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (betManager != null)
            betManager.OnWinAdded -= CheckActivateForWin;
    }
}