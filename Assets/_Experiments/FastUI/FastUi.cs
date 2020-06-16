using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using Button = UnityEngine.UI.Button;


public class FastUi : MonoBehaviour
{


    private int _RowIndex;
    private int _ColumnIndex;
    private PolyHydra _Poly;
    public Transform RowContainer;
    public Transform RowPrefab;
    public Transform ButtonPrefab;
    private List<PolyHydra.ConwayOperator> _Stack;
    private List<List<Button>> Buttons;
    private enum ButtonType {
        ShapeType, GridType, UniformType, JohnsonType, OtherType,
        PolyTypeCategory, GridShape, PolyP, PolyQ,
        OpType, Amount, Amount2, FaceSelection, Tags,
        Unknown
    }
    private List<List<ButtonType>> ButtonTypeMapping;

    void Start()
    {
        _Poly = FindObjectOfType<PolyHydra>();
        _Stack = _Poly.ConwayOperators;

        UpdateUI();
    }

    private void GetRowButtons(int rowIndex, out List<Button> rowButtons, out List<ButtonType> rowButtonTypes)
    {
        rowButtons = new List<Button>();
        rowButtonTypes = new List<ButtonType>();

        var rowContainer = Instantiate(RowPrefab, RowContainer);

        for (int i = 0; i <= 3; i++)
        {
            (string, ButtonType) nextButtonProps = GetButton(rowIndex, i);
            if (nextButtonProps.Item2 != ButtonType.Unknown)
            {
                var nextButton = Instantiate(ButtonPrefab, rowContainer).GetComponent<Button>();
                nextButton.GetComponentInChildren<TextMeshProUGUI>().text = nextButtonProps.Item1;
                rowButtons.Add(nextButton);
                rowButtonTypes.Add(nextButtonProps.Item2);
            }
        }
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    void HandleKeyboardInput()
    {

        int stackIndex = _RowIndex - 1;

        bool uiDirty = false;
        bool polyDirty = false;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _RowIndex -= 1;
            _RowIndex = Mathf.Clamp(_RowIndex, 0, _Stack.Count);
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, Buttons[_RowIndex].Count - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _RowIndex += 1;
            _RowIndex = Mathf.Clamp(_RowIndex, 0, _Stack.Count);
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, Buttons[_RowIndex].Count - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _ColumnIndex -= 1;
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, Buttons[_RowIndex].Count - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _ColumnIndex += 1;
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, Buttons[_RowIndex].Count - 1);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (stackIndex < 0) return;
            _Stack.RemoveAt(stackIndex);
            _RowIndex = Mathf.Clamp(_RowIndex, 0, Buttons.Count - 1);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            _Stack.Insert(stackIndex + 1, new PolyHydra.ConwayOperator());
            _RowIndex += 1;
            _ColumnIndex = 0;
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            ChangeRow(-1);
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            ChangeRow(1);
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

    private void ChangeRow(int direction)
    {
        if (_RowIndex == 0)
        {
            ChangeBaseShapeRow(direction);
        }
        else
        {
            ChangeOpRow(direction);
        }
    }

    private void ChangeOpRow(int direction)
    {

        int stackIndex = _RowIndex - 1;

        ButtonType buttonType = ButtonTypeMapping[_RowIndex][_ColumnIndex];
        switch (buttonType)
        {
            case ButtonType.OpType:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeOpType(direction);
                break;
            case ButtonType.Amount:
                _Stack[stackIndex] = _Stack[stackIndex].ChangeAmount(direction * 0.025f);
                break;
            case ButtonType.Amount2:
                _Stack[stackIndex] = _Stack[stackIndex].ChangeAmount2(direction * 0.025f);
                break;
            case ButtonType.FaceSelection:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeFaceSelection(direction);
                break;
            case ButtonType.Tags:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
                _Stack[stackIndex] = _Stack[stackIndex].ChangeTags(direction);
                break;
        }
    }

    void ChangeBaseShapeRow(int direction)
    {
        // GetKey brought us here but we only want GetKeyDown in this case
        if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
        int idx;
        switch (_ColumnIndex)
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
        foreach (Transform child in RowContainer)
        {
            Destroy(child.gameObject);
        }

        Buttons = new List<List<Button>>();
        ButtonTypeMapping = new List<List<ButtonType>>();

        var polyRowButtons = new List<Button>();
        var polyRowButtonTypes = new List<ButtonType>();
        var polyRowContainer = Instantiate(RowPrefab, RowContainer);

        Button polyButton;
        (string, ButtonType) polyButtonProps;

        int polyBtnCount = 2;

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
            polyButton = Instantiate(ButtonPrefab, polyRowContainer).GetComponent<Button>();
            polyButtonProps = GetButton(0, i);
            polyButton.GetComponentInChildren<TextMeshProUGUI>().text = polyButtonProps.Item1;
            polyRowButtons.Add(polyButton);
            polyRowButtonTypes.Add(polyButtonProps.Item2);
        }

        Buttons.Add(polyRowButtons);
        ButtonTypeMapping.Add(polyRowButtonTypes);

        for (var rowIndex = 1; rowIndex <= _Stack.Count; rowIndex++)
        {
            var rowButtons = new List<Button>();
            var rowButtonTypes = new List<ButtonType>();
            GetRowButtons(rowIndex, out rowButtons, out rowButtonTypes);
            Buttons.Add(rowButtons);
            ButtonTypeMapping.Add(rowButtonTypes);
        }

        for (int rowIndex=0; rowIndex < Buttons.Count; rowIndex++)
        {

            Color normalColor = rowIndex == 0 ? new Color(1, .8f, .8f) : Color.white;

            for (int colIndex = 0; colIndex < Buttons[rowIndex].Count; colIndex++)
            {
                var btn = Buttons[rowIndex][colIndex];
                var buttonProps = GetButton(rowIndex, colIndex);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = buttonProps.Item1;

                var colors = btn.colors;

                if (rowIndex == _RowIndex && _ColumnIndex == colIndex)
                {
                    colors.normalColor = Color.cyan;
                }
                else
                {
                    colors.normalColor = normalColor;
                }

                btn.colors = colors;
            }
        }
    }

    (string, ButtonType) GetButton(int currentRowIndex, int col)
    {
        string label = "";
        ButtonType buttonType = ButtonType.Unknown;

        if (currentRowIndex == 0)
        {
            switch (col)
            {
                case 0:
                    label = $"{_Poly.ShapeType}"
                        .Replace("Uniform", "Uniform Poly:")
                        .Replace("Johnson", "Johnson Poly:")
                        .Replace("Grid", "Grid:")
                        .Replace("Other", "Other Poly:");
                    break;
                    buttonType = ButtonType.ShapeType;
                case 1:
                    switch (_Poly.ShapeType)
                    {
                        case PolyHydra.ShapeTypes.Uniform:
                            label = $"{_Poly.UniformPolyType}".Replace("_", " "); break;
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

            int stackIndex = currentRowIndex - 1;

            var opConfig = _Poly.opconfigs[_Stack[stackIndex].opType];

            var lookup = (opConfig.usesAmount, opConfig.usesAmount2, opConfig.usesFaces, col);

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


            if (col == 0)
            {
                buttonType = ButtonType.OpType;
            }
            else
            {
                if (_Stack[stackIndex].opType == PolyHydra.Ops.TagFaces)  // Special case
                {
                    switch (col)
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

            switch (buttonType)
            {
                case ButtonType.OpType:
                    label =  $"{_Stack[stackIndex].opType}"; break;
                case ButtonType.Amount:
                    label = $"{_Stack[stackIndex].amount}"; break;
                case ButtonType.Amount2:
                    label = $"{_Stack[stackIndex].amount2}"; break;
                case ButtonType.FaceSelection:
                    label =  $"{_Stack[stackIndex].faceSelections}"; break;
                case ButtonType.Tags:
                    label =  $"{_Stack[stackIndex].Tags}"; break;
            }
        }

        return (label, buttonType);
    }

}
