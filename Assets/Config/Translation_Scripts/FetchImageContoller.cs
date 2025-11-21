using UnityEngine;

public class FetchImageContoller : MonoBehaviour
{
    public FetchImage_Obj[] Objs;
    private void OnEnable()
    {
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
        for(int i = 0; i < Objs.Length; i++)
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
        }else{
            for (int i = 0; i < Objs.Length; i++)
            {
                if (Objs[i].TheLanguage == TheLanguage.English)
                {
                    Objs[i].gameObject.SetActive(true);
                    imagefound = true;
                }
                else
                {
                    Objs[i].gameObject.SetActive(false);
                }

            }
            if (imagefound == false)
            {
                for (int i = 0; i < Objs.Length; i++)
                {
                    if (i==0)
                    {
                        Objs[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        Objs[i].gameObject.SetActive(false);
                    }

                }
            }
        }
    }
}
