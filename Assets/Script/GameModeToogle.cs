using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameModeToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Toggle modeToggle;
    [SerializeField] private TextMeshProUGUI toggleLabel;
    [SerializeField] private TextMeshProUGUI modeStatusText;

    [Header("Settings")]
    [SerializeField] private Color demoModeColor = new Color(1f, 0f, 0f);   // Red
    [SerializeField] private Color serverModeColor = new Color(0f, 1f, 0f); // Green
    [SerializeField] private bool startInDemoMode = true;

    private void Start()
    {
        // Ensure references are set
        if (WebManAPI.Instance == null)
        {
                // Debug.LogError("WebManAPI reference not found!");
                enabled = false;
                return;
        }

        // Initialize the toggle state
        modeToggle.isOn = startInDemoMode;

        // Apply the initial mode
        UpdateGameMode(startInDemoMode);

        // Add listener for future changes
        modeToggle.onValueChanged.AddListener(UpdateGameMode);
    }

    public void UpdateGameMode(bool isDemoMode)
    {
        // Update the WebManAPI
        //WebManAPI.Instance.SetDemoMode(isDemoMode);

        // Update UI elements
        UpdateUIForCurrentMode(isDemoMode);

        // Log the mode change
        // Debug.Log($"Game mode changed to: {(isDemoMode ? "Demo Mode" : "Server Mode")}");
    }

    private void UpdateUIForCurrentMode(bool isDemoMode)
    {
        if (toggleLabel != null)
        {
            toggleLabel.text = isDemoMode ? "Demo Mode" : "Server Mode";
            toggleLabel.color = isDemoMode ? demoModeColor : serverModeColor;
        }

        if (modeStatusText != null)
        {
            modeStatusText.text = isDemoMode ?
                "Demo Mode: Using simulated balance" :
                "Server Mode: Connected to live server";
            modeStatusText.color = isDemoMode ? demoModeColor : serverModeColor;
        }
    }

    // This can be called from a button if you want to toggle via button instead of toggle
    public void ToggleGameMode()
    {
        modeToggle.isOn = !modeToggle.isOn;
    }
}