using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{
    public Slider redSlider;
    public Slider blueSlider;

    public int RedCount => (int)redSlider.value;
    public int BlueCount => (int)blueSlider.value;
}
