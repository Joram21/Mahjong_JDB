using UnityEngine;
namespace Config_Assets
{
    public class DontDestroyObject : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
