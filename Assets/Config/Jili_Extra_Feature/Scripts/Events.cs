namespace Config_Assets
{
    using UnityEngine;

    public class Events : MonoBehaviour
    {
        private void OnEnable()
        {
            ExtraMan.Instance.fakeLoading.Open(3);
        }
    }
}
