using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PromptManager : MonoBehaviour
{
    // Singleton pattern
    private static PromptManager _instance;
    public static PromptManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PromptManager>();
                if (_instance == null)
                {
                    // Debug.LogError("No PromptManager instance found in scene!");
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject errorPromptPanel;
    [SerializeField] private GameObject closeGamePanel;
    [SerializeField] private TextMeshProUGUI errorHeadingText;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private TextMeshProUGUI errorCodeText;
    [SerializeField] private TextMeshProUGUI betIdText; // NEW: Display bet ID
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeGameConfirmButton;

    [Header("Default Messages")]
    [SerializeField] private string defaultHeading = "Error";
    [SerializeField] private string closeGameMessage = "Please close your game and try again later.";

    // Error state tracking
    private bool isErrorActive = false;
    private string currentErrorType = "";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializeUI();
    }

    private void InitializeUI()
    {
        // Setup button listeners
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonPressed);
        }

        if (closeGameConfirmButton != null)
        {
            closeGameConfirmButton.onClick.AddListener(OnCloseGameConfirmed);
        }

        // Hide panels initially
        if (errorPromptPanel != null)
            errorPromptPanel.SetActive(false);

        if (closeGamePanel != null)
            closeGamePanel.SetActive(false);
    }

    #region Public Error Display Methods

    /// <summary>
    /// Show a network error prompt
    /// </summary>
    public void ShowNetworkError(string errorMessage, string errorCode = "", string betId = "")
    {
        ShowError("Network Error", errorMessage, errorCode, "NETWORK", betId);
    }

    /// <summary>
    /// Show an insufficient funds error
    /// </summary>
    public void ShowInsufficientFundsError(string errorCode = "", string betId = "")
    {
        ShowError("Insufficient Funds", "You do not have enough balance to place this bet.", errorCode, "INSUFFICIENT_FUNDS", betId);
    }

    /// <summary>
    /// Show an invalid game ID error
    /// </summary>
    public void ShowInvalidGameIdError(string errorCode = "", string betId = "")
    {
        ShowError("Invalid Game", "Game ID is invalid or expired.", errorCode, "INVALID_GAME_ID", betId);
    }

    /// <summary>
    /// Show an invalid player ID error
    /// </summary>
    public void ShowInvalidPlayerIdError(string errorCode = "", string betId = "")
    {
        ShowError("Invalid Player", "Player ID is invalid or not found.", errorCode, "INVALID_PLAYER_ID", betId);
    }

    /// <summary>
    /// Show a bet placement error
    /// </summary>
    public void ShowBetPlacementError(string errorMessage, string errorCode = "", string betId = "")
    {
        ShowError("Bet Error", errorMessage, errorCode, "BET_ERROR", betId);
    }

    /// <summary>
    /// Show an API timeout error
    /// </summary>
    public void ShowTimeoutError(string errorCode = "", string betId = "")
    {
        ShowError("Connection Timeout", "Request timed out. Please check your connection.", errorCode, "TIMEOUT", betId);
    }

    /// <summary>
    /// Show a server error
    /// </summary>
    public void ShowServerError(string errorMessage, string errorCode = "", string betId = "")
    {
        ShowError("Server Error", errorMessage, errorCode, "SERVER_ERROR", betId);
    }

    /// <summary>
    /// Show an authentication error
    /// </summary>
    public void ShowAuthenticationError(string errorCode = "", string betId = "")
    {
        ShowError("Authentication Failed", "Your session has expired. Please login again.", errorCode, "AUTH_ERROR", betId);
    }

    /// <summary>
    /// Show a generic API error
    /// </summary>
    public void ShowApiError(string errorMessage, string errorCode = "", string betId = "")
    {
        ShowError("API Error", errorMessage, errorCode, "API_ERROR", betId);
    }

    /// <summary>
    /// Show a configuration error
    /// </summary>
    public void ShowConfigurationError(string errorMessage, string errorCode = "", string betId = "")
    {
        ShowError("Configuration Error", errorMessage, errorCode, "CONFIG_ERROR", betId);
    }

    #endregion

    #region Private Core Methods

    /// <summary>
    /// Core method to show any error with dynamic content
    /// </summary>
    private void ShowError(string heading, string message, string errorCode, string errorType, string betId = "")
    {
        if (isErrorActive)
        {
            // Debug.LogWarning($"[PromptManager] Error already active, ignoring new error: {errorType}");
            return;
        }

        isErrorActive = true;
        currentErrorType = errorType;

        // Set text content
        if (errorHeadingText != null)
            errorHeadingText.text = heading;

        if (errorMessageText != null)
            errorMessageText.text = message;

        if (errorCodeText != null)
        {
            if (!string.IsNullOrEmpty(errorCode))
            {
                errorCodeText.text = $"Error Code: {errorCode}";
                errorCodeText.gameObject.SetActive(true);
            }
            else
            {
                errorCodeText.gameObject.SetActive(false);
            }
        }

        // NEW: Display bet ID if available
        if (betIdText != null)
        {
            if (!string.IsNullOrEmpty(betId))
            {
                betIdText.text = $"Bet ID: {betId}";
                betIdText.gameObject.SetActive(true);
            }
            else
            {
                betIdText.gameObject.SetActive(false);
            }
        }

        // Show the error panel
        if (errorPromptPanel != null)
        {
            errorPromptPanel.SetActive(true);
        }

        // Pause the game during error display
        PauseGameplay();

        // Debug.Log($"[PromptManager] Showing error: {heading} | {message} | Code: {errorCode} | BetID: {betId} | Type: {errorType}");
    }

    private void OnCancelButtonPressed()
    {
        // Debug.Log("[PromptManager] Cancel button pressed - showing close game prompt");

        // Hide error prompt
        if (errorPromptPanel != null)
            errorPromptPanel.SetActive(false);

        // Show close game panel
        if (closeGamePanel != null)
            closeGamePanel.SetActive(true);
    }

    private void OnCloseGameConfirmed()
    {
        // Debug.Log("[PromptManager] Close game confirmed - attempting to close application");

        // Try different methods to close the game
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            Application.OpenURL("javascript:window.close();");
#else
            Application.Quit();
#endif
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Pause gameplay when error is shown
    /// </summary>
    private void PauseGameplay()
    {
        // Disable game input/buttons
        if (SlotMachineController.Instance != null)
        {
            // Disable spin buttons and other controls
            var uiElements = SlotMachineController.Instance.uiElements;
            if (uiElements != null)
            {
                DisableUIButtons(uiElements);
            }
        }

        if (FreeSpinSlotMachineController.Instance != null)
        {
            var uiElements = FreeSpinSlotMachineController.Instance.uiElements;
            if (uiElements != null)
            {
                DisableUIButtons(uiElements);
            }
        }

        // Stop auto-spin if active
        if (AutoSpinManager.Instance != null)
        {
            AutoSpinManager.Instance.ClearAutoSpin();
        }
    }

    private void DisableUIButtons(UIElementManager uiElements)
    {
        // Disable all interactive buttons during error state
        if (uiElements.mainBetButton.landscape != null)
            uiElements.mainBetButton.landscape.interactable = false;
        if (uiElements.mainBetButton.portrait != null)
            uiElements.mainBetButton.portrait.interactable = false;

        if (uiElements.autoSpinButton.landscape != null)
            uiElements.autoSpinButton.landscape.interactable = false;
        if (uiElements.autoSpinButton.portrait != null)
            uiElements.autoSpinButton.portrait.interactable = false;

        if (uiElements.increaseButton.landscape != null)
            uiElements.increaseButton.landscape.interactable = false;
        if (uiElements.increaseButton.portrait != null)
            uiElements.increaseButton.portrait.interactable = false;

        if (uiElements.decreaseButton.landscape != null)
            uiElements.decreaseButton.landscape.interactable = false;
        if (uiElements.decreaseButton.portrait != null)
            uiElements.decreaseButton.portrait.interactable = false;
    }

    /// <summary>
    /// Clear current error state (for internal use only)
    /// </summary>
    public void ClearErrorState()
    {
        isErrorActive = false;
        currentErrorType = "";

        if (errorPromptPanel != null)
            errorPromptPanel.SetActive(false);

        if (closeGamePanel != null)
            closeGamePanel.SetActive(false);
    }

    /// <summary>
    /// Check if an error is currently being displayed
    /// </summary>
    public bool IsErrorActive => isErrorActive;

    /// <summary>
    /// Get the current error type
    /// </summary>
    public string CurrentErrorType => currentErrorType;

    #endregion

    #region Utility Methods

    /// <summary>
    /// Parse common HTTP status codes to user-friendly messages
    /// </summary>
    public void ShowHttpError(long statusCode, string responseText = "", string betId = "")
    {
        string heading = "Connection Error";
        string message = "An error occurred while communicating with the server.";
        string errorCode = statusCode.ToString();

        switch (statusCode)
        {
            case 400:
                heading = "Bad Request";
                message = "Invalid request sent to server.";
                break;
            case 401:
                heading = "Unauthorized";
                message = "Your session has expired. Please login again.";
                break;
            case 403:
                heading = "Access Denied";
                message = "You don't have permission to perform this action.";
                break;
            case 404:
                heading = "Service Not Found";
                message = "The requested service is not available.";
                break;
            case 408:
                heading = "Request Timeout";
                message = "Request timed out. Please check your connection.";
                break;
            case 429:
                heading = "Too Many Requests";
                message = "Too many requests sent. Please wait and try again.";
                break;
            case 500:
                heading = "Server Error";
                message = "Internal server error occurred. Please try again later.";
                break;
            case 502:
                heading = "Bad Gateway";
                message = "Server is temporarily unavailable.";
                break;
            case 503:
                heading = "Service Unavailable";
                message = "Service is temporarily unavailable. Please try again later.";
                break;
            case 504:
                heading = "Gateway Timeout";
                message = "Server response timeout. Please try again.";
                break;
            default:
                if (statusCode >= 500)
                {
                    heading = "Server Error";
                    message = "Server error occurred. Please try again later.";
                }
                else if (statusCode >= 400)
                {
                    heading = "Client Error";
                    message = "Request error occurred. Please check your connection.";
                }
                break;
        }

        ShowError(heading, message, errorCode, "HTTP_ERROR", betId);
    }

    #endregion

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}