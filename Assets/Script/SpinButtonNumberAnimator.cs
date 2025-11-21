using System;
using UnityEngine;
using System.Collections;
using TMPro;

public class SpinButtonNumberAnimator : MonoBehaviour
{
    public TextMeshProUGUI oldText;
    public TextMeshProUGUI newText;
    public float duration = 0.3f;
    public Vector2 slideOffset = new Vector2(0, 50);
    private Coroutine animRoutine;

    public void AnimateTo(int newValue)
    {
        // Don't start coroutine if GameObject is inactive
        if (!gameObject.activeInHierarchy)
        {
            // Just update the text directly if inactive
            if (newText != null)
                newText.text = newValue.ToString();
            return;
        }

        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateTextChange(newValue.ToString()));
    }


    private IEnumerator AnimateTextChange(string newValue)
    {
        // Set old text to current value
        oldText.text = newText.text;

        // Set new text to the new value
        newText.text = newValue;
        newText.rectTransform.anchoredPosition = Vector2.zero;
        newText.alpha = 0f;

        // Set old text to start position
        oldText.rectTransform.anchoredPosition = Vector2.zero;
        oldText.alpha = 1f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Animate only old text moving and fading out
            oldText.alpha = Mathf.Lerp(1f, 0f, t);
            oldText.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.zero, -slideOffset, t);

            // New text stays in place, just fades in
            newText.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Final state
        oldText.alpha = 0f;
        newText.alpha = 1f;
        oldText.rectTransform.anchoredPosition = -slideOffset;
        newText.rectTransform.anchoredPosition = Vector2.zero;
    }
}
