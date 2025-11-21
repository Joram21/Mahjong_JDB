using UnityEngine;
namespace Config_Assets
{
    using UnityEngine.UI;
    public class JdbExtraBtn : MonoBehaviour
    {
        public Transform Parent;
        bool IsDragged;
        Vector2 StartPos;
        Vector3 offset;
        public Button[] Btns;
        float DragTimestamp;
        public Vector2 IdlePos;
        float thescale;
        public float OpenRightPos;
        public float OpenLeftPos;

        public float CloseRightPos;
        public float CloseLeftPos;
        public PosAnim OpenAnim;
        public PosAnim CloseAnim;
        private void Start()
        {
            thescale = Parent.localScale.x;
            IdlePos= Parent.GetComponent<RectTransform>().anchoredPosition;
        }
        void Update()
        {
            if (IsDragged)
            {
              //  Debug.Log("Dragged");
                if (Vector2.SqrMagnitude(StartPos-new Vector2(Input.mousePosition.x,Input.mousePosition.y)) > 10)
                {
                    Parent.position = Input.mousePosition+offset;
                }
                DragTimestamp += Time.deltaTime;

            }
            else
            {
                if (DragTimestamp > 0)
                {
                    if (Parent.position.x > (float)Screen.width / 2)
                    {
                        // Debug.Log("Right");
                        Parent.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
                        Parent.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                        //Parent.localScale = new Vector3(-thescale, thescale, thescale);
                        OpenAnim.TargetData[0].TargetPos = new Vector3(CloseRightPos, 0, 0);
                        OpenAnim.TargetData[1].TargetPos = new Vector3(OpenRightPos, 0, 0);

                        CloseAnim.TargetData[0].TargetPos = new Vector3(OpenRightPos, 0, 0);
                        CloseAnim.TargetData[1].TargetPos = new Vector3(CloseRightPos, 0, 0);
                        CloseAnim.Target = 1;
                        OpenAnim.Target = 1;
                    }
                    else
                    {
                        //   Debug.Log("Left");
                        Parent.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                        Parent.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                        //Parent.localScale = new Vector3(thescale, thescale, thescale);
                        OpenAnim.TargetData[0].TargetPos = new Vector3(CloseLeftPos, 0, 0);
                        OpenAnim.TargetData[1].TargetPos = new Vector3(OpenLeftPos, 0, 0);

                        CloseAnim.TargetData[0].TargetPos = new Vector3(OpenLeftPos, 0, 0);
                        CloseAnim.TargetData[1].TargetPos = new Vector3(CloseLeftPos, 0, 0);

                        CloseAnim.Target = 1;
                        OpenAnim.Target =1;
                    }
                    DragTimestamp = 0;
                    IdlePos.y=Parent.GetComponent<RectTransform>().anchoredPosition.y;
                    if (IdlePos.y > -100)
                    {
                        IdlePos.y = -100;
                    }else if(IdlePos.y< (-(float)Screen.currentResolution.height / 2)+100)
                    {
                        IdlePos.y = -(float)Screen.currentResolution.height / 2 + 100;
                    }
                    
                }
                Parent.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(Parent.GetComponent<RectTransform>().anchoredPosition, IdlePos, 10 * Time.deltaTime);
            }
            if (DragTimestamp > 0.2f)
            {
                ToggleBtns(false);

            }
            else
            {
                ToggleBtns(true);
            }


        }
        void ToggleBtns(bool IsTrue)
        {
            for(int i = 0; i < Btns.Length; i++)
            {
                Btns[i].enabled = IsTrue;
            }
        }
        public void Mouse_Down()
        {
            DragTimestamp = 0;
            StartPos = Input.mousePosition;
            offset = new Vector2(Parent.position.x,Parent.position.y) - StartPos;
            IsDragged = true;
        }
        public void Mouse_Up()
        {
            IsDragged = false;
        }
    }
}
