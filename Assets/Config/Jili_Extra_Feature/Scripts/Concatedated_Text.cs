namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    [System.Serializable]
    public class ConcaText
    {
        public string TheText;
        public bool UseTranslation = true;
        public bool FetchTime;
    }
    public class Concatedated_Text : MonoBehaviour
    {
        public Extra_LanguageMan extra_LanguageMan;
        public ConcaText[] TheTexts;
        private void OnEnable()
        {
            if (!extra_LanguageMan)
            {
                extra_LanguageMan = GetComponentInParent<Extra_LanguageMan>();
            }
            string FullString = "";
            for (int i = 0; i < TheTexts.Length; i++)
            {
                string which = TheTexts[i].TheText;
                if (TheTexts[i].UseTranslation)
                {
                    which = extra_LanguageMan.FetchTranslation(which);
                }
                else if (TheTexts[i].FetchTime)
                {
                    which = ExtraMan.Instance.missionsMan.MidnightTime().ToShortTimeString();
                }
                FullString = FullString + which;
            }
            GetComponent<TMP_Text>().text = FullString;
        }
    }
}
