using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OpPrefabManager : MonoBehaviour
{
    public int Index;
    public PolyPreset Preset;
    
    public Dropdown OpTypeDropdown;
    public Dropdown FaceSelectionDropdown;
    public Toggle RandomizeToggle;
    public Slider AmountSlider;
    public InputField AmountInput;
    public Toggle DisabledToggle;
    public Button UpButton;
    public Button DownButton;
    public Button DeleteButton;
    public Toggle ToggleAnimate;
    public Transform AnimationControls;
    public InputField AnimRateInput;
    public InputField AnimAmountInput;
    public InputField AudioLowAmountInput;
    public InputField AudioMidAmountInput;
    public InputField AudioHighAmountInput;

//    public void DisableAll()
//    {
//        OpTypeDropdown.interactable = false;
//        FaceSelectionDropdown.interactable = false;
//        AmountSlider.interactable = false;
//        AmountInput.interactable = false;
//        DisabledToggle.interactable = false;
//        UpButton.interactable = false;
//        DownButton.interactable = false;
//        DeleteButton.interactable = false;
//    }
//
//    public void EnableAll()
//    {
//        OpTypeDropdown.interactable = true;
//        FaceSelectionDropdown.interactable = true;
//        AmountSlider.interactable = true;
//        AmountInput.interactable = true;
//        DisabledToggle.interactable = true;
//        UpButton.interactable = true;
//        DownButton.interactable = true;
//        DeleteButton.interactable = true;
//    }
}
