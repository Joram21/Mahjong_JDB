using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class ExternalAssetManager : MonoBehaviour
{
    [Header("Initialization")]
    public bool initializeOnStart = true;

    private Dictionary<string, AsyncOperationHandle> loadedAssets = new Dictionary<string, AsyncOperationHandle>();

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeAddressables();
        }
    }

    public async void InitializeAddressables()
    {
        try
        {
            // Initialize Addressables system
            var initHandle = Addressables.InitializeAsync();
            await initHandle.Task;

            if (initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // Optional: Download/update catalog with proper error handling
                var catalogHandle = Addressables.CheckForCatalogUpdates();
                await catalogHandle.Task;

                // CHECK STATUS BEFORE ACCESSING RESULT
                if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (catalogHandle.Result != null && catalogHandle.Result.Count > 0)
                    {
                        var updateHandle = Addressables.UpdateCatalogs(catalogHandle.Result);
                        await updateHandle.Task;
                        // Release the update handle
                        Addressables.Release(updateHandle);
                    }
                }

                // Release the catalog check handle
                Addressables.Release(catalogHandle);
            }

            // Release the initialization handle
            Addressables.Release(initHandle);
        }
        catch (System.Exception e)
        {
        }
    }

    public async void LoadAssetAsync<T>(string address, System.Action<T> onComplete) where T : UnityEngine.Object
    {
        if (loadedAssets.ContainsKey(address))
        {
            return;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            loadedAssets[address] = handle;

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                loadedAssets.Remove(address);
            }
        }
        catch (System.Exception e)
        {
            loadedAssets.Remove(address);
        }
    }

    public void ReleaseAsset(string address)
    {
        if (loadedAssets.ContainsKey(address))
        {
            Addressables.Release(loadedAssets[address]);
            loadedAssets.Remove(address);
        }
    }

    private void OnDestroy()
    {
        // Clean up all loaded assets
        foreach (var asset in loadedAssets.Values)
        {
            if (asset.IsValid())
            {
                Addressables.Release(asset);
            }
        }
        loadedAssets.Clear();
    }
}
