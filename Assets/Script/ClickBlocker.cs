using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple Click Blocker for Unity
/// This script handles detecting clicks outside a panel to close it
/// </summary>
public class ClickBlocker : MonoBehaviour
{
    [Header("Target to Close")]
    [Tooltip("The GameObject or Canvas to close when clicking outside")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private GameObject mainFeatureButton;

    [Header("Click Blocker Settings")]
    [SerializeField] private Image clickBlockerImage;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float maxAlpha = 0.5f;

    private void Awake()
    {
        targetPanel.SetActive(false);
        if (clickBlockerImage == null)
        {
            // Try to get the image component from this GameObject
            clickBlockerImage = GetComponent<Image>();
        }

        // Make sure we have required components
        if (clickBlockerImage == null)
        {
            return;
        }

        // Initialize click blocker state
        DisableClickBlocker();

        // Add click event listener
        AddTriggerListener(clickBlockerImage.gameObject, EventTriggerType.PointerClick, OnClickAway);
    }

    /// <summary>
    /// Activates the click blocker with fade in effect
    /// </summary>
    public void EnableClickBlocker()
    {
        if (clickBlockerImage != null)
        {
            clickBlockerImage.raycastTarget = true;
            StartCoroutine(FadeIn(clickBlockerImage));
        }
    }

    public void EnableMainFeatureButton()
    {
        if (WebManAPI.Instance.isDemoMode)
            mainFeatureButton.SetActive(true);
        else
            mainFeatureButton.SetActive(false);
    }

    /// <summary>
    /// Deactivates the click blocker
    /// </summary>
    public void DisableClickBlocker()
    {
        if (clickBlockerImage != null && clickBlockerImage.raycastTarget && clickBlockerImage.gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeOut(clickBlockerImage));
        }
        EnableMainFeatureButton();
    }

    /// <summary>
    /// Toggles the panel state and click blocker accordingly
    /// </summary>
    public void TogglePanel()
    {
        if (targetPanel == null)
        {
            return;
        }

        bool newState = !targetPanel.activeSelf;

        if (newState)
        {
            targetPanel.SetActive(true);
            mainFeatureButton.SetActive(false);
            EnableClickBlocker();
        }
        else
        {
            DisableClickBlocker();
            // Panel will be closed after fade out
        }
    }

    /// <summary>
    /// Called when clicking on the blocker background
    /// </summary>
    private void OnClickAway(BaseEventData data)
    {
        if (targetPanel != null && targetPanel.activeSelf)
        {
            targetPanel.SetActive(false);
            mainFeatureButton.SetActive(true);
            DisableClickBlocker();
        }
    }

    /// <summary>
    /// Fade in animation for the click blocker
    /// </summary>
    private IEnumerator FadeIn(Image target)
    {
        float alpha = 0;
        target.color = new Color(0, 0, 0, alpha);

        while (alpha < maxAlpha)
        {
            alpha += Time.deltaTime / fadeDuration;
            target.color = new Color(0, 0, 0, Mathf.Min(alpha, maxAlpha));
            yield return null;
        }
    }

    /// <summary>
    /// Fade out animation for the click blocker
    /// </summary>
    private IEnumerator FadeOut(Image target)
    {
        float alpha = target.color.a;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeDuration;
            target.color = new Color(0, 0, 0, Mathf.Max(0, alpha));
            yield return null;
        }

        target.raycastTarget = false;

        // Close the panel after fade out completes
        if (targetPanel != null && targetPanel.activeSelf)
        {
            targetPanel.SetActive(false);
            mainFeatureButton.SetActive(false);
        }
    }

    /// <summary>
    /// Utility function to add event trigger listeners
    /// </summary>
    private void AddTriggerListener(GameObject obj, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();

        // Check if we already have this event type
        bool hasEventType = false;
        foreach (EventTrigger.Entry entry in trigger.triggers)
        {
            if (entry.eventID == type)
            {
                entry.callback.AddListener(action);
                hasEventType = true;
                break;
            }
        }

        // Add new entry if we don't have this event type
        if (!hasEventType)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }
    }
}
