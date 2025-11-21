using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple script to handle animation events for mask transformation.
/// Attach this to your mask animation GameObject.
/// </summary>
public class MaskAnimationEventHandler : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Called by animation event during the mask transform animation.
    /// This method will be called at the specific frame you set in the animation.
    /// </summary>
    public void OnMaskTransformationPoint()
    {
        if (enableDebugLogs)
            // Debug.Log("[MaskAnimationEventHandler] Animation event triggered - executing mask transformation");

        // Call the FreeSpinManager to execute the actual symbol transformation
        if (FreeSpinManager.Instance != null)
        {
            // Add safety check to prevent multiple calls
            FreeSpinManager.Instance.ExecuteCentralizedMaskTransformationOnce();
        }
        else
        {
            // Debug.LogError("[MaskAnimationEventHandler] FreeSpinManager.Instance is null!");
        }
    }


    /// <summary>
    /// Optional: Called when transformation animation starts
    /// </summary>
    public void OnTransformationStart()
    {
        // if (enableDebugLogs)
        // Debug.Log("[MaskAnimationEventHandler] Transformation animation started");
    }

    /// <summary>
    /// Optional: Called when transformation animation ends
    /// </summary>
    public void OnTransformationEnd()
    {
        // if (enableDebugLogs)
        // Debug.Log("[MaskAnimationEventHandler] Transformation animation ended");
    }
}