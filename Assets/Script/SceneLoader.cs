using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private float minimumLoadTime = 4f;
    [SerializeField] private Image progressBar;
    [SerializeField] private bool unloadCurrentScene = false;
    [SerializeField] private bool setAsActiveScene = true;

    // Fake loading parameters
    [Header("Fake Loading Settings")]
    [SerializeField] private AnimationCurve loadingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useRandomizedLoading = true;
    [SerializeField] private float randomVariance = 0.5f;

    private string currentSceneName;

    private void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        // Validate scene name exists
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[SceneLoader] Next scene name is empty!");
            yield break;
        }

        // Check if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(nextSceneName) == false)
        {
            Debug.LogError($"[SceneLoader] Scene '{nextSceneName}' is not in build settings or doesn't exist!");
            yield break;
        }

        AsyncOperation asyncLoad = null;
        
        try
        {
            // Start the actual scene loading additively
            asyncLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{nextSceneName}'");
                yield break;
            }
            
            asyncLoad.allowSceneActivation = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneLoader] Exception when starting scene load: {e.Message}");
            yield break;
        }

        float elapsedTime = 0f;
        float targetLoadTime = minimumLoadTime;

        // Add some randomization to make loading feel more natural
        if (useRandomizedLoading)
        {
            targetLoadTime += Random.Range(-randomVariance, randomVariance);
            targetLoadTime = Mathf.Max(1f, targetLoadTime); // Ensure minimum 1 second
        }
        
        // Wait for balance fetch - add null check
        // if (FetchPlayerBalance.BalanceFetched == false)
        // {
        //     while (!FetchPlayerBalance.BalanceFetched)
        //     {
        //         yield return null; 
        //     }
        // }

        while (!asyncLoad.isDone || elapsedTime < targetLoadTime)
        {
            elapsedTime += Time.deltaTime;

            // Calculate fake progress using animation curve
            float timeProgress = Mathf.Clamp01(elapsedTime / targetLoadTime);
            float fakeProgress = loadingCurve.Evaluate(timeProgress);

            // Get actual loading progress (Unity loads to 90%, then waits for activation)
            float asyncProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Use the minimum of fake progress and actual progress for more realistic loading
            float displayProgress = Mathf.Min(fakeProgress, asyncProgress);

            // Update progress bar - add null check
            if (progressBar != null)
                progressBar.fillAmount = displayProgress;

            // Check if we can activate the scene
            if (asyncLoad.progress >= 0.9f && elapsedTime >= targetLoadTime)
            {
                // Ensure progress bar reaches 100% before activation
                if (progressBar != null)
                    progressBar.fillAmount = 1f;

                yield return new WaitForSeconds(0.1f); // Brief pause at 100%

                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Wait for scene to fully load
        yield return new WaitUntil(() => asyncLoad.isDone);

        // Post-loading setup
        yield return StartCoroutine(HandlePostLoading());
    }

    private IEnumerator HandlePostLoading()
    {
        // Find the newly loaded scene
        Scene loadedScene = SceneManager.GetSceneByName(nextSceneName);

        if (loadedScene.IsValid())
        {
            // Unload the current scene if requested
            if (unloadCurrentScene && !string.IsNullOrEmpty(currentSceneName) && currentSceneName != nextSceneName)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentSceneName);
                if (unloadOperation != null)
                {
                    yield return new WaitUntil(() => unloadOperation.isDone);
                }
            }
            
            // Optional: Fade out loading screen or trigger completion event
            yield return StartCoroutine(OnLoadingComplete());
            
            // Set as active scene if requested
            if (setAsActiveScene)
            {
                SceneManager.SetActiveScene(loadedScene);
            }
        }
        else
        {
            Debug.LogError($"[SceneLoader] Loaded scene '{nextSceneName}' is not valid!");
        }
    }

    private IEnumerator OnLoadingComplete()
    {
        // You can add fade out animations or other completion logic here
        yield return new WaitForSeconds(0.5f);

        // Disable or destroy the loading screen
        if (gameObject != null)
            gameObject.SetActive(false);
    }

    // Optional: Method to manually trigger loading
    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            nextSceneName = sceneName;
            StartCoroutine(LoadSceneAsync());
        }
    }
}
