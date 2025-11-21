namespace Config_Assets
{
    using UnityEngine;

    public class InfoHolder : MonoBehaviour
    {
        public TheInfo WhichInfo;
        public void UseRichCard()
        {
            ExtraMan.Instance.richCardMan.richCard_ThisGame.UseRichCard();
        }
    }
}