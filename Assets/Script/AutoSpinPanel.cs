using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoSpinPanel : MonoBehaviour
{
    [SerializeField] private Button[] autoSpinButtons;
    [SerializeField] private AutoSpinManager autoSpinManager;

    private void Start()
    {
        for (int i = 0; i < autoSpinButtons.Length; i++)
        {
            int index = i;
            int spins = int.Parse(autoSpinButtons[i].GetComponentInChildren<TextMeshProUGUI>().text);

            autoSpinButtons[i].onClick.AddListener(() =>
            {
                autoSpinManager.StartAutoSpin(spins);
                autoSpinManager.RecordAutoSpinSelection(index); // optional
                UpdateAutoSpinIndicators(index);
            });
        }

        UpdateAutoSpinIndicators(-1); // No selection by default
    }

    private void UpdateAutoSpinIndicators(int selectedIndex)
    {
        for (int i = 0; i < autoSpinButtons.Length; i++)
        {
            Transform indicator = autoSpinButtons[i].transform.Find("Indicator");
            if (indicator != null)
                indicator.gameObject.SetActive(i == selectedIndex);
        }
    }
    public void ResetIndicators()
    {
        UpdateAutoSpinIndicators(-1);
    }
}
