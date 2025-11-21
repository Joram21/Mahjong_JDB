using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;

public enum ScreenCategory
{
    Portrait,
    Tablet,
    Landscape
}

public class ViewMan : MonoBehaviour
{
    public bool IsLandScape;
    public Vector2 CurrentScale;
    public Vector2 RefScale;
    public Vector2 ScaleMultiplier;
    public float NewScaleMultiplier;
    public SlotMachineViewControl thebar;
    float forceupdatetimestamp;
    public ScreenCategory CurrentScreenCategory;

    private void Start()
    {
        forceupdatetimestamp = Time.time + 3;
        RefreshAll();
    }

    private void Update()
    {
        CurrentScale = new Vector2(Screen.width, Screen.height);
        float width = CurrentScale.x;
        float height = CurrentScale.y;

        // Categorize screen width

        if (Screen.width < Screen.height)
        {
            SetScreenCategory(ScreenCategory.Portrait);
        }
        else if (Screen.width >= 1280)
        {
            SetScreenCategory(ScreenCategory.Landscape);
        }
        else
        {
            SetScreenCategory(ScreenCategory.Tablet);
        }


        // Update scale multipliers if needed
        ScaleMultiplier.x = RefScale.x / width;
        ScaleMultiplier.y = RefScale.y / height;
        NewScaleMultiplier = ScaleMultiplier.x / ScaleMultiplier.y;
    }

    void SetScreenCategory(ScreenCategory newCategory)
    {
        if (CurrentScreenCategory == newCategory && forceupdatetimestamp <= Time.time)
            return;

        CurrentScreenCategory = newCategory;

        // Flag to track orientation
        IsLandScape = (newCategory == ScreenCategory.Landscape);

        // Optional: Tell the bar to reposition
        if (thebar != null)
        {
            switch (newCategory)
            {
                case ScreenCategory.Landscape:
                    thebar.SetLandScape();
                    break;
                case ScreenCategory.Tablet:
                    thebar.SetTabletPortrait();
                    break;
                case ScreenCategory.Portrait:
                    thebar.SetPotrait();
                    break;
            }
        }

        RefreshAll();
    }

    void RefreshAll()
    {
        // Refresh ViewControl UIs
        ViewControl[] views = Object.FindObjectsByType<ViewControl>(FindObjectsSortMode.None);
        foreach (var view in views)
        {
            view.Refresh();
        }

        // Refresh ResponsiveAnchors
        ResponsiveAnchor[] anchors = Object.FindObjectsByType<ResponsiveAnchor>(FindObjectsSortMode.None);
        foreach (var anchor in anchors)
        {
            anchor.ApplyLayout();
        }

        // Refresh ResponsiveImageLayouts
        ResponsiveImageLayout[] imageLayouts = Object.FindObjectsByType<ResponsiveImageLayout>(FindObjectsSortMode.None);
        foreach (var imageLayout in imageLayouts)
        {
            imageLayout.ApplyLayout();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }

    // Editor buttons (optional)
    [ContextMenu("Landscape")]
    public void Debug_SetLandscape() => SetScreenCategory(ScreenCategory.Landscape);

    [ContextMenu("Portrait")]
    public void Debug_SetPortrait() => SetScreenCategory(ScreenCategory.Portrait);

    [ContextMenu("Tablet")]
    public void Debug_SetTablet() => SetScreenCategory(ScreenCategory.Tablet);
}
