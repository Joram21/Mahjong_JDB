using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class FreeSpinStarterClickBlocker : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private FreeSpinManager freeSpinManager;
    [SerializeField] private Image clickBlockerImage;

    [Header("Visual Feedback")]
    [SerializeField] private bool showClickFeedback = true;

    // Simple state - no complex flags
    private bool canClick = true;

    // Events
    public System.Action OnFreeSpinStarted;

    private void Awake()
    {
        ValidateComponents();
        SetupClickBlocker();
    }

    private void ValidateComponents()
    {
        if (freeSpinManager == null)
        {
            freeSpinManager = FreeSpinManager.Instance;
        }

        if (clickBlockerImage == null)
        {
            clickBlockerImage = GetComponent<Image>();
            if (clickBlockerImage == null)
            {
                clickBlockerImage = gameObject.AddComponent<Image>();
            }
        }
    }

    private void SetupClickBlocker()
    {
        // Setup the click blocker image
        if (clickBlockerImage != null)
        {
            clickBlockerImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            clickBlockerImage.raycastTarget = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick) return;

        // Debug.Log("[FreeSpinStarterClickBlocker] Clicked - starting free spins...");

        // Disable further clicks and hide immediately (like MaskReel does)
        canClick = false;
        gameObject.SetActive(false);

        // Just tell the manager to start - no coroutines from click handler
        if (freeSpinManager != null)
        {
            freeSpinManager.StartFreeSpinsFromClick(); // Simple method call
        }

        OnFreeSpinStarted?.Invoke();
    }

    // ADD THESE PUBLIC METHODS FOR STATE MANAGEMENT:

    /// <summary>
    /// Resets the click blocker for a new freespin session
    /// </summary>
    public void ResetForNewSession()
    {
        canClick = true;
        gameObject.SetActive(false); // Hidden until needed
        // Debug.Log("[FreeSpinStarterClickBlocker] Reset for new freespin session");
    }

    /// <summary>
    /// Enables clicking and shows the blocker
    /// </summary>
    public void EnableClickBlocker()
    {
        canClick = true;
        gameObject.SetActive(true);
        // Debug.Log("[FreeSpinStarterClickBlocker] Click blocker enabled");
    }

    /// <summary>
    /// Disables clicking and hides the blocker
    /// </summary>
    public void DisableClickBlocker()
    {
        canClick = false;
        gameObject.SetActive(false);
        // Debug.Log("[FreeSpinStarterClickBlocker] Click blocker disabled");
    }

    /// <summary>
    /// Check if clicking is currently allowed
    /// </summary>
    public bool CanClick => canClick;

    private IEnumerator ShowClickFeedback()
    {
        if (clickBlockerImage == null) yield break;

        Color originalColor = clickBlockerImage.color;
        clickBlockerImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f); // Green flash
        yield return new WaitForSeconds(0.2f);
        clickBlockerImage.color = originalColor;
    }

    // Context menu for testing
    [ContextMenu("Test Activate")]
    private void TestActivate()
    {
        EnableClickBlocker();
        // Debug.Log("[FreeSpinStarterClickBlocker] Test activated");
    }
}
