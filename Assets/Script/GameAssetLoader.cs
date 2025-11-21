using UnityEngine;

public class GameAssetLoader : MonoBehaviour
{
    private ExternalAssetManager assetManager;

    private void Start()
    {
        assetManager = FindAnyObjectByType<ExternalAssetManager>();

        // FIXED: Changed GameObject to Texture2D for the PNG file

        assetManager.LoadAssetAsync<Texture2D>("Free_Desc.png", OnBackgroundLoaded);
    }

    private void OnBackgroundLoaded(Texture2D background)
    {
        if (background != null)
        {
            // Debug.Log($"Successfully loaded: {background.name}");

            // FIXED: Added null check for Image component
            var imageComponent = GetComponent<UnityEngine.UI.Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = Sprite.Create(background, new Rect(0, 0, background.width, background.height), Vector2.zero);
            }
            else
            {
                // Debug.LogWarning("No Image component found on this GameObject");
            }
        }
        else
        {
            // Debug.LogError("Failed to load texture");
        }
    }
}