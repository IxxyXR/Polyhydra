using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class PolyUI : MonoBehaviour {
    
    public PolyComponent poly;
    private RotateObject rotateObject;

    public InputField PresetNameInput;
    public Slider XRotateSlider;
    public Slider YRotateSlider;
    public Slider ZRotateSlider;
    public Toggle TwoSidedToggle;
    public Toggle BypassOpsToggle;
    public RectTransform OperatorContainer;
    public Transform OpTemplate;
    public Button ButtonTemplate;
    public RectTransform PresetButtonContainer;
    public Dropdown BasePolyDropdown; 
    public Button SavePresetButton;
    public PolyPresets Presets;
    public GameObject[] Tabs; 
    public GameObject[] TabButtons;
    
    private List<Button> presetButtons;
    private List<Button> basePolyButtons;
    private List<Transform> opItems;

    void Start()
    {
        InitUI();
    }

    public void InitUI()
    {
        opItems = new List<Transform>();
        presetButtons = new List<Button>();
        
        rotateObject = poly.GetComponent<RotateObject>();
        PresetNameInput.onValueChanged.AddListener(delegate{PresetNameChanged();});
        SavePresetButton.onClick.AddListener(SavePresetButtonClicked);
        XRotateSlider.onValueChanged.AddListener(delegate{XSliderChanged();});
        YRotateSlider.onValueChanged.AddListener(delegate{YSliderChanged();});
        ZRotateSlider.onValueChanged.AddListener(delegate{ZSliderChanged();});
        TwoSidedToggle.onValueChanged.AddListener(delegate{TwoSidedToggleChanged();});
        BypassOpsToggle.onValueChanged.AddListener(delegate{BypassOpsToggleChanged();});
        
        try {
            Presets.LoadAllPresets();
        } catch (FileNotFoundException e) {
            Presets.ResetInitialPresets();
            Presets.SaveAllPresets();
        }

        UpdatePolyUI();
        UpdateOpsUI();
        UpdateAnimUI();
        CreatePresetButtons();
        
        ShowTab(TabButtons[0].gameObject);
    }

    void Update() {
        if (Input.GetKeyDown("space")) {
            int num = (int)poly.PolyType;
            num = (num + 1) % Enum.GetValues(typeof(PolyComponent.PolyTypes)).Length;
            poly.PolyType = (PolyComponent.PolyTypes)num;
            Presets.RebuildPoly();
        } else if (Input.GetKeyDown("p")) {
            gameObject.GetComponent<RotateObject>().Pause();
            UpdateAnimUI();
        } else if (Input.GetKeyDown("r")) {
            gameObject.GetComponent<RotateObject>().Randomize();
            UpdateAnimUI();
        } else if (Input.GetKeyDown("1")) {
            // TODO something useful
            //meshFilter.sharedMesh = _polyhedron.Explode();
        }
    }

    void UpdatePolyUI() {
        TwoSidedToggle.isOn = poly.TwoSided;
        CreateBasePolyDropdown();
        BasePolyDropdown.value = (int)poly.PolyType;
    }

    void UpdateAnimUI() {
        XRotateSlider.value = rotateObject.x;
        YRotateSlider.value = rotateObject.y;
        ZRotateSlider.value = rotateObject.z;
    }

    void UpdateOpsUI()
    {
        BypassOpsToggle.isOn = poly.BypassOps;
        CreateOps();        
    }

    void DestroyOps() {
        if (opItems == null) {return;}
        foreach (var item in opItems) {
            Destroy(item.gameObject);
        }
        opItems.Clear();
    }
    
    void CreateOps() {
        DestroyOps();
        if (poly.ConwayOperators == null) {return;}
        for (var index = 0; index < poly.ConwayOperators.Length; index++) {
            var op = poly.ConwayOperators[index];
            var opItem = Instantiate(OpTemplate);
            opItem.name = "Operator " + index;
            opItem.transform.SetParent(OperatorContainer);
            foreach (var opType in Enum.GetValues(typeof(PolyComponent.Ops))) {
                var opLabel = new Dropdown.OptionData(opType.ToString());
                opItem.GetComponentInChildren<Dropdown>().options.Add(opLabel);
            }
            opItem.GetComponentInChildren<Toggle>().isOn = !op.disabled;
            opItem.GetComponentInChildren<InputField>().text = op.amount.ToString();
            opItem.GetComponentInChildren<Dropdown>().onValueChanged.AddListener(delegate{OpDropdownChanged();});
            opItem.GetComponentInChildren<Toggle>().onValueChanged.AddListener(delegate{OpDisabledChanged();});
            opItem.GetComponentInChildren<InputField>().onValueChanged.AddListener(delegate{OpInputChanged();});
            opItems.Add(opItem);
        }
    }

    void CreateBasePolyDropdown()
    {
        BasePolyDropdown.ClearOptions();
        foreach (var polyType in Enum.GetValues(typeof(PolyComponent.PolyTypes))) {
            var polyLabel = new Dropdown.OptionData(polyType.ToString().Replace("_", " "));
            BasePolyDropdown.options.Add(polyLabel);
        }
        BasePolyDropdown.onValueChanged.AddListener(delegate{BasePolyDropdownChanged(BasePolyDropdown);});
    }
     
    void DestroyPresetButtons() {
        if (presetButtons == null) {return;}
        foreach (var btn in presetButtons) {
            Destroy(btn.gameObject);
        }
        presetButtons.Clear();
    }
    
    void CreatePresetButtons() {
        DestroyPresetButtons();
        foreach (var preset in Presets.Items) {
            var presetButton = Instantiate(ButtonTemplate);
            presetButton.transform.SetParent(PresetButtonContainer);
            presetButton.name = preset.Name;
            presetButton.GetComponentInChildren<Text>().text = preset.Name;
            presetButton.onClick.AddListener(delegate{LoadPresetButtonClicked();});
            presetButtons.Add(presetButton);
        }
    }
    
    // Event handlers

    void PresetNameChanged() {
        if (String.IsNullOrEmpty(PresetNameInput.text)) {
            SavePresetButton.interactable = false;
        } else {
            SavePresetButton.interactable = true;
            ////poly.name = PresetNameInput.text;
        }
    }

    void BasePolyDropdownChanged(Dropdown change) {
        poly.PolyType = (PolyComponent.PolyTypes)change.value;
        Presets.RebuildPoly();
    }

    void XSliderChanged() {
        rotateObject.x = XRotateSlider.value;
    }

    void YSliderChanged() {
        rotateObject.y = YRotateSlider.value;
    }

    void ZSliderChanged() {
        rotateObject.z = ZRotateSlider.value;
    }

    void TwoSidedToggleChanged() {
        poly.TwoSided = TwoSidedToggle.isOn;
        Presets.RebuildPoly();
    }

    void BypassOpsToggleChanged() {
        poly.BypassOps = BypassOpsToggle.isOn;
        Presets.RebuildPoly();
    }

    void OpDropdownChanged() {
        Debug.Log(EventSystem.current.currentSelectedGameObject.name);
    }
    
    void OpDisabledChanged() {
        Debug.Log(EventSystem.current.currentSelectedGameObject.name);
    }
    
    void OpInputChanged() {
        Debug.Log(EventSystem.current.currentSelectedGameObject.name);
    }

    void LoadPresetButtonClicked() {
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        Presets.ApplyPresetToPoly(buttonName);
        UpdatePolyUI();
    }
    
    void SavePresetButtonClicked() {
        Presets.AddPresetFromPoly(poly.name);
        CreatePresetButtons();
    }

    public void HandleTabButton()
    {
        var button = EventSystem.current.currentSelectedGameObject;
        ShowTab(button);
    }

    private void ShowTab(GameObject button)
    {
        foreach (var tab in Tabs)
        {
            tab.gameObject.SetActive(false);
        }
        Tabs[button.transform.GetSiblingIndex()].gameObject.SetActive(true);
        
        foreach (Transform child in button.transform.parent)
        {
            child.GetComponent<Button>().interactable = true;
        }
        button.GetComponent<Button>().interactable = false;

    }
}
