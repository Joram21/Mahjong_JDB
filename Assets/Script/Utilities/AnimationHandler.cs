using DG.Tweening;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    public void DoTotalWildsRewardedAnim()
    {
        Keyframe[] ks = new Keyframe[4];
        ks[0] = new Keyframe(0, 0);
        ks[1] = new Keyframe(0.25f, 1);
        ks[2] = new Keyframe(0.75f, -1);
        ks[3] = new Keyframe(1f, 0);
        var animCurve = new AnimationCurve(ks);
        transform.DOScale(new Vector3(1.65f, 1.75f, 1f), 0.6f).SetEase(animCurve).onComplete += () =>
        {
            transform.localScale = new Vector3(1.5f, 1.6f, 1f);
        };
    }
}
