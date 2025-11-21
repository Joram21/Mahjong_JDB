using UnityEngine;
using UnityEngine.UI;

public class RenderingOrderManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject reelsParent;
    [SerializeField] private GameObject linesParent;
    
    [Header("Sorting Orders")]
    [SerializeField] private int linesSortingOrder = 5;
    [SerializeField] private int symbolsSortingOrder = 10;
    
    private Canvas reelsCanvas;
    private Canvas linesCanvas;
    
    void Start()
    {
        SetupCanvasSorting();
    }
    
    void SetupCanvasSorting()
    {
        if (reelsParent != null)
        {
            reelsCanvas = reelsParent.GetComponent<Canvas>();
            if (reelsCanvas == null)
            {
                reelsCanvas = reelsParent.AddComponent<Canvas>();
                reelsParent.AddComponent<GraphicRaycaster>();
            }
            reelsCanvas.overrideSorting = true;
            reelsCanvas.sortingOrder = symbolsSortingOrder;
        }
        
        if (linesParent != null)
        {
            linesCanvas = linesParent.GetComponent<Canvas>();
            if (linesCanvas == null)
            {
                linesCanvas = linesParent.AddComponent<Canvas>();
            }
            linesCanvas.overrideSorting = true;
            linesCanvas.sortingOrder = linesSortingOrder;
        }
    }
}
