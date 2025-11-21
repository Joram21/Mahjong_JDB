using UnityEngine;

public class LogoViewControl : MonoBehaviour
{
    public GameObject Logo;

    private void OnEnable()
    {
        SetUp();
    }

    void SetUp()
    {
        Refresh();
    }

    // Add this to handle real-time orientation changes
    private void Update()
    {
        // Optional: Only call if orientation changes frequently
        Refresh();
    }

    public void Refresh()
    {
        ViewMan viewMan = FindFirstObjectByType<ViewMan>();
        if (viewMan == null)
        {
            return;
        }

        

        switch (viewMan.CurrentScreenCategory)
        {
            case ScreenCategory.Landscape:
                Logo.SetActive(false);
                break;
            case ScreenCategory.Tablet:
                Logo.SetActive(false);
                break;
            case ScreenCategory.Portrait:
                Logo.SetActive(true);
                // Debug logo state
                break;
            default:
                
                break;
        }
    }
}