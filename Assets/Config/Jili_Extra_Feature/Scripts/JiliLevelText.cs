namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    public class JiliLevelText : MonoBehaviour
    {
        public Extra_LanguageMan extra_LanguageMan;
        public TMP_Text TheText;
        private void OnEnable()
        {
            extra_LanguageMan = GetComponentInParent<Extra_LanguageMan>();
        }
        void Update()
        {
            string TheString = "Current JILI LV";
            TheString = extra_LanguageMan.FetchTranslation(TheString);
            TheText.text = TheString + " " + ExtraMan.Instance.giftsMan.GetJiliLevel().ToString();
        }
    }
}
