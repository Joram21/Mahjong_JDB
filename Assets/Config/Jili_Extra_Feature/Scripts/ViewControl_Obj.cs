using UnityEngine;
using UnityEditor;
public class ViewControl_Obj : MonoBehaviour
{
    public Transform TheObj;
    public Transform[] TheObjs;
    public Transform LandScape;
    public Transform Potrait;
    public void SetPotrait()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfAnyPrefab(TheObj.transform.parent))
        {
            return;
        }
#endif
        Potrait.gameObject.SetActive(true);
        if (TheObj)
        {
            
            TheObj.transform.SetParent(Potrait);
            TheObj.transform.SetAsFirstSibling();
            TheObj.transform.localPosition = Vector3.zero;
            TheObj.transform.localScale = Vector3.one;
        }
        LandScape.gameObject.SetActive(false);

        SetArray(Potrait);

    }
    public void SetLandScape()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfAnyPrefab(TheObj.transform.parent))
        {
            return;
        }
#endif
        LandScape.gameObject.SetActive(true);
        if (TheObj)
        {
            TheObj.transform.SetParent(LandScape);
            TheObj.transform.SetAsFirstSibling();
            TheObj.transform.localPosition = Vector3.zero;
            TheObj.transform.localScale = Vector3.one;
        }
        Potrait.gameObject.SetActive(false);

        SetArray(LandScape);
    }
    void SetArray(Transform TheParent)
    {
        for(int i = 0; i < TheObjs.Length; i++)
        {
            TheObjs[i].transform.SetParent(TheParent);
            TheObjs[i].transform.SetAsFirstSibling();
            TheObjs[i].transform.localPosition = Vector3.zero;
            TheObjs[i].transform.localScale = Vector3.one;
        }
    }
    private void OnEnable()
    {
        SetUp();
    }
    void SetUp()
    {
        Refresh();

    }
    public void Refresh()
    {
        if (FindFirstObjectByType<ViewMan>().IsLandScape)
        {
            SetLandScape();
        }
        else
        {
            SetPotrait();
        }
    }
}
