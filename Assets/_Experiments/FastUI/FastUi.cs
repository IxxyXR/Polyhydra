using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;


public class FastUi : MonoBehaviour
{


    private int _PanelIndex;
    private int _PanelWidgetIndex;
    private PolyHydra _Poly;

    public int FrameSkip = 3;
    public Color HighlightColor;
    public Color MainMenuHighlightColor;
    public Color ShapeButtonColor;
    public Transform PanelContainer;
    public Transform MainMenuContainer;
    public Transform PanelPrefab;
    public Transform TextButtonPrefab;
    public Transform ValueButtonPrefab;
    public Transform ImagePanelPrefab;
    public TextMeshProUGUI TipsText;
    public TextMeshProUGUI DebugText;

    private List<PolyPreset> Presets;
    private List<PolyHydra.ConwayOperator> _Stack;
    private List<List<Transform>> Widgets;
    private List<List<ButtonType>> ButtonTypeMapping;
    private int _MainMenuCount;
    private int _CurrentMainMenuIndex;

    private enum ButtonType {
        ShapeType, GridType, UniformType, JohnsonType, OtherType,
        PolyTypeCategory, GridShape, PolyP, PolyQ,
        OpType, Amount, Amount2, FaceSelection, Tags,
        Unknown
    }

    void Start()
    {
        _Poly = FindObjectOfType<PolyHydra>();
        _Stack = _Poly.ConwayOperators;
        _MainMenuCount = MainMenuContainer.childCount;

        UpdateUI();
        UpdateTips();
        StartCoroutine(GetPresets("http://polyhydra.org.uk/api/presets/"));
    }

    private void UpdateTips()
    {
        PlayerPrefs.GetInt("currentTip");
    }

