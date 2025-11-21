// UIElementManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIElementManager : MonoBehaviour
{
    [System.Serializable]
    public class DualButton
    {
        public Button landscape;
        public Button portrait;
    }
    [System.Serializable]
    public class OrientationUI
    {
        public Button landscape;
        public Button portrait;
        public Button tablet;
    }

    [System.Serializable]
    public class OrientationUIText
    {
        public TextMeshProUGUI landscape;
        public TextMeshProUGUI portrait;
        public TextMeshProUGUI tablet;
    }
    [System.Serializable]
    public class DualImage
    {
        public Image landscape;
        public Image portrait;
    }
    [System.Serializable]
    public class DualSpinVisuals
    {
        public Image normalLandscape;
        public Image spinningLandscape;
        public Image normalPortrait;
        public Image spinningPortrait;
        public Image circularLandscape;   // New circular image for landscape
        public Image circularPortrait;    // New circular image for portrait
    }

    [System.Serializable]
    public class DualText
    {
        public TextMeshProUGUI landscape;
        public TextMeshProUGUI portrait;
    }
    public OrientationUI autoSpinButton;
    public OrientationUI spinButton;
    public OrientationUI mainBetButton;
    public OrientationUIText spinButtonText;    
    public DualButton increaseButton;
    public DualButton decreaseButton;
   
    public DualText balanceText;
    public DualText betText;
    public DualText winText;
    
    public DualSpinVisuals spinButtonVisuals;
    public DualButton speedButton;
    public DualImage speedButtonImage;
    public Sprite normalSpeedSprite;
    public Sprite fastSpeedSprite;
    public Button targetSpinButton1; 
    public Button targetSpinButton2; 
    public Button targetSpinButton3; 
    public Button mainFeatureButton;
    public DualText TransactionID;
    public DualButton instructionButton;
    public DualText autospinbetamount;
    public DualText betbetamount;
    public DualText instructionsbetamount;
    public DualText autospinbalanceamount;
    public DualText betbalanceamount;
    public DualText instructionsbalanceamount;
}