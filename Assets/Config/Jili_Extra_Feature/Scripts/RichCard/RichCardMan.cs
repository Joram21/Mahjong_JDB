namespace Config_Assets
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using System;
    using System.Text;

    [System.Serializable]
    public class AddRichCardData
    {
        public int customer_id = 12;
        public float rich_card = 2;
        public string client_id;
        public string game_id;
    }
    public class RichCardMan : MonoBehaviour
    {
        public int AvailableRichCards;
        public RichCard_ThisGame richCard_ThisGame;
        public RichCard_OtherGame richCard_OtherGame;
        public RichCardUseMan richCardUseMan;
        public RichCardIndicator richCardIndicator;
        private void Start()
        {
            richCardIndicator.gameObject.SetActive(false);
            Refresh();
        }
        public void UpdateRichCard_ThisGame(int RichCards)
        {
            richCard_ThisGame.Count = RichCards;
            richCard_ThisGame.SpawnCards();
        }
        public void UpdateRichCard_OtherGame(int RichCards)
        {
            richCard_OtherGame.Count = RichCards;
            richCard_OtherGame.SpawnCards();
        }
        [ContextMenu("AddRichCards")]
        public void AddAvailableRichCards()
        {
            AvailableRichCards += 1;
            Refresh();

        }
        [ContextMenu("UseRichCards")]
        public void UseRichCard()
        {
            AvailableRichCards -= 1;
            if (AvailableRichCards <= 0)
            {
                AvailableRichCards = 0;
            }
            Refresh();
        }
        void Refresh()
        {
            richCardIndicator.Refresh();
            if (AvailableRichCards > 0)
            {
                richCardIndicator.gameObject.SetActive(true);
            }

        }

        [ContextMenu("CloseRichCardIndicator")]
        public void CloseRichCardIndicator()
        {
            richCardIndicator.CloseIndicator();
        }
        public void AddWins(float Amount)
        {
            richCardIndicator.AddWins(Amount);
        }
        [ContextMenu("AwardGiftCard")]
        public void AwardGiftCard(int TheLevel)
        {
            AddRichCardAmount(1, TheLevel);
        }
        public void AddRichCardAmount(float _Amount, int TheLevel)
        {
            AddRichCardData Data = new AddRichCardData();
            Data.customer_id = int.Parse(ConfigMan.Instance.PlayerId);
            Data.client_id = ConfigMan.Instance.ClientId;
            Data.game_id = ConfigMan.Instance.GameId;
            Data.rich_card = _Amount;
            string jsonString = JsonUtility.ToJson(Data);
            StartCoroutine(_AddRichCardAmount(ExtraMan.Instance.games_Catalog.ServerLink + "/api/v1/add_rich_card", jsonString, TheLevel));
        }
        IEnumerator _AddRichCardAmount(string url, string bodyJsonString, int TheLevel)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            request.SetRequestHeader("Pragma", "no-cache");
            request.SetRequestHeader("Expires", "0");
            yield return request.SendWebRequest();
            //Debug.Log("Status Code: " + request.responseCode);
            if (request.result == UnityWebRequest.Result.Success)
            {
                // PlayerPrefs.SetInt("AwardRichCard_MissionLevel_" + TheLevel.ToString(), 0);
                Debug.Log("Received: " + request.downloadHandler.text);
                ExtraMan.Instance.ForceRefresh();
            }
            else
            {
                Debug.Log("Error: " + request.error);
            }
        }


    }
}
