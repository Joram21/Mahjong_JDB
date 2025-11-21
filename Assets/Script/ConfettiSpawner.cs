using UnityEngine;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class ConfettiSpawner : MonoBehaviour
{
    public GameObject confettiPrefab;
    public RectTransform spawnArea;
    public RectTransform targetPoint; // Reference to the slot machine top position
    public int confettiCount = 40;

    [Header("Visual Settings")]
    [Range(0.5f, 2.0f)] public float desiredFlightTime = 1.0f; // Time to reach peak in seconds
    [Range(0.1f, 1.0f)] public float spawnWidthMultiplier = 1f;
    [Range(0.01f, 0.1f)] public float delayBetweenSpawns = 0.02f;
    [Range(0.1f, 1.0f)] public float spreadFactor = 0.3f; // How wide the confetti spreads

    public bool IsSpawning { get; private set; }
    private Coroutine spawnRoutine;

    // Automatically calculated parameters
    private float calculatedLaunchForce;
    private float calculatedLinearDamping;
    private float calculatedTorque;
    private float calculatedGravityScale;

    // --- MODIFICATION START ---
    // Reference to the root canvas to get the UI scale factor. This is more reliable than screen pixels.
    public Canvas rootCanvas;
    // --- MODIFICATION END ---

    private void Awake()
    {
        // Get the root canvas in the hierarchy. This is crucial for getting the correct UI scale factor.
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            rootCanvas = parentCanvas.rootCanvas;
            Debug.Log($"[ConfettiSpawner] Root canvas found: {rootCanvas.name}, scale factor: {rootCanvas.scaleFactor}");
        }
        else
        {
            Debug.LogWarning("[ConfettiSpawner] No parent canvas found! Confetti scaling may not work correctly.");
        }

        if (targetPoint == null)
        {
            Debug.LogWarning("[ConfettiSpawner] Target point not set! Please assign a RectTransform at the top of the slot machine.");
        }
        
        if (spawnArea == null)
        {
            Debug.LogError("[ConfettiSpawner] Spawn area is not assigned!");
        }
    }

    public void LaunchConfetti()
    {
        Debug.Log("[ConfettiSpawner] LaunchConfetti called!");
        if (confettiPrefab == null)
    {
        Debug.LogError("[ConfettiSpawner] Confetti prefab is NULL!");
        return;
    }
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnConfettiLoop());
        Debug.Log("Spawn routine working");
    }

    public void StopConfetti()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void CalculatePhysicsParameters(Vector3 spawnPos)
    {
        if (targetPoint == null) return;

        float heightDifference = targetPoint.position.y - spawnPos.y;
        float g = Physics2D.gravity.magnitude;

        // Use the canvas scale factor for consistent physics scaling
        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

        calculatedLaunchForce = (2 * heightDifference) / (desiredFlightTime * desiredFlightTime);
        
        // This calculation is now primarily a fallback for non-portrait modes
        if (g > 0)
        {
            // We divide by scaleFactor here because heightDifference is already scaled up by the canvas
            calculatedGravityScale = (calculatedLaunchForce / g) / scaleFactor;
        }

        calculatedLinearDamping = 0.5f * scaleFactor * (1 + heightDifference / (500f * scaleFactor));
        calculatedTorque = calculatedLaunchForce * 0.075f * scaleFactor;
    }

    private float lifetimeMultiplier = 1;
    private IEnumerator SpawnConfettiLoop()
    {
        Debug.Log("[ConfettiSpawner] SpawnConfettiLoop initiated!");
        
        if (spawnArea == null)
        {
            Debug.LogError("[ConfettiSpawner] Spawn area is NULL! Cannot spawn confetti.");
            yield break;
        }
        
        if (confettiPrefab == null)
        {
            Debug.LogError("[ConfettiSpawner] Confetti prefab is NULL! Cannot spawn confetti.");
            yield break;
        }
        
        Debug.Log($"[ConfettiSpawner] Spawn area: {spawnArea.name}, active: {spawnArea.gameObject.activeInHierarchy}");
        Debug.Log($"[ConfettiSpawner] Spawn area position: {spawnArea.position}, world position: {spawnArea.TransformPoint(Vector3.zero)}");
        
        IsSpawning = true;
        
        var viewMan = FindFirstObjectByType<ViewMan>();
        ScreenCategory currentCategory = viewMan != null ? viewMan.CurrentScreenCategory : ScreenCategory.Landscape;

        Rect rect = spawnArea.rect;
        float actualWidth = rect.width * spawnWidthMultiplier;

        while (true)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-actualWidth / 2f, actualWidth / 2f),
                Random.Range(-rect.height / 4f, rect.height / 4f),
                0f
            );

            Vector3 spawnPos = spawnArea.TransformPoint(randomOffset);
            
            // Spawn confetti in the same parent as spawn area (not as child of spawn area)
            Transform spawnParent = spawnArea.parent != null ? spawnArea.parent : spawnArea.root;
            GameObject confetti = Instantiate(confettiPrefab, spawnPos, Quaternion.identity, spawnParent);
            
            // Debug.Log($"[ConfettiSpawner] Spawned confetti at world position: {spawnPos}, parent: {spawnParent.name}");
            
            // --- MODIFICATION START: Using Canvas Scale Factor for physics calculations ---
            // Get the scale factor from the canvas. Fallback to 1 if canvas is not found.
            float uiScaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

            if (currentCategory == ScreenCategory.Portrait)
            {
                // The base values are what looked good on your reference device (likely with a scale factor near 1.0)
                // Now we multiply them by the actual current UI scale factor for consistency.
                const float baseGravity = 750f;
                const float baseMinForce = 2500f;
                const float baseMaxForce = 4000f;

                calculatedGravityScale = baseGravity * uiScaleFactor;
                calculatedLaunchForce = Random.Range(baseMinForce * uiScaleFactor, baseMaxForce * uiScaleFactor);

                // Set other portrait-specific properties
                confetti.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
                spreadFactor = 0f;
                lifetimeMultiplier = 4f;
                delayBetweenSpawns = 0.02f;
            }
            else // Landscape or Tablet
            {
                CalculatePhysicsParameters(spawnPos);
                
                confetti.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                spreadFactor = 0.1f;
                lifetimeMultiplier = 4f;
                lifetimeMultiplier = 4f;
            }
            // --- MODIFICATION END ---

            Rigidbody2D rb = confetti.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = calculatedGravityScale;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.linearDamping = calculatedLinearDamping;
                rb.freezeRotation = true;

                float angleVariation = Random.Range(-15f, 15f) * spreadFactor;
                Vector2 direction = Quaternion.Euler(0, 0, angleVariation) * Vector2.up;

                float massAdjustedForce = calculatedLaunchForce * rb.mass;
                rb.AddForce(direction * massAdjustedForce, ForceMode2D.Impulse);

                if (rb.linearVelocity.magnitude > massAdjustedForce * 1.2f)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * (massAdjustedForce * 1.2f);
                }
            }
            
            ConfettiAutoDestroy destroyer = confetti.AddComponent<ConfettiAutoDestroy>();
            destroyer.SetLifetime(desiredFlightTime * lifetimeMultiplier); 

            yield return new WaitForSeconds(Random.Range(delayBetweenSpawns * 0.5f, delayBetweenSpawns));
        }
    }
}