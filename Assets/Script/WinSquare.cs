using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WinSquare : MonoBehaviour
{
    [Header("Grid Position (editable in Inspector)")]
    public int col;
    public int row;

    public SymbolWin symbol;
    public SymbolData symbolData;

    public Image sqBg;
    public Sprite freeSpinBGSprite;
    public Sprite normalBGSprite;

    public (int row, int col) GetPositions()
    {
        return new(row, col);
    }

    public void HandleGridInitialized(SymbolData _symbolData)
    {
        symbolData = _symbolData;
    }

    public void UpdateExistingSymbol()
    {
        if (symbol == null)
        {
            Debug.LogWarning("[WinSquare] Symbol is null, cannot update");
            return;
        }

        // NEW: If symbolData is null, try to get it from the grid
        if (symbolData == null && LineManager.instance != null)
        {
            Symbol gridSymbol = LineManager.instance.GetElement(row, col);
            if (gridSymbol != null && gridSymbol.Data != null)
            {
                symbolData = gridSymbol.Data;
            }
            else
            {
                Debug.LogWarning($"[WinSquare] SymbolData is null for position ({row}, {col}), grid not initialized yet");
                return;
            }
        }

        if (symbolData == null)
        {
            Debug.LogWarning($"[WinSquare] SymbolData is still null for position ({row}, {col}), cannot update");
            return;
        }

        if (symbol.Data == symbolData)
        {
            symbol.Initialize(symbolData);
            return;
        }

        symbol.ResetAnimation();

        symbol.Initialize(symbolData);

        Transform defaultImageTransform = symbol.transform.Find("DefaultImage");

        if (defaultImageTransform == null)
        {
            Debug.LogWarning("[WinSquare] DefaultImage transform not found");
            return;
        }

        Image baseImage = defaultImageTransform.GetComponent<Image>();

        if (baseImage == null)
        {
            Debug.LogWarning("[WinSquare] DefaultImage has no Image component");
            return;
        }

        bool isInFreeSpinMode = (FreeSpinManager.Instance != null &&
                                 FreeSpinManager.Instance.freeSpinFeatureActive);

        if (isInFreeSpinMode && symbolData.freeSpinSprite != null)
        {
            baseImage.sprite = symbolData.freeSpinSprite;
        }
        else if (symbolData.sprite != null)
        {
            baseImage.sprite = symbolData.sprite;
        }

        baseImage.preserveAspect = true;

        symbol.ValidateSymbolSize();
    }



    public void EnableFreeSpinBG(bool isInFreeSpinMode)
    {
        if (sqBg != null)
        {
            sqBg.sprite = isInFreeSpinMode ? freeSpinBGSprite : normalBGSprite;
        }
    }


}
