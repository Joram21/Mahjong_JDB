namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    public class Jili_IconImg : MonoBehaviour
    {

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ExtraMan.Instance)
            {
                Invoke(nameof(Refresh), 0.1f);
                return;
            }
            GetComponent<Image>().sprite = ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.GameId);
        }
    }
}
