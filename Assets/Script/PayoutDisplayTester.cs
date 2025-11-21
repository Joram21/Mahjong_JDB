using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PayoutDisplayTester : MonoBehaviour
{
    [SerializeField] private GameObject payoutDisplayPrefab;
    [SerializeField] private float testAmount = 123.45f;
    [SerializeField] private RectTransform testSpawnPoint; // Changed to RectTransform for direct positioning
    [SerializeField] private GameObject containerObject;
    [SerializeField] private Button testButton;

    private GameObject currentDisplay;

    void Start()
    {
        if (testButton != null)
        {
            testButton.onClick.AddListener(SpawnPayoutDisplay);
        }
    }

    public void SpawnPayoutDisplay()
    {
        // Clean up any existing display
        if (currentDisplay != null)
        {
            Destroy(currentDisplay);
        }

        // Make sure we have a container object
        if (containerObject == null)
        {
            return;
        }

        // Instantiate under the container
        currentDisplay = Instantiate(payoutDisplayPrefab, containerObject.transform);

        // Position at the spawn point
        RectTransform displayRectTransform = currentDisplay.GetComponent<RectTransform>();
        if (displayRectTransform != null)
        {
            if (testSpawnPoint != null)
            {
                // Use the test spawn point's position directly
                displayRectTransform.anchoredPosition = testSpawnPoint.anchoredPosition;
                displayRectTransform.anchorMin = testSpawnPoint.anchorMin;
                displayRectTransform.anchorMax = testSpawnPoint.anchorMax;
                displayRectTransform.pivot = testSpawnPoint.pivot;

            }
            else
            {
                // Default to center if no spawn point
                displayRectTransform.anchoredPosition = Vector2.zero;
                displayRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                displayRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                displayRectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        // Set the payout amount
        PayoutDisplay payoutDisplay = currentDisplay.GetComponent<PayoutDisplay>();
        if (payoutDisplay != null)
        {
            payoutDisplay.SetPayout(testAmount);
        }
    }

    // Add a method to test with a specific amount
    public void SpawnWithAmount(float amount)
    {
        testAmount = amount;
        SpawnPayoutDisplay();
    }

    // Add a method to test at a specific position
    public void SpawnAtPosition(Vector2 anchoredPosition)
    {
        // Clean up any existing display
        if (currentDisplay != null)
        {
            Destroy(currentDisplay);
        }

        // Make sure we have a container object
        if (containerObject == null)
        {
            return;
        }

        // Instantiate under the container
        currentDisplay = Instantiate(payoutDisplayPrefab, containerObject.transform);

        // Position at the provided anchored position
        RectTransform displayRectTransform = currentDisplay.GetComponent<RectTransform>();
        if (displayRectTransform != null)
        {
            displayRectTransform.anchoredPosition = anchoredPosition;
            displayRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            displayRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            displayRectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        // Set the payout amount
        PayoutDisplay payoutDisplay = currentDisplay.GetComponent<PayoutDisplay>();
        if (payoutDisplay != null)
        {
            payoutDisplay.SetPayout(testAmount);
        }
    }
}