using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


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
    [FormerlySerializedAs("OperatorContainer")] public RectTransform OpContainer;
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
    private List<Transform> opPrefabs;
    private bool _shouldReBuild = true;

    void Start()
    {
        opPrefabs = new List<Transform>();
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
        var newOp = new PolyComponent.ConwayOperator {disabled = false};
        poly.ConwayOperators.Add(newOp);
        AddOpItemToUI(newOp);
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void UpdatePolyUI()
    {
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
        if (opPrefabs == null) {return;}
        foreach (var item in opPrefabs) {
            Destroy(item.gameObject);
        }
        opPrefabs.Clear();
    }
    
    void CreateOps()
    {
        DestroyOps();
        if (poly.ConwayOperators == null) {return;}
        for (var index = 0; index < poly.ConwayOperators.Count; index++)
        {
            var conwayOperator = poly.ConwayOperators[index];
            AddOpItemToUI(conwayOperator);
        }
    }

    void ConfigureOpControls(OpPrefabManager opPrefabManager)
    {
        var opType = (PolyComponent.Ops)opPrefabManager.OpTypeDropdown.value;
        var opConfig = poly.opconfigs[opType];
        
        opPrefabManager.FaceSelectionDropdown.gameObject.SetActive(opConfig.usesFaces);
        opPrefabManager.AmountSlider.gameObject.SetActive(opConfig.usesAmount);
        
        opPrefabManager.AmountSlider.minValue = opConfig.amountMin;
        opPrefabManager.AmountSlider.maxValue = opConfig.amountMax;

    }

    void AddOpItemToUI(PolyComponent.ConwayOperator op)
    {
        var opPrefab = Instantiate(OpTemplate);
        opPrefab.transform.SetParent(OpContainer);
        var opPrefabManager = opPrefab.GetComponent<OpPrefabManager>();
        
        opPrefab.name = op.opType.ToString();
        foreach (var item in Enum.GetValues(typeof(PolyComponent.Ops))) {
            var label = new Dropdown.OptionData(item.ToString());
            opPrefabManager.OpTypeDropdown.options.Add(label);
        }
        
        foreach (var item in Enum.GetValues(typeof(PolyComponent.FaceSelections))) {
            var label = new Dropdown.OptionData(item.ToString());
            opPrefabManager.FaceSelectionDropdown.options.Add(label);
        }
                
        opPrefabManager.OpTypeDropdown.value = (int) op.opType;
        opPrefabManager.FaceSelectionDropdown.value = (int) op.faceSelections;
        opPrefabManager.DisabledToggle.isOn = op.disabled;
        opPrefabManager.AmountSlider.value = op.amount;
        opPrefabManager.OpTypeDropdown.onValueChanged.AddListener(delegate{OpTypeChanged();});
        opPrefabManager.FaceSelectionDropdown.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.DisabledToggle.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AmountSlider.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.UpButton.onClick.AddListener(MoveOpUp);
        opPrefabManager.DownButton.onClick.AddListener(MoveOpDown);
        opPrefabManager.DeleteButton.onClick.AddListener(DeleteOp);
        opPrefabManager.Index = opPrefabs.Count;
        
        ConfigureOpControls(opPrefab.GetComponent<OpPrefabManager>());

        // Enable/Disable down buttons as appropriate:
        // We are adding this at the end so it can't move down
        opPrefab.GetComponent<OpPrefabManager>().DownButton.enabled = false;
        if (opPrefabs.Count == 0) // Only one item exists
        {
            // First item can't move up
            opPrefab.GetComponent<OpPrefabManager>().UpButton.enabled = false;
        }
        else
        {
            // Reenable down button on the previous final item
            opPrefabs[opPrefabs.Count - 1].GetComponent<OpPrefabManager>().DownButton.enabled = true;
        }
        opPrefabs.Add(opPrefab);
    }

    void OpTypeChanged()
    {
        ConfigureOpControls(EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>());
        OpsUIToPoly();
    }

    void OpsUIToPoly()
    {
        if (opPrefabs == null) {return;}
        
        for (var index = 0; index < opPrefabs.Count; index++) {
            
            var opPrefab = opPrefabs[index];
            var opPrefabManager = opPrefab.GetComponent<OpPrefabManager>();
            
            var op = poly.ConwayOperators[index];
            
            op.opType = (PolyComponent.Ops)opPrefabManager.OpTypeDropdown.value;
            op.faceSelections = (PolyComponent.FaceSelections) opPrefabManager.FaceSelectionDropdown.value;
            op.disabled = opPrefabManager.DisabledToggle.isOn;
            op.amount = opPrefabManager.AmountSlider.value;
            poly.ConwayOperators[index] = op;
            
        }
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void MoveOpUp()
    {
        SwapOpWith(-1);
    }

    void MoveOpDown()
    {
        SwapOpWith(1);
    }

    private void SwapOpWith(int offset)
    {
        var opPrefabManager = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>();
        var src = opPrefabManager.Index;
        var dest = src + offset;
        var temp = poly.ConwayOperators[src];
        poly.ConwayOperators[src] = poly.ConwayOperators[dest];
        poly.ConwayOperators[dest] = temp;
        CreateOps();
        if (_shouldReBuild) poly.MakePolyhedron();
    }
    
    void DeleteOp()
    {
        var opPrefabManager = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>();
        poly.ConwayOperators.RemoveAt(opPrefabManager.Index);
        CreateOps();
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void CreateBasePolyDropdown()
    {
        BasePolyDropdown.ClearOptions();
        foreach (var polyType in Enum.GetValues(typeof(PolyComponent.PolyTypes))) {
            var label = new Dropdown.OptionData(polyType.ToString().Replace("_", " "));
            BasePolyDropdown.options.Add(label);
        }
        BasePolyDropdown.onValueChanged.AddListener(delegate{BasePolyDropdownChanged(BasePolyDropdown);});
    }
     
    void DestroyPresetButtons()
    {
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
            presetButton.onClick.AddListener(LoadPresetButtonClicked);
            presetButtons.Add(presetButton);
        }
    }
    
    // Event handlers

    void PresetNameChanged()
    {
        if (String.IsNullOrEmpty(PresetNameInput.text))
        {
            SavePresetButton.interactable = false;
        }
        else
        {
            SavePresetButton.interactable = true;
        }
    }

    void BasePolyDropdownChanged(Dropdown change)
    {
        poly.PolyType = (PolyComponent.PolyTypes)change.value;
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void XSliderChanged()
    {
        rotateObject.x = XRotateSlider.value;
    }

    void YSliderChanged()
    {
        rotateObject.y = YRotateSlider.value;
    }

    void ZSliderChanged()
    {
        rotateObject.z = ZRotateSlider.value;
    }

    void TwoSidedToggleChanged()
    {
        poly.TwoSided = TwoSidedToggle.isOn;
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void BypassOpsToggleChanged()
    {
        poly.BypassOps = BypassOpsToggle.isOn;
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void LoadPresetButtonClicked()
    {
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
    
    void SavePresetButtonClicked()
    {
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
    
    #if UNITY_EDITOR
        [MenuItem ("Window/Open PersistentData Folder")]
        public static void OpenPersistentDataFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        } 
    #endif

}
