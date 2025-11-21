using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.UI;
using UnityTimer;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
public class WebManAPI : MonoBehaviour
{
    // Singleton Instance
    private static WebManAPI _instance;

    // Public property to access the singleton instance
    public static WebManAPI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<WebManAPI>();

                if (_instance == null)
                {
                    // Debug.LogError("No WebManAPI instance found in scene. Creating one...");
                    GameObject go = new GameObject("WebManAPI");
                    _instance = go.AddComponent<WebManAPI>();
                    DontDestroyOnLoad(go); // Makes the instance persistent across scenes
                }
            }
            return _instance;
        }
    }

    // Ensure only one instance exists
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Debug.LogWarning("Multiple WebManAPI instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    [Header("API Settings - Fetched from ConfigMan")]
    [SerializeField] private string ServerLink;
    [SerializeField] private string BaseURL = "https://s.api.ibibe.africa"; // Keep this hardcoded if it doesn't change
    [SerializeField] private string playerId;
    [SerializeField] private string gameId;
    [SerializeField] private string clientId;
    public bool isDemoMode;
    public ConfigMan configMan;

    [Header("References")]
    [SerializeField] private BetManager betManager;
    [SerializeField] private SlotGameServer slotGameServer;

    [Header("Manually Assigned Reels")]
    [SerializeField] private SlotReel[] slotReels;

    [Header("Demo Mode Button")]
    [SerializeField] private Button mainFeatureButton;

    // Configuration loaded flag
    private bool configurationLoaded = false;

    // API Response Storage
    private MahjongSpinResponse lastMahjongResponse;
    private string currentBetId;
    private Dictionary<string, SymbolData.SymbolType> symbolMapping;

    [Header("API Configuration")]
    [SerializeField] private string apiAction = "spin";
    [SerializeField] private string apiState = "development";  // or "production"


    // Add after the existing state variables:
    [Header("API Integration")]
    private float apiWinAmount = 0f;
    private bool hasAPIWinAmount = false;
    public double walletBalance;

    public List<(int, int, float)> payLines = new List<(int, int, float)>();
    public List<(int, int, float)> tempPayLines = new List<(int, int, float)>();
    public float tempWin = 0f;
    public float actionDuration = 0.0f;

    #region API Request/Response Classes for New API

    [Serializable]
    public class MahjongSpinRequest
    {
        public float bet_amount;
        public string user_id;
        public string client_id;
        public string game_id;
        public string player_id;
        public string bet_id;
        public string action;              // NEW: "spin", "freespin", etc.
        public string state;               // NEW: "development"
    }

    [Serializable]
    public class MahjongSpinResponse
    {
        public string status;                    // "success" instead of bool success
        public string[][] reels;                 // Direct reels array
        public Payline[] win_lines;              // "win_lines" instead of nested in game_state
        public float total_win;                  // Direct total_win
        public string message;                   // New field
        public bool is_free_spin;                // Direct is_free_spin
        public float multiplier;                 // New field
        public bool has_bonus;                   // New field

        // Computed property for backward compatibility
        public bool success => status == "success";

        // Create a game_state-like object for backward compatibility with existing code
        public GameState game_state => new GameState
        {
            reels = this.reels,
            paylines = this.win_lines,
            total_win = this.total_win,
            is_free_spin = this.is_free_spin
        };
    }

    [Serializable]
    public class GameState
    {
        public string client_id;
        public string user_id;
        public string player_id;
        public string game_id;
        public string bet_id;
        public float bet_amount;
        public string[][] reels;
        public Payline[] paylines;
        public float total_win;
        public bool is_free_spin;
        public int scatter_count;
        public string timestamp;
    }


    [Serializable]
    public class Payline
    {
        public int line_number;
        public string[] symbols;
        public int count;
        public string symbol;
        public float? win_amount;
        public int[][] positions;  // Changed from Positions[] to int[][]

        // Helper property to determine if payline should be displayed
        public bool ShouldShowPayline
        {
            get
            {
                // Show payline if there's a win amount OR if it's a BONUS/FreeSpin symbol
                return (win_amount != null && win_amount > 0) ||
                       symbol == "BONUS" ||
                       symbol == "FreeSpin" ||
                       symbol == "FREE_SPIN";
            }
        }
    }

    [Serializable]
    public class WinDetail
    {
        public string symbol;
        public int count;
        public float win_amount;
        public Position[] positions;
    }

    [Serializable]
    public class WinDetail2
    {
        public string symbol;
        public int count;
        public float win_amount;
        public Position[] positions;
    }

    [Serializable]
    public class Position
    {
        public int reel;
        public int row;
    }

    [Serializable]
    public class MaskReelBonusRequest
    {
        public string client_id;
        public string game_id;
        public string player_id;
        public string bet_id;
        public float bet_amount;
    }

    [Serializable]
    public class MaskReelBonusResponse
    {
        public int multiplier;
        public float win_amount;
    }

    #endregion

    #region Legacy API Classes (for backward compatibility)

    [Serializable]
    private class SpinRequest
    {
        public float bet_amount;
        public bool is_free_spin;
        public int current_free_spin_index;
        public int remaining_free_spins;
        public int total_free_spins_awarded;
        public int free_spin_multiplier;
        public string client_id;
        public string game_id;
        public string player_id;
        public string bet_id;
    }

    [Serializable]
    private class SpinResponse
    {
        public string[][] reels;
        public float win_amount;
        public WinDetail[] win_details;
        public int scatter_count;
        public float scatter_win_amount;
        public Position[] scatter_positions;
        public int free_spin_count;
        public float free_spin_win_amount;
        public Position[] free_spin_positions;
        public bool free_spin_triggered;
        public bool free_spin_retriggered;
        public bool is_free_spin;
        public int remaining_free_spins;
        public int current_free_spin_index;
        public int free_spin_multiplier;
        public int total_free_spins_awarded;
        public float bet_amount;
        public int bet_multiplier;
    }

    [Serializable]
    private class BetResponse
    {
        public int status_code;
        public string message;
        public string bet_id;
        public float amount_won;
        public double new_wallet_balance;
        public string status;
    }

    [Serializable]
    private class PlaceBetRequest
    {
        public string bet_id;
        public string player_id;
        public float amount;
        public string game_id;
        public string client_id;
    }

    [Serializable]
    private class UpdateBetRequest
    {
        public string bet_id;
        public float amount_won;
        public string client_id;
    }

    [Serializable]
    public class PlayerDetailsResponse
    {
        public float wallet_balance;
    }

    #endregion

    private SpinResponse lastSpinResponse;
    private bool isFreeSpinSelectionPending = false;

    void Start()
    {
        InitializeSymbolMapping();
        SetupReels();

        // If demo, no need to wait for ConfigMan
        if (ConfigMan.Instance != null && ConfigMan.Instance.IsDemo)
        {
            isDemoMode = true;
            configurationLoaded = true;
            Debug.LogError("[WebManAPI] Running in DEMO mode - skipping ConfigMan config load");

            // FIX: Properly initialize demo mode for all components
            SetDemoMode(true);

            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnSpinStart += PlaceBetDemo;
                SlotMachineController.Instance.OnSpinComplete += HandleSpinComplete;
                SlotMachineController.Instance.isDemoMode = isDemoMode;
            }

            if (betManager != null)
            {
                betManager.IsDemoMode = true;
                betManager.ResetDemoBalance();
            }

            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.isDemoMode = true;
            }
        }
        else
        {
            // Load configuration from ConfigMan for non-demo
            StartCoroutine(LoadConfigurationFromConfigMan());
            Debug.LogError("[WebManAPI] Waiting for ConfigMan to load configuration...");
        }
        UpdateButtonState();
        slotGameServer.SpinReels(betManager.CurrentBet, false);
    }

    private IEnumerator LoadConfigurationFromConfigMan()
    {
        if (isDemoMode) yield break;

        while (ConfigMan.Instance == null || !ConfigMan.Instance.ReceivedConfigs)
        {
            configMan = ConfigMan.Instance; // Cache the reference
            Debug.Log("[WebManAPI] Waiting for ConfigMan to load configuration 2...");
            if (ConfigMan.Instance == null)
                Debug.LogError("Config not found");
            if (!ConfigMan.Instance.ReceivedConfigs)
                Debug.LogError("Not recieving configs");
            yield return new WaitForSeconds(0.5f);
        }


        LoadConfigFromConfigMan();
        InitializeAPIConnections();
        UpdateButtonState();
        Debug.LogError("[WebManAPI] ConfigMan configuration loaded. Applying settings..."); // Add this to update button state after config load
    }

    private void LoadConfigFromConfigMan()
    {
        if (ConfigMan.Instance != null)
            Debug.LogError("Loading Config");
        {
            ServerLink = ConfigMan.Instance.Base_url;
            playerId = ConfigMan.Instance.PlayerId;
            gameId = ConfigMan.Instance.GameId;
            clientId = ConfigMan.Instance.ClientId;
            isDemoMode = ConfigMan.Instance.IsDemo;

            configurationLoaded = true;
            Debug.LogError($"[WebManAPI] Configuration loaded from ConfigMan:\nServerLink: {ServerLink}\nPlayerId: {playerId}\nGameId: {gameId}\nClientId: {clientId}\nIsDemoMode: {isDemoMode}");
        }
    }

    // Alternative fix: Modify InitializeAPIConnections to handle demo mode properly
    private void InitializeAPIConnections()
    {
        if (isDemoMode)
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnSpinStart += PlaceBetDemo;
                SlotMachineController.Instance.OnSpinComplete += HandleSpinComplete;
                SlotMachineController.Instance.isDemoMode = isDemoMode;
            }

            if (betManager != null)
            {
                betManager.IsDemoMode = true;
                betManager.ResetDemoBalance();
            }

            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.isDemoMode = true;
            }

            return;
        }

        if (!configurationLoaded)
        {
            return;
        }

        // Rest of the method remains the same for server mode...
        if (!isDemoMode)
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnSpinStart += RecordBet;
                SlotMachineController.Instance.OnSpinComplete += HandleSpinComplete;
                SlotMachineController.Instance.isDemoMode = isDemoMode;
            }

            if (betManager != null)
            {
                betManager.OnWinAdded += RecordWin;
                betManager.OnWinAnimationComplete += BetUpdate;
            }

            // Get player details on start
            StartCoroutine(GetPlayerDetails());
            Debug.LogError("Getting player details from server");
        }
    }

    public void SetDemoMode(bool isDemo)
    {
        // Update ConfigMan as well
        if (ConfigMan.Instance != null)
        {
            ConfigMan.Instance.IsDemo = isDemo;
        }

        // First, clean up any existing event subscriptions
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnSpinStart -= RecordBet;
            SlotMachineController.Instance.OnSpinComplete -= HandleSpinComplete;
            SlotMachineController.Instance.isDemoMode = isDemo;
        }

        if (betManager != null)
        {
            betManager.OnWinAdded -= RecordWin;
            betManager.OnWinAnimationComplete -= BetUpdate;
            betManager.IsDemoMode = isDemo;

            if (isDemo)
            {
                betManager.ResetDemoBalance();
            }
        }

        // Set the mode flag
        isDemoMode = isDemo;

        // UPDATE BUTTON STATE WHEN MODE CHANGES
        UpdateButtonState();

        // Re-establish connections for server mode
        if (!isDemo)
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnSpinStart += RecordBet;
                SlotMachineController.Instance.OnSpinComplete += HandleSpinComplete;
            }

            if (betManager != null)
            {
                betManager.OnWinAdded += RecordWin;
                betManager.OnWinAnimationComplete += BetUpdate;
            }

            // Get player details to update balance
            StartCoroutine(GetPlayerDetails());
            Debug.LogError("Getting player details from server to update balance");
        }
        else
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnSpinStart += PlaceBetDemo;
                SlotMachineController.Instance.OnSpinComplete += HandleSpinComplete;
            }

            if (betManager != null)
            {
                betManager.OnWinAdded += RecordWin;
            }
        }

        // Force update UI elements
        if (betManager != null)
        {
            // This will trigger a UI refresh
            betManager.SetBetIndex(BetManager.CurrentBetIndex);
        }
    }

    private void UpdateButtonState()
    {
        if (mainFeatureButton != null)
        {
            mainFeatureButton.interactable = isDemoMode;
            mainFeatureButton.gameObject.SetActive(isDemoMode);
        }
    }

    private void InitializeSymbolMapping()
    {
        symbolMapping = new Dictionary<string, SymbolData.SymbolType>
    {
        // Mahjong API Symbol Names → Your Symbol Types
        {"RED_DRAGON", SymbolData.SymbolType.RedDragon},
        {"GREEN_DRAGON", SymbolData.SymbolType.GreenDragon},
        {"WHITE_DRAGON", SymbolData.SymbolType.WhiteDragon},
        {"WIND_TILES", SymbolData.SymbolType.BlackDragon}, // Map to existing BlackDragon
        {"BLACK_DRAGON", SymbolData.SymbolType.BlackDragon}, // Map to existing BlackDragon

        {"K", SymbolData.SymbolType.King},
        {"Q", SymbolData.SymbolType.Queen},
        {"J", SymbolData.SymbolType.Jack},
        {"10", SymbolData.SymbolType.Ten},
        {"WILD", SymbolData.SymbolType.Wild},
        {"BONUS", SymbolData.SymbolType.FreeSpin }, // BONUS triggers free spins

        {"RedDragon", SymbolData.SymbolType.RedDragon},
        {"GreenDragon", SymbolData.SymbolType.GreenDragon},
        {"WhiteDragon", SymbolData.SymbolType.WhiteDragon},
        {"BlackDragon", SymbolData.SymbolType.BlackDragon},
        {"Wild", SymbolData.SymbolType.Wild },
        {"FreeSpin", SymbolData.SymbolType.FreeSpin}

    };
    }


    private void SetupReels()
    {
        // Ensure we have reels assigned
        if (slotReels == null || slotReels.Length == 0)
        {
            // Debug.LogWarning("[WebManAPI] Slot reels are not assigned in inspector. Attempting to find them automatically.");
            slotReels = FindObjectsByType<SlotReel>(FindObjectsSortMode.None);

            if (slotReels != null && slotReels.Length > 0)
            {
                // Sort by sibling index to maintain proper order
                Array.Sort(slotReels, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            }
        }

        if (slotReels == null || slotReels.Length == 0)
        {
            // Debug.LogError("[WebManAPI] Failed to find any SlotReel components!");
        }
        else
        {
            // Debug.Log($"[WebManAPI] Found {slotReels.Length} slot reels");

            // Validate each reel
            for (int i = 0; i < slotReels.Length; i++)
            {
                if (slotReels[i] == null)
                {
                    // Debug.LogError($"[WebManAPI] Reel at index {i} is null!");
                }
            }
        }
    }

    public IEnumerator GetPlayerDetails()
    {
        if (isDemoMode || !configurationLoaded) yield break;

        // NEW: Check if ConfigMan already has cached balance
        if (ConfigMan.Instance != null && ConfigMan.Instance.BalanceLoaded && ConfigMan.Instance.CachedWalletBalance > 0)
        {
            Debug.Log($"Using cached balance from ConfigMan: {ConfigMan.Instance.CachedWalletBalance}");

            if (betManager != null)
            {
                betManager.SetBalance(ConfigMan.Instance.CachedWalletBalance);
                walletBalance = ConfigMan.Instance.CachedWalletBalance;
                Debug.Log($"Balance set to {walletBalance} from ConfigMan cache");
            }
            yield break; // Don't make another API call
        }

        // Existing API call code for fallback...
        string endpoint = $"{ServerLink}/api/v1/customer/details?customer_id={playerId}";

        using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Player details: {responseText}");

                try
                {
                    PlayerDetailsResponse responseData = JsonUtility.FromJson<PlayerDetailsResponse>(responseText);

                    if (betManager != null && responseData.wallet_balance > 0)
                    {
                        betManager.SetBalance(responseData.wallet_balance);
                        walletBalance = responseData.wallet_balance;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse player details: {e.Message}");
                }
            }

            else if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                // HandleNetworkError(www.error);
                Debug.LogError($"NetworkError: {www.error}");
            }
            else
            {
                // HandleApiError(www.error, endpoint);
                Debug.LogError($"Failed to get player details: {www.error}");

            }
        }
    }
    // private void HandleApiError(string errorMessage, string endpoint = "")
    // {
    //     Debug.LogError($"API Error at {endpoint}: {errorMessage}");

    //     // CRITICAL: Set error state FIRST
    //     hasAPIError = true;
    //     lastAPIErrorMessage = $"API Error: {errorMessage}";

    //     if (PromptMan.Instance != null)
    //     {
    //         PromptMan.Instance.ShowApiError($"Failed to connect to server. Please check your connection and try again.");
    //     }
    // }

    // private void HandleNetworkError(string errorMessage)
    // {
    //     Debug.LogError($"Network Error: {errorMessage}");

    //     // CRITICAL: Set error state FIRST  
    //     hasAPIError = true;
    //     lastAPIErrorMessage = $"Network Error: {errorMessage}";

    //     if (PromptMan.Instance != null)
    //     {
    //         PromptMan.Instance.ShowNetworkError("Unable to reach game servers. Please check your internet connection.");
    //     }
    // }

    public void RecordBet()
    {
        Debug.LogError("Recording Bet to server");
        if (isDemoMode || !configurationLoaded) return;
        StartCoroutine(PlaceBet());
        Debug.LogError("Calling Placebet method");
    }

    private IEnumerator PlaceBet()
    {
        if (!configurationLoaded)
        {
            yield break;
        }

        string endpoint = $"{ServerLink}/api/v1/bet/place_bet";
        Debug.LogError("Serverlink is establishing");


        currentBetId = ConfigMan.Instance != null ? ConfigMan.Instance.GetBetId() : Guid.NewGuid().ToString();

        PlaceBetRequest payload = new PlaceBetRequest
        {
            player_id = playerId,
            game_id = gameId,
            amount = betManager != null ? betManager.CurrentBet : 0.25f,
            bet_id = currentBetId,
            client_id = clientId
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                BetResponse response = JsonUtility.FromJson<BetResponse>(www.downloadHandler.text);

                if (response.status_code == 0)
                {
                    if (betManager != null)
                    {
                        betManager.SetBalance(response.new_wallet_balance);
                    }
                    walletBalance = response.new_wallet_balance;
                    StartCoroutine(FetchSpinResult());
                }
            }
        }
    }

    private TargetSpinController.SpinMode spinMode = TargetSpinController.SpinMode.None;
    public void PlaceFeatureBet(TargetSpinController.SpinMode mode)
    {
        spinMode = mode;
        PlaceBetDemo();
        Timer.Register(1f, () =>
        {
            spinMode = TargetSpinController.SpinMode.None;
        });
    }

    public void PlaceBetDemo()
    {
        StartCoroutine(FetchSpinResultDemo());
        Debug.LogError("Placing Demo Bet");
    }

    public bool useBonus = false;
    private IEnumerator FetchSpinResultDemo()
    {
        Debug.LogError("Calling slot game server");
        ResetCallOnce();
        string result = "";
        if (spinMode == TargetSpinController.SpinMode.None)
        {
            result = slotGameServer.SpinReels(betManager != null ? betManager.CurrentBet : 0.25f, FreeSpinManager.Instance.IsFreeSpinActive);
            Debug.Log("Calling slot game server");
        }
        else
        {
            switch (spinMode)
            {
                case TargetSpinController.SpinMode.WildWins:
                    result = @"{""status"":""success"",""reels"":[
                [""WILD"",""J"",""10""],
                [""WILD"",""GREEN_DRAGON"",""10""],
                [""WILD"",""Q"",""10""],
                [""WILD"",""K"",""K""],
                [""WILD"",""GREEN_DRAGON"",""BLACK_DRAGON""]],
                ""win_lines"":
                [
                {""line_number"":2,""symbols"":[""WILD"",""WILD"",""WILD"",""WILD"",""WILD""],""count"":5,""symbol"":""WILD"",""win_amount"":100.00,""positions"":[[0,0],[1,0],[2,0],[3,0],[4,0]]},
                {""line_number"":3,""symbols"":[""10"",""10"",""10"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.05,""positions"":[[0,2],[1,2],[2,2],[3,2],[4,2]]},
                {""line_number"":4,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""WILD""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,1],[4,0]]},
                {""line_number"":6,""symbols"":[""WILD"",""WILD"",""Q"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.10,""positions"":[[0,0],[1,0],[2,1],[3,2],[4,2]]},
                {""line_number"":10,""symbols"":[""WILD"",""GREEN_DRAGON"",""Q"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,1],[3,1],[4,2]]},
                {""line_number"":14,""symbols"":[""J"",""WILD"",""WILD"",""WILD"",""GREEN_DRAGON""],""count"":4,""symbol"":""WILD"",""win_amount"":0.30,""positions"":[[0,1],[1,0],[2,0],[3,0],[4,1]]},
                {""line_number"":16,""symbols"":[""WILD"",""WILD"",""Q"",""K"",""GREEN_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.10,""positions"":[[0,0],[1,0],[2,1],[3,2],[4,1]]},
                {""line_number"":20,""symbols"":[""WILD"",""WILD"",""WILD"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":5.0,""positions"":[[0,0],[1,0],[2,0],[3,1],[4,2]]},
                {""line_number"":21,""symbols"":[""10"",""10"",""10"",""K"",""WILD""],""count"":3,""symbol"":""WILD"",""win_amount"":0.05,""positions"":[[0,2],[1,2],[2,2],[3,1],[4,0]]},
                {""line_number"":23,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,2],[4,2]]},
                {""line_number"":24,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,1],[4,2]]}
               ],
                ""total_win"":105.68,""is_free_spin"":false,""message"":""Win!"",""multiplier"":1.0,""has_bonus"":false}";

                    break;
                case TargetSpinController.SpinMode.WildWins2:
                    result = @"{""status"":""success"",""reels"":[
                [""WILD"",""J"",""10""],
                [""WILD"",""GREEN_DRAGON"",""10""],
                [""WILD"",""Q"",""10""],
                [""WILD"",""K"",""K""],
                [""WILD"",""GREEN_DRAGON"",""BLACK_DRAGON""]],
                ""win_lines"":
                [
                {""line_number"":2,""symbols"":[""WILD"",""WILD"",""WILD"",""WILD"",""WILD""],""count"":5,""symbol"":""WILD"",""win_amount"":100.00,""positions"":[[0,0],[1,0],[2,0],[3,0],[4,0]]},
                {""line_number"":3,""symbols"":[""10"",""10"",""10"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.05,""positions"":[[0,2],[1,2],[2,2],[3,2],[4,2]]},
                {""line_number"":4,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""WILD""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,1],[4,0]]},
                {""line_number"":6,""symbols"":[""WILD"",""WILD"",""Q"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.10,""positions"":[[0,0],[1,0],[2,1],[3,2],[4,2]]},
                {""line_number"":10,""symbols"":[""WILD"",""GREEN_DRAGON"",""Q"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,1],[3,1],[4,2]]},
                {""line_number"":14,""symbols"":[""J"",""WILD"",""WILD"",""WILD"",""GREEN_DRAGON""],""count"":4,""symbol"":""WILD"",""win_amount"":0.30,""positions"":[[0,1],[1,0],[2,0],[3,0],[4,1]]},
                {""line_number"":16,""symbols"":[""WILD"",""WILD"",""Q"",""K"",""GREEN_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":0.10,""positions"":[[0,0],[1,0],[2,1],[3,2],[4,1]]},
                {""line_number"":20,""symbols"":[""WILD"",""WILD"",""WILD"",""K"",""BLACK_DRAGON""],""count"":3,""symbol"":""WILD"",""win_amount"":5.0,""positions"":[[0,0],[1,0],[2,0],[3,1],[4,2]]},
                {""line_number"":21,""symbols"":[""10"",""10"",""10"",""K"",""WILD""],""count"":3,""symbol"":""WILD"",""win_amount"":0.05,""positions"":[[0,2],[1,2],[2,2],[3,1],[4,0]]},
                {""line_number"":23,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,2],[4,2]]},
                {""line_number"":24,""symbols"":[""WILD"",""GREEN_DRAGON"",""10"",""K"",""BLACK_DRAGON""],""count"":2,""symbol"":""WILD"",""win_amount"":0.02,""positions"":[[0,0],[1,1],[2,2],[3,1],[4,2]]}
               ],
                ""total_win"":105.68,""is_free_spin"":false,""message"":""Win!"",""multiplier"":1.0,""has_bonus"":false}";

                    break;
                case TargetSpinController.SpinMode.BonusWins:
                    result = @"{""status"":""success"",""reels"":[
                [""WHITE_DRAGON"",""K"",""BLACK_DRAGON""],
                [""WILD"",""10"",""WHITE_DRAGON""],
                [""BONUS"",""Q"",""GREEN_DRAGON""],
                [""BONUS"",""BLACK_DRAGON"",""10""],
                [""BONUS"",""Q"",""J""]],
                ""win_lines"":
                [
                {""line_number"":2,""symbols"":[""WHITE_DRAGON"",""WILD"",""BONUS"",""BONUS"",""BONUS""],""symbol"":""BONUS"",""win_amount"":null},
                {""line_number"":22,""symbols"":[""BLACK_DRAGON"",""10"",""BONUS"",""BONUS"",""BONUS""],""symbol"":""BONUS"",""win_amount"":null}
                ],
                ""total_win"":0.0,""is_free_spin"":false,""message"":""Bonus!"",""multiplier"":1.0,""has_bonus"":true}";
                    // NEW: Trigger bonus win sequence after symbols are shown
                    StartCoroutine(HandleBonusWinSequence());

                    break;
            }
        }

        Debug.Log(result);

        lastMahjongResponse = JsonConvert.DeserializeObject<MahjongSpinResponse>(result);
        payLines = ConvertPaylinesToTuples(lastMahjongResponse.win_lines);
        ProcessMahjongSpinResponse(lastMahjongResponse);
        yield return null;
    }
    private IEnumerator HandleBonusWinSequence()
    {
        // Wait for reels to stop and symbols to be shown
        yield return new WaitUntil(() => !SlotMachineController.Instance.IsSpinning);

        // Wait a moment for paylines to be displayed
        yield return new WaitForSeconds(2f);

        // Check if we have bonus symbols
        bool hasBonusWin = CheckForBonusWin();

        if (hasBonusWin)
        {
            // Play bonus win animation
            yield return StartCoroutine(PlayBonusWinAnimation());
            Debug.LogError("Playing bonus win animation");

            // Switch to free spin screen
            yield return StartCoroutine(SwitchToFreeSpinScreen());
        }
    }

    private bool CheckForBonusWin()
    {
        // Check if we have 3 or more bonus symbols on the first 3 reels
        int bonusCount = 0;
        string[][] reels = lastMahjongResponse?.reels;

        if (reels != null)
        {
            for (int reel = 0; reel < 3 && reel < reels.Length; reel++)
            {
                foreach (string symbol in reels[reel])
                {
                    if (symbol == "BONUS")
                    {
                        bonusCount++;
                        break; // Only count one per reel
                    }
                }
            }
        }

        return bonusCount >= 3;
    }

    private IEnumerator PlayBonusWinAnimation()
    {
        // Play freespin/bonus sound
        SoundManager.Instance.PlaySound("freespin");

        // Find and trigger bonus symbol animations
        var bonusSymbols = FindBonusSymbolsOnReels();

        foreach (var symbol in bonusSymbols)
        {
            if (symbol != null)
            {
                // Trigger bonus win animation on the symbol
                symbol.PlaySpecialAnimation(SymbolData.SymbolType.FreeSpin);
                Debug.LogError("[WebManAPI] Triggered bonus symbol animation");
            }
        }

        // Wait for animation to complete
        yield return new WaitForSeconds(3f);

        SoundManager.Instance.StopSound("freespin");
    }

    private List<Symbol> FindBonusSymbolsOnReels()
    {
        List<Symbol> bonusSymbols = new List<Symbol>();

        SlotReel[] activeReels = SlotMachineController.Instance.GetReels();

        for (int reelIndex = 3; reelIndex <= 5 && reelIndex < activeReels.Length; reelIndex++)
        {
            var visibleSymbols = activeReels[reelIndex].GetTopVisibleSymbols(activeReels[reelIndex].visibleSymbols);

            foreach (var symbol in visibleSymbols)
            {
                if (symbol.Data.type == SymbolData.SymbolType.FreeSpin) // Bonus symbols
                {
                    bonusSymbols.Add(symbol);
                }
            }
        }

        return bonusSymbols;
    }

    private IEnumerator SwitchToFreeSpinScreen()
    {
        // Use existing FreeSpinManager to handle the transition
        if (FreeSpinManager.Instance != null)
        {
            // Trigger the free spin sequence
            yield return StartCoroutine(FreeSpinManager.Instance.HandleFreeSpinSequence());
        }
    }


    void ResetCallOnce()
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            if (FreeSpinSlotMachineController.Instance != null)
            {
                FreeSpinSlotMachineController.Instance.callOnce = false;
            }
        }
        else
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.callOnce = false;
            }
        }
    }

    bool once = false;
    private IEnumerator FetchSpinResult()
    {
        if (!configurationLoaded)
        {
            yield break;
        }
        ResetCallOnce();

        string endpoint = $"{BaseURL}/api/mahjong/spin";

        bool isFreeSpinActive = FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;

        MahjongSpinRequest payload = new MahjongSpinRequest
        {
            client_id = clientId,
            player_id = playerId,
            game_id = gameId,
            bet_id = currentBetId,
            bet_amount = betManager != null ? betManager.CurrentBet : 1.0f,
            action = apiAction,                    // NEW
            state = apiState,                      // NEW
            user_id = playerId
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        Debug.Log($"[WebManAPI] Sending spin request: {jsonPayload}");

        using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"[WebManAPI] Mahjong response received: {responseText}");

                try
                {
                    lastMahjongResponse = JsonConvert.DeserializeObject<MahjongSpinResponse>(responseText);

                    if (lastMahjongResponse != null && lastMahjongResponse.success)
                    {
                        Debug.Log("[WebManAPI] Deserialization successful!");
                        once = true;

                        if (lastMahjongResponse.win_lines != null)
                        {
                            payLines = ConvertPaylinesToTuples(lastMahjongResponse.win_lines);
                        }
                        else
                        {
                            Debug.Log("[WebManAPI] No win lines in response");
                            payLines = new List<(int, int, float)>();
                        }

                        ProcessMahjongSpinResponse(lastMahjongResponse);
                    }
                    else
                    {
                        Debug.LogError($"[WebManAPI] API returned failure status: {lastMahjongResponse?.status}");
                        Debug.LogError($"[WebManAPI] Message: {lastMahjongResponse?.message}");
                    }
                }
                catch (Newtonsoft.Json.JsonSerializationException jsonEx)
                {
                    Debug.LogError($"[WebManAPI] JSON serialization error: {jsonEx.Message}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WebManAPI] General error deserializing Mahjong response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[WebManAPI] Mahjong spin request failed: {www.error}");
                Debug.LogError($"[WebManAPI] Response Code: {www.responseCode}");
            }
        }
    }



    // New method for processing Winning Mask API responses
    private void ProcessMahjongSpinResponse(MahjongSpinResponse response)
    {
        if (response == null)
        {
            // Debug.LogError("[WebManAPI] Response is null!");
            return;
        }

        // Use reels for initial display
        if (response.reels != null && response.reels.Length > 0)
        {
            Debug.Log($"[WebManAPI] Processing {response.reels.Length} reels from API response");
            ProcessReelsData(response.reels);
        }
        else
        {
            // Debug.LogError("[WebManAPI] reels data is null!");
            return;
        }

        // Set win amounts
        if (betManager != null)
        {
            betManager.targetWinAmount = response.total_win;
        }
        if (response.is_free_spin && CheckForBonusWin())
        {
            // Don't start the sequence immediately - let the reels finish first
            // The HandleBonusWinSequence will be triggered from the BonusWins case
        }
    }


    private void ProcessReelsData(string[][] reels)
    {
        // Validate inputs
        if (reels == null)
        {
            // Debug.LogError("[WebManAPI] Reels data is null!");
            return;
        }

        SlotReel[] targetReels = null;

        // Check if free spins are active
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            // Use FREE SPIN reels
            if (FreeSpinSlotMachineController.Instance != null)
            {
                targetReels = FreeSpinSlotMachineController.Instance.GetReels();
                // Debug.Log("[WebManAPI] Using FREE SPIN reels for symbol injection");
            }
            else
            {
                // Debug.LogError("[WebManAPI] FreeSpinSlotMachineController.Instance is null during free spins!");
                return;
            }
        }
        else
        {
            // Use NORMAL reels
            targetReels = slotReels;
            // Debug.Log("[WebManAPI] Using NORMAL reels for symbol injection");
        }

        if (targetReels == null)
        {
            // Debug.LogError("[WebManAPI] targetReels array is null!");
            return;
        }

        if (targetReels.Length == 0)
        {
            // Debug.LogError("[WebManAPI] targetReels array is empty!");
            return;
        }

        // Make sure we have enough reels
        if (targetReels.Length < reels.Length)
        {
            // Debug.LogError($"[WebManAPI] Not enough reels! Found {targetReels.Length}, need {reels.Length}");
            return;
        }

        // Set target symbols for each reel
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            if (targetReels[reelIndex] == null)
            {
                // Debug.LogError($"[WebManAPI] Reel at index {reelIndex} is null!");
                continue;
            }

            if (reels[reelIndex] == null)
            {
                // Debug.LogError($"[WebManAPI] Reel data at index {reelIndex} is null!");
                continue;
            }

            // Clear existing target symbols
            targetReels[reelIndex].targetSymbols.Clear();

            // Add new target symbols from API response
            for (int i = 0; i < reels[reelIndex].Length; i++)
            {
                string symbolName = reels[reelIndex][i];

                if (string.IsNullOrEmpty(symbolName))
                {
                    // Debug.LogWarning($"[WebManAPI] Symbol name at reel {reelIndex}, position {i} is null or empty!");
                    continue;
                }

                if (symbolMapping.TryGetValue(symbolName, out SymbolData.SymbolType symbolType))
                {
                    if (targetReels[reelIndex].symbolDatabase != null)
                    {
                        SymbolData symbolData = Array.Find(targetReels[reelIndex].symbolDatabase, x => x.type == symbolType);
                        if (symbolData != null)
                        {
                            targetReels[reelIndex].targetSymbols.Add(symbolData);
                        }
                        else
                        {
                            // Debug.LogWarning($"[WebManAPI] Symbol data not found for type: {symbolType} in reel {reelIndex}");
                            if (targetReels[reelIndex].symbolDatabase.Length > 0)
                            {
                                targetReels[reelIndex].targetSymbols.Add(targetReels[reelIndex].symbolDatabase[0]);
                            }
                        }
                    }
                }
                else
                {
                    if (targetReels[reelIndex].symbolDatabase != null && targetReels[reelIndex].symbolDatabase.Length > 0)
                    {
                        targetReels[reelIndex].targetSymbols.Add(targetReels[reelIndex].symbolDatabase[0]);
                    }
                }
            }
        }

        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            if (FreeSpinSlotMachineController.Instance != null)
            {
                FreeSpinSlotMachineController.Instance.OnAPIResponseReceived();
            }
        }
        else
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnAPIResponseReceived();
            }
        }
    }

    private void HandleSpinComplete()
    {
        if (isFreeSpinSelectionPending && lastSpinResponse != null && lastSpinResponse.free_spin_triggered)
        {
            isFreeSpinSelectionPending = false;
        }
    }

    public void RecordFreeSpinTotalWin(float totalWinAmount)
    {
        if (isDemoMode || !configurationLoaded) return;

        StartCoroutine(UpdateBet(totalWinAmount));
    }

    public void RecordWin(float winAmount)
    {
        if (isDemoMode || !configurationLoaded) return;

        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            return;
        }

        StartCoroutine(UpdateBet(winAmount));
    }

    public void BetUpdate(float winAmount)
    {
        StartCoroutine(UpdateBet(winAmount));
    }

    public void InitiateFreeSpinRequest()
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            int currentIndex = FreeSpinManager.Instance.CurrentSpinIndex - 1;
            int remainingSpins = FreeSpinManager.Instance.RemainingFreeSpins;
            int totalSpins = FreeSpinManager.Instance.TotalFreeSpins;

            StartCoroutine(SendFreeSpinRequest(currentIndex, remainingSpins, totalSpins));
        }

        if (isDemoMode)
        {
            // For demo mode, generate demo results instead of making API calls
            StartCoroutine(FetchSpinResultDemo());
            return; // PlaceBetDemo();
        }
    }

    public IEnumerator SendFreeSpinRequest(int currentFreeSpinIndex, int remainingFreeSpins, int totalFreeSpinsAwarded)
    {
        // if (isDemoMode)
        // {
        //     yield break;
        // }

        if (!configurationLoaded)
        {
            Debug.LogError("[WebManAPI] Configuration not loaded, cannot fetch spin result");
            yield break;
        }

        string endpoint = $"{BaseURL}/api/mahjong/freespin";

        // Get current free spin data from FreeSpinManager
        bool isFreeSpinActive = FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;
        int currentIndex = isFreeSpinActive ? FreeSpinManager.Instance.CurrentSpinIndex - 1 : 0;
        int remainingSpins = isFreeSpinActive ? FreeSpinManager.Instance.RemainingFreeSpins : 0;
        int totalSpins = isFreeSpinActive ? FreeSpinManager.Instance.TotalFreeSpins : 0;

        MahjongSpinRequest payload = new MahjongSpinRequest
        {
            bet_amount = betManager != null ? betManager.CurrentBet : 1.0f,
            client_id = clientId,
            game_id = gameId,
            player_id = playerId,
            bet_id = currentBetId,
            user_id = playerId
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log($"[WebManAPI] Fetching Mahjong spin result with payload: {jsonPayload}");

        using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[WebManAPI] Mahjong Free Spin response received: {www.downloadHandler.text}");

                lastMahjongResponse = JsonConvert.DeserializeObject<MahjongSpinResponse
        >(www.downloadHandler.text);
                payLines = ConvertPaylinesToTuples(lastMahjongResponse.win_lines);
                ProcessMahjongSpinResponse
        (lastMahjongResponse);
            }
            // else
            // {
            //     Debug.LogError($"[WebManAPI] Network error fetching Winning Mask spin result: {www.error}");
            // }
        }
    }

    public List<(int, int, float)> ConvertPaylinesToTuples(Payline[] paylines)
    {
        var result = new List<(int, int, float)>();

        if (paylines == null)
        {
            Debug.Log("[WebManAPI] No paylines (win_lines is null) - no wins this spin");
            return result;
        }

        foreach (var payline in paylines)
        {
            if (payline == null)
            {
                Debug.LogWarning("[WebManAPI] Encountered null payline, skipping");
                continue;
            }
            // Skip paylines that shouldn't be displayed
            if (!payline.ShouldShowPayline)
            {
                Debug.Log($"[WebManAPI] Skipping payline {payline.line_number} - not displayable");
                continue;
            }

            try
            {
                int lineNumber = payline.line_number;
                int wins = payline.count;
                float winAmount = payline.win_amount ?? 0f;

                result.Add((lineNumber, wins, winAmount));

                // NEW: Log positions for debugging
                if (payline.positions != null && payline.positions.Length > 0)
                {
                    Debug.Log($"[WebManAPI] Payline {lineNumber}: {wins} wins, amount: {winAmount}");

                    foreach (var position in payline.positions)
                    {
                        if (position != null && position.Length >= 2)
                        {
                            int reelIndex = position[0];
                            int rowIndex = position[1];
                            Debug.Log($"  - Winning symbol at Reel {reelIndex}, Row {rowIndex}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WebManAPI] Error processing payline: {e.Message}");
            }
        }

        return result;
    }
    public List<(int reel, int row)> GetWinningPositions()
    {
        var winningPositions = new List<(int reel, int row)>();

        if (lastMahjongResponse == null || lastMahjongResponse.win_lines == null)
        {
            return winningPositions;
        }

        foreach (var payline in lastMahjongResponse.win_lines)
        {
            if (payline == null || payline.positions == null)
            {
                continue;
            }

            foreach (var position in payline.positions)
            {
                if (position != null && position.Length >= 2)
                {
                    int reelIndex = position[0];
                    int rowIndex = position[1];
                    winningPositions.Add((reelIndex, rowIndex));
                }
            }
        }

        return winningPositions;
    }




    private IEnumerator WaitForReelsToStopThenShowBonus(float winAmount)
    {
        while (SlotMachineController.Instance != null && SlotMachineController.Instance.IsSpinning)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        if (FreeSpinManager.Instance != null)
        {
            StartCoroutine(FreeSpinManager.Instance.HandleFreeSpinsDuringFreeSpins(winAmount));
        }
    }

    private IEnumerator UpdateBet(float winAmount)
    {
        if (!configurationLoaded)
        {
            yield break;
        }

        string endpoint = $"{ServerLink}/api/v1/update_bet";
        float validatedWinAmount = Mathf.Max(0, winAmount);

        if (string.IsNullOrEmpty(currentBetId))
        {
            yield break;
        }

        UpdateBetRequest payload = new UpdateBetRequest
        {
            bet_id = currentBetId,
            amount_won = validatedWinAmount,
            client_id = clientId
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 10;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                BetResponse response = JsonUtility.FromJson<BetResponse>(www.downloadHandler.text);
                if (response.status_code == 0)
                {
                    if (betManager != null)
                    {
                        betManager.SetBalance(response.new_wallet_balance);
                    }
                    walletBalance = response.new_wallet_balance;
                }
            }
            else
            {
                // Debug.LogError($"[WebManAPI] Network error updating bet: {www.error}");
            }
        }
    }


    #region Public API Methods

    // public bool HasMaskTransformation()
    // {
    //     return lastMahjongResponse != null && lastMahjongResponse.mask_transformation_used;
    // }

    // public string GetSelectedMaskType()
    // {
    //     return lastMahjongResponse?.selected_mask_type ?? "";
    // }

    // public string[][] GetStage2Reels()
    // {
    //     return lastMahjongResponse?.stage2_reels;
    // }

    // public float GetStage1WinAmount()
    // {
    //     return lastMahjongResponse?.stage1_win_amount ?? 0f;
    // }

    // public float GetStage2WinAmount()
    // {
    //     return lastMahjongResponse?.stage2_win_amount ?? 0f;
    // }

    public MahjongSpinResponse GetLastResponse()
    {
        return lastMahjongResponse;
    }

    #endregion

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnSpinStart -= RecordBet;
            SlotMachineController.Instance.OnSpinComplete -= HandleSpinComplete;
        }

        if (betManager != null)
        {
            betManager.OnWinAdded -= RecordWin;
            betManager.OnWinAnimationComplete -= BetUpdate;
        }
    }
}