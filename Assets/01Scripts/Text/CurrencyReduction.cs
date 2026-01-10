using UnityEngine;
using TMPro;

public class CurrencyReduction : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    public void CurrencyReductionText(int amount)
    {
        text.text = amount.ToString();
        
    }
}
