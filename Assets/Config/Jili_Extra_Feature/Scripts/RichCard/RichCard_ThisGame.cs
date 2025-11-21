namespace Config_Assets
{
    using UnityEngine;

    public class RichCard_ThisGame : MonoBehaviour
    {
        public int Count = 5;
        public GameObject ThePref;
        public Transform[] SpawnPos;
        public GameObject[] NoRichCardObj;
        public GameObject[] Windows;
        public GameObject ToggleObj;
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
                    Go.GetComponent<RichCardBtn>().UpdateImage(
                        ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.GameId));
                }
            }
        }
        [ContextMenu("ClearCards")]
        void ClearCards()
        {
            for (int r = 0; r < SpawnPos.Length; r++)
            {
                RichCardBtn[] btns = SpawnPos[r].GetComponentsInChildren<RichCardBtn>();
                for (int i = 0; i < btns.Length; i++)
                {
                    Destroy(btns[i].gameObject);
                }
            }
        }
        public void UseRichCard()
        {
            Count -= 1;
            if (Count < 0)
            {
                Count = 0;
            }
            SpawnCards();


            for (int i = 0; i < Windows.Length; i++)
            {
                Windows[i].SetActive(false);
            }
            ToggleObj.SetActive(true);

            ExtraMan.Instance.richCardMan.richCardUseMan.Spawn();
        }
    }
}
