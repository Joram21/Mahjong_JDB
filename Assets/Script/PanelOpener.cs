using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PanelOpener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIElementManager uIElement;
    [SerializeField] private GameObject panel;
    [SerializeField] private Image clickBlocker;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;

    private void Start()
    {
        panel.SetActive(false);
        clickBlocker.gameObject.SetActive(false);
        clickBlocker.raycastTarget = false;

        uIElement.mainFeatureButton.onClick.AddListener(OpenPanel);

        AddTriggerListener(clickBlocker.gameObject, EventTriggerType.PointerClick, ClosePanel);
    }

    private void OpenPanel()
    {
        panel.SetActive(true);
        clickBlocker.gameObject.SetActive(true);
        clickBlocker.raycastTarget = true;
        StartCoroutine(FadeIn(clickBlocker));
    }

    private void ClosePanel(BaseEventData _)
    {
        StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator FadeIn(Image target)
    {
        float alpha = 0;
        target.color = new Color(0, 0, 0, alpha);

        while (alpha < 0.5f)
        {
            alpha += Time.deltaTime / fadeDuration;
            target.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }

    private IEnumerator FadeOutAndClose()
    {
        float alpha = clickBlocker.color.a;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeDuration;
            clickBlocker.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        clickBlocker.raycastTarget = false;
        clickBlocker.gameObject.SetActive(false);
        panel.SetActive(false);
    }

    private void AddTriggerListener(GameObject obj, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
