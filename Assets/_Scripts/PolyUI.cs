using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Conway;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wythoff;
using Debug = UnityEngine.Debug;
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
    public Toggle SafeLimitsToggle;
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
    public Dropdown ShapeTypesDropdown;
    public Dropdown BasePolyCategoryDropdown;
    public Dropdown BasePolyDropdown;
    public Dropdown GridTypeDropdown;
    public Dropdown GridShapeDropdown;
    public Dropdown JohnsonTypeDropdown;
    public Dropdown OtherTypeDropdown;
    public InputField PrismPInput;
    public InputField PrismQInput;
    public Toggle LoadMatchingAppearanceToggle;
    public Button SavePresetButton;
    public Button PresetSnapshotButton;
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
        
        ShapeTypesDropdown.ClearOptions();
        foreach (var shapeType in Enum.GetValues(typeof(PolyHydra.ShapeTypes))) {
            var label = new Dropdown.OptionData(shapeType.ToString().Replace("_", " "));
            ShapeTypesDropdown.options.Add(label);
        }


        BasePolyCategoryDropdown.ClearOptions();
        foreach (var cat in Enum.GetValues(typeof(PolyHydra.PolyTypeCategories))) {
            var label = new Dropdown.OptionData(cat.ToString().Replace("_", " "));
            BasePolyCategoryDropdown.options.Add(label);
        }

        BasePolyDropdown.ClearOptions();
        foreach (var polyType in Enum.GetValues(typeof(PolyTypes))) {
            var label = new Dropdown.OptionData(polyType.ToString().Replace("_", " "));
            BasePolyDropdown.options.Add(label);
        }

        GridTypeDropdown.ClearOptions();        
        foreach (var gridType in Enum.GetValues(typeof(PolyHydra.GridTypes))) {
            var label = new Dropdown.OptionData(gridType.ToString().Replace("_", ""));
            GridTypeDropdown.options.Add(label);
        }

        GridShapeDropdown.ClearOptions();
        foreach (var gridShape in Enum.GetValues(typeof(PolyHydra.GridShapes))) {
            var label = new Dropdown.OptionData(gridShape.ToString().Replace("_", " "));
            GridShapeDropdown.options.Add(label);
        }

        JohnsonTypeDropdown.ClearOptions();
        foreach (var johnsonType in Enum.GetValues(typeof(PolyHydra.JohnsonPolyTypes))) {
            var label = new Dropdown.OptionData(CamelCaseSpaces(johnsonType.ToString()));
            JohnsonTypeDropdown.options.Add(label);
        }

        OtherTypeDropdown.ClearOptions();
        foreach (var otherType in Enum.GetValues(typeof(PolyHydra.OtherPolyTypes))) {
            var label = new Dropdown.OptionData(otherType.ToString());
            OtherTypeDropdown.options.Add(label);
        }

        ShapeTypesDropdown.onValueChanged.AddListener(delegate{ShapeTypesDropdownChanged(ShapeTypesDropdown);});
        BasePolyCategoryDropdown.onValueChanged.AddListener(delegate{BasePolyCategoryDropdownChanged(BasePolyCategoryDropdown);});
        BasePolyDropdown.onValueChanged.AddListener(delegate{BasePolyDropdownChanged(BasePolyDropdown);});
        PrevPolyButton.onClick.AddListener(PrevPolyButtonClicked);
        NextPolyButton.onClick.AddListener(NextPolyButtonClicked);
        SafeLimitsToggle.onValueChanged.AddListener(delegate{SafeLimitsToggleChanged();});
        GridTypeDropdown.onValueChanged.AddListener(delegate{GridTypeDropdownChanged(GridTypeDropdown);});
        GridShapeDropdown.onValueChanged.AddListener(delegate{GridShapeDropdownChanged(GridShapeDropdown);});
        JohnsonTypeDropdown.onValueChanged.AddListener(delegate{JohnsonTypeDropdownChanged(JohnsonTypeDropdown);});
        OtherTypeDropdown.onValueChanged.AddListener(delegate{OtherTypeDropdownChanged(OtherTypeDropdown);});
        PrismPInput.onValueChanged.AddListener(delegate{PrismPInputChanged();});
        PrismQInput.onValueChanged.AddListener(delegate{PrismQInputChanged();});
        BypassOpsToggle.onValueChanged.AddListener(delegate{BypassOpsToggleChanged();});
        AddOpButton.onClick.AddListener(AddOpButtonClicked);
        AddRandomOpButton.onClick.AddListener(AddRandomOpButtonClicked);
        
        PresetNameInput.onValueChanged.AddListener(delegate{PresetNameChanged();});
        SavePresetButton.onClick.AddListener(SavePresetButtonClicked);
        PresetSnapshotButton.onClick.AddListener(PresetSnapshotButtonClicked);
        ResetPresetsButton.onClick.AddListener(ResetPresetsButtonClicked);
        OpenPresetsFolderButton.onClick.AddListener(OpenPersistentDataFolder);
        
        PrevAPresetButton.onClick.AddListener(PrevAPresetButtonClicked);
        NextAPresetButton.onClick.AddListener(NextAPresetButtonClicked);
        
        XRotateSlider.onValueChanged.AddListener(delegate{XSliderChanged();});
        YRotateSlider.onValueChanged.AddListener(delegate{YSliderChanged();});
        ZRotateSlider.onValueChanged.AddListener(delegate{ZSliderChanged();});

        ObjExportButton.onClick.AddListener(ObjExportButtonClicked);
        
        Presets.LoadAllPresets();
        
        AddMissingPresetImages();
        CreatePresetButtons();
        ShowTab(TabButtons[0].gameObject);
        InitPolySpecificUI();
    }

    private void AddMissingPresetImages()
    {
        foreach (var preset in Presets.Items)
        {
            var filePath = ScreenCaptureTool.PresetScreenShotName(preset.Name);
            if (!File.Exists(filePath))
            {
//                var imagePath = $"Resources/InitialPresets/preset_{preset.Name}.png";
//                var bytes = File.ReadAllBytes(imagePath);
//                File.WriteAllBytes(filePath, bytes);
                
//                string address = $"InitialPresets/preset_{preset.Name}";
//                byte[] byteArray = File.ReadAllBytes(@address);
//                var pic = new Texture2D(12080,1024); 
//                bool check = pic.LoadImage(byteArray);
                
                Texture2D tex2d = Resources.Load<Texture2D>($"InitialPresets/preset_{preset.Name}");
                byte[] bytes = tex2d.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
            }
        }
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
            Rebuild();
        }
    }

    private void PrismQInputChanged()
    {
        int q;
        if (int.TryParse(PrismQInput.text, out q))
        {
            poly.PrismQ = q;
            Rebuild();
        }
    }

    private void PrevPolyButtonClicked()
    {
        CyclePoly(-1);
    }

    private void NextPolyButtonClicked()
    {
        CyclePoly(1);
    }

    private void CyclePoly(int direction)
    {
        switch (poly.ShapeType)
        {
            case PolyHydra.ShapeTypes.Grid:
                GridTypeDropdown.value = SaneMod(GridTypeDropdown.value + direction, GridTypeDropdown.options.Count);
                break;
            case PolyHydra.ShapeTypes.Johnson:
                JohnsonTypeDropdown.value = SaneMod(JohnsonTypeDropdown.value + direction, JohnsonTypeDropdown.options.Count);
                break;
            case PolyHydra.ShapeTypes.Other:
                OtherTypeDropdown.value = SaneMod(OtherTypeDropdown.value + direction, OtherTypeDropdown.options.Count);
                break;
            case PolyHydra.ShapeTypes.Uniform:
                int polyIndex = SaneMod(BasePolyDropdown.value + direction, BasePolyDropdown.options.Count);
                //polyIndex = polyIndex == 0 ? 1 : polyIndex;
                BasePolyDropdown.value = polyIndex;
                break;
        }
    }

    private int SaneMod(int x, int m)  // coz C# just *has* to be different...
    {
        int val = x < 0 ? x+m : x;
        return val % m;
    }
    
    private void PrevAPresetButtonClicked()
    {
        currentAPreset--;
        currentAPreset = SaneMod(currentAPreset, APresets.Items.Count);
        APresets.ApplyPresetToPoly(APresets.Items[currentAPreset]);  // TODO
        AppearancePresetNameText.text = poly.APresetName;
    }

    private void NextAPresetButtonClicked()
    {
        currentAPreset++;
        currentAPreset = SaneMod(currentAPreset, APresets.Items.Count);
        APresets.ApplyPresetToPoly(APresets.Items[currentAPreset]);  // TODO 
        AppearancePresetNameText.text = poly.APresetName;
    }
    
    public void InitPolySpecificUI()
    {
        UpdatePolyUI();
        UpdateOpsUI();
        //UpdateAnimUI();
    }

    void Rebuild()
    {
        if (_shouldReBuild) poly.Rebuild();
        InfoText.text = poly.GetInfoText();
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
        SafeLimitsToggle.isOn = poly.SafeLimits;
        ShapeTypesDropdown.value = (int) poly.ShapeType;
        BasePolyCategoryDropdown.value = (int) poly.UniformPolyTypeCategory;
//        BasePolyCategoryDropdown.value = 1;
        int i = 0;
        foreach (var item in BasePolyDropdown.options)
        {
            if (item.text == poly.UniformPolyType.ToString().Replace("_", " ")) break;
            i++;
        }
        BasePolyDropdown.value = i;
        JohnsonTypeDropdown.value = (int) poly.JohnsonPolyType;
        OtherTypeDropdown.value = (int) poly.OtherPolyType;
        GridTypeDropdown.value = (int) poly.GridType;
        GridShapeDropdown.value = (int) poly.GridShape;
        PrismPInput.text = poly.PrismP.ToString();
        PrismQInput.text = poly.PrismQ.ToString();
        InitShapeTypesUI((int) poly.ShapeType);
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
        opPrefabManager.OpTypeDropdown.GetComponentInChildren<DropdownIconManager>().SetIcon(opType);
        var opConfig = poly.opconfigs[opType];
        
        opPrefabManager.FaceSelectionDropdown.gameObject.SetActive(opConfig.usesFaces);
        opPrefabManager.RandomizeToggle.gameObject.SetActive(opConfig.usesRandomize);

        opPrefabManager.AmountSlider.gameObject.SetActive(opConfig.usesAmount);
        opPrefabManager.AmountInput.gameObject.SetActive(opConfig.usesAmount);
        opPrefabManager.Amount2Slider.gameObject.SetActive(opConfig.usesAmount2);
        opPrefabManager.Amount2Input.gameObject.SetActive(opConfig.usesAmount2);
        opPrefabManager.ToggleAnimate.gameObject.SetActive(opConfig.usesAmount);
        opPrefabManager.GetComponent<RectTransform>().sizeDelta = new Vector2(200, opConfig.usesAmount?238:100);

        opPrefabManager.AmountSlider.value = opConfig.amountDefault;
        opPrefabManager.Amount2Slider.value = opConfig.amount2Default;

        if (poly.SafeLimits)
        {
            opPrefabManager.AmountSlider.minValue = opConfig.amountSafeMin;
            opPrefabManager.AmountSlider.maxValue = opConfig.amountSafeMax;
        }
        else
        {
            opPrefabManager.AmountSlider.minValue = opConfig.amountMin;
            opPrefabManager.AmountSlider.maxValue = opConfig.amountMax;
        }
    }

    static string  CamelCaseSpaces(string str)
    {
        return Regex.Replace(str, "(\\B[A-Z])", " $1");
    }

    void AddOpItemToUI(PolyHydra.ConwayOperator op)
    {
        var opPrefab = Instantiate(OpTemplate);
        opPrefab.transform.SetParent(OpContainer);
        var opPrefabManager = opPrefab.GetComponent<OpPrefabManager>();
        
        opPrefab.name = op.opType.ToString();
        foreach (PolyHydra.Ops item in Enum.GetValues(typeof(PolyHydra.Ops))) {
            var label = new Dropdown.OptionData(CamelCaseSpaces(item.ToString()));
            opPrefabManager.OpTypeDropdown.options.Add(label);
        }
        
        foreach (var item in Enum.GetValues(typeof(ConwayPoly.FaceSelections))) {
            var label = new Dropdown.OptionData(CamelCaseSpaces(item.ToString()));
            opPrefabManager.FaceSelectionDropdown.options.Add(label);
        }

        opPrefabManager.OpTypeDropdown.value = (int)op.opType;
        ConfigureOpControls(opPrefab.GetComponent<OpPrefabManager>());

        opPrefabManager.DisabledToggle.isOn = op.disabled;
        opPrefabManager.FaceSelectionDropdown.value = (int) op.faceSelections;
        opPrefabManager.AmountSlider.value = op.amount;
        opPrefabManager.AmountInput.text = op.amount.ToString();
        opPrefabManager.Amount2Slider.value = op.amount2;
        opPrefabManager.Amount2Input.text = op.amount2.ToString();
        opPrefabManager.RandomizeToggle.isOn = op.randomize;
        opPrefabManager.ToggleAnimate.isOn = op.animate;
        opPrefabManager.AnimRateInput.text = op.animationRate.ToString();
        opPrefabManager.AnimAmountInput.text = op.animationAmount.ToString();
        opPrefabManager.AudioLowAmountInput.text = op.audioLowAmount.ToString();
        opPrefabManager.AudioMidAmountInput.text = op.audioMidAmount.ToString();
        opPrefabManager.AudioHighAmountInput.text = op.audioHighAmount.ToString();
        AnimateToggleChanged(op.animate);

        opPrefabManager.OpTypeDropdown.onValueChanged.AddListener(delegate{OpTypeChanged();});
        opPrefabManager.FaceSelectionDropdown.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.DisabledToggle.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AmountSlider.onValueChanged.AddListener(delegate{AmountSliderChanged();});
        opPrefabManager.AmountInput.onValueChanged.AddListener(delegate{AmountInputChanged();});
        opPrefabManager.Amount2Slider.onValueChanged.AddListener(delegate{Amount2SliderChanged();});
        opPrefabManager.Amount2Input.onValueChanged.AddListener(delegate{Amount2InputChanged();});
        opPrefabManager.RandomizeToggle.onValueChanged.AddListener(delegate{OpsUIToPoly();});

        opPrefabManager.UpButton.onClick.AddListener(MoveOpUp);
        opPrefabManager.DownButton.onClick.AddListener(MoveOpDown);
        opPrefabManager.DeleteButton.onClick.AddListener(DeleteOp);

        opPrefabManager.ToggleAnimate.onValueChanged.AddListener(AnimateToggleChanged);
        opPrefabManager.AnimRateInput.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AnimAmountInput.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AudioLowAmountInput.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AudioMidAmountInput.onValueChanged.AddListener(delegate{OpsUIToPoly();});
        opPrefabManager.AudioHighAmountInput.onValueChanged.AddListener(delegate{OpsUIToPoly();});

        opPrefabManager.Index = opPrefabs.Count;
        
        void AnimateToggleChanged(bool value)
        {
            opPrefabManager.AnimationControls.gameObject.SetActive(value);
            OpsUIToPoly();
        }


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
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountSlider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountInput;
        _AmountSliderChanged(slider, input);
    }

    void AmountInputChanged()
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountSlider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().AmountInput;
        _AmountInputChanged(slider, input);
    }

    void Amount2SliderChanged()
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().Amount2Slider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().Amount2Input;
        _AmountSliderChanged(slider, input);
    }

    void Amount2InputChanged()
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().Amount2Slider;
        var input = EventSystem.current.currentSelectedGameObject.GetComponentInParent<OpPrefabManager>().Amount2Input;
        _AmountInputChanged(slider, input);
    }

    void _AmountSliderChanged(Slider slider, InputField input)
    {
        slider.value = Mathf.Round(slider.value * 100) / 100f;
        input.text = slider.value.ToString();
        // Not needed if we also modify the text field
        // OpsUIToPoly();
    }

    void _AmountInputChanged(Slider slider, InputField input)
    {
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
            op.amount2 = opPrefabManager.Amount2Slider.value;
            op.randomize = opPrefabManager.RandomizeToggle.isOn;
            op.animate = opPrefabManager.ToggleAnimate.isOn;
            float tempVal;
            if (float.TryParse(opPrefabManager.AnimRateInput.text, out tempVal)) op.animationRate = tempVal;
            if (float.TryParse(opPrefabManager.AnimAmountInput.text, out tempVal)) op.animationAmount = tempVal;
            if (float.TryParse(opPrefabManager.AudioLowAmountInput.text, out tempVal)) op.audioLowAmount = tempVal;
            if (float.TryParse(opPrefabManager.AudioMidAmountInput.text, out tempVal)) op.audioMidAmount = tempVal;
            if (float.TryParse(opPrefabManager.AudioHighAmountInput.text, out tempVal)) op.audioHighAmount = tempVal;
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

    private Button AddOrUpdatePresetButton(PolyPreset preset)
    {
        foreach (var btn in PresetButtonContainer.GetComponentsInChildren<Button>())
        {
            if (btn.name == preset.Name)
            {
                btn.transform.GetComponentInChildren<GetPresetImageForButton>().UpdateImage();
                return btn;
            }
        }

        var presetButton = Instantiate(ButtonTemplate);
        presetButton.transform.SetParent(PresetButtonContainer);
        presetButton.name = preset.Name;
        presetButton.GetComponentInChildren<Text>().text = preset.Name;
        presetButton.onClick.AddListener(LoadPresetButtonClicked);
        presetButtons.Add(presetButton);
        presetButton.transform.GetComponentInChildren<GetPresetImageForButton>().UpdateImage();
        return presetButton;

    }
    
    void CreatePresetButtons()
    {
        DestroyPresetButtons();
        for (var index = 0; index < Presets.Items.Count; index++)
        {
            var preset = Presets.Items[index];
            AddOrUpdatePresetButton(preset);
        }

//        foreach (var presetButton in presetButtons)
//        {
//            presetButton.GetComponentInChildren<GetPresetImageForButton>().UpdateImage();
//        }
    }
    
    // Event handlers

    void PresetNameChanged()
    {
        if (String.IsNullOrEmpty(PresetNameInput.text))
        {
            SavePresetButton.interactable = false;
            PresetSnapshotButton.interactable = false;
        }
        else
        {
            SavePresetButton.interactable = true;
            PresetSnapshotButton.interactable = true;
        }
    }

    void InitShapeTypesUI(int value)
    {
        PrismPInput.gameObject.SetActive(false);
        PrismQInput.gameObject.SetActive(false);
        BasePolyCategoryDropdown.gameObject.SetActive(false);
        BasePolyDropdown.gameObject.SetActive(false);
        GridTypeDropdown.gameObject.SetActive(false);
        GridShapeDropdown.gameObject.SetActive(false);
        JohnsonTypeDropdown.gameObject.SetActive(false);
        OtherTypeDropdown.gameObject.SetActive(false);

        switch (value)
        {
            case (int)PolyHydra.ShapeTypes.Uniform:
                // P and Q buttons are set per poly type
                BasePolyCategoryDropdown.gameObject.SetActive(true);
                BasePolyDropdown.gameObject.SetActive(true);
                break;
            case (int)PolyHydra.ShapeTypes.Grid:
                PrismPInput.gameObject.SetActive(true);
                PrismQInput.gameObject.SetActive(true);
                GridTypeDropdown.gameObject.SetActive(true);
                GridShapeDropdown.gameObject.SetActive(true);
                break;
            case (int)PolyHydra.ShapeTypes.Johnson:
                PrismPInput.gameObject.SetActive(true);
                JohnsonTypeDropdown.gameObject.SetActive(true);
                break;
            case (int)PolyHydra.ShapeTypes.Other:
                PrismPInput.gameObject.SetActive(true);
                OtherTypeDropdown.gameObject.SetActive(true);
                break;
        }

        poly.ShapeType = (PolyHydra.ShapeTypes)value;

    }

    void ShapeTypesDropdownChanged(Dropdown change)
    {
        InitShapeTypesUI(change.value);
        Rebuild();
    }

    private void FilterBasePolyDropdown(Uniform[] polySubset)
    {
        BasePolyDropdown.ClearOptions();
        polySubset = polySubset == null ? Uniform.Uniforms : polySubset;
        foreach (var uniform in polySubset) {
            var label = new Dropdown.OptionData(ToTitleCase(uniform.Name));
            BasePolyDropdown.options.Add(label);
        }

        BasePolyDropdown.value = 0;
    }

    private static string ToTitleCase(string str)
    {
        var textInfo = new CultureInfo("en-US").TextInfo;
        return textInfo.ToTitleCase(str);
    }

    void BasePolyCategoryDropdownChanged(Dropdown change)
    {
        Uniform[] polySubset = null;
        poly.UniformPolyTypeCategory = (PolyHydra.PolyTypeCategories)change.value;

        switch (poly.UniformPolyTypeCategory)
        {
            case PolyHydra.PolyTypeCategories.All:
                polySubset = Uniform.Uniforms;
                break;
            case PolyHydra.PolyTypeCategories.Archimedean:
                polySubset = Uniform.Archimedean;
                break;
            case PolyHydra.PolyTypeCategories.Convex:
                polySubset = Uniform.Convex;
                break;
            case PolyHydra.PolyTypeCategories.Platonic:
                polySubset = Uniform.Platonic;
                break;
            case PolyHydra.PolyTypeCategories.Prismatic:
                polySubset = Uniform.Prismatic;
                break;
            case PolyHydra.PolyTypeCategories.Star:
                polySubset = Uniform.Star;
                break;
            case PolyHydra.PolyTypeCategories.KeplerPoinsot:
                polySubset = Uniform.KeplerPoinsot;
                break;
       }

        FilterBasePolyDropdown(polySubset);
        Rebuild();
    }

    void BasePolyDropdownChanged(Dropdown change)
    {
        var polyName = ToTitleCase(change.options[change.value].text).Replace(" ", "_");
        poly.UniformPolyType = (PolyTypes)Enum.Parse(typeof(PolyTypes), polyName);
        Rebuild();
        
        if (poly.WythoffPoly!=null && poly.WythoffPoly.IsOneSided)
        {
            OpsWarning.enabled = true;
        }
        else
        {
            OpsWarning.enabled = false;
        }

        PrismPInput.gameObject.SetActive((int)poly.UniformPolyType > 0 && change.value < 6);
        PrismQInput.gameObject.SetActive((int)poly.UniformPolyType > 2 && change.value < 6);
        
    }
    
    void GridTypeDropdownChanged(Dropdown change)
    {
        poly.GridType = (PolyHydra.GridTypes)change.value;
        Rebuild();        
    }

    void GridShapeDropdownChanged(Dropdown change)
    {
        poly.GridShape = (PolyHydra.GridShapes)change.value;
        Rebuild();
    }

    void JohnsonTypeDropdownChanged(Dropdown change)
    {
        poly.JohnsonPolyType = (PolyHydra.JohnsonPolyTypes) change.value;
        Rebuild();
    }

    void OtherTypeDropdownChanged(Dropdown change)
    {
        poly.OtherPolyType = (PolyHydra.OtherPolyTypes) change.value;
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

    void SafeLimitsToggleChanged()
    {
        poly.SafeLimits = SafeLimitsToggle.isOn;
        var opSliders = OpContainer.GetComponentsInChildren<Slider>();
        for (var i = 0; i < opSliders.Length; i++)
        {
            var opSlider = opSliders[i];
            var op = poly.ConwayOperators[i];
            var opConfig = poly.opconfigs[op.opType];
            if (poly.SafeLimits)
            {
                opSlider.minValue = opConfig.amountSafeMin;
                opSlider.maxValue = opConfig.amountSafeMax;
            }
            else
            {
                opSlider.minValue = opConfig.amountMin;
                opSlider.maxValue = opConfig.amountMax;
            }
        }

        Rebuild();
    }

    void BypassOpsToggleChanged()
    {
        poly.BypassOps = BypassOpsToggle.isOn;
        Rebuild();
    }

    void LoadPresetButtonClicked()
    {
        var btn = EventSystem.current.currentSelectedGameObject;
        PolyPreset preset = null;
        foreach (var p in Presets.Items)
        {
            if (p.Name == btn.name)
            {
                preset = p;
                break;
            }
        }

        if (preset == null)
        {
            Debug.LogError($"No matching preset found for button: {btn.name}");
            return;
        }

        poly.gameObject.GetComponent<MeshFilter>().mesh = null;
        _shouldReBuild = false;
        Presets.ApplyPresetToPoly(preset, LoadMatchingAppearanceToggle.isOn);
        PresetNameInput.text = preset.Name;
        AppearancePresetNameText.text = poly.APresetName;
        InitPolySpecificUI();
        _shouldReBuild = true;
        poly.Rebuild();
    }
    
    void SavePresetButtonClicked()
    {
        var cap = FindObjectOfType<ScreenCaptureTool>();
        cap.TakePresetScreenshotNow(PresetNameInput.text);
        poly.PresetName = PresetNameInput.text;

        // TODO Handle saving over an existing preset
        int countBefore = Presets.Items.Count;
        var preset = Presets.AddOrUpdateFromPoly(PresetNameInput.text);
        preset.Save();
        AddOrUpdatePresetButton(preset);
    }

    void PresetSnapshotButtonClicked()
    {
        // TODO Find a less clunky way to get the button that matches the current preset
        var cap = FindObjectOfType<ScreenCaptureTool>();
        foreach (Transform button in PresetButtonContainer.transform)
        {
            if (button.GetComponentInChildren<Text>().text == PresetNameInput.text)
            {
                cap.TakePresetScreenshotNow(PresetNameInput.text);
                button.GetComponentInChildren<GetPresetImageForButton>().UpdateImage();
                break;
            }
        }
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
