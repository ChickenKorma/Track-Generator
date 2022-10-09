using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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

    // Update the accompanying text with the correct decimal places
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

        DeselectUIObject();
    }

    // Reset teh slider value to the starting value
    public void ResetValue()
    {
        slider.value = initialValue;

        UpdateValue();

        DeselectUIObject();
    }

    // Deselects the currently selected UI element in the event system, fixes visuals of buttons
    public void DeselectUIObject()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
