using UnityEngine;
using System.Runtime.InteropServices;
public class URLReader : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string GetURLFromPage();

    [DllImport("__Internal")]
    private static extern string GetQueryParam(string paramId);

    public string ReadQueryParam(string paramId)
    {
        return GetQueryParam(paramId);
    }

    public string ReadURL()
    {
        return GetURLFromPage();
    }
}