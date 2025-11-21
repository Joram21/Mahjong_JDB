namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    public class Jili_GameNameText : MonoBehaviour
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
            GetComponent<TMP_Text>().text = ExtraMan.Instance.games_Catalog.GameName(ExtraMan.Instance.GameId);
        }
    }
}
