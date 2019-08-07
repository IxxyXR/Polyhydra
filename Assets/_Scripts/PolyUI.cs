using System;
using System.Collections.Generic;
using Conway;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
    using UnityEditor;
#endif


public class PolyUI : MonoBehaviour {
    
    public PolyHydra poly;
    public PolyPresets Presets;
    public AppearancePresets APresets;
    public int currentAPreset;
    private RotateObject rotateObject;

    public InputField PresetNameInput;
    public Slider XRotateSlider;
    public Slider YRotateSlider;
    public Slider ZRotateSlider;
    public Toggle TwoSidedToggle;
    public Button PrevPolyButton;
    public Button NextPolyButton;
    public Text InfoText;
    public Text OpsWarning;
    public Button AddOpButton;
    public Button AddRandomOpButton;
    public Toggle BypassOpsToggle;
    public RectTransform OpContainer;
    public Transform OpTemplate;
    public Button ButtonTemplate;
    public RectTransform PresetButtonContainer;
    public Dropdown BasePolyDropdown; 
    public Dropdown GridTypeDropdown;
    public InputField PrismPInput;
    public InputField PrismQInput;
    public Toggle LoadMatchingAppearanceToggle;
    public Button SavePresetButton;
    public Button ResetPresetsButton;
    public Button OpenPresetsFolderButton;
    public Text AppearancePresetNameText;
    public Button PrevAPresetButton;
    public Button NextAPresetButton;
    public Button ObjExportButton;
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
        
        BasePolyDropdown.ClearOptions();        
        foreach (var polyType in Enum.GetValues(typeof(PolyTypes))) {
            var label = new Dropdown.OptionData(polyType.ToString().Replace("_", " "));
            BasePolyDropdown.options.Add(label);
        }

        GridTypeDropdown.ClearOptions();        
        foreach (var gridType in Enum.GetValues(typeof(PolyHydra.GridTypes))) {
            var label = new Dropdown.OptionData(gridType.ToString());
            GridTypeDropdown.options.Add(label);
        }
        
        BasePolyDropdown.onValueChanged.AddListener(delegate{BasePolyDropdownChanged(BasePolyDropdown);});
        PrevPolyButton.onClick.AddListener(PrevPolyButtonClicked);
        NextPolyButton.onClick.AddListener(NextPolyButtonClicked);
        TwoSidedToggle.onValueChanged.AddListener(delegate{TwoSidedToggleChanged();});
        GridTypeDropdown.onValueChanged.AddListener(delegate{GridTypeDropdownChanged(GridTypeDropdown);});
        PrismPInput.onValueChanged.AddListener(delegate{PrismPInputChanged();});
        PrismQInput.onValueChanged.AddListener(delegate{PrismQInputChanged();});
        BypassOpsToggle.onValueChanged.AddListener(delegate{BypassOpsToggleChanged();});
        AddOpButton.onClick.AddListener(AddOpButtonClicked);
        AddRandomOpButton.onClick.AddListener(AddRandomOpButtonClicked);
        
        PresetNameInput.onValueChanged.AddListener(delegate{PresetNameChanged();});
        SavePresetButton.onClick.AddListener(SavePresetButtonClicked);
        ResetPresetsButton.onClick.AddListener(ResetPresetsButtonClicked);
        OpenPresetsFolderButton.onClick.AddListener(OpenPersistentDataFolder);
        
        PrevAPresetButton.onClick.AddListener(PrevAPresetButtonClicked);
        NextAPresetButton.onClick.AddListener(NextAPresetButtonClicked);
        
        XRotateSlider.onValueChanged.AddListener(delegate{XSliderChanged();});
        YRotateSlider.onValueChanged.AddListener(delegate{YSliderChanged();});
        ZRotateSlider.onValueChanged.AddListener(delegate{ZSliderChanged();});

        ObjExportButton.onClick.AddListener(ObjExportButtonClicked);
        
        Presets.LoadAllPresets();
        
        InitUI();
        CreatePresetButtons();
        ShowTab(TabButtons[0].gameObject);
    }

    void Update()
    {
        // TODO hook up a signal or something to only set this when the mesh has changed
        InfoText.text = poly.GetInfoText();
    }

    private void ObjExportButtonClicked()
    {
        ObjExport.ExportMesh(poly.gameObject, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Foo");
    }

    private void PrismPInputChanged()
    {
        int p;
        if (int.TryParse(PrismPInput.text, out p))
        {
            poly.PrismP = p;
        }
        Rebuild();    
    }

    private void PrismQInputChanged()
    {
        int q;        
        if (int.TryParse(PrismQInput.text, out q))
        {
            poly.PrismQ = q;
        }
        Rebuild();    
    }

    private void PrevPolyButtonClicked()
    {
        BasePolyDropdown.value -= 1;
        BasePolyDropdown.value %= Enum.GetValues(typeof(PolyTypes)).Length;
    }
    
    private void NextPolyButtonClicked()
    {
        BasePolyDropdown.value += 1;
        BasePolyDropdown.value %= Enum.GetValues(typeof(PolyTypes)).Length;
    }
    
    int mod(int x, int m) {return (x % m + m) % m;}  // Cos C# just *has* to be different...
    
    private void PrevAPresetButtonClicked()
    {
        currentAPreset--;
        currentAPreset = mod(currentAPreset, APresets.Items.Count);
        APresets.ApplyPresetToPoly(APresets.Items[currentAPreset]);  // TODO
        AppearancePresetNameText.text = poly.APresetName;
    }

    private void NextAPresetButtonClicked()
    {
        currentAPreset++;
        currentAPreset = mod(currentAPreset, APresets.Items.Count);
        APresets.ApplyPresetToPoly(APresets.Items[currentAPreset]);  // TODO 
        AppearancePresetNameText.text = poly.APresetName;
    }
    
    public void InitUI()
    {
        UpdatePolyUI();
        UpdateOpsUI();
        UpdateAnimUI();
    }

    void Rebuild()
    {
        if (_shouldReBuild) poly.MakePolyhedron();
    }

    void AddOpButtonClicked()
    {
        var newOp = new PolyHydra.ConwayOperator {disabled = false};
        poly.ConwayOperators.Add(newOp);
        AddOpItemToUI(newOp);
        Rebuild();
    }

    void AddRandomOpButtonClicked()
    {
        var newOp = poly.AddRandomOp();
        AddOpItemToUI(newOp);
        Rebuild();        
    }

    void UpdatePolyUI()
    {
        _shouldReBuild = false;
        TwoSidedToggle.isOn = poly.TwoSided;
        BasePolyDropdown.value = (int)poly.UniformPolyType;        
        GridTypeDropdown.value = (int)poly.GridType;
        PrismPInput.text = poly.PrismP.ToString();
        PrismQInput.text = poly.PrismQ.ToString();
        _shouldReBuild = true;
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
        var opType = (PolyHydra.Ops)opPrefabManager.OpTypeDropdown.value;
        var opConfig = poly.opconfigs[opType];
        
        opPrefabManager.FaceSelectionDropdown.gameObject.SetActive(opConfig.usesFaces);
        opPrefabManager.RandomizeToggle.gameObject.SetActive(opConfig.usesRandomize);
        opPrefabManager.AmountSlider.gameObject.SetActive(opConfig.usesAmount);
        opPrefabManager.AmountInput.gameObject.SetActive(opConfig.usesAmount);
        
        opPrefabManager.AmountSlider.minValue = opConfig.amountMin;
        opPrefabManager.AmountSlider.maxValue = opConfig.amountMax;
        opPrefabManager.AmountSlider.value = opConfig.amountDefault;
    }

    void AddOpItemToUI(PolyHydra.ConwayOperator op)
    {
        var opPrefab = Instantiate(OpTemplate);
        opPrefab.transform.SetParent(OpContainer);
        var opPrefabManager = opPrefab.GetComponent<OpPrefabManager>();
        
        opPrefab.name = op.opType.ToString();
        foreach (var item in Enum.GetValues(typeof(PolyHydra.Ops))) {
            var label = new Dropdown.OptionData(item.ToString());
            opPrefabManager.OpTypeDropdown.options.Add(label);
        }
        
        foreach (var item in Enum.GetValues(typeof(ConwayPoly.FaceSelections))) {
            var label = new Dropdown.OptionData(item.ToString());
            opPrefabManager.FaceSelectionDropdown.options.Add(label);
        }
        
        opPrefabManager.DisabledToggle.isOn = op.disabled;
        opPrefabManager.OpTypeDropdown.value = (int) op.opType;
        opPrefabManager.FaceSelectionDropdown.value = (int) op.faceSelections;
        opPrefabManager.AmountSlider.value = op.amount;
        opPrefabManager.AmountInput.text = op.amount.ToString();
        opPrefabManager.RandomizeToggle.isOn = op.randomize;

        ConfigureOpControls(opPrefab.GetComponent<OpPrefabManager>());

        opPrefabManager.OpTypeDropdown.onValueChanged.AddListener(delegate{OpTypeChanged();});
        opPrefabManager.FaceSelectionDropdown.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.DisabledToggle.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AmountSlider.onValueChanged.AddListener(delegate{AmountSliderChanged();});
        opPrefabManager.AmountInput.onValueChanged.AddListener(delegate{AmountInputChanged();});
        opPrefabManager.RandomizeToggle.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        
        opPrefabManager.UpButton.onClick.AddListener(MoveOpUp);
        opPrefabManager.DownButton.onClick.AddListener(MoveOpDown);
        opPrefabManager.DeleteButton.onClick.AddListener(DeleteOp);
        
        opPrefabManager.Index = opPrefabs.Count;
        
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

    void AmountSliderChanged()
    {
        //if (!poly.disableThreading && !poly.done) return;
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountSlider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountInput;
        slider.value = Mathf.Round(slider.value * 100) / 100f;
        input.text = slider.value.ToString();
        // Not needed if we also modify the text field
        // OpsUIToPoly();
    }

    void AmountInputChanged()
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountSlider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountInput;
        float value;
        if (float.TryParse(input.text, out value))
        {
            slider.value = value;
        }        
        OpsUIToPoly();
    }

    void OpsUIToPoly()
    {
        if (opPrefabs == null) {return;}
        
        for (var index = 0; index < opPrefabs.Count; index++) {
            
            var opPrefab = opPrefabs[index];
            var opPrefabManager = opPrefab.GetComponent<OpPrefabManager>();
            
            var op = poly.ConwayOperators[index];
            
            op.opType = (PolyHydra.Ops)opPrefabManager.OpTypeDropdown.value;
            op.faceSelections = (ConwayPoly.FaceSelections) opPrefabManager.FaceSelectionDropdown.value;
            op.disabled = opPrefabManager.DisabledToggle.isOn;
            op.amount = opPrefabManager.AmountSlider.value;
            op.randomize = opPrefabManager.RandomizeToggle.isOn;
            poly.ConwayOperators[index] = op;
            
        }
        Rebuild();
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
        if (dest < 0 || dest > poly.ConwayOperators.Count - 1) return;
        var temp = poly.ConwayOperators[src];
        poly.ConwayOperators[src] = poly.ConwayOperators[dest];
        poly.ConwayOperators[dest] = temp;
        CreateOps();
        Rebuild();
    }
    
    void DeleteOp()
    {
        var opPrefabManager = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>();
        poly.ConwayOperators.RemoveAt(opPrefabManager.Index);
        CreateOps();
        Rebuild();
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
        poly.UniformPolyType = (PolyTypes)change.value;
        Rebuild();
        
        if (poly.WythoffPoly!=null && poly.WythoffPoly.IsOneSided)
        {
            OpsWarning.enabled = true;
        }
        else
        {
            OpsWarning.enabled = false;
        }

        GridTypeDropdown.gameObject.SetActive(change.value == 0);
        PrismPInput.gameObject.SetActive(change.value > 0 && change.value < 6);
        PrismQInput.gameObject.SetActive(change.value > 2 && change.value < 6);
        
    }
    
    void GridTypeDropdownChanged(Dropdown change)
    {
        poly.GridType = (PolyHydra.GridTypes)change.value;
        Rebuild();        
    }

    void XSliderChanged()
    {
        var r = poly.gameObject.transform.eulerAngles;
        poly.gameObject.transform.eulerAngles = new Vector3(XRotateSlider.value, r.y, r.z);
    }

    void YSliderChanged()
    {
        var r = poly.gameObject.transform.eulerAngles;
        poly.gameObject.transform.eulerAngles = new Vector3(r.x, YRotateSlider.value, r.z);
    }

    void ZSliderChanged()
    {
        var r = poly.gameObject.transform.eulerAngles;
        poly.gameObject.transform.eulerAngles = new Vector3(r.x, r.y, ZRotateSlider.value);
    }

    void TwoSidedToggleChanged()
    {
        poly.TwoSided = TwoSidedToggle.isOn;
        Rebuild();
    }

    void BypassOpsToggleChanged()
    {
        poly.BypassOps = BypassOpsToggle.isOn;
        Rebuild();
    }

    void LoadPresetButtonClicked()
    {
        int buttonIndex = 0;
        if (Int32.TryParse(EventSystem.current.currentSelectedGameObject.name, out buttonIndex))
        {
            _shouldReBuild = false;
            var preset = Presets.ApplyPresetToPoly(buttonIndex, LoadMatchingAppearanceToggle.isOn);
            PresetNameInput.text = preset.Name;
            AppearancePresetNameText.text = poly.APresetName;
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

    public void ResetPresetsButtonClicked()
    {
        Presets.ResetPresets();
    }
    
    #if UNITY_EDITOR
        [MenuItem ("Window/Open PersistentData Folder")]
        public static void OpenPersistentDataFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    #else
        public static void OpenPersistentDataFolder()
        {
            string path = Application.persistentDataPath.TrimEnd(new[]{'\\', '/'}); // Mac doesn't like trailing slash
            // TODO
            //Process.Start(path);
        } 
    #endif

}
