using UnityEngine;

[CreateAssetMenu(fileName = "SymbolData", menuName = "SlotMachine/SymbolData")]
public class SymbolData : ScriptableObject
{
    [Header("Base Animations")]
    public AnimatorOverrideController animationOverride;
    public AnimationClip idleAnimation;
    public AnimationClip winAnimation;

    [Header("Special Animations")]
    public AnimationClip wildAnimation;
    public AnimationClip freeSpinAnimation;

    [Header("Mask Transformation Animations")]
    public AnimationClip maskTransformAnimation; // Animation for mask transformation during free spins
    public AnimationClip maskIdleInFreeSpinAnimation; // Idle animation for masks in free spins

    public enum SymbolType
    {
        // Base card symbols
        Ten, Jack, Queen, King,
        // Special symbols
        FreeSpin, Wild,
        // New symbols
        WhiteDragon, BlackDragon, GreenDragon, RedDragon,
        
    }

    public SymbolType type;
    public Sprite sprite; // Normal game sprite
    public Sprite winHighlightSprite;
    public Sprite winBgSprite;

    [Header("Free Spin Mask Sprites")]
    public Sprite freeSpinSprite; // Different sprite used during free spins
    public Sprite freeSpinWinHighlightSprite; // Win highlight for free spin version
    public Sprite freeSpinBgSprite; // Win highlight for free spin version

    [Header("Mask Transformation")]
    public bool canTransformInFreeSpins = false; // Only true for mask symbols
    public Sprite transformationSprite; // Sprite shown during transformation animation
    public float transformationDuration = 1.5f; // How long the transformation takes

    [Header("Payouts & Properties")]
    public float[] payouts = new float[3]; // [3-reel, 4-reel, 5-reel]
    public bool wildCanSubstitute = true;
    public bool canAppearInFirstReel = true;
    public float spawnWeight = 1f;

    [Header("Mask Reel Specific")]
    public int multiplierValue = 1; // For MaskReel symbols, stores the multiplier value

    [Header("Free Spin Behavior")]
    public bool excludeFromFreeSpins = false; // Set true for Nine and MaskReel
    public float freeSpinSpawnWeight = 1f; // Different spawn weight during free spins

    /// <summary>
    /// Gets the appropriate sprite based on current game mode
    /// </summary>
    public Sprite GetCurrentSprite(bool isFreeSpinMode = false)
    {
        if (isFreeSpinMode && freeSpinSprite != null && IsMaskSymbol())
        {
            return freeSpinSprite;
        }
        return sprite;
    }

    /// <summary>
    /// Gets the appropriate win highlight sprite based on current game mode
    /// </summary>
    public Sprite GetCurrentWinHighlightSprite(bool isFreeSpinMode = false)
    {
        if (isFreeSpinMode && freeSpinWinHighlightSprite != null && IsMaskSymbol())
        {
            return freeSpinWinHighlightSprite;
        }
        return winHighlightSprite;
    }

    /// <summary>
    /// Checks if this symbol is a mask symbol (not MaskReel)
    /// </summary>
    public bool IsMaskSymbol()
    {
        return type == SymbolType.WhiteDragon ||
               type == SymbolType.GreenDragon ||
               type == SymbolType.RedDragon ||
               type == SymbolType.BlackDragon;
    }

    /// <summary>
    /// Checks if this symbol should be excluded from free spins
    /// </summary>
    

    /// <summary>
    /// Gets the spawn weight for free spins
    /// </summary>
    public float GetFreeSpinSpawnWeight()
    {
        return freeSpinSpawnWeight > 0 ? freeSpinSpawnWeight : spawnWeight;
    }
}