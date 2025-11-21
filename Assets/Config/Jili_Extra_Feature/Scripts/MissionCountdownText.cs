namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    public class MissionCountdownText : MonoBehaviour
    {
        public TMP_Text TimeText;

        void Update()
        {
            TimeText.text = ExtraMan.Instance.missionsMan.GetTime();
        }
    }
}
