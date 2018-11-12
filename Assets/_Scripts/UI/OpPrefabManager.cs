using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OpPrefabManager : MonoBehaviour
{
    public int Index;
    public PolyPreset Preset;
    
    public Dropdown OpTypeDropdown;
    public Dropdown FaceSelectionDropdown;
    public Slider AmountSlider;
    public InputField AmountInput;
    public Toggle DisabledToggle;
    public Button UpButton;
    public Button DownButton;
    public Button DeleteButton;
}
