using UnityEngine;
using UnityEngine.UI;

public class MaskReelSymbol : MonoBehaviour
{
    private MaskReelSymbolData symbolData;
    private Image symbolImage;

    private void Awake()
    {
        symbolImage = GetComponentInChildren<Image>();
    }

    public void Initialize(MaskReelSymbolData data)
    {
        symbolData = data;

        if (symbolImage == null)
            symbolImage = GetComponent<Image>();

        if (symbolImage != null && symbolData != null)
        {
            symbolImage.sprite = symbolData.sprite;
            symbolImage.color = symbolData.symbolColor;
        }
    }

    public int GetMultiplier()
    {
        return symbolData?.multiplierValue ?? 1;
    }

    public MaskReelSymbolData GetSymbolData()
    {
        return symbolData;
    }
}
