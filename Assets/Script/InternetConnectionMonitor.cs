using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InternetConnectionMonitor : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private float checkInterval = 2f;
    [SerializeField] private string pingUrl = "https://www.cloudflare.com/cdn-cgi/trace";
    [SerializeField] private float timeoutDuration = 10f;
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float retryDelay = 2f;
    
    private bool isConnected = true;
    private bool isChecking = false;
    
    void Start()
    {
        // Start the continuous connection monitoring
        StartCoroutine(MonitorConnection());
    }
    
    private IEnumerator MonitorConnection()
    {
        while (true)
        {
            if (!isChecking)
            {
                StartCoroutine(CheckInternetConnection());
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    private IEnumerator CheckInternetConnection()
    {
        isChecking = true;
        
        bool connectionSuccess = false;
        int attemptCount = 0;
        
        // Try initial connection check plus retries
        while (attemptCount <= maxRetries && !connectionSuccess)
        {
            // Perform a web request to check connection
            using (UnityWebRequest request = UnityWebRequest.Get(pingUrl))
            {
                request.timeout = (int)timeoutDuration;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    connectionSuccess = true;
                    HandleConnectionRestored();
                }
                else
                {
                    attemptCount++;
                    
                    // If this wasn't the last attempt, wait before retrying
                    if (attemptCount <= maxRetries)
                    {
                        yield return new WaitForSeconds(retryDelay);
                    }
                }
            }
        }
        
        // If all attempts failed, handle connection loss
        if (!connectionSuccess)
        {
            HandleConnectionLost();
        }
        
        isChecking = false;
    }
    
    private void HandleConnectionLost()
    {
        if (isConnected)
        {
            isConnected = false;
            // Time.timeScale = 0;
            ShowNoConnectionPrompt();
        }
    }
    
    private void HandleConnectionRestored()
    {
        if (!isConnected)
        {
            isConnected = true;
            HideNoConnectionPrompt();
        }
    }
    
    private void ShowNoConnectionPrompt()
    {
        GameManager.Instance.PromptMan.DisplayPrompt(PromptType.ConnectionError);
    }
    
    private void HideNoConnectionPrompt()
    {
            
    }
}