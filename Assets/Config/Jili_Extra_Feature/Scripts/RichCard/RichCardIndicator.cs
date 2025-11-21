namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    public class RichCardIndicator : MonoBehaviour
    {
        public Extra_LanguageMan extra_LanguageMan;
        public GameObject AccumulatedWinObj;
        public float AccumulatedWins;

        public TMP_Text AccumulatedWinsText;
        public TMP_Text[] CountText;
        public Image[] IconImages;
        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {

            AccumulatedWinObj.SetActive(false);

            if (!ExtraMan.Instance || ExtraMan.Instance && !ExtraMan.Instance.games_Catalog.IsLoaded)
            {
                Invoke(nameof(Refresh), 0.1f);
                return;
            }
            UpdateImage();
            UpdateCount();
            ExtraMan.Instance.ForceRefresh();
        }
        public void UpdateImage()
        {
            for (int i = 0; i < IconImages.Length; i++)
            {
                IconImages[i].sprite = ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.GameId);
            }
        }
        public void UpdateCount()
        {
            for (int i = 0; i < CountText.Length; i++)
            {
                CountText[i].text = ExtraMan.Instance.richCardMan.AvailableRichCards.ToString();
            }
        }
        public void CloseIndicator()
        {
            AccumulatedWinObj.SetActive(true);
            AccumulatedWinsText.text = extra_LanguageMan.FetchTranslation("Use cards to get") + " " + AccumulatedWins.ToString("n2");
            if (ExtraMan.Instance.richCardMan.AvailableRichCards <= 0)
            {
                Invoke(nameof(FinalClose), 3);
            }
        }
        void FinalClose()
        {
            gameObject.SetActive(false);
            AccumulatedWins = 0;
            ExtraMan.Instance.ForceRefresh();
        }
        public void AddWins(float Amount)
        {
            AccumulatedWins += Amount;
        }
    }
}
