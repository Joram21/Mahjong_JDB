namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    public class RichCardBtn : MonoBehaviour
    {
        public string TheName = "Game_A";
        public Image TheImg;
        public void Pressed()
        {
            ExtraMan.Instance.infoTab.ShowInfo(TheInfo.FreeGame);
            //GetComponentInParent<HistoryMan>().OpenHistory(this);
        }
        public void UpdateImage(Sprite _S)
        {
            TheImg.sprite = _S;
        }
    }
}
