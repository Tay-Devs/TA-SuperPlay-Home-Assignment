using DG.Tweening;
using UnityEngine;
using TMPro;

public class CurrencyReduction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float deductionDuration = 3f;

    [Header("UI")]
    [SerializeField] private TMP_Text currencyText;

    public int CurrentCurrency { get; private set; } = 3000;

    private Tween currencyTween;

    private void Start()
    {
        UpdateUI(CurrentCurrency);
    }

    public void DeductCurrency(int amount)
    {
        if (amount <= 0) return;

        int startValue = CurrentCurrency;
        int targetValue = Mathf.Max(0, CurrentCurrency - amount);

        // Kill previous tween if still running
        currencyTween?.Kill();

        currencyTween = DOTween.To(
                () => startValue,
                x =>
                {
                    CurrentCurrency = x;
                    UpdateUI(CurrentCurrency);
                },
                targetValue,
                deductionDuration
            )
            .SetEase(Ease.Linear);
    }

    private void UpdateUI(int value)
    {
        currencyText.text = value.ToString();
    }
}
