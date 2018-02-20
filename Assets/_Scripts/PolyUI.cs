using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class PolyUI : MonoBehaviour {
    
    private PolyComponent poly;
    private RotateObject rotateObject;
    
    public Slider xRotateSlider;
    public Slider yRotateSlider;
    public Slider zRotateSlider;
    public Toggle twoSidedToggle;
    public Toggle bypassConwayToggle;
    public Button ButtonTemplate;
    public RectTransform PresetButtonContainer;
    public Dropdown BasePolyDropdown; 
    public Button SavePresetButton;
    public PolyPresets Presets;
    
    private List<Button> PresetButtons;
    private List<Button> BasePolyButtons;

    private int _rotateSliderMin = -10;
    private int _rotateSliderMax = 10;

    void Start() {
        
        poly = GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
        rotateObject = poly.GetComponent<RotateObject>();

        xRotateSlider.value = rotateObject.x;
        yRotateSlider.value = rotateObject.y;
        zRotateSlider.value = rotateObject.z;
        xRotateSlider.onValueChanged.AddListener(delegate{xSliderChanged();});
        yRotateSlider.onValueChanged.AddListener(delegate{ySliderChanged();});
        zRotateSlider.onValueChanged.AddListener(delegate{zSliderChanged();});

        poly.preset.TwoSided = twoSidedToggle.isOn;
        twoSidedToggle.onValueChanged.AddListener(delegate{twoSidedToggleChanged();});
        poly.preset.BypassConway = bypassConwayToggle.isOn;
        bypassConwayToggle.onValueChanged.AddListener(delegate{bypassConwayToggleChanged();});
        
        CreateBasePolyDropdown();
        
        SavePresetButton.onClick.AddListener(delegate{SavePresetButtonClicked();});

        try {
            Presets.LoadPresetsFromDisk();
        } catch (FileNotFoundException e) {
            Presets.CreateInitialPresets();
            Presets.SavePresetsToDisk();
        }

        PresetButtons = new List<Button>();
        CreatePresetButtons();

        UpdateUI();

    }

    void UpdateUI() {
        xRotateSlider.value = rotateObject.x;
        yRotateSlider.value = rotateObject.y;
        zRotateSlider.value = rotateObject.z;
        twoSidedToggle.isOn = poly.preset.TwoSided;
        bypassConwayToggle.isOn = poly.preset.BypassConway;
        BasePolyDropdown.value = (int)poly.preset.PolyType;
    }

    void xSliderChanged() {
        rotateObject.x = Mathf.Lerp(_rotateSliderMin, _rotateSliderMax, xRotateSlider.value);
    }

    void ySliderChanged() {
        rotateObject.y = Mathf.Lerp(_rotateSliderMin, _rotateSliderMax, yRotateSlider.value);
    }

    void zSliderChanged() {
        rotateObject.z = Mathf.Lerp(_rotateSliderMin, _rotateSliderMax, zRotateSlider.value);
    }

    void twoSidedToggleChanged() {
        poly.preset.TwoSided = twoSidedToggle.isOn;
        Presets.RebuildPoly();
    }

    void bypassConwayToggleChanged() {
        poly.preset.BypassConway = bypassConwayToggle.isOn;
        Presets.RebuildPoly();
    }

    void DestroyPresetButtons() {
        if (PresetButtons == null) {return;}
        foreach (var btn in PresetButtons) {
            Destroy(btn.gameObject);
        }
        PresetButtons.Clear();
    }

    void CreateBasePolyDropdown() {
        foreach (var polyType in Enum.GetValues(typeof(PolyPreset.PolyTypes))) {
            var polyLabel = new Dropdown.OptionData(polyType.ToString().Replace("_", " "));
            BasePolyDropdown.options.Add(polyLabel);
        }
        BasePolyDropdown.onValueChanged.AddListener(delegate{BasePolyDropdownChanged(BasePolyDropdown);});
    }

    void BasePolyDropdownChanged(Dropdown change) {
        poly.preset.PolyType = (PolyPreset.PolyTypes)change.value;
        Presets.RebuildPoly();
    }

    void CreatePresetButtons() {
        DestroyPresetButtons();
        foreach (var preset in Presets.Items) {
            var presetButton = Instantiate(ButtonTemplate);
            presetButton.transform.SetParent(PresetButtonContainer);
            presetButton.name = preset.Name;
            presetButton.GetComponentInChildren<Text>().text = preset.Name;
            presetButton.onClick.AddListener(delegate{LoadPresetButtonClicked();});
            PresetButtons.Add(presetButton);
        }
    }

    void SavePresetButtonClicked() {
        Presets.AddPreset("Preset " + Presets.Items.Count);
        CreatePresetButtons();
    }

    void LoadPresetButtonClicked() {
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        Presets.LoadPreset(buttonName);
        UpdateUI();
    }

}
