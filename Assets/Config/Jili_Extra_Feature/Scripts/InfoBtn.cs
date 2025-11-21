namespace Config_Assets
{
    using UnityEngine;

    public class InfoBtn : MonoBehaviour
    {
        public TheInfo theInfo;
        public void Pressed()
        {
            ExtraMan.Instance.infoTab.ShowInfo(theInfo);
        }
    }
}
