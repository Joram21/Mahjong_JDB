using UnityEngine;
using UnityEngine.UI;
public enum TheCurrency
{
    USD,
    Brazillian_R,
    JAPAN_JPY,
    Euro,
    KES,
    DANISH_Krone,
    THAI_Baht,
    INDONESIA_Rp,
    VIETNAM_Vnd,
    PORTOGUESE_Euro,
    KOREA_Won,
    BURMESE_MMK
}
public class CurrencyMan : MonoBehaviour
{
    public TheCurrency _Currency;
   
    public void SetCurrency(Dropdown _DropDown)
    {
        _Currency = (TheCurrency)_DropDown.value;
    }
}
