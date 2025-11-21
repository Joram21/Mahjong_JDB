using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FetchPlayerBalance : MonoBehaviour
{
    // Public static flag that can be accessed from any script
    public static bool BalanceFetched { get; private set; } = false;

    private void Awake()
    {
        BalanceFetched = false;
    }

    private void Start()
    {
        StartCoroutine(LoadConfigurationFromConfigMan());
    }

    public IEnumerator LoadConfigurationFromConfigMan()
    {
        while (ConfigMan.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        StartCoroutine(GetPlayerDetails());
    }

    private IEnumerator GetPlayerDetails()
    {
        while (!ConfigMan.Instance.ReceivedConfigs)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (ConfigMan.Instance.IsDemo)
        {
            PlayerPrefs.SetFloat("Balance", 2000.0f);
            BalanceFetched = true;
            yield break;
        }

        // Handle API call
        string endpoint = $"{ConfigMan.Instance.Base_url}/api/v1/customer/details?customer_id={ConfigMan.Instance.PlayerId}";

        using (UnityWebRequest www = UnityWebRequest.Get(endpoint))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                WebManAPI.PlayerDetailsResponse responseData = JsonUtility.FromJson<WebManAPI.PlayerDetailsResponse>(responseText);

                PlayerPrefs.SetFloat("Balance", responseData.wallet_balance);
                BalanceFetched = true;
            }
            else
            {
                Debug.LogError($"[FetchPlayerBalance] Failed to fetch player details: {www.error}");
            }
        }
    }
}
