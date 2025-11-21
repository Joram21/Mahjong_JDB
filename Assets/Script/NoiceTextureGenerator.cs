using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class NoiceTextureGenerator : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private int textureSize = 256;
    [SerializeField][Range(0f, 1f)] private float noiseScale = 0.1f;
    [SerializeField][Range(0f, 1f)] private float intensity = 0.3f;

    [Header("Output")]
    [SerializeField] private Image targetImage;

    [ContextMenu("Generate Noise Texture")]
    public void GenerateNoiseTexture()
    {
        Texture2D noiseTexture = new Texture2D(textureSize, textureSize);

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float noise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale);
                noise = (noise - 0.5f) * intensity + 0.5f; // Adjust contrast

                Color pixelColor = new Color(noise, noise, noise, 1f);
                noiseTexture.SetPixel(x, y, pixelColor);
            }
        }

        noiseTexture.Apply();
        noiseTexture.filterMode = FilterMode.Point; // Keep it pixelated for subtle effect

        if (targetImage != null)
        {
            Sprite noiseSprite = Sprite.Create(noiseTexture,
                new Rect(0, 0, textureSize, textureSize),
                Vector2.one * 0.5f);
            targetImage.sprite = noiseSprite;
        }
    }
}