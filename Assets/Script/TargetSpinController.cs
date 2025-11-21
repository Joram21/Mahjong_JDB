using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TargetSpinController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SlotReel[] reels; // Direct reference to reels
    [SerializeField] private GameObject FeaturePanel;
    [SerializeField] private UIElementManager uiElements;

    [Header("Card Symbol References")]
    [SerializeField] private SymbolData tenSymbol;
    [SerializeField] private SymbolData jackSymbol;
    [SerializeField] private SymbolData queenSymbol;
    [SerializeField] private SymbolData kingSymbol;
    [SerializeField] private SymbolData aceSymbol;

    [Header("Special Symbol References")]
    [SerializeField] private SymbolData freeSpinSymbol;
    [SerializeField] private SymbolData wildSymbol;

    [Header("NEW: Mask Symbol References")]
    [SerializeField] private SymbolData dragonSymbol;
    [SerializeField] private SymbolData cabinetSymbol;
    [SerializeField] private SymbolData frogSymbol;
    [SerializeField] private SymbolData fanSymbol;
    [SerializeField] private SymbolData greenJadeSymbol;

    private bool isFirstSpinComplete = false;
    private bool isCurrentSpinTarget = false;
    private SpinMode currentSpinMode = SpinMode.None;

    public enum SpinMode
    {
        None,
        WildWins,
        WildWins2,
        BonusWins
    }

    private void Start()
    {
        if (uiElements.targetSpinButton1 != null)
        {
            uiElements.targetSpinButton1.onClick.AddListener(() =>
            {
                WebManAPI.Instance.PlaceFeatureBet(SpinMode.WildWins);
                OnTargetSpinButtonClicked();
            });
        }
        
        if (uiElements.targetSpinButton2 != null)
        {
            uiElements.targetSpinButton2.onClick.AddListener(() =>
            {
                WebManAPI.Instance.PlaceFeatureBet(SpinMode.WildWins2);
                OnTargetSpinButtonClicked();
            });
        }
        
        if (uiElements.targetSpinButton3 != null)
        {
            uiElements.targetSpinButton3.onClick.AddListener(() =>
            {
                WebManAPI.Instance.PlaceFeatureBet(SpinMode.BonusWins);
                OnTargetSpinButtonClicked();
            });
        }
    }

    private void OnTargetSpinButtonClicked()
    {
        FeaturePanel.SetActive(false);
        if (SlotMachineController.Instance.IsSpinning)
            return;

        // NEW: Disable random symbol generation for target spins
        SlotMachineController.Instance.SetUseRandomSymbolsInDemo(false);

        isCurrentSpinTarget = true;
        isFirstSpinComplete = false;

        // // Set the appropriate symbols based on the button clicked
        // switch (mode)
        // {
        //     case SpinMode.DragonWins:
        //         SetDragonSymbols();
        //         break;
        //     case SpinMode.DragonWins2:
        //         SetDragonSymbols2();
        //         break;
        //     case SpinMode.BonusWins:
        //         SetBonusSymbols();
        //         break;
        // }

        SlotMachineController.Instance.StartAllReels();
    }


    // ==========================================
    // SYMBOL SETTING METHODS - UPDATED FOR 5x4 GRID
    // ==========================================

    private void SetDragonSymbols()
    {
        if (reels == null || reels.Length < 5) return;

        reels[0].targetSymbols.Clear();
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);

        reels[1].targetSymbols.Clear();
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);

        reels[2].targetSymbols.Clear();
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);

        reels[3].targetSymbols.Clear();
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);

        reels[4].targetSymbols.Clear();
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);

        WebManAPI.Payline[] paylines = new WebManAPI.Payline[50];

        for (int i = 0; i < 50; i++)
        {
            paylines[i] = new WebManAPI.Payline
            {
                line_number = 1 + i,
                symbols = null,
                count = 5,
                symbol = null,
                win_amount = 5.00f,
                positions = null
            };
        }
        if (SlotMachineController.Instance.betManager != null)
        {
            SlotMachineController.Instance.betManager.targetWinAmount = 250.0f;
        }
    }

    private void SetDragonSymbols2()
    {
        if (reels == null || reels.Length < 5) return;

        reels[0].targetSymbols.Clear();
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);
        reels[0].targetSymbols.Add(dragonSymbol);

        reels[1].targetSymbols.Clear();
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);
        reels[1].targetSymbols.Add(dragonSymbol);

        reels[2].targetSymbols.Clear();
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);
        reels[2].targetSymbols.Add(dragonSymbol);

        reels[3].targetSymbols.Clear();
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);

        reels[4].targetSymbols.Clear();
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);

        WebManAPI.Payline[] paylines = new WebManAPI.Payline[50];

        for (int i = 0; i < 50; i++)
        {
            paylines[i] = new WebManAPI.Payline
            {
                line_number = 1 + i,
                symbols = null,
                count = 5,
                symbol = null,
                win_amount = 5.00f,
                positions = null
            };
        }
        
        if (SlotMachineController.Instance.betManager != null)
        {
            SlotMachineController.Instance.betManager.targetWinAmount = 250.0f;
        }
    }

    private void SetBonusSymbols()
    {
        if (reels == null || reels.Length < 5) return;

        reels[0].targetSymbols.Clear();
        reels[0].targetSymbols.Add(freeSpinSymbol);
        reels[0].targetSymbols.Add(jackSymbol);
        reels[0].targetSymbols.Add(frogSymbol);
        reels[0].targetSymbols.Add(jackSymbol);

        reels[1].targetSymbols.Clear();
        reels[1].targetSymbols.Add(freeSpinSymbol);
        reels[1].targetSymbols.Add(jackSymbol);
        reels[1].targetSymbols.Add(cabinetSymbol);
        reels[1].targetSymbols.Add(wildSymbol);

        reels[2].targetSymbols.Clear();
        reels[2].targetSymbols.Add(freeSpinSymbol);
        reels[2].targetSymbols.Add(aceSymbol);
        reels[2].targetSymbols.Add(wildSymbol);
        reels[2].targetSymbols.Add(fanSymbol);

        reels[3].targetSymbols.Clear();
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);
        reels[3].targetSymbols.Add(dragonSymbol);

        reels[4].targetSymbols.Clear();
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);
        reels[4].targetSymbols.Add(dragonSymbol);

    }


    void SpinComplete()
    {
        if (FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive)
        {
            if (FreeSpinSlotMachineController.Instance != null)
            {
                FreeSpinSlotMachineController.Instance.OnAPIResponseReceived();
            }
        }
        else
        {
            if (SlotMachineController.Instance != null)
            {
                SlotMachineController.Instance.OnAPIResponseReceived();
            }
        }
    }

    // ==========================================
    // EVENT HANDLING
    // ==========================================

    private void OnSpinComplete()
    {
        if (!isCurrentSpinTarget)
            return;

        // For testing purposes, just reset after one spin for most modes
        switch (currentSpinMode)
        {
            case SpinMode.WildWins:
            case SpinMode.WildWins2:
            case SpinMode.BonusWins:
                ResetTargetSymbols();
                isCurrentSpinTarget = false;
                currentSpinMode = SpinMode.None;
                // Debug.Log($"{currentSpinMode} test complete. Symbols reset.");
                break;
        }
    }

    private void ResetTargetSymbols()
    {
        if (reels == null || reels.Length < 5) return;

        // Clear all target symbols
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].targetSymbols.Clear();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (SlotMachineController.Instance != null)
        {
            SlotMachineController.Instance.OnSpinComplete -= OnSpinComplete;
        }

        // Clean up button listeners
        if (uiElements.targetSpinButton1 != null)
            uiElements.targetSpinButton1.onClick.RemoveAllListeners();

        if (uiElements.targetSpinButton2 != null)
            uiElements.targetSpinButton2.onClick.RemoveAllListeners();

        if (uiElements.targetSpinButton3 != null)
            uiElements.targetSpinButton3.onClick.RemoveAllListeners();
    }
}