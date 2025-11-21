namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using System.Collections;

    public class HistoryMan : MonoBehaviour
    {
        public TMP_Text GameNameText;
        public GameObject SelectionObj;
        public GameObject HistoryObj;
        public Transform SpawnTrans;
        public GameObject BtnPref;
        public List<GameObject> SpawnedBtns = new List<GameObject>();
        private void OnEnable()
        {
            OpenSelection();
        }
        public void OpenSelection()
        {
            SelectionObj.SetActive(true);
            HistoryObj.SetActive(false);
            SpawnGames();
        }
        public void OpenHistory(HistoryBtn which)
        {
            GameNameText.text = which.TheName;
            SelectionObj.SetActive(false);
            HistoryObj.SetActive(true);
        }
        void ClearBtns()
        {
            for (int i = 0; i < SpawnedBtns.Count; i++)
            {
                Destroy(SpawnedBtns[i]);
            }
            SpawnedBtns.Clear();
        }
        public void SpawnGames()
        {

            ClearBtns();
            for (int i = 0; i < ExtraMan.Instance.games_Catalog.gameList.games.Length; i++)
            {
                if (ExtraMan.Instance.games_Catalog.gameList.games[i].approved == 1)
                {
                    int TheGameId = ExtraMan.Instance.games_Catalog.gameList.games[i].thegame_id;
                    Game_Data _Game = ExtraMan.Instance.games_Catalog.gameList.games[i];
                    GameObject go = Instantiate(BtnPref, SpawnTrans);
                    go.GetComponent<HistoryBtn>().UpdateImage(ExtraMan.Instance.games_Catalog.GetSavedIcon(i));
                    go.GetComponent<HistoryBtn>().TheName = _Game.game_title;
                    SpawnedBtns.Add(go);
                }

            }
            // StartCoroutine(Refresh());
            SpawnTrans.GetComponent<Image>().enabled = false;
            Invoke(nameof(DelayUpdate), 0.1f);
        }
        void DelayUpdate()
        {
            SpawnTrans.GetComponent<Image>().enabled = true;

        }
        IEnumerator Refresh()
        {
            SpawnTrans.GetComponent<Image>().enabled = true;
            yield return new WaitForEndOfFrame();
            SpawnTrans.GetComponent<Image>().enabled = true;
        }

    }
}
