using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class FetchTextController : MonoBehaviour
{
    public TextMeshProUGUI myText;
    public Text _myText;
    public string CODE;
    public bool IsHardCoded;
    private void OnEnable()
    {
        myText = GetComponent<TextMeshProUGUI>();
        _myText = GetComponent<Text>();
        Setup();
        
    }
    void Setup()
    {
        if (!LanguageMan.instance||LanguageMan.instance&&LanguageMan.instance.Data.Length==0)
        {
            Invoke(nameof(Setup), 0.1f);
        }
        else
        {
            RefreshFetch();
        }
    }
    [ContextMenu("Refresh")]
    public void RefreshFetch() 
    {
        
        if (CODE == "")
            return;
        if (myText)
        {
            myText.SetText(LanguageMan.instance.RequestForText(CODE));

        }
        if (_myText)
        {
            _myText.text = LanguageMan.instance.RequestForText(CODE);
        }
    }
}
