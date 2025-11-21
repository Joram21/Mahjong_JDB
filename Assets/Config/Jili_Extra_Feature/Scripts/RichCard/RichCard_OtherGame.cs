namespace Config_Assets
{
    using UnityEngine;

    public class RichCard_OtherGame : MonoBehaviour
    {
        public int Count = 5;
        public GameObject ThePref;
        public Transform[] SpawnPos;
        public GameObject[] NoRichCardObj;
        private void OnEnable()
        {
            SpawnCards();
        }
        [ContextMenu("SpawnCards")]
        public void SpawnCards()
        {
            if (!ExtraMan.Instance || ExtraMan.Instance && !ExtraMan.Instance.games_Catalog.IsLoaded)
            {
                Invoke(nameof(SpawnCards), 0.1f);
                return;
            }
            if (Count <= 0)
            {
                for (int i = 0; i < NoRichCardObj.Length; i++)
                {
                    NoRichCardObj[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < NoRichCardObj.Length; i++)
                {
                    NoRichCardObj[i].SetActive(false);
                }
            }
            ClearCards();
            for (int r = 0; r < SpawnPos.Length; r++)
            {
                for (int i = 0; i < Count; i++)
                {
                    GameObject Go = Instantiate(ThePref, SpawnPos[r]);
                    int rand = Random.Range(0, ExtraMan.Instance.games_Catalog.gameList.games.Length);
                    while (ExtraMan.Instance.games_Catalog.gameList.games[rand].id == ExtraMan.Instance.GameId ||
                         ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.games_Catalog.gameList.games[rand].id) == null)
                    {
                        rand = Random.Range(0, ExtraMan.Instance.games_Catalog.gameList.games.Length);
                    }
                    rand = ExtraMan.Instance.games_Catalog.gameList.games[rand].id;
                    //Debug.Log(rand);
                    Go.GetComponent<RichCardBtn_OtherGame>().UpdateImage(
                        ExtraMan.Instance.games_Catalog.GetSavedRichCard(rand));
                }
            }
        }
        [ContextMenu("ClearCards")]
        void ClearCards()
        {
            for (int r = 0; r < SpawnPos.Length; r++)
            {
                RichCardBtn_OtherGame[] btns = SpawnPos[r].GetComponentsInChildren<RichCardBtn_OtherGame>();
                for (int i = 0; i < btns.Length; i++)
                {
                    Destroy(btns[i].gameObject);
                }
            }
        }
        public void UseRichCard()
        {

        }

    }
}
