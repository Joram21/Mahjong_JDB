using UnityEngine;
using UnityEngine.UI;

public class MuteSoundAndSwitchSprite : MonoBehaviour
{
    public Sprite mutedSprite;
    public Sprite unmutedSprite;
    private static bool isMuted = false; // Shared across all instances
    private Image buttonImage;

    void OnEnable()
    {
        // Register this button when enabled
        buttonImage = GetComponent<Image>();
        UpdateButtonSprite(); // Make sure it reflects current state
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMute(isMuted);
        }

        // Update all buttons in the scene to reflect the current mute state
        MuteSoundAndSwitchSprite[] allButtons = FindObjectsByType<MuteSoundAndSwitchSprite>(FindObjectsSortMode.None);
        foreach (var button in allButtons)
        {
            button.UpdateButtonSprite();
        }
    }

    private void UpdateButtonSprite()
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = isMuted ? mutedSprite : unmutedSprite;
        }
    }
}