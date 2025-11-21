using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FetchImageController : MonoBehaviour
{
    [System.Serializable]
    public class LanguageImage
    {
        public TheLanguage language;
        public Sprite sprite;
    }
    public FetchImage_Obj[] Objs;
    public Image targetImage;
    public LanguageImage[] languageImages;

    private void OnEnable()
    {
        StartCoroutine(WaitForLanguageMan());
        Setup();
    }
    void Setup()
    {
        if (!LanguageMan.instance)
        {
            Invoke(nameof(Setup), 0.1f);
        }
        else
        {
            RefreshFetch();
        }
    }
    //[ContextMenu("Refresh")]
    public void RefreshFetch()
    {
        bool imagefound = false;
        for (int i = 0; i < Objs.Length; i++)
        {
            if (Objs[i].TheLanguage == LanguageMan.instance.ActiveLanguage)
            {
                imagefound = true;
                Objs[i].gameObject.SetActive(true);
            }

        }
        if (imagefound)
        {
            for (int i = 0; i < Objs.Length; i++)
            {
                if (Objs[i].TheLanguage != LanguageMan.instance.ActiveLanguage)
                {
                    Objs[i].gameObject.SetActive(false);
                }

            }
        }
    }

    private IEnumerator WaitForLanguageMan()
    {
        // Wait until LanguageMan is ready
        while (LanguageMan.instance == null || LanguageMan.instance.Data.Length == 0)
        {
            yield return null;
        }

        // Now safe to subscribe and update
        LanguageMan.instance.onLanguageRefresh.AddListener(UpdateImage);
        UpdateImage();
    }

    private void OnDisable()
    {
        // Prevent errors if LanguageMan is destroyed first
        if (LanguageMan.instance != null)
            LanguageMan.instance.onLanguageRefresh.RemoveListener(UpdateImage);
    }

    private void UpdateImage()
    {
        if (targetImage == null || languageImages == null)
            return;

        TheLanguage currentLang = LanguageMan.instance.ActiveLanguage;

        foreach (var entry in languageImages)
        {
            if (entry.language == currentLang && entry.sprite != null)
            {
                targetImage.sprite = entry.sprite;
                return;
            }
        }

        // Debug.LogWarning($"No sprite assigned for language: {currentLang}");
    }
}
