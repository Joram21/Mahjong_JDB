using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class Line : MonoBehaviour
{
    public WinSquares winSquaresParent;
    public PayoutDisplay payoutDisplay;
    
    public void EnableLineOnlyInitial(int index)
    {
        if (LineManager.instance == null || winSquaresParent == null)
        {
            Debug.LogWarning("[Line] LineManager or winSquaresParent is null");
            return;
        }

        if (winSquaresParent.winSquares == null || index > winSquaresParent.winSquares.Length)
        {
            Debug.LogWarning($"[Line] winSquares is null or index {index} exceeds array length");
            return;
        }

        for (int i = 0; i < index; i++)
        {
            if (i >= winSquaresParent.winSquares.Length)
            {
                Debug.LogWarning($"[Line] Index {i} out of bounds for winSquares array");
                break;
            }

            WinSquare currentWinSquare = winSquaresParent.winSquares[i];
            if (currentWinSquare == null)
            {
                continue;
            }

            foreach (var symbol in LineManager.instance.VisibleSymbolsGrid)
            {
                if (symbol == null)
                {
                    continue;
                }

                if (currentWinSquare.GetPositions() == symbol.GetPositions() && !symbol.IsAnimating())
                {
                    if (symbol.Data != null && 
                        (symbol.Data.type == SymbolData.SymbolType.FreeSpin || 
                         symbol.Data.type == SymbolData.SymbolType.Wild))
                    {
                        symbol.PlayWinAnimation();
                    }
                }
            }
        }
    }

    public void EnableLineOnly()
    {
        
        gameObject.SetActive(true);
    }

    public void SetFreeSpinBgSprite(bool isInFreeSpinMode)
    {
        if (winSquaresParent == null || winSquaresParent.winSquares == null)
        {
            Debug.LogWarning("[Line] Cannot set free spin bg sprite - winSquaresParent or winSquares is null");
            return;
        }

        int squareCount = Mathf.Min(5, winSquaresParent.winSquares.Length);
        
        for (int i = 0; i < squareCount; i++)
        {
            if (winSquaresParent.winSquares[i] != null)
            {
                winSquaresParent.winSquares[i].EnableFreeSpinBG(isInFreeSpinMode);
            }
        }
    }
    
    public void EnableSquaresThenLine(int index, float lineWinAmnt)
    {
        if (winSquaresParent == null || winSquaresParent.winSquares == null)
        {
            Debug.LogWarning("[Line] Cannot enable squares - winSquaresParent or winSquares is null");
            return;
        }

        if (payoutDisplay != null)
        {
            payoutDisplay.SetPayout(lineWinAmnt);
        }

        int validIndex = Mathf.Min(index, winSquaresParent.winSquares.Length);
        
        for (int i = 0; i < validIndex; i++)
        {
            WinSquare square = winSquaresParent.winSquares[i];
            
            if (square == null)
            {
                Debug.LogWarning($"[Line] WinSquare at index {i} is null");
                continue;
            }

            square.UpdateExistingSymbol();
            square.gameObject.SetActive(true);
        }
        
        gameObject.SetActive(true);
    }
    
    public void DisableSquaresThenLine()
    {
        if (winSquaresParent == null || winSquaresParent.winSquares == null)
        {
            Debug.LogWarning("[Line] Cannot disable squares - winSquaresParent or winSquares is null");
            gameObject.SetActive(false);
            return;
        }

        foreach (var square in winSquaresParent.winSquares)
        {
            if (square != null && square.gameObject != null)
            {
                square.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false);
    }
}
