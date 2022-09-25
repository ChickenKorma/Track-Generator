using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderUpdate: MonoBehaviour
{
    private bool intValue;

    private TMP_Text valueText;

    private Slider slider;

    private float initialValue;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        valueText = GetComponentInChildren<TMP_Text>();

        intValue = slider.wholeNumbers;
        initialValue = slider.value;
    }

    private void Start()
    {
        UpdateValue();
    }

    public void UpdateValue()
    {
        if (intValue)
        {
            valueText.text = slider.value.ToString();
        }
        else
        {
            valueText.text = slider.value.ToString("F2");
        }
    }

    public void ResetValue()
    {
        slider.value = initialValue;

        UpdateValue();
    }
}