    IEnumerator GetPresets(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
                DebugText.text = webRequest.error;
            }
            else
            {
                var presetsDictionary = JsonConvert.DeserializeObject<Dictionary<string, RemotePreset>>(webRequest.downloadHandler.text);
                var remotePresets = presetsDictionary.Values.ToList();
                Presets = remotePresets.Select(item => item.preset).ToList();
            }
        }
    }

    private void GetPanelWidgets(int panelIndex, out List<Transform> panelWidgets, out List<ButtonType> panelWidgetTypes)
    {
        panelWidgets = new List<Transform>();
        panelWidgetTypes = new List<ButtonType>();

        int stackIndex = panelIndex - 1;
        bool isDisabled = _Stack[stackIndex].disabled;

        var panelContainer = Instantiate(PanelPrefab, PanelContainer);
        panelContainer.GetComponent<CanvasGroup>().alpha = isDisabled ? 0.5f : 1f;


        for (int i = 0; i <= 3; i++)
        {
            Transform nextWidget = null;
            (string label, ButtonType buttonType) = GetButton(panelIndex, i);

            if (buttonType == ButtonType.OpType)
            {
                nextWidget = Instantiate(TextButtonPrefab, panelContainer);
                var img = Instantiate(ImagePanelPrefab, panelContainer);
                img.GetComponentInChildren<SVGImage>().sprite = Resources.Load<Sprite>("Icons/" + _Stack[stackIndex].opType);
            }
            else if (buttonType == ButtonType.Amount || buttonType == ButtonType.Amount2)
            {
                nextWidget = Instantiate(ValueButtonPrefab, panelContainer);
                (float amount, float amount2) = GetNormalisedAmountValues(_Stack[stackIndex]);
                var rt = nextWidget.gameObject.GetComponentsInChildren<Image>().Last().transform as RectTransform;
                var d = rt.sizeDelta;
                d.x = buttonType == ButtonType.Amount ? amount : amount2;
                rt.sizeDelta = d;
            }
            else if (buttonType == ButtonType.FaceSelection)
            {
                nextWidget = Instantiate(TextButtonPrefab, panelContainer);
            }

            if (nextWidget != null)
            {
                nextWidget.GetComponentInChildren<TextMeshProUGUI>().text = label;
                // nextWidget.GetComponent<Button>().interactable = !isDisabled;
                panelWidgets.Add(nextWidget);
                panelWidgetTypes.Add(buttonType);
            }
        }
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    int GetWidgetCount()
    {
        // Number of widgets in this column
        // Special case for index -1 as it's the main menu
        return _PanelIndex >= 0 ? Widgets[_PanelIndex].Count : _MainMenuCount;
    }

    void HandleKeyboardInput()
    {

        int stackIndex = _PanelIndex - 1;

        bool uiDirty = false;
        bool polyDirty = false;
        if (Input.GetKeyDown(KeyCode.R))
        {
            var rb = _Poly.transform.parent.GetComponent<Rigidbody>();
            rb.angularVelocity = new Vector3(Random.value, Random.value, Random.value);
            rb.isKinematic = !rb.isKinematic;
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            var preset = Presets[Random.Range(0, Presets.Count)];
            preset.Ops = preset.Ops.Where(i => !i.Disabled && i.OpType!=PolyHydra.Ops.Identity).ToArray();
            preset.ApplyToPoly(_Poly);
            _Stack = _Poly.ConwayOperators;
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            if (stackIndex < 0) return;
            var op = _Stack[stackIndex];
            op.disabled = !op.disabled;
            _Stack[stackIndex] = op;
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            var rb = _Poly.transform.parent.GetComponent<Rigidbody>();
            rb.isKinematic = !rb.isKinematic;
            if (rb.isKinematic)
            {
                Transform parent = _Poly.transform.parent;
                parent.rotation = Quaternion.identity;
                _Poly.transform.parent = parent;
            }
            else
            {
                rb.angularVelocity = new Vector3(Random.value, Random.value, Random.value);
            }
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetAxis("Controller Right Stick X") < -0.1f)
        {
            _PanelIndex -= 1;
            _PanelIndex = Mathf.Clamp(_PanelIndex, -1, _Stack.Count);
            _PanelWidgetIndex = Mathf.Clamp(_PanelWidgetIndex, 0, GetWidgetCount() - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetAxis("Controller Right Stick X") > 0.1f)
        {
            _PanelIndex += 1;
            _PanelIndex = Mathf.Clamp(_PanelIndex, -1, _Stack.Count);
            _PanelWidgetIndex = Mathf.Clamp(_PanelWidgetIndex, 0, GetWidgetCount() - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetAxis("Controller Right Stick Y") > 0.1f)
        {
            _PanelWidgetIndex -= 1;
            _PanelWidgetIndex = Mathf.Clamp(_PanelWidgetIndex, 0, GetWidgetCount() - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetAxis("Controller Right Stick Y") < -0.1f)
        {
            _PanelWidgetIndex += 1;
            _PanelWidgetIndex = Mathf.Clamp(_PanelWidgetIndex, 0, GetWidgetCount() - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (stackIndex < 0) return;
            _Stack.RemoveAt(stackIndex);
            _PanelIndex = Mathf.Clamp(_PanelIndex, 0, _Stack.Count);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (_PanelIndex == -1)
            {
                _CurrentMainMenuIndex = _PanelWidgetIndex;
            }
            else
            {
                _Stack.Insert(stackIndex + 1, new PolyHydra.ConwayOperator());
                _PanelIndex += 1;
                _PanelWidgetIndex = 0;
                polyDirty = true;
            }
            uiDirty = true;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetAxis("Horizontal") < -0.1f)
        {
            if (_PanelIndex < 0) return;
            ChangeValue(-1);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetAxis("Horizontal") > 0.1f)
        {
            if (_PanelIndex < 0) return;
            ChangeValue(1);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetAxis("Vertical") < -0.1f)
        {
            if (_PanelIndex < 0) return;
            ChangeValue(-10);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0.1f)
        {
            if (_PanelIndex < 0) return;
            ChangeValue(10);
            uiDirty = true;
            polyDirty = true;
        }

        if (uiDirty) UpdateUI();
        if (polyDirty)
        {
            _Poly.Validate();
            _Poly.Rebuild();
        }
    }

    private void ChangeValue(int direction)
    {
        if (_PanelIndex == 0)
        {
            ChangeValueOnPolyPanel(direction);
        }
        else
        {
            ChangeValueOnOpPanel(direction);
        }
    }

    private bool IsKeyDownValid()
    {
        return !(Input.GetKeyDown(KeyCode.A) ||
                 Input.GetKeyDown(KeyCode.D) ||
                 Input.GetKeyDown(KeyCode.W) ||
                 Input.GetKeyDown(KeyCode.S)) &&
               (Mathf.Abs(Input.GetAxis("Horizontal")) < 0.5f && Mathf.Abs(Input.GetAxis("Vertical")) < 0.5f);
    }


    private void ChangeValueOnOpPanel(int direction)
    {

        int stackIndex = _PanelIndex - 1;
        var opConfig = _Poly.opconfigs[_Stack[stackIndex].opType];


        ButtonType buttonType = ButtonTypeMapping[_PanelIndex][_PanelWidgetIndex];
        switch (buttonType)
        {
            case ButtonType.OpType:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (IsKeyDownValid()) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeOpType(direction);_Stack[stackIndex] = _Stack[stackIndex].ChangeOpType(direction);
                _Stack[stackIndex] = _Stack[stackIndex].SetDefaultValues(opConfig);
                break;
            case ButtonType.Amount:
                if (Time.frameCount % FrameSkip == 0) return;  // Rate limit
                _Stack[stackIndex] = _Stack[stackIndex].ChangeAmount(direction * 0.05f);
                _Stack[stackIndex] = _Stack[stackIndex].ClampAmount(opConfig);
                break;
            case ButtonType.Amount2:
                if (Time.frameCount % FrameSkip == 0) return;  // Rate limit
                _Stack[stackIndex] = _Stack[stackIndex].ChangeAmount2(direction * 0.05f);
                _Stack[stackIndex] = _Stack[stackIndex].ClampAmount2(opConfig);
                break;
            case ButtonType.FaceSelection:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (IsKeyDownValid()) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeFaceSelection(direction);
                break;
            case ButtonType.Tags:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (IsKeyDownValid()) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeTags(direction);
                break;
        }
    }

    void ChangeValueOnPolyPanel(int direction)
    {
        // GetKey brought us here but we only want GetKeyDown in this case
        if (IsKeyDownValid()) return;
        int idx;
        switch (_PanelWidgetIndex)
        {
            case 0:
                idx = (int) _Poly.ShapeType;
                idx += direction;
                idx = Mathf.Clamp(idx, 0, Enum.GetNames(typeof(PolyHydra.ShapeTypes)).Length - 1);
                _Poly.ShapeType = (PolyHydra.ShapeTypes)idx;
                break;
            case 1:
                switch (_Poly.ShapeType)
                {
                    case PolyHydra.ShapeTypes.Uniform:
                        idx = (int) _Poly.UniformPolyType;
                        idx += direction;
                        idx = Mathf.Clamp(idx, 0, Enum.GetNames(typeof(PolyTypes)).Length - 1);
                        _Poly.UniformPolyType = (PolyTypes)idx;
                        break;
                    case PolyHydra.ShapeTypes.Johnson:
                        idx = (int) _Poly.JohnsonPolyType;
                        idx += direction;
                        idx = Mathf.Clamp(idx, 0, Enum.GetNames(typeof(PolyHydra.JohnsonPolyTypes)).Length - 1);
                        _Poly.JohnsonPolyType = (PolyHydra.JohnsonPolyTypes)idx;
                        break;
                    case PolyHydra.ShapeTypes.Grid:
                        idx = (int) _Poly.GridType;
                        idx += direction;
                        idx = Mathf.Clamp(idx, 0, Enum.GetNames(typeof(PolyHydra.GridTypes)).Length - 1);
                        _Poly.GridType = (PolyHydra.GridTypes)idx;
                        break;
                    case PolyHydra.ShapeTypes.Other:
                        idx = (int) _Poly.OtherPolyType;
                        idx += direction;
                        idx = Mathf.Clamp(idx, 0, Enum.GetNames(typeof(PolyHydra.OtherPolyTypes)).Length - 1);
                        _Poly.OtherPolyType = (PolyHydra.OtherPolyTypes)idx;
                        break;
                }
                break;
            case 2:
                int p = _Poly.PrismP;
                p += direction;
                p = Mathf.Clamp(p, 0, 32);
                _Poly.PrismP = p;
                break;
            case 3:
                int q = _Poly.PrismQ;
                q += direction;
                q = Mathf.Clamp(q, 0, 32);
                _Poly.PrismQ = q;
                break;
        }

    }

    void UpdateUI()
    {
        foreach (Transform child in PanelContainer)
        {
            if (child.gameObject.name != "Main Menu")
            {
                Destroy(child.gameObject);
            }
        }

        Widgets = new List<List<Transform>>();
        ButtonTypeMapping = new List<List<ButtonType>>();

        var polyWidgets = new List<Transform>();
        var polyWidgetTypes = new List<ButtonType>();
        var polyPanel = Instantiate(PanelPrefab, PanelContainer);

        Transform polyButton;

        int polyBtnCount = 2;

        for (int i=0; i < _MainMenuCount; i++)
        {
            var img = MainMenuContainer.GetChild(i).GetComponent<Image>();

            var menuColor = i == _CurrentMainMenuIndex ? MainMenuHighlightColor : Color.white;

            if (_PanelIndex == -1 && _PanelWidgetIndex == i)
            {
                img.color = HighlightColor;
            }
            else
            {
                img.color = menuColor;
            }

        }

        if (
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Uniform && (int)_Poly.UniformPolyType < 5) ||
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Other && (int)_Poly.OtherPolyType == 0) ||
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Johnson)
            )
        {
            polyBtnCount = 3;
        }
        else if (
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Uniform && (int)_Poly.UniformPolyType < 5) ||
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Other && (int)_Poly.OtherPolyType < 4) ||
            (_Poly.ShapeType==PolyHydra.ShapeTypes.Grid)
        )
        {
            polyBtnCount = 4;
        }

        for (int i = 0; i < polyBtnCount; i++)
        {
            polyButton = Instantiate(TextButtonPrefab, polyPanel);
            if (i == 2)
            {
                //Instantiate(ImagePanelPrefab, polyPanel);
            }

            (string label, ButtonType buttonType) = GetButton(0, i);
            polyButton.GetComponentInChildren<TextMeshProUGUI>().text = label;
            polyWidgets.Add(polyButton);
            polyWidgetTypes.Add(buttonType);
        }

        Widgets.Add(polyWidgets);
        ButtonTypeMapping.Add(polyWidgetTypes);

        for (var widgetIndex = 1; widgetIndex <= _Stack.Count; widgetIndex++)
        {
            var panelButtons = new List<Transform>();
            var panelWidgetTypes = new List<ButtonType>();
            GetPanelWidgets(widgetIndex, out panelButtons, out panelWidgetTypes);
            Widgets.Add(panelButtons);
            ButtonTypeMapping.Add(panelWidgetTypes);
        }

        for (int panelIndex=0; panelIndex < Widgets.Count; panelIndex++)
        {

            Color normalColor = panelIndex == 0 ? ShapeButtonColor : Color.white;

            for (int widgetIndex = 0; widgetIndex < Widgets[panelIndex].Count; widgetIndex++)
            {
                var widget = Widgets[panelIndex][widgetIndex];
                (string label, ButtonType buttonType) = GetButton(panelIndex, widgetIndex);
                widget.GetComponentInChildren<TextMeshProUGUI>().text = label;

                var colors = widget.GetComponent<Button>().colors;

                if (panelIndex == _PanelIndex && _PanelWidgetIndex == widgetIndex)
                {
                    colors.normalColor = HighlightColor;
                }
                else
                {
                    colors.normalColor = normalColor;
                }

                widget.GetComponent<Button>().colors = colors;
            }
        }
    }

    (string, ButtonType) GetButton(int currentPanelIndex, int widgetIndex)
    {
        string label = "";
        ButtonType buttonType = ButtonType.Unknown;

        if (currentPanelIndex == 0)
        {
            switch (widgetIndex)
            {
                case 0:
                    label = $"{_Poly.ShapeType}"
                        .Replace("Uniform", "Wythoff Poly")
                        .Replace("Johnson", "Johnson Poly")
                        .Replace("Grid", "Grid")
                        .Replace("Other", "Other Poly");
                    break;
                    buttonType = ButtonType.ShapeType;
                case 1:
                    switch (_Poly.ShapeType)
                    {
                        case PolyHydra.ShapeTypes.Uniform:
                            string uniformName = _Poly.UniformPolyType.ToString().Replace("_", " ");

                            label = $"{uniformName}"; break;
                            buttonType = ButtonType.UniformType;
                        case PolyHydra.ShapeTypes.Grid:
                            label = $"{_Poly.GridType}"; break;
                            buttonType = ButtonType.GridType;
                        case PolyHydra.ShapeTypes.Johnson:
                            label = $"{_Poly.JohnsonPolyType}"; break;
                            buttonType = ButtonType.JohnsonType;
                        case PolyHydra.ShapeTypes.Other:
                            label = $"{_Poly.OtherPolyType}"; break;
                            buttonType = ButtonType.OtherType;

                    }
                    break;
                case 2:
                    label = $"{_Poly.PrismP}"; break;
                case 3:
                    label = $"{_Poly.PrismQ}"; break;
            }
        }
        else
        {

            int stackIndex = currentPanelIndex - 1;

            var opConfig = _Poly.opconfigs[_Stack[stackIndex].opType];

            var lookup = (opConfig.usesAmount, opConfig.usesAmount2, opConfig.usesFaces, col: widgetIndex);

            // Handle all the permutations of config, column and button type
            var logicTable = new Dictionary<(bool, bool, bool, int), ButtonType>
            {
                {(false, false, false, 1), ButtonType.Unknown},
                {(false, false, false, 2), ButtonType.Unknown},
                {(false, false, false, 3), ButtonType.Unknown},
                {(false, false, false, 4), ButtonType.Unknown},

                {(false, false, true, 1), ButtonType.FaceSelection},
                {(false, false, true, 2), ButtonType.Tags},
                {(false, false, true, 3), ButtonType.Unknown},
                {(false, false, true, 4), ButtonType.Unknown},

                {(true, false, false, 1), ButtonType.Amount},
                {(true, false, false, 2), ButtonType.Unknown},
                {(true, false, false, 3), ButtonType.Unknown},
                {(true, false, false, 4), ButtonType.Unknown},

                {(true, false, true, 1), ButtonType.Amount},
                {(true, false, true, 2), ButtonType.FaceSelection},
                {(true, false, true, 3), ButtonType.Tags},
                {(true, false, true, 4), ButtonType.Unknown},

                {(true, true, false, 1), ButtonType.Amount},
                {(true, true, false, 2), ButtonType.Amount2},
                {(true, true, false, 3), ButtonType.Unknown},
                {(true, true, false, 4), ButtonType.Unknown},

                {(true, true, true, 1), ButtonType.Amount},
                {(true, true, true, 2), ButtonType.Amount2},
                {(true, true, true, 3), ButtonType.FaceSelection},
                {(true, true, true, 4), ButtonType.Tags},
            };


            if (widgetIndex == 0)
            {
                buttonType = ButtonType.OpType;
            }
            else
            {
                if (_Stack[stackIndex].opType == PolyHydra.Ops.TagFaces)  // Special case
                {
                    switch (widgetIndex)
                    {
                        case 1: buttonType = ButtonType.Tags; break;
                        case 2: buttonType = ButtonType.FaceSelection; break;
                        case 3: buttonType = ButtonType.Unknown; break;
                        case 4: buttonType = ButtonType.Unknown; break;
                    }
                }
                else  // Use normal lookup
                {
                    buttonType = logicTable[lookup];
                }
            }

            (float amount, float amount2) = GetNormalisedAmountValues(_Stack[stackIndex]);

            switch (buttonType)
            {
                case ButtonType.OpType:
                    label =  $"{_Stack[stackIndex].opType}"; break;
                case ButtonType.Amount:
                    label = $"{amount}%"; break;
                case ButtonType.Amount2:
                    label = $"{amount2}%"; break;
                case ButtonType.FaceSelection:
                    label =  $"{_Stack[stackIndex].faceSelections}"; break;
                case ButtonType.Tags:
                    label =  $"{_Stack[stackIndex].Tags}"; break;
            }
        }

        return (label, buttonType);
    }

    private (float, float) GetNormalisedAmountValues(PolyHydra.ConwayOperator conwayOperator)
    {
        var config = _Poly.opconfigs[conwayOperator.opType];
        float rawVal = conwayOperator.amount;
        float rawVal2 = conwayOperator.amount2;
        float minVal = config.amountSafeMin;
        float minVal2 = config.amount2SafeMin;
        float maxVal = config.amountSafeMax;
        float maxVal2 = config.amount2SafeMax;
        return (
            Mathf.Floor(Mathf.InverseLerp(minVal, maxVal, rawVal) * 100f),
            Mathf.Floor(Mathf.InverseLerp(minVal2, maxVal2, rawVal2) * 100f)
        );
    }
}

public struct RemotePreset
{
    public string thumbnail;
    public PolyPreset preset;
}
