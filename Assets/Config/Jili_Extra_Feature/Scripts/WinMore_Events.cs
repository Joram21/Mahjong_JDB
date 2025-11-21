using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WinMore_Events : MonoBehaviour
{
    public bool IsLoading;
    public GameObject LoadingObj;
    public GameObject EventsObj;
 
   
    public void ShowEvents()
    {
        StopAllCoroutines();
        StartCoroutine(_showEvents());
    }
    IEnumerator _showEvents()
    {
        IsLoading = true;
        LoadingObj.SetActive(true);
        EventsObj.SetActive(false);
        yield return new WaitForSeconds(3f);
        LoadingObj.SetActive(false);
        EventsObj.SetActive(true);
        IsLoading = false;
    }
}
