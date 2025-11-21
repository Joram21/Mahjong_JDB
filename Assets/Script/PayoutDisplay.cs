using System.Collections;
using System.Collections.Generic;
// using System.Threading.Tasks.Dataflow;
using UnityEngine;
using UnityEngine.UI;

// This script is intended to be placed on a prefab that will display payouts using sprite images
public class PayoutDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Transform digitContainer; // Parent object for digit images
    [SerializeField] private Transform bluedigitContainer; // Parent object for blue digit images
    [SerializeField] private GameObject bluedigitPrefab;   // Image prefab for each digit

    [SerializeField] private GameObject digitPrefab;   // Image prefab for each digit
    [SerializeField] private GameObject decimalPointPrefab; // Image prefab for decimal point
    [SerializeField] private GameObject bluedecimalPointPrefab; // Image prefab for decimal point

    [SerializeField] private GameObject dollarSignPrefab; // Optional dollar sign prefab
    [SerializeField] private GameObject commaPrefab;
    [SerializeField] private GameObject bluecommaPrefab;


    [Header("Digit Sprites")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10]; // Sprites for digits 0-9
    [SerializeField] private Sprite[] bluedigitSprites = new Sprite[10]; // Sprites for blue digits 0-9

    [SerializeField] private float digitSpacing = 0f;  // Spacing between digits
    [SerializeField] private bool showDollarSign = true; // Whether to show a dollar sign

    // [Header("Animation Settings")]
    // [SerializeField] private float initialScale = 1.5f; // Initial scale (larger than final)
    // [SerializeField] private float animationDuration = 0.5f;
    // [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    // [SerializeField] private bool bounceEffect = true; // Whether to use bounce effect

    // [Header("Size Constraints")]
    // [SerializeField] private float maximumWidth = 210f;
    // [SerializeField] private float minimumScale = 0.5f; // Don't go smaller than this
    // [SerializeField] private float maximumScale = 1.5f;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    private List<GameObject> activeDigits = new List<GameObject>();
    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    void Awake()
    {
        // Validate required components and objects
        if (digitContainer == null && bluedigitContainer == null)
        {
            digitContainer = transform;
            bluedigitContainer = transform;
        }

        // // Store original scale for animations
        // originalScale = transform.localScale;
    }

    // void Start()
    // {
    //     // Only run animations at runtime
    //     if (Application.isPlaying)
    //     {
    //         // Play entrance animation
    //         transform.localScale = Vector3.zero;
    //         if (animationCoroutine != null)
    //         {
    //             StopCoroutine(animationCoroutine);
    //         }

    //         if (bounceEffect)
    //         {
    //             animationCoroutine = StartCoroutine(AnimateScaleWithBounce(Vector3.zero, originalScale, animationDuration));
    //         }
    //         else
    //         {
    //             animationCoroutine = StartCoroutine(AnimateScale(Vector3.zero, originalScale, animationDuration));
    //         }
    //     }
    // }

    /// <summary>
    /// ENHANCED: SetPayout with better debugging and position validation
    /// </summary>
    public void SetPayout(float amount)
    {
        // Clear any existing digits
        ClearDigits();

        // Convert amount to string with 2 decimal places
        string amountStr = amount.ToString("N2");

        // First, calculate the total width so we can center properly
        float totalWidth = CalculateTotalWidth(amountStr);

        // Start position for layout - now we'll start from the left based on total width
        float currentXPos = -totalWidth / 2f;

        // Add dollar sign if enabled
        if (showDollarSign && dollarSignPrefab != null)
        {
            GameObject dollarObj = Instantiate(dollarSignPrefab, digitContainer);
            RectTransform dollarRect = dollarObj.GetComponent<RectTransform>();
            dollarRect.anchoredPosition = new Vector2(currentXPos, 0);
            activeDigits.Add(dollarObj);
            currentXPos += dollarRect.sizeDelta.x + digitSpacing;
        }

        // Create images for each digit and decimal point
        for (int i = 0; i < amountStr.Length; i++)
        {
            char c = amountStr[i];

            if (c == '.')
            {
                // Add decimal point
                if (decimalPointPrefab != null)
                {
                    GameObject decimalObj = Instantiate(decimalPointPrefab, digitContainer);
                    RectTransform decimalRect = decimalObj.GetComponent<RectTransform>();
                    decimalRect.pivot = new Vector2(0f, 1f);


                    // Force position with a larger offset 
                    decimalRect.anchoredPosition = new Vector2(currentXPos, 110);

                    // Ensure any layout components don't override our position
                    LayoutElement layoutElement = decimalObj.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                        layoutElement = decimalObj.AddComponent<LayoutElement>();
                    layoutElement.ignoreLayout = true;

                    activeDigits.Add(decimalObj);
                    float extraDecimalSpacing = 20f;
                    currentXPos += decimalRect.sizeDelta.x + digitSpacing + extraDecimalSpacing;
                }
            }
            else if (c == ',')
            {
                // Add comma
                if (commaPrefab != null)
                {
                    GameObject commaObj = Instantiate(commaPrefab, digitContainer);
                    RectTransform commaRect = commaObj.GetComponent<RectTransform>();
                    commaRect.anchoredPosition = new Vector2(currentXPos, 0);
                    activeDigits.Add(commaObj);
                    currentXPos += commaRect.sizeDelta.x + digitSpacing;
                }
            }
            else if (char.IsDigit(c))
            {
                // Add digit
                int digit = c - '0';
                if (digitPrefab != null && digit >= 0 && digit < digitSprites.Length)
                {
                    GameObject digitObj = Instantiate(digitPrefab, digitContainer);
                    Image digitImage = digitObj.GetComponent<Image>();

                    if (digitImage != null && digitSprites[digit] != null)
                    {
                        digitImage.sprite = digitSprites[digit];

                        RectTransform digitRect = digitObj.GetComponent<RectTransform>();
                        digitRect.pivot = new Vector2(0.5f, 0.5f);
                        digitRect.anchoredPosition = new Vector2(currentXPos, 0);
                        activeDigits.Add(digitObj);

                        currentXPos += digitRect.sizeDelta.x + digitSpacing;
                    }
                    else
                    {
                        Destroy(digitObj);
                    }
                }
            }
        }

        if (enableDebugLogs)
        {
            // Validate final positioning
            ValidateDisplayPosition();
        }
    }
    public void SetPayoutBlue(float amount)
    {
        // Clear any existing digits
        ClearDigits();

        // Convert amount to string with 2 decimal places
        string amountStr = amount.ToString("N2");

        // First, calculate the total width so we can center properly
        float totalWidth = CalculateTotalWidth(amountStr);

        // Start position for layout - now we'll start from the left based on total width
        float currentXPos = -totalWidth / 2f;

        // Add dollar sign if enabled
        if (showDollarSign && dollarSignPrefab != null)
        {
            GameObject dollarObj = Instantiate(dollarSignPrefab, digitContainer);
            RectTransform dollarRect = dollarObj.GetComponent<RectTransform>();
            dollarRect.anchoredPosition = new Vector2(currentXPos, 0);
            activeDigits.Add(dollarObj);
            currentXPos += dollarRect.sizeDelta.x + digitSpacing;
        }

        // Create images for each digit and decimal point
        for (int i = 0; i < amountStr.Length; i++)
        {
            char c = amountStr[i];

            if (c == '.')
            {
                // Add decimal point
                if (bluedecimalPointPrefab != null)
                {
                    GameObject decimalObj = Instantiate(bluedecimalPointPrefab, bluedigitContainer);
                    RectTransform decimalRect = decimalObj.GetComponent<RectTransform>();

                    // Force position with a larger offset 
                    decimalRect.anchoredPosition = new Vector2(currentXPos, -30);

                    // Ensure any layout components don't override our position
                    LayoutElement layoutElement = decimalObj.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                        layoutElement = decimalObj.AddComponent<LayoutElement>();
                    layoutElement.ignoreLayout = true;

                    activeDigits.Add(decimalObj);
                    currentXPos += decimalRect.sizeDelta.x + digitSpacing;
                }
            }
            else if (c == ',')
            {
                // Add comma
                if (bluecommaPrefab != null)
                {
                    GameObject commaObj = Instantiate(bluecommaPrefab, bluedigitContainer);
                    RectTransform commaRect = commaObj.GetComponent<RectTransform>();
                    commaRect.anchoredPosition = new Vector2(currentXPos, 0);
                    activeDigits.Add(commaObj);
                    currentXPos += commaRect.sizeDelta.x + digitSpacing;
                }
            }
            else if (char.IsDigit(c))
            {
                // Add digit
                int digit = c - '0';
                if (bluedigitPrefab != null && digit >= 0 && digit < bluedigitSprites.Length)
                {
                    GameObject digitObj = Instantiate(bluedigitPrefab, bluedigitContainer);
                    Image digitImage = digitObj.GetComponent<Image>();

                    if (digitImage != null && bluedigitSprites[digit] != null)
                    {
                        digitImage.sprite = bluedigitSprites[digit];

                        RectTransform digitRect = digitObj.GetComponent<RectTransform>();
                        digitRect.anchoredPosition = new Vector2(currentXPos, 0);
                        activeDigits.Add(digitObj);

                        currentXPos += digitRect.sizeDelta.x + digitSpacing;
                    }
                    else
                    {
                        Destroy(digitObj);
                    }
                }
            }
        }

        if (enableDebugLogs)
        {
            // Validate final positioning
            ValidateDisplayPosition();
        }
    }

    /// <summary>
    /// NEW: Validate the display is positioned correctly
    /// </summary>
    private void ValidateDisplayPosition()
    {
        if (!enableDebugLogs) return;

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Check if we're in viewport
            Canvas parentCanvas = GetComponentInParent<Canvas>();

            // Check if position looks reasonable
            Vector3 pos = rectTransform.position;
        }
    }

    // Calculate the total width of the display based on the value string
    private float CalculateTotalWidth(string amountStr)
    {
        float totalWidth = 0f;

        // Include dollar sign in calculation if enabled
        if (showDollarSign && dollarSignPrefab != null)
        {
            RectTransform dollarRect = dollarSignPrefab.GetComponent<RectTransform>();
            if (dollarRect != null)
            {
                totalWidth += dollarRect.sizeDelta.x + digitSpacing;
            }
        }

        // Calculate width for each character
        for (int i = 0; i < amountStr.Length; i++)
        {
            char c = amountStr[i];

            if (c == '.')
            {
                if (decimalPointPrefab != null)
                {
                    RectTransform decimalRect = decimalPointPrefab.GetComponent<RectTransform>();
                    if (decimalRect != null)
                    {
                        totalWidth += decimalRect.sizeDelta.x + digitSpacing;
                    }
                }
            }
            else if (c == ',')
            {
                if (commaPrefab != null)
                {
                    RectTransform commaRect = commaPrefab.GetComponent<RectTransform>();
                    if (commaRect != null)
                    {
                        totalWidth += commaRect.sizeDelta.x + digitSpacing;
                    }
                }
            }
            else if (char.IsDigit(c))
            {
                if (digitPrefab != null)
                {
                    RectTransform digitRect = digitPrefab.GetComponent<RectTransform>();
                    if (digitRect != null)
                    {
                        totalWidth += digitRect.sizeDelta.x + digitSpacing;
                    }
                }
            }
        }

        // Remove the last spacing as it's not needed
        if (totalWidth > digitSpacing)
        {
            totalWidth -= digitSpacing;
        }

        return totalWidth;
    }

    private void ClearDigits()
    {
        foreach (GameObject digit in activeDigits)
        {
            if (digit != null)
            {
                Destroy(digit);
            }
        }
        activeDigits.Clear();
    }

    // private IEnumerator AnimateScale(Vector3 start, Vector3 end, float duration)
    // {
    //     float elapsed = 0;

    //     while (elapsed < duration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
    //         transform.localScale = Vector3.Lerp(start, end, t);
    //         yield return null;
    //     }

    //     transform.localScale = end;
    //     animationCoroutine = null;
    // }

    // private IEnumerator AnimateScaleWithBounce(Vector3 start, Vector3 end, float duration)
    // {
    //     // First grow larger than final size
    //     Vector3 bounceScale = end * initialScale;
    //     float halfDuration = duration * 0.6f;

    //     float elapsed = 0;
    //     while (elapsed < halfDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / halfDuration));
    //         transform.localScale = Vector3.Lerp(start, bounceScale, t);
    //         yield return null;
    //     }

    //     // Then shrink to final size
    //     elapsed = 0;
    //     while (elapsed < halfDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / halfDuration));
    //         transform.localScale = Vector3.Lerp(bounceScale, end, t);
    //         yield return null;
    //     }

    //     transform.localScale = end;
    //     animationCoroutine = null;
    // }

    public void Hide()
    {
        // if (animationCoroutine != null)
        // {
        //     StopCoroutine(animationCoroutine);
        // }
        // animationCoroutine = StartCoroutine(AnimateScale(transform.localScale, Vector3.zero, animationDuration));

        // Queue destroy once animation is complete
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DestroyAfterDelay(0.5f));
        }
        else
        {
            OnComplete();
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnComplete();
    }

    public void OnComplete()
    {
        Destroy(gameObject);
    }

    // ==========================================
    // DEBUGGING METHODS
    // ==========================================

    /// <summary>
    /// NEW: Debug method to log current state
    /// </summary>
    [ContextMenu("Debug Payout Display State")]
    public void DebugPayoutDisplayState()
    {
        // Check if we're in the right canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();

        // Debug digit positions
        for (int i = 0; i < activeDigits.Count; i++)
        {
            if (activeDigits[i] != null)
            {
                RectTransform digitRect = activeDigits[i].GetComponent<RectTransform>();
            }
        }
    }

    /// <summary>
    /// NEW: Force position update (useful for testing)
    /// </summary>
    [ContextMenu("Force Position Update")]
    public void ForcePositionUpdate()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    /// <summary>
    /// NEW: Test method to verify prefab references
    /// </summary>
    [ContextMenu("Validate Prefab References")]
    public void ValidatePrefabReferences()
    {
    }
}