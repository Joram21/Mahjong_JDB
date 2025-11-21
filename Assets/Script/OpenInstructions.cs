using UnityEngine;

[System.Serializable]
public class InstructionPanelSet
{
    public GameObject landscape; // Used for both landscape and tablet
    public GameObject portrait;

}

public class OpenInstructions : MonoBehaviour
{
    [SerializeField] private InstructionPanelSet instructionPanels;
    [SerializeField] private ViewMan viewManager;
    [SerializeField] private UIElementManager uiElements;
    public GameObject mainFeatureButton;
    private ScreenCategory currentScreenCategory;

    private void Start()
    {
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

    public void OpenInstructionsPage()
    {
        SetAllPanelsInactive();

        GameObject panel = GetCurrentPanel();
        if (panel != null)
        {
            panel.SetActive(true);
            if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
            {
                uiElements.instructionButton.landscape.interactable = false;
                uiElements.instructionButton.portrait.interactable = false;
            }
            else
            {
                uiElements.instructionButton.landscape.interactable = true;
                uiElements.instructionButton.portrait.interactable = true;
            }
            mainFeatureButton.SetActive(false);
        }
    }
    public void playSound()
    {
        SoundManager.Instance.PlaySound("button");
    }
    public void CloseInstructionsPage()
    {
        SoundManager.Instance.PlaySound("button");
        SetAllPanelsInactive();

    }

    private void UpdatePanelsBasedOnOrientation()
    {
        // Ensure only the current panel is visible if open
        if (instructionPanels.landscape.activeSelf || instructionPanels.portrait.activeSelf)
        {
            OpenInstructionsPage(); // This will reopen the correct one
        }
    }

    private GameObject GetCurrentPanel()
    {
        switch (currentScreenCategory)
        {
            case ScreenCategory.Landscape:
            case ScreenCategory.Tablet:
                return instructionPanels.landscape;
            case ScreenCategory.Portrait:
                return instructionPanels.portrait;
            default:
                return null;
        }
    }

    private void SetAllPanelsInactive()
    {
        if (instructionPanels.landscape != null)
            instructionPanels.landscape.SetActive(false);

        if (instructionPanels.portrait != null)
            instructionPanels.portrait.SetActive(false);
    }
}
