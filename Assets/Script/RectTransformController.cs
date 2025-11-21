using UnityEngine;
using System.Collections;

public class RectTransformController : MonoBehaviour
{
    [System.Serializable]
    public class LanguageRectTransform
    {
        public TheLanguage language;
        public float height;
    }

    public RectTransform targetRectTransform;
    public LanguageRectTransform[] languageRectTransforms;

    private void OnEnable()
    {
        StartCoroutine(WaitForLanguageMan());
    }

    private IEnumerator WaitForLanguageMan()
    {
        // Wait until LanguageMan is ready
        while (LanguageMan.instance == null || LanguageMan.instance.Data.Length == 0)
        {
            yield return null;
        }

        // Now safe to subscribe and update
        LanguageMan.instance.onLanguageRefresh.AddListener(UpdateRectTransform);
        UpdateRectTransform();
    }

    private void OnDisable()
    {
        // Prevent errors if LanguageMan is destroyed first
        if (LanguageMan.instance != null)
            LanguageMan.instance.onLanguageRefresh.RemoveListener(UpdateRectTransform);
    }

    private void UpdateRectTransform()
    {
        if (targetRectTransform == null || languageRectTransforms == null)
            return;

        TheLanguage currentLang = LanguageMan.instance.ActiveLanguage;

        foreach (var entry in languageRectTransforms)
        {
            if (entry.language == currentLang)
            {
                // Only change the height, keep width the same
                Vector2 currentSize = targetRectTransform.sizeDelta;
                targetRectTransform.sizeDelta = new Vector2(currentSize.x, entry.height);
                return;
            }
        }
    }
}