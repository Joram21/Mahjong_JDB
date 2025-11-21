using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Extra_FetchTextController : MonoBehaviour
{
    public Extra_LanguageMan TheOwner;
    public TextMeshProUGUI myText;
    public string CODE;
    private void OnEnable()
    {
        Setup();
        
    }
    void Setup()
    {
        if (TheOwner)
        {
            RefreshFetch();

        }
        else
        {
            Debug.Log("NoOwner_"+transform.name);
        }
       
    }
    [ContextMenu("Refresh")]
    public void RefreshFetch() 
    {
        if (CODE == "")
            return;
        myText.SetText(TheOwner.RequestForText(CODE));
    }
}
