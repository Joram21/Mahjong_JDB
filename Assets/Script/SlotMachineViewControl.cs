using UnityEngine;

public class SlotMachineViewControl : MonoBehaviour
{
    public Transform TheBar;
    public Transform LandScape;
    public Transform Potrait;
    public Transform Tablet;
    public void SetPotrait()
    {
        TheBar.transform.SetParent(Potrait);
        TheBar.transform.localPosition = Vector3.zero;
        TheBar.transform.localScale = Vector3.one;
    }
    public void SetLandScape()
    {
        TheBar.transform.SetParent(LandScape);
        TheBar.transform.localPosition = Vector3.zero;
        TheBar.transform.localScale = Vector3.one;
    }
    public void SetTabletPortrait()
    {
        // You can either use a separate Transform or reuse the portrait one with different positioning
        TheBar.transform.SetParent(Tablet); // Or a new transform like TabletPortrait if needed
        TheBar.transform.localPosition = new Vector3(0, 100, 0); // example offset
        TheBar.transform.localScale = Vector3.one * 1.1f; // example scale for tablets
    }

}
