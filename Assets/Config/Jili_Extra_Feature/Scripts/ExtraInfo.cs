namespace Config_Assets
{
    using UnityEngine;
    public enum TheInfo
    {
        Mission_Description,
        Favorite_Info,
        FreeGift,
        RichCard,
        FreeGame
    }
    public class ExtraInfo : MonoBehaviour
    {
        public InfoHolder[] TheTabs;
        public void ShowInfo(TheInfo Which)
        {
            for (int i = 0; i < TheTabs.Length; i++)
            {
                if (TheTabs[i].WhichInfo == Which)
                {
                    TheTabs[i].gameObject.SetActive(true);
                }
                else
                {
                    TheTabs[i].gameObject.SetActive(false);
                }
            }
            if (Which != TheInfo.FreeGift)
            {
                gameObject.SetActive(true);
            }

        }
    }
}