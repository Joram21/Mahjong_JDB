using UnityEngine;
using UnityEngine.UI;

public class BetButtonController : MonoBehaviour
{
    [SerializeField] private BetManager betManager;
    [SerializeField] private UIElementManager uiElements;

    private void Start()
    {
        betManager.OnBalanceChanged += UpdateButtonStates;
        UpdateButtonStates();
    }

    private void Update()
    {
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool isInFreeSpin = FreeSpinManager.Instance != null && FreeSpinManager.Instance.IsFreeSpinActive;
        bool isSpinning = SlotMachineController.Instance != null && SlotMachineController.Instance.IsSpinning;

        bool canIncrease = BetManager.CurrentBetIndex < betManager.MaxBetIndex && !isSpinning && !isInFreeSpin;
        bool canDecrease = BetManager.CurrentBetIndex > 0 && !isSpinning && !isInFreeSpin;

        uiElements.increaseButton.landscape.interactable = canIncrease;
        uiElements.increaseButton.portrait.interactable = canIncrease;
        uiElements.decreaseButton.landscape.interactable = canDecrease;
        uiElements.decreaseButton.portrait.interactable = canDecrease;
    }

    private void OnDestroy()
    {
        betManager.OnBalanceChanged -= UpdateButtonStates;
    }
}