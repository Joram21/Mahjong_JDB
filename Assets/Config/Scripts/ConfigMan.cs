using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Security.Cryptography;
using System;

[System.Serializable]
public class ConfigRefresh : UnityEvent { }
public class ConfigMan : MonoBehaviour
{
    public static ConfigMan Instance;
    public bool ReceivedConfigs;
    [Header("Config Details")]
    public bool IsDemo;

    public string PlayerId;
    public string GameId;
    public string ClientId;
    public string Currency;
    public string Base_url = "https://admin-api3.ibibe.africa";
    public ConfigRefresh Refresh;
    public CurrencyMan currencyMan;

    [Header("Debug Canvas")]
    public GameObject TheDebugObj;
    public TMP_InputField PlayerIdText;
    public TMP_InputField GameIdText;
    public TMP_InputField ClientIdText;
    public Toggle DemoToggle;
    public GameObject ExpiredSessionObj;
    public TMP_Text GameVersionText;
    [Header("Player Data")]
    public double CachedWalletBalance = 0;
    public bool BalanceLoaded = false;
    void Start()
    {
        GameVersionText.text = "V " + Application.version;
        DontDestroyOnLoad(this);
        Instance = this;
        if (!Application.isEditor)
        {
            TheDebugObj.SetActive(false);
        }
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.Z)
            )
           
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TheDebugObj.SetActive(true);

            }
        }
       


    }
   
    public void ToggleDemoMode(Toggle which)
    {
        IsDemo = which.isOn;
    }
    void OnApplicationFocus(bool hasFocus)
    {
        Silence(!hasFocus);
    }

    void OnApplicationPause(bool isPaused)
    {
        Silence(isPaused);
    }

    private void Silence(bool silence)
    {
        AudioListener.pause = silence;
        // Or / And
        AudioListener.volume = silence ? 0 : 1;
    }
    public void PassPlayerId(string TheId)
    {
        PlayerId = TheId;
        ReceivedConfigs = true;
       // Debug.Log("TheFetchedPlayerIdIs_" + TheId);
        Invoke(nameof(RefreshConfig), 0.1f);
    }
    public void RefreshConfig()
    {
        Debug.Log("ConfigReceived" +
            "\nPlayerId:" + PlayerId +
            "\nClientId:" + ClientId + "" +
            "\nGameId:" + GameId + "" +
             "\nBaseUrl:" + Base_url + "" +
            "\nDemoMode:" + IsDemo.ToString());

        // NEW: Fetch player balance immediately after config is received
        FetchPlayerDetailsInLoadingScene();

        Refresh.Invoke();
    }

    public void PassCurrency(string Which)
    {
        Currency = Which;
        // Debug.Log("TheFetchedGameIdIs_" + GameId);
    }
    public void PassGameId(string Id)
    {
        GameId = Id;
       // Debug.Log("TheFetchedGameIdIs_" + GameId);
    }
    public void PassClientId(string Id)
    {
        ClientId = Id;
       // Debug.Log("TheFetchedClientIdIs_" + ClientId);
    }
    public void CheckTextInput()
    {
        if (!string.IsNullOrEmpty(PlayerIdText.text))
        {
            PassPlayerId(PlayerIdText.text);
        }
        if (!string.IsNullOrEmpty(GameIdText.text))
        {
            PassGameId(GameIdText.text);
        }
        if (!string.IsNullOrEmpty(ClientIdText.text))
        {
            PassClientId(ClientIdText.text);
        }
    }
    public string GetBetId_NoDash()
    {
        string first = UnityEngine.Random.Range(10000, 99999).ToString();
        string Second = UnityEngine.Random.Range(100000, 999999).ToString();
        string Third = UnityEngine.Random.Range(10000000, 99999999).ToString();
        string final = first + Second +  Third;
        return final;
    }
        public string GetBetId()
    {
        string first = UnityEngine.Random.Range(10000, 99999).ToString();
        string Second = UnityEngine.Random.Range(100000, 999999).ToString();
        string Third = UnityEngine.Random.Range(10000000, 99999999).ToString();
        string final = first + "-" + Second + "-" + Third;
        return final;
        /*
        var bytes = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(bytes);
        }

        // and if you need it as a string...
        string hash1 = BitConverter.ToString(bytes);
        string[] tocken = hash1.Split("-");
        string data = "";
        for(int i = 0; i < tocken.Length; i++)
        {
            data = data + tocken[i];
            //Debug.Log(tocken[i]);
        }
      //  Debug.Log(data);
        string timestamp= ((short)DateTime.Now.ToBinary()).ToString();
        string final =timestamp.Substring(3)+ ClientId+ data.Substring(20);
        //final = final.Substring(10);
        return final;*/
    }
    public void FetchPlayerDetailsInLoadingScene()
    {
        if (!IsDemo && !string.IsNullOrEmpty(PlayerId))
        {
            StartCoroutine(GetPlayerDetailsCoroutine());
        }
    }
    private System.Collections.IEnumerator GetPlayerDetailsCoroutine()
    {
        string endpoint = $"{Base_url}/api/v1/customer/details?customer_id={PlayerId}";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Player details fetched in loading scene: {responseText}");

                try
                {
                    // Parse the JSON response to extract wallet_balance
                    var jsonResponse = JsonUtility.FromJson<PlayerDetailsResponse>(responseText);
                    CachedWalletBalance = jsonResponse.wallet_balance;
                    BalanceLoaded = true;

                    Debug.Log($"Balance cached in ConfigMan: {CachedWalletBalance}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse player details in loading scene: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Failed to get player details in loading scene: {www.error}");
            }
        }
    }

    [System.Serializable]
    private class PlayerDetailsResponse
    {
        public float wallet_balance;
    }
    [ContextMenu("TestBetId")]
    void TestBetId()
    {
        Debug.Log(GetBetId());
    }
    [ContextMenu("TestBetId_NoDash")]
    void TestBetId_NoDash()
    {
        Debug.Log(GetBetId_NoDash());
    }


}