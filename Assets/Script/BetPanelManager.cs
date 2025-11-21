using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;


public class BetPanelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelSet betPanels;
    [SerializeField] private GameObject mainFeatureButton;
    [SerializeField] private ClickBlockerSet clickBlockers;
    [SerializeField] private UIElementManager uiElement;
    [SerializeField] private ViewMan viewManager;

    private ScreenCategory currentScreenCategory;

    private void Start()
    {
        // Initial state
        SetAllPanelsInactive();
        DisableAllClickBlockers();

        // Setup main button listeners for both orientations
        uiElement.mainBetButton.portrait.onClick.AddListener(() => TogglePanel());
        uiElement.mainBetButton.landscape.onClick.AddListener(() => TogglePanel());

        // Setup click blocker listeners
        AddTriggerListener(clickBlockers.landscape.gameObject, EventTriggerType.PointerClick, OnClickAway);
        AddTriggerListener(clickBlockers.portrait.gameObject, EventTriggerType.PointerClick, OnClickAway);

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
        bool anyPanelActive = betPanels.landscape.activeSelf ||
                              betPanels.portrait.activeSelf;

        if (anyPanelActive)
        {
            SetAllPanelsInactive();
            EnableClickBlockerForCurrentOrientation();

            switch (currentScreenCategory)
            {
                case ScreenCategory.Landscape:
                    betPanels.landscape.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
                case ScreenCategory.Portrait:
                    betPanels.portrait.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
                case ScreenCategory.Tablet:
                    betPanels.landscape.SetActive(true);
                    mainFeatureButton.SetActive(false);
                    break;
            }
        }
    }

    private void SetAllPanelsInactive()
    {
        betPanels.landscape.SetActive(false);
        betPanels.portrait.SetActive(false);
        if(WebManAPI.Instance.isDemoMode)
            mainFeatureButton.SetActive(true);
    }

    private void DisableAllClickBlockers()
    {
        clickBlockers.landscape.raycastTarget = false;
        clickBlockers.portrait.raycastTarget = false;
    }

    private void EnableClickBlockerForCurrentOrientation()
    {
        DisableAllClickBlockers();

        Image targetBlocker = GetCurrentBlocker();
        if (targetBlocker != null)
        {
            targetBlocker.raycastTarget = true;
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
                return betPanels.landscape;
            case ScreenCategory.Portrait:
                return betPanels.portrait;
            case ScreenCategory.Tablet:
                return betPanels.landscape;
            default:
                return null;
        }
    }

    public void TogglePanel()
    {

        GameObject currentPanel = GetCurrentPanel();
        if (currentPanel == null) return;

        bool newState = !currentPanel.activeSelf;

        // Set all panels to the new state
        SetAllPanelsInactive();

        if (newState)
        {
            SoundManager.Instance.PlaySound("Button");
            currentPanel.SetActive(true);
            mainFeatureButton.SetActive(false);
            EnableClickBlockerForCurrentOrientation();
        }
        else
        {
            DisableAllClickBlockers();
        }
    }

    private void OnClickAway(BaseEventData data)
    {
        TogglePanel();
    }

    // Add to your existing BetButton script
    public void OnBetButtonClicked()
    {
        TogglePanel();
        // Your existing bet change logic here
    }

    // Utility function for event triggers
    private void AddTriggerListener(GameObject obj, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}