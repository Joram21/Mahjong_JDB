using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
[System.Serializable]
public class HeaderOnPressed : UnityEvent { }
public class HeaderBtns : MonoBehaviour
{
    public bool CanDeactivateBtn = true;
    [System.Serializable]
    public class HeaderFunction
    {
        public GameObject TargetObj;
        public GameObject[] SameTargetObj;
        public Button TheBtn;
        public Button[] SameBtns;
        public GameObject TheBtnHolder;
        public GameObject[] SameBtnHolder;
        public HeaderOnPressed OnPresssed;
    }
    public HeaderFunction[] funtions;
    private void Start()
    {
        Pressed(funtions[0].TheBtn);
    }
    public void Pressed(Button Which)
    {
        int active = 0;
        for(int i = 0; i < funtions.Length; i++)
        {
            bool IsCurrent = false;
            Button TargetBtn = funtions[i].TheBtn;
            if (TargetBtn == Which)
            {
                IsCurrent = this;
            }
            else
            {
                if (funtions[i].SameBtns.Length > 0)
                {
                    for (int r = 0; r < funtions[i].SameBtns.Length; r++)
                    {
                        if (funtions[i].SameBtns[r] == Which)
                        {
                            IsCurrent = true;
                            break;
                        }
                    }
                }
            }

            if (IsCurrent)
            {
                if (funtions[i].TargetObj)
                {
                    funtions[i].TargetObj.SetActive(true);
                }
                for (int r = 0; r < funtions[i].SameTargetObj.Length; r++)
                {
                    funtions[i].SameTargetObj[r].SetActive(true);
                }
                if (funtions[i].TheBtn&&CanDeactivateBtn)
                {
                    funtions[i].TheBtn.interactable = false;

                }
              
                if (funtions[i].TheBtnHolder) { 
                    funtions[i].TheBtnHolder.SetActive(true);

                }
                for(int r=0;r< funtions[i].SameBtnHolder.Length; r++)
                {
                    funtions[i].SameBtnHolder[r].SetActive(true);
                }
                active = i;

            }
            else
            {
                if (funtions[i].TargetObj)
                {
                    funtions[i].TargetObj.SetActive(false);

                }
                for (int r = 0; r < funtions[i].SameTargetObj.Length; r++)
                {
                    funtions[i].SameTargetObj[r].SetActive(false);
                }
                if (funtions[i].TheBtn&&CanDeactivateBtn)
                {
                    funtions[i].TheBtn.interactable = true;


                }
                if (funtions[i].TheBtnHolder)
                {
                    funtions[i].TheBtnHolder.SetActive(false);

                }
                for (int r = 0; r < funtions[i].SameBtnHolder.Length; r++)
                {
                    funtions[i].SameBtnHolder[r].SetActive(false);
                }
            }

            for (int r = 0; r < funtions[i].SameBtns.Length; r++)
            {
                funtions[i].SameBtns[r].interactable = funtions[i].TheBtn.interactable;
            }
        }
        funtions[active].OnPresssed.Invoke();

    }
}
