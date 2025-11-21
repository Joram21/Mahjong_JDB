using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class DynamicImageLoader : MonoBehaviour
{
    [System.Serializable]
    public class ImageEntry
    {
        public string addressKey;
        public Image targetImage;
        public bool preserveAspectRatio = false;
    }

    public List<ImageEntry> imageMappings;

    void Start()
    {
        foreach (var entry in imageMappings)
        {
            LoadAndAssignImage(entry.addressKey, entry.targetImage, entry.preserveAspectRatio);
        }
    }

    async void LoadAndAssignImage(string key, Image target, bool preserveAspectRatio)
    {
        try
        {
            // Try loading as Sprite first
            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            var sprite = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                target.sprite = sprite;
                target.preserveAspect = preserveAspectRatio; // Apply preserve aspect ratio setting
                // Debug.Log($"Successfully loaded sprite: {key} | Preserve Aspect: {preserveAspectRatio}");
            }
            else
            {
                // Debug.LogError($"Failed to load sprite {key}: {handle.OperationException}");
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"Exception loading {key}: {e.Message}");
        }
    }
}