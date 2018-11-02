using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Presets;
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
    public Button AddOpButton;
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
    private bool _shouldReBuild = true;

    void Start()
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
        AddOpButton.onClick.AddListener(AddOpButtonClicked);
        Presets.LoadAllPresets();
        InitUI();
        CreatePresetButtons();
        ShowTab(TabButtons[0].gameObject);
    }

    public void InitUI()
    {
        UpdatePolyUI();
        UpdateOpsUI();
        UpdateAnimUI();
    }

    void AddOpButtonClicked()
    {
        AddOpItem(new PolyComponent.ConwayOperator {disabled = true});  // No need to rebuild as it's disabled initially
        Array.Resize(ref poly.ConwayOperators, poly.ConwayOperators.Length + 1);
        poly.ConwayOperators[poly.ConwayOperators.Length - 1] = new PolyComponent.ConwayOperator {disabled = true};
    }

    void UpdatePolyUI() {
        TwoSidedToggle.isOn = poly.TwoSided;
        CreateBasePolyDropdown();
        BasePolyDropdown.value = (int)poly.PolyType;
    }

    void UpdateAnimUI()
    {
        XRotateSlider.value = rotateObject.x;
        YRotateSlider.value = rotateObject.y;
        ZRotateSlider.value = rotateObject.z;
    }

    void UpdateOpsUI()
    {
        BypassOpsToggle.isOn = poly.BypassOps;
        CreateOps();        
    }

    void DestroyOps()
    {
        if (opItems == null) {return;}
        foreach (var item in opItems) {
            Destroy(item.gameObject);
        }
        opItems.Clear();
    }
    
    void CreateOps()
    {
        DestroyOps();
        if (poly.ConwayOperators == null) {return;}
        for (var index = 0; index < poly.ConwayOperators.Length; index++)
        {
            var conwayOperator = poly.ConwayOperators[index];
            AddOpItem(conwayOperator);
        }
    }

    void AddOpItem(PolyComponent.ConwayOperator opItem)
    {
        var opUIItem = Instantiate(OpTemplate);
        opUIItem.transform.SetParent(OperatorContainer);
        var opsDropdown = opUIItem.GetComponentInChildren<Dropdown>();
        foreach (var opType in Enum.GetValues(typeof(PolyComponent.Ops))) {
            var opLabel = new Dropdown.OptionData(opType.ToString());
            opsDropdown.options.Add(opLabel);
        }
        opsDropdown.value = (int) opItem.opType;
        opUIItem.GetComponentInChildren<Toggle>().isOn = opItem.disabled;
        opUIItem.GetComponentInChildren<Slider>().value = opItem.amount;
        opUIItem.GetComponentInChildren<Dropdown>().onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opUIItem.GetComponentInChildren<Toggle>().onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opUIItem.GetComponentInChildren<Slider>().onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opItems.Add(opUIItem);
    }

    void OpsUIToPoly()
    {
        if (opItems == null) {return;}
        for (var index = 0; index < opItems.Count; index++) {
            var opUIItem = opItems[index];
            var opsDropdown = opUIItem.GetComponentInChildren<Dropdown>();
            poly.ConwayOperators[index].opType = (PolyComponent.Ops)opsDropdown.value;
            poly.ConwayOperators[index].disabled = opUIItem.GetComponentInChildren<Toggle>().isOn;
            poly.ConwayOperators[index].amount = opUIItem.GetComponentInChildren<Slider>().value;
        }
        if (_shouldReBuild) poly.MakePolyhedron();
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
    
    void CreatePresetButtons()
    {
        DestroyPresetButtons();
        for (var index = 0; index < Presets.Items.Count; index++)
        {
            var preset = Presets.Items[index];
            var presetButton = Instantiate(ButtonTemplate);
            presetButton.transform.SetParent(PresetButtonContainer);
            presetButton.name = index.ToString();
            presetButton.GetComponentInChildren<Text>().text = preset.Name;
            presetButton.onClick.AddListener(delegate { LoadPresetButtonClicked(); });
            presetButtons.Add(presetButton);
        }
    }
    
    // Event handlers

    void PresetNameChanged() {
        if (String.IsNullOrEmpty(PresetNameInput.text)) {
            SavePresetButton.interactable = false;
        } else {
            SavePresetButton.interactable = true;
        }
    }

    void BasePolyDropdownChanged(Dropdown change) {
        poly.PolyType = (PolyComponent.PolyTypes)change.value;
        if (_shouldReBuild) poly.MakePolyhedron();
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
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void BypassOpsToggleChanged() {
        poly.BypassOps = BypassOpsToggle.isOn;
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void LoadPresetButtonClicked() {
        int buttonIndex = 0;
        if (Int32.TryParse(EventSystem.current.currentSelectedGameObject.name, out buttonIndex))
        {
            _shouldReBuild = false;
            var preset = Presets.ApplyPresetToPoly(buttonIndex);
            PresetNameInput.text = preset.Name;
            InitUI();
            _shouldReBuild = true;
            poly.MakePolyhedron();
        }
        else
        {
            Debug.LogError("Invalid button name: " + buttonIndex);
        }        
        
    }
    
    void SavePresetButtonClicked() {
        Presets.AddPresetFromPoly(PresetNameInput.text);
        Presets.SaveAllPresets();
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
