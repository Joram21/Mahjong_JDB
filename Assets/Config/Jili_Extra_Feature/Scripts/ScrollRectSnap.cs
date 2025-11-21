using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScrollRectSnap : MonoBehaviour
{
    public GameObject StartTarget;
    public float TheDist;
    private void Start()
    {
        if (StartTarget)
        {
            autosnap();
        }
    }
    [ContextMenu("AutoSnap")]
    void autosnap()
    {
        SnapTo(StartTarget.GetComponent<RectTransform>(), 1);
    }
    public void SnapTo(RectTransform target, float T)
    {
        StopAllCoroutines();
        /*if (!isActiveAndEnabled)
        {
            return;
        }*/

        RectTransform contentPanel = target.GetComponentInParent<ContentSizeFitter>().GetComponent<RectTransform>();
        ScrollRect scrollRect = target.GetComponentInParent<ScrollRect>();
        if (contentPanel == null || scrollRect == null ||
            scrollRect != null && scrollRect.gameObject.activeInHierarchy == false)
            return;
        StartCoroutine(_SnapTo(target, contentPanel, scrollRect, T));
    }
    IEnumerator _SnapTo(RectTransform target, RectTransform contentPanel, ScrollRect scrollRect, float T)
    {
        //Debug.Log("Snap");
        yield return new WaitForSeconds(T);
        if (contentPanel == null || target == null || scrollRect == null || !scrollRect.gameObject.activeSelf)
        {

        }
        else
        {
            Vector2 TargetPos = (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(contentPanel.position.x, contentPanel.position.y))
                - (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(target.position.x, target.position.y));
            if (scrollRect.horizontal)
            {
                TargetPos.y = contentPanel.anchoredPosition.y;
            }
            else if (scrollRect.vertical)
            {
                TargetPos.x = contentPanel.anchoredPosition.x;
            }
            //contentPanel.anchoredPosition =
            //  TargetPos;

            TheDist = 1000;
            float Thet = 3;

            /* while (TheDist>1|| Thet > 0)
             {
                 Thet -= Time.deltaTime;
                 // Debug.Log(TheDist);
                 TheDist = Vector2.Distance(contentPanel.anchoredPosition, TargetPos);
                 contentPanel.anchoredPosition = Vector2.Lerp(contentPanel.anchoredPosition, TargetPos, 20 * Time.deltaTime);
                 Canvas.ForceUpdateCanvases();

                 yield return new WaitForSeconds(0.001f);
             }*/
            while (Thet > 0)
            {
                Thet -= Time.deltaTime;
                contentPanel.anchoredPosition = TargetPos;
                Canvas.ForceUpdateCanvases();
            }
            contentPanel.anchoredPosition = TargetPos;
            Canvas.ForceUpdateCanvases();

        }
        //while (T > 0)
        //{
        //    Canvas.ForceUpdateCanvases();
        //    contentPanel.anchoredPosition =
        //        (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(contentPanel.position.x, contentPanel.position.y))
        //        - (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(target.position.x, target.position.y));
        //    T -= 0.01f;
        //    yield return new WaitForSeconds(0.01f);
        //}
        //Canvas.ForceUpdateCanvases();
        //contentPanel.anchoredPosition =
        //    (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(contentPanel.position.x, contentPanel.position.y))
        //    - (Vector2)scrollRect.transform.InverseTransformPoint(new Vector2(target.position.x, target.position.y));
        //  Canvas.ForceUpdateCanvases();
    }
}