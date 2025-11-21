using UnityEngine;

[CreateAssetMenu(fileName = "MaskReelSymbolData", menuName = "SlotMachine/MaskReelSymbolData")]
public class MaskReelSymbolData : ScriptableObject
{
    [Header("Visual")]
    public Sprite sprite;

    [Header("Multiplier")]
    public int multiplierValue = 2;

    [Header("Spawn Settings")]
    public float spawnWeight = 1f;

    [Header("Visual Style")]
    public Color symbolColor = Color.white;
}
