using UnityEngine;

public class ViewControl : MonoBehaviour
{
    public GameObject LandScapeView;
    public GameObject PotraitView;

    private void OnEnable()
    {
        SetUp();
    }

    void SetUp()
    {
        Refresh();
    }

    public void Refresh()
    {
        ViewMan viewMan = FindFirstObjectByType<ViewMan>();
        if (viewMan == null) return;

        switch (viewMan.CurrentScreenCategory)
        {
            case ScreenCategory.Landscape:
                LandScapeView.SetActive(true);
                PotraitView.SetActive(false);
                break;
            case ScreenCategory.Tablet:
                LandScapeView.SetActive(true);
                PotraitView.SetActive(false);
                break;
            case ScreenCategory.Portrait:
                LandScapeView.SetActive(false);
                PotraitView.SetActive(true);

                break;
        }
    }

}
