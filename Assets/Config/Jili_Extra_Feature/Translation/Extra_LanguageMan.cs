using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEditor;



public class Extra_LanguageMan : MonoBehaviour
{
    public TextAsset TranslationDocument;
    public TheLanguage ActiveLanguage;
    
    public string All_Game_Text;
    public string[] Data;
    void Awake()
    {
       
       
        RefreshAll();
        if (Data.Length == 0)
        {
            

        }
    }
    private void Start()
    {
        
    }
    void SetUp()
    {
        if (!LanguageMan.instance)
        {
            Invoke(nameof(SetUp), 0.1f);
        }
        else
        {
            LanguageMan.instance.SetExtraLanguage();
        }
    }


    public string RequestForText(string CODE)
    {
        for (int i = 0; i < Data.Length; i++)
        {
            if (CODE == Data[i])
            {
                return Data[i + (int)ActiveLanguage+1];
            }
        }

        return "ERROR NO TEXT FOUND";
    }
    [ContextMenu("Refresh")]
    public void RefreshAll()
    {
        if (!Application.isPlaying)
            return;
        Extra_FetchTextController[] texts = FindObjectsOfType<Extra_FetchTextController>();
        for(int i = 0; i < texts.Length; i++)
        {
            texts[i].RefreshFetch();
        }
    }
    public string FetchTranslation(string TheString)
    {
        for (int r = 0; r < Data.Length; r++)
        {
            if (Data[r] == TheString)
            {
                return  Data[r + (int)ActiveLanguage];
            }

        }
        return TheString;
    }
    public void DynamicAssignCode(Extra_FetchTextController Which,string TheString)
    {
        for (int r = 0; r < Data.Length; r++)
        {
            if (Data[r] == TheString)
            {
                Which.CODE= Data[r - 1];
                Which.RefreshFetch();
                break;
            }

        }
    }
    [ContextMenu("AssignCode")]
    public void AssignCodes()
    {
        Data = TranslationDocument.text.Split(new string[] { "\t", "\n" }, System.StringSplitOptions.None);
        Extra_FetchTextController[] texts = FindObjectsOfType<Extra_FetchTextController>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            bool found = false;
            string thetext = texts[i].GetComponent<TMP_Text>().text;
            for (int r = 0; r < Data.Length; r++)
            {
//                Debug.Log(Data[r]);
                if (Data[r] == thetext)
                {
                    Debug.Log(Data[r]);

                    texts[i].CODE = Data[r - 1];
                    texts[i].TheOwner = this;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(texts[i]);
#endif
                    found = true;
                    break;
                }
                
            }
            if (!found)
            {
                texts[i].TheOwner = this;
                texts[i].CODE = "";
                Debug.Log("NoTrans_" + thetext);
            }

        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }
    public void SetLanguage(TheLanguage which)
    {
        ActiveLanguage =which;
        RefreshAll();
    }
}
