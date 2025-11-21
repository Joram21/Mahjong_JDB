
namespace Config_Assets
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    public class CustomDropDown : MonoBehaviour
    {
        public TMP_Text ActiveText;
        public Button[] TheList;
        int Active;
        private void Start()
        {
            Refresh();
        }
        public void Refresh()
        {
            for(int i = 0; i < TheList.Length; i++)
            {
                if (Active == i)
                {
                    TheList[i].enabled = false;
                }
                else {
                    TheList[i].enabled = true;
                }
            }
            ActiveText.text = TheList[Active].GetComponentInChildren<TMP_Text>().text;
        }
        public void SetActive(int Which)
        {
            Active = Which;
            Refresh();
        }
    }
}
