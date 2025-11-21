using System;
using UnityEngine;
using UnityEngine.UI;

public class UpdateSpriteIfWeAreInFreeSpin : MonoBehaviour
{
    public Sprite originalSprite;
    public Sprite freeSpinSprite;

    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
        FreeSpinManager.OnSwitchToFreeSpin += UpdateSourceImage;
    }

    private void OnDisable()
    {
        FreeSpinManager.OnSwitchToFreeSpin -= UpdateSourceImage;
    }

    void UpdateSourceImage()
    {
        image.sprite = FreeSpinManager.Instance.FreeSpinHasBegun ? freeSpinSprite : originalSprite;
    }
}
