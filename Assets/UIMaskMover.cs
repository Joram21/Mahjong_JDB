using UnityEngine;

public class UIMaskMover : MonoBehaviour
{
    public RectTransform mask;   // The parent with RectMask2D
    public RectTransform content; // The child inside

    private Vector3 lastMaskPos;

    void Start()
    {
        lastMaskPos = mask.localPosition;
    }

    void LateUpdate()
    {
        Vector3 delta = mask.localPosition - lastMaskPos;
        content.localPosition -= delta; // Cancel parent's movement
        lastMaskPos = mask.localPosition;
    }
}
