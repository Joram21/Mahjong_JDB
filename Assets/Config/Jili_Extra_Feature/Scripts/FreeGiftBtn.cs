namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    public class FreeGiftBtn : MonoBehaviour
    {
        public Extra_LanguageMan extra_LanguageMan;
        public int Level;
        public TMP_Text LevelText;
        public GameObject[] LevelsObj;
        public TMP_Text PointsText;
        public Slider PointsSlider;
        public TMP_Text LockText;
        public GameObject LockObj;
        public GameObject ClaimBtn;
        public GameObject ClaimedHolder;
        private void OnEnable()
        {
            extra_LanguageMan = GetComponentInParent<Extra_LanguageMan>();
            Refresh();
        }
        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (!ExtraMan.Instance)
            {
                Invoke(nameof(Refresh), 0.1f);
                return;
            }
            int TheActiveLevel = PlayerPrefs.GetInt("ActiveGiftLevel");
            if (TheActiveLevel == Level && transform.GetComponentInParent<ScrollRectSnap>())
            {
                transform.GetComponentInParent<ScrollRectSnap>().SnapTo(GetComponent<RectTransform>(), 0);
            }


            LevelText.text = "LV " + (Level + 1).ToString();
            string NewString = "Upgrade to LV" + (Level + 1).ToString() + " to unlock rewards.";
            LockText.text = NewString;
            for (int i = 0; i < LevelsObj.Length; i++)
            {
                if (i == Level)
                {
                    LevelsObj[i].SetActive(true);
                }
                else
                {
                    LevelsObj[i].SetActive(false);
                }
            }

            float TheTotalPoints = ExtraMan.Instance.giftsMan.FreeMaxGiftPoints[Level];
            float thepoint = ExtraMan.Instance.giftsMan.GetGiftPoints(Level);
            PointsText.text = gettext(thepoint) + "/" + gettext(TheTotalPoints);

            PointsSlider.value = thepoint / TheTotalPoints;

            LockObj.SetActive(!ExtraMan.Instance.giftsMan.IsGiftUnlocked(Level));
            if (!LockObj.activeSelf)
            {
                if (PlayerPrefs.GetInt("ClaimGiftPoints_" + Level.ToString()) == 0)
                {
                    ClaimBtn.SetActive(true);
                    ClaimedHolder.SetActive(false);
                    ClaimBtn.GetComponent<Animator>().Play("Claim");
                }
                else
                {
                    ClaimBtn.SetActive(false);
                    ClaimedHolder.SetActive(true);
                }
            }
            else
            {
                ClaimBtn.SetActive(true);
                ClaimedHolder.SetActive(false);
            }

            extra_LanguageMan.DynamicAssignCode(LockText.GetComponent<Extra_FetchTextController>(), NewString);


        }
        string gettext(float Amount)
        {
            string Extra = "";
            if ((Amount / 1000) >= 1)
            {
                Amount = Amount / 1000;
                Extra = "K";
            }
            return Mathf.RoundToInt(Amount).ToString() + Extra;
        }
        public void ClaimReward()
        {
            if (LockObj.activeSelf || PlayerPrefs.GetInt("ClaimGiftPoints_" + Level.ToString()) == 1)
            {
                return;
            }
            ClaimBtn.GetComponent<Animator>().Rebind();
            PlayerPrefs.SetInt("ClaimGiftPoints_" + Level.ToString(), 1);
            ExtraMan.Instance.giftsMan.GoToNextLevel();
            ClaimBtn.SetActive(false);
            ClaimedHolder.SetActive(true);

        }
    }
}