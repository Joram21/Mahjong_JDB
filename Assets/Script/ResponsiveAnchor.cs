using UnityEngine;
using System;

public class ResponsiveAnchor : MonoBehaviour
{
    [Serializable]
    public class AnchorRule
    {
        public string Name; // optional for clarity in inspector
        public ScreenCategory[] AppliesTo;
        public Transform TargetParent;
    }

    public AnchorRule[] Rules;

    private void OnEnable()
    {
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        var viewMan = FindFirstObjectByType<ViewMan>();
        if (viewMan == null || Rules == null) return;

        ScreenCategory current = viewMan.CurrentScreenCategory;

        foreach (var rule in Rules)
        {
            foreach (var cat in rule.AppliesTo)
            {
                if (cat == current)
                {
                    transform.SetParent(rule.TargetParent, false);
                    transform.localPosition = Vector3.zero;
                    if (name == "Loading image" && current == ScreenCategory.Portrait)
                    {
                        transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(2549.107f, 1349.48f);
                        transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 297f);
                    }
                    else if(name == "Loading image" && (current == ScreenCategory.Landscape || current == ScreenCategory.Tablet))
                    {
                        transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(1700f, 900f);
                        transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 136f);
                    }
                    transform.localScale = Vector3.one;
                    return;
                }
            }
        }
    }
}
