namespace Config_Assets
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    public class MissionStat : MonoBehaviour
    {
        public Extra_LanguageMan extra_LanguageMan;
        public GameObject LockObj;
        public TMP_Text CountText;
        public TMP_Text TotalBetText;
        public Slider FillSlider;
        public int Level;
        public Image IconImage;
        private void OnEnable()
        {
            extra_LanguageMan = GetComponentInParent<Extra_LanguageMan>();
            Refresh();
        }

        public void Refresh()
        {
            if (!ExtraMan.Instance)
            {
                Invoke(nameof(Refresh), 0.1f);
                return;
            }
            float Amount = ExtraMan.Instance.missionsMan.GetMissionPoints(Level);
            float MaxAmount = ExtraMan.Instance.missionsMan.MissionGoals[Level];

            FillSlider.value = Amount / MaxAmount;
            CountText.text = "(" + Amount.ToString() + "/" + MaxAmount.ToString() + ")";
            string TheString = "Total bet over";
            TheString = extra_LanguageMan.FetchTranslation(TheString);
            TotalBetText.text = TheString + " " + MaxAmount.ToString();


            LockObj.SetActive(!ExtraMan.Instance.missionsMan.IsMissionUnlocked(Level));
            if (LockObj.activeSelf)
            {

            }
            else
            {

            }
            //        Debug.Log("FetchIcon"+ ExtraMan.Instance.GameId);
            IconImage.sprite = ExtraMan.Instance.games_Catalog.GetSavedRichCard(ExtraMan.Instance.GameId);
        }
    }
}
