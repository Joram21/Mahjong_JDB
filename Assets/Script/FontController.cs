using UnityEngine;
using System.Collections;
using TMPro;

public class FontController : MonoBehaviour
{
    [System.Serializable]
    public class FontTransform
    {
        public TheLanguage language;
        public int fontSize;
        public int lineDif;
        public FontWeight fontStyle;
    }

    public TextMeshProUGUI targetTextMeshPro;
    public FontTransform[] fontTransforms;

    private void OnEnable()
    {
        targetTextMeshPro = GetComponent<TextMeshProUGUI>();
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
        if (targetTextMeshPro == null || fontTransforms == null)
            return;

        TheLanguage currentLang = LanguageMan.instance.ActiveLanguage;

        foreach (var entry in fontTransforms)
        {
            if (entry.language == currentLang)
            {
                if(entry.fontSize > 0)
                    targetTextMeshPro.fontSize = entry.fontSize;
                if(entry.lineDif > 0)
                    targetTextMeshPro.lineSpacing = entry.lineDif;
                targetTextMeshPro.fontWeight = entry.fontStyle;
                return;
            }
        }
    }
}