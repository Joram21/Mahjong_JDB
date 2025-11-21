using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WinAnimationController : MonoBehaviour
{
    [Header("Payout Display")]
    [SerializeField] private GameObject payoutDisplayPrefab;
    [SerializeField] private float displayDuration = 1.8f;
    [SerializeField] private GameObject payoutDisplayContainer;

    [Header("Payout Display Positions - 3 Rows")]
    [SerializeField] private RectTransform topRowPosition;       // Row 1 (top)
    [SerializeField] private RectTransform middleRowPosition;    // Row 2 (middle)
    [SerializeField] private RectTransform bottomRowPosition;    // Row 3 (bottom)

    [Header("Scatter Payout Display Positions - 3 Rows Each")]
    [SerializeField] private RectTransform reel1TopPosition;
    [SerializeField] private RectTransform reel1MiddlePosition;
    [SerializeField] private RectTransform reel1BottomPosition;
    [SerializeField] private RectTransform reel2TopPosition;
    [SerializeField] private RectTransform reel2MiddlePosition;
    [SerializeField] private RectTransform reel2BottomPosition;
    [SerializeField] private RectTransform reel3TopPosition;
    [SerializeField] private RectTransform reel3MiddlePosition;
    [SerializeField] private RectTransform reel3BottomPosition;

    [Header("Animation Settings")]
    [SerializeField] public bool loopAnimations = true;
    [SerializeField] public float pauseBetweenSymbolTypes = 0.5f;

    [Header("Win Highlight Sprites - 3 Rows")]
    [SerializeField] private Sprite topPositionHighlight;
    [SerializeField] private Sprite middlePositionHighlight;
    [SerializeField] private Sprite bottomPositionHighlight;
    [SerializeField] private Sprite freeSpinHighlight;

    private Dictionary<SymbolData.SymbolType, List<WinningCombination>> symbolWinMap = new Dictionary<SymbolData.SymbolType, List<WinningCombination>>();
    public Coroutine animationSequenceCoroutine;
    private Coroutine freeSpinWinDisplayCoroutine;
    public bool IsAnimatingWin => animationSequenceCoroutine != null;
    private bool isAnimating = false;
    private bool shouldStopAnimating = false;
    private List<GameObject> activePayoutDisplays = new List<GameObject>();

    private GameObject currentPayoutDisplay = null;
    private SymbolData.SymbolType currentSymbolType;

    public class WinningCombination
    {
        public List<Symbol> symbols = new List<Symbol>();
        public float payout;
        public Symbol firstReelSymbol;
        public SymbolData.SymbolType primaryType;
        public WinPosition position;
        public int reelIndex;
    }

    // Updated enum to support only 3 rows
    public enum WinPosition
    {
        Top,
        Middle,
        Bottom,
        FreeSpin,
        Reel1Top,
        Reel1Middle,
        Reel1Bottom,
        Reel2Top,
        Reel2Middle,
        Reel2Bottom,
        Reel3Top,
        Reel3Middle,
        Reel3Bottom
    }

    private SymbolData.SymbolType GetPrimarySymbolType(List<Symbol> symbols)
    {
        foreach (Symbol symbol in symbols)
        {
            if (symbol.Data.type != SymbolData.SymbolType.Wild)
            {
                return symbol.Data.type;
            }
        }
        return SymbolData.SymbolType.Wild;
    }

    public void RegisterWinningCombination(List<Symbol> symbols, float payout, int reelIndex = -1)
    {
        if (symbols == null || symbols.Count < 3)
            return;

        Symbol firstReelSymbol = symbols[0];
        SymbolData.SymbolType primaryType = GetPrimarySymbolType(symbols);

        // Determine the position based on the first symbol's location
        WinPosition position = DetermineWinPosition(firstReelSymbol, reelIndex);

        WinningCombination combo = new WinningCombination
        {
            symbols = new List<Symbol>(symbols),
            payout = payout,
            firstReelSymbol = firstReelSymbol,
            primaryType = primaryType,
            reelIndex = reelIndex,
            position = position
        };

        if (!symbolWinMap.ContainsKey(primaryType))
        {
            symbolWinMap[primaryType] = new List<WinningCombination>();
        }
        symbolWinMap[primaryType].Add(combo);
    }

    public WinPosition DetermineWinPositionPublic(Symbol firstSymbol, int reelIndex = -1)
    {
        return DetermineWinPosition(firstSymbol, reelIndex);
    }

    // Updated to work with 3 rows
    private WinPosition DetermineWinPosition(Symbol firstSymbol, int reelIndex = -1)
    {
        if (firstSymbol == null) return WinPosition.Middle; // Default fallback

        float symbolY = firstSymbol.transform.position.y;

        // Handle special symbol types first
        if (firstSymbol.Data.type == SymbolData.SymbolType.FreeSpin)
        {
            // FreeSpin symbols can appear on reels 3, 4, 5
            if (IsInRow(symbolY, topRowPosition))
                return WinPosition.Top;
            else if (IsInRow(symbolY, bottomRowPosition))
                return WinPosition.Bottom;
            else
                return WinPosition.Middle; // Default middle
        }
        else if (firstSymbol.Data.type == SymbolData.SymbolType.FreeSpin) // This seems like a duplicate condition - you may want to check for MaskReel type here
        {
            // MaskReel symbols appear on reels 3, 4, 5
            // Use reel-specific positions based on reelIndex
            if (reelIndex == 2) // Reel 3 (0-indexed)
            {
                if (IsInRow(symbolY, reel3TopPosition))
                    return WinPosition.Reel3Top;
                else if (IsInRow(symbolY, reel3BottomPosition))
                    return WinPosition.Reel3Bottom;
                else
                    return WinPosition.Reel3Middle;
            }
            else if (reelIndex == 3) // Reel 4
            {
                if (IsInRow(symbolY, reel2TopPosition))
                    return WinPosition.Reel2Top;
                else if (IsInRow(symbolY, reel2BottomPosition))
                    return WinPosition.Reel2Bottom;
                else
                    return WinPosition.Reel2Middle;
            }
            else // Reel 5 or default
            {
                if (IsInRow(symbolY, reel1TopPosition))
                    return WinPosition.Reel1Top;
                else if (IsInRow(symbolY, reel1BottomPosition))
                    return WinPosition.Reel1Bottom;
                else
                    return WinPosition.Reel1Middle;
            }
        }
        else
        {
            // Regular symbols - use the standard 3-row positions
            if (IsInRow(symbolY, topRowPosition))
                return WinPosition.Top;
            else if (IsInRow(symbolY, bottomRowPosition))
                return WinPosition.Bottom;
            else
                return WinPosition.Middle; // Default middle
        }
    }

    private bool IsInRow(float symbolY, RectTransform row)
    {
        if (row == null) return false;

        Vector3[] rowCorners = new Vector3[4];
        row.GetWorldCorners(rowCorners);

        // Check if symbol's Y position is within the row's bounds
        float rowTop = rowCorners[1].y;    // Top-left corner Y
        float rowBottom = rowCorners[0].y;  // Bottom-left corner Y

        return symbolY >= rowBottom && symbolY <= rowTop;
    }

    public void ClearWinningCombinations()
    {
        symbolWinMap.Clear();
    }

    public void StopAnimations(bool fromButton = false)
    {
        shouldStopAnimating = true;

        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
            animationSequenceCoroutine = null;
        }
        
        if (freeSpinWinDisplayCoroutine != null && fromButton)
        {
            StopCoroutine(freeSpinWinDisplayCoroutine);
            freeSpinWinDisplayCoroutine = null;
            CleanupPayoutDisplays();
        }
        
        LineManager.instance.ResetLines();
        isAnimating = false;
        currentPayoutDisplay = null;
    }

    public void CleanupPayoutDisplays()
    {
        foreach (GameObject displayObj in activePayoutDisplays)
        {
            if (displayObj != null)
            {
                PayoutDisplay display = displayObj.GetComponent<PayoutDisplay>();
                if (display != null)
                {
                    display.Hide();
                }
                else
                {
                    Destroy(displayObj);
                }
            }
        }
        activePayoutDisplays.Clear();
        currentPayoutDisplay = null;
    }

    private void ResetAllSymbolAnimations()
    {
        foreach (var symbolTypeEntry in symbolWinMap)
        {
            foreach (WinningCombination combo in symbolTypeEntry.Value)
            {
                foreach (Symbol symbol in combo.symbols)
                {
                    if (symbol != null)
                    {
                        symbol.ResetAnimation();
                        Transform highlightTransform = symbol.transform.Find("WinHighlightImage");
                        if (highlightTransform != null)
                        {
                            Image winHighlightImage = highlightTransform.GetComponent<Image>();
                            if (winHighlightImage != null)
                            {
                                winHighlightImage.enabled = false;
                            }
                        }
                    }
                }
            }
        }
    }

    public void PlayWinAnimations(List<(int, int, float)> payLines = null)
    {
        if (isAnimating)
            return;

        var paylines = payLines;
        if (payLines == null)
        {
            paylines = WebManAPI.Instance.payLines;
        }
        Debug.Log($"[WebManAPI] Starting win animations for {WebManAPI.Instance.payLines?.Count ?? 0} paylines.");
        StopAnimations();
        shouldStopAnimating = false;
        currentPayoutDisplay = null;
        currentSymbolType = SymbolData.SymbolType.Wild;
        
        if (payLines?.Count <= 0)
            return;
        
        animationSequenceCoroutine = StartCoroutine(PlayWinSequence(paylines));
    }

    private IEnumerator PlayWinSequence(List<(int,int,float)> winningLines)
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            loopAnimations = false;
        }
        
        isAnimating = true;
    
        LineManager.instance.linesParent.SetActive(true);
        
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1-1].EnableLineOnlyInitial(t.Item2);
        }
    
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1-1].EnableLineOnly();
            yield return new WaitForSeconds(winningLines.Count > 5 ? 0.1f : 0.08f);
        }
    
        yield return new WaitForSeconds(0.2f);
    
        foreach (var t in winningLines)
        {
            LineManager.instance.lines[t.Item1-1].gameObject.SetActive(false);
        }
    
        LineManager.instance.linesParent.SetActive(false);

        if (shouldStopAnimating)
        {
            isAnimating = false;
            yield break;
        }
    
        // Continue with the existing individual symbol type highlighting
        LineManager.instance.linesParent.SetActive(true);
    
        int currentLineIndex = 0;
    
        while (!shouldStopAnimating)
        {
            if (winningLines.Count <= 0)
                break;
            LineManager.instance.lines[winningLines[currentLineIndex].Item1-1].EnableSquaresThenLine(winningLines[currentLineIndex].Item2,winningLines[currentLineIndex].Item3);

            yield return new WaitForSeconds(pauseBetweenSymbolTypes);
        
            LineManager.instance.lines[winningLines[currentLineIndex].Item1-1].DisableSquaresThenLine();
        
            currentLineIndex++;

            if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive && currentLineIndex == (winningLines.Count > 15 ? 6 : 3))
            {
                break;
            }

            // If we've gone through all lines
            if (currentLineIndex >= winningLines.Count)
            {
                if (loopAnimations)
                {
                    currentLineIndex = 0;
                }
                else
                {
                    break;
                }
            }
        }
    
        LineManager.instance.linesParent.SetActive(false);

        if (!loopAnimations)
        {
            ClearWinningCombinations();
        }
        
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.freeSpinFeatureActive)
        {
            StopAnimations();
        }

        isAnimating = false;
    }

    private IEnumerator PlayAllWinsHighlight()
    {
        // Reset all animations first
        ResetAllSymbolAnimations();

        // Apply highlights and animations to ALL winning combinations
        foreach (var symbolTypeEntry in symbolWinMap)
        {
            foreach (WinningCombination combo in symbolTypeEntry.Value)
            {
                // Apply highlight to this combination
                ApplyWinHighlights(combo);

                // Play animations for this combination
                PlayWinAnimations(combo);
            }
        }

        // Wait for half a second while all symbols are highlighted
        yield return new WaitForSeconds(0.5f);

        // Reset all animations before proceeding to individual highlighting
        ResetAllSymbolAnimations();
    }

    private IEnumerator PlaySymbolTypeWins(SymbolData.SymbolType symbolType)
    {
        if (!symbolWinMap.ContainsKey(symbolType) || shouldStopAnimating)
        {
            yield break;
        }

        List<WinningCombination> combinations = symbolWinMap[symbolType];
        ResetAllSymbolAnimations();

        if (combinations.Count > 0 && currentPayoutDisplay == null)
        {
            float totalPayoutForSymbol = 0f;
            foreach (var combo in combinations)
            {
                totalPayoutForSymbol += combo.payout;
            }

            WinPosition displayPosition = combinations[0].position;
            currentPayoutDisplay = CreatePayoutDisplay(displayPosition, totalPayoutForSymbol);

            if (currentPayoutDisplay != null)
            {
                activePayoutDisplays.Add(currentPayoutDisplay);
            }
        }

        foreach (WinningCombination combo in combinations)
        {
            ApplyWinHighlights(combo);
            PlayWinAnimations(combo);
        }

        float elapsedTime = 0f;
        while (elapsedTime < displayDuration && !shouldStopAnimating)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void PlayWinAnimations(WinningCombination combo)
    {
        foreach (Symbol symbol in combo.symbols)
        {
            if (symbol != null)
            {
                // todo top playing win anims for combos
                symbol.PlayWinAnimation();
            }
        }
    }

    // Updated to handle 3 row positions
    private void ApplyWinHighlights(WinningCombination combo)
    {
        Sprite highlightToUse = null;

        switch (combo.position)
        {
            case WinPosition.Top:
            case WinPosition.Reel1Top:
            case WinPosition.Reel2Top:
            case WinPosition.Reel3Top:
                highlightToUse = topPositionHighlight;
                break;
            case WinPosition.Middle:
            case WinPosition.Reel1Middle:
            case WinPosition.Reel2Middle:
            case WinPosition.Reel3Middle:
                highlightToUse = middlePositionHighlight;
                break;
            case WinPosition.Bottom:
            case WinPosition.Reel1Bottom:
            case WinPosition.Reel2Bottom:
            case WinPosition.Reel3Bottom:
                highlightToUse = bottomPositionHighlight;
                break;
            default:
                highlightToUse = middlePositionHighlight; // Default fallback
                break;
        }

        if (highlightToUse == null)
        {
            return;
        }

        foreach (Symbol symbol in combo.symbols)
        {
            Transform highlightTransform = symbol.transform.Find("WinHighlightImage");
            if (highlightTransform == null)
            {
                continue;
            }

            Image winHighlightImage = highlightTransform.GetComponent<Image>();
            if (winHighlightImage != null)
            {
                winHighlightImage.sprite = highlightToUse;
                winHighlightImage.enabled = true;
            }
        }
    }

    // Updated to handle 3 row positions
    private GameObject CreatePayoutDisplay(WinPosition position, float payout)
    {
        if (!Application.isPlaying)
        {
            return null;
        }

        if (payoutDisplayContainer == null)
        {
            return null;
        }

        GameObject displayObj = Instantiate(payoutDisplayPrefab, payoutDisplayContainer.transform);
        RectTransform rectTransform = displayObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Destroy(displayObj);
            return null;
        }

        // Default to middle position
        RectTransform targetPosition = middleRowPosition;

        switch (position)
        {
            // Regular 3-row positions
            case WinPosition.Top:
                if (topRowPosition != null) targetPosition = topRowPosition;
                break;
            case WinPosition.Middle:
                if (middleRowPosition != null) targetPosition = middleRowPosition;
                break;
            case WinPosition.Bottom:
                if (bottomRowPosition != null) targetPosition = bottomRowPosition;
                break;

            // Reel 1 positions
            case WinPosition.Reel1Top:
                if (reel1TopPosition != null) targetPosition = reel1TopPosition;
                break;
            case WinPosition.Reel1Middle:
                if (reel1MiddlePosition != null) targetPosition = reel1MiddlePosition;
                break;
            case WinPosition.Reel1Bottom:
                if (reel1BottomPosition != null) targetPosition = reel1BottomPosition;
                break;

            // Reel 2 positions
            case WinPosition.Reel2Top:
                if (reel2TopPosition != null) targetPosition = reel2TopPosition;
                break;
            case WinPosition.Reel2Middle:
                if (reel2MiddlePosition != null) targetPosition = reel2MiddlePosition;
                break;
            case WinPosition.Reel2Bottom:
                if (reel2BottomPosition != null) targetPosition = reel2BottomPosition;
                break;

            // Reel 3 positions
            case WinPosition.Reel3Top:
                if (reel3TopPosition != null) targetPosition = reel3TopPosition;
                break;
            case WinPosition.Reel3Middle:
                if (reel3MiddlePosition != null) targetPosition = reel3MiddlePosition;
                break;
            case WinPosition.Reel3Bottom:
                if (reel3BottomPosition != null) targetPosition = reel3BottomPosition;
                break;
        }

        if (targetPosition != null)
        {
            rectTransform.position = targetPosition.position;
            rectTransform.sizeDelta = targetPosition.sizeDelta;
        }

        PayoutDisplay payoutDisplay = displayObj.GetComponent<PayoutDisplay>();
        if (payoutDisplay != null)
        {
            payoutDisplay.SetPayout(payout);
        }
        else
        {
            TextMeshProUGUI payoutText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
            if (payoutText != null)
            {
                payoutText.text = payout.ToString("0.00");
            }
        }

        return displayObj;
    }

    public IEnumerable<WinningCombination> GetWinningCombinations()
    {
        foreach (var symbolTypeEntry in symbolWinMap)
        {
            foreach (var combo in symbolTypeEntry.Value)
            {
                yield return combo;
            }
        }
    }

    // MODIFIED: Show free spin payout display with specific position
    private float pay;
    private WinPosition pos;
    public void ShowFreeSpinPayout(float totalPayout, WinPosition position)
    {
        GameObject payoutDisplay = CreatePayoutDisplay(position, totalPayout);
        pay = totalPayout;
        pos = position;
        if (payoutDisplay != null)
        {
            activePayoutDisplays.Add(payoutDisplay);

            freeSpinWinDisplayCoroutine = StartCoroutine(ShowPayoutDisplayCoroutine(payoutDisplay, displayDuration));
        }
    }

    // MODIFIED: Show mask reel payout display with specific position  
    public void ShowMaskReelPayout(float totalPayout, WinPosition position)
    {
        // Create and show payout display at specified position
        GameObject payoutDisplay = CreatePayoutDisplay(position, totalPayout);
        if (payoutDisplay != null)
        {
            activePayoutDisplays.Add(payoutDisplay);
        }
    }

    // LEGACY: Keep old methods for backward compatibility (without position parameter)
    public void ShowFreeSpinPayout(float totalPayout)
    {
        // Update existing free spin combinations with the total payout
        UpdateFreeSpinComboPayouts(SymbolData.SymbolType.FreeSpin, totalPayout);

        // Find free spin combination to determine display position
        WinPosition displayPosition = WinPosition.Top; // Default

        foreach (var symbolTypeEntry in symbolWinMap)
        {
            if (symbolTypeEntry.Key == SymbolData.SymbolType.FreeSpin)
            {
                foreach (var combo in symbolTypeEntry.Value)
                {
                    displayPosition = combo.position;
                    break; // Use first combination's position
                }
                break;
            }
        }

        // Create and show payout display
        GameObject payoutDisplay = CreatePayoutDisplay(displayPosition, totalPayout);
        if (payoutDisplay != null)
        {
            activePayoutDisplays.Add(payoutDisplay);
        }
    }

    // LEGACY: Keep old method for backward compatibility  
    public void ShowMaskReelPayout(float totalPayout)
    {
        // Update existing mask reel combinations with the total payout
        UpdateFreeSpinComboPayouts(SymbolData.SymbolType.FreeSpin, totalPayout);

        // Find mask reel combination to determine display position (default to reel 3 middle)
        WinPosition displayPosition = WinPosition.Reel3Top; // Default for mask reel

        foreach (var symbolTypeEntry in symbolWinMap)
        {
            if (symbolTypeEntry.Key == SymbolData.SymbolType.FreeSpin)
            {
                foreach (var combo in symbolTypeEntry.Value)
                {
                    displayPosition = combo.position;
                    break; // Use first combination's position
                }
                break;
            }
        }

        // Create and show payout display
        GameObject payoutDisplay = CreatePayoutDisplay(displayPosition, totalPayout);
        if (payoutDisplay != null)
        {
            activePayoutDisplays.Add(payoutDisplay);
        }
    }

    /// <summary>
    /// Stop mask reel animations specifically
    /// </summary>
    public void StopMaskReelAnimations()
    {
        // Stop mask reel symbol animations
        if (symbolWinMap.ContainsKey(SymbolData.SymbolType.FreeSpin))
        {
            foreach (WinningCombination combo in symbolWinMap[SymbolData.SymbolType.FreeSpin])
            {
                foreach (Symbol symbol in combo.symbols)
                {
                    if (symbol != null)
                    {
                        symbol.ResetAnimation();

                        // Disable highlight images for mask reel symbols
                        Transform highlightTransform = symbol.transform.Find("WinHighlightImage");
                        if (highlightTransform != null)
                        {
                            Image winHighlightImage = highlightTransform.GetComponent<Image>();
                            if (winHighlightImage != null)
                            {
                                winHighlightImage.enabled = false;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Coroutine to show payout display for a specific duration
    /// </summary>
    private IEnumerator ShowPayoutDisplayCoroutine(GameObject payoutDisplay, float duration)
    {
        if (payoutDisplay == null) yield break;
        
        while (true)
        {
            if (payoutDisplay == null)
            {
                payoutDisplay = CreatePayoutDisplay(pos, pay);
                if (payoutDisplay != null)
                {
                    activePayoutDisplays.Add(payoutDisplay);
                }
            }
            
            yield return new WaitForSeconds(duration);
            
            // Make sure display is active
            payoutDisplay.SetActive(true);
            
            yield return new WaitForSeconds(duration);

            // Make sure display is inactive
            payoutDisplay.SetActive(false);
        }
    }

    public void UpdateFreeSpinComboPayouts(SymbolData.SymbolType symbolType, float payout)
    {
        if (!symbolWinMap.ContainsKey(symbolType))
            return;

        foreach (var combo in symbolWinMap[symbolType])
        {
            combo.payout = payout;
        }
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}
