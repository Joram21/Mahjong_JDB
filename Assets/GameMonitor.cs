using UnityEngine;
using System.Collections;

public class GameMonitor : MonoBehaviour
{
    private static GameMonitor _instance;
    public static GameMonitor Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameMonitor>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameMonitor");
                    _instance = go.AddComponent<GameMonitor>();
                }
            }
            return _instance;
        }
    }

    [Header("Monitoring Settings")]
    [SerializeField] private float inactivityTimeLimit = 120f; // 120 seconds
    [SerializeField] private float internetCheckInterval = 1f; // Check every second

    private float lastActivityTime;
    private bool isMonitoringInactivity = false;
    private bool hasInternetConnection = true;
    private Coroutine inactivityCoroutine;
    private Coroutine internetCheckCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ResetInactivityTimer();
        StartInternetMonitoring();
    }

    private void Update()
    {
        // Detect any player input
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ResetInactivityTimer();
        }
    }

    public void ResetInactivityTimer()
    {
        lastActivityTime = Time.time;

        // Only start monitoring if we're not in auto-spin or free spins
        bool shouldMonitor = !IsInAutoSpin() && !IsInFreeSpins();

        if (shouldMonitor && !isMonitoringInactivity)
        {
            StartInactivityMonitoring();
        }
        else if (!shouldMonitor && isMonitoringInactivity)
        {
            StopInactivityMonitoring();
        }
    }

    private bool IsInAutoSpin()
    {
        return AutoSpinManager.Instance != null && AutoSpinManager.Instance.IsAutoSpinning;
    }

    private bool IsInFreeSpins()
    {
        return FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;
    }

    private void StartInactivityMonitoring()
    {
        if (inactivityCoroutine != null)
        {
            StopCoroutine(inactivityCoroutine);
        }

        isMonitoringInactivity = true;
        inactivityCoroutine = StartCoroutine(MonitorInactivity());
        // Debug.Log("[GameMonitor] Started inactivity monitoring");
    }

    private void StopInactivityMonitoring()
    {
        if (inactivityCoroutine != null)
        {
            StopCoroutine(inactivityCoroutine);
            inactivityCoroutine = null;
        }

        isMonitoringInactivity = false;
        // Debug.Log("[GameMonitor] Stopped inactivity monitoring");
    }

    private IEnumerator MonitorInactivity()
    {
        while (isMonitoringInactivity)
        {
            float timeSinceLastActivity = Time.time - lastActivityTime;

            if (timeSinceLastActivity >= inactivityTimeLimit)
            {
                // Debug.Log("[GameMonitor] Inactivity detected - showing error");
                ShowInactivityError();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void ShowInactivityError()
    {
        if (PromptManager.Instance != null)
        {
            // Add these methods directly to your existing PromptManager.cs:
            // ShowError("Inactivity", "You have been inactive for too long.", "INACTIVITY_001", "INACTIVITY");
            PromptManager.Instance.ShowNetworkError("You have been inactive for too long.", "INACTIVITY_001");
        }

        StopInactivityMonitoring();
    }

    private void StartInternetMonitoring()
    {
        if (internetCheckCoroutine != null)
        {
            StopCoroutine(internetCheckCoroutine);
        }

        internetCheckCoroutine = StartCoroutine(MonitorInternetConnection());
    }

    private IEnumerator MonitorInternetConnection()
    {
        while (true)
        {
            bool previousConnectionState = hasInternetConnection;
            hasInternetConnection = CheckInternetConnection();

            // If connection was lost, show error immediately
            if (previousConnectionState && !hasInternetConnection)
            {
                // Debug.Log("[GameMonitor] Internet connection lost - showing network abnormal error");
                ShowNetworkAbnormalError();
            }

            yield return new WaitForSeconds(internetCheckInterval);
        }
    }

    private bool CheckInternetConnection()
    {
        try
        {
            using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
            {
                System.Net.NetworkInformation.PingReply reply = ping.Send("8.8.8.8", 3000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }

    private void ShowNetworkAbnormalError()
    {
        if (PromptManager.Instance != null)
        {
            // Add these methods directly to your existing PromptManager.cs:
            // ShowError("Network Abnormal", "Internet connection lost. Please check your connection.", "NETWORK_001", "NETWORK_ABNORMAL");
            PromptManager.Instance.ShowNetworkError("Internet connection lost. Please check your connection.", "NETWORK_001");
        }
    }

    public bool HasInternetConnection => hasInternetConnection;

    public void OnSpinButtonPressed()
    {
        ResetInactivityTimer();
    }

    public void OnGameStateChanged()
    {
        // Call this when switching between normal/auto/free spins
        ResetInactivityTimer();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}