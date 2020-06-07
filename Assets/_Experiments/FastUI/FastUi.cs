using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;


public class FastUi : MonoBehaviour
{
    private int _StackIndex = -1;
    private int _ColumnIndex;
    private PolyHydra _Poly;
    public Transform _PrevRow;
    public Transform _CurrentRow;
    public Transform _NextRow;
    private List<PolyHydra.ConwayOperator> _Stack;
    public Button[][] buttons;

    void Start()
    {
        _Poly = FindObjectOfType<PolyHydra>();
        _Stack = _Poly.ConwayOperators;
        buttons = new []
        {
            _PrevRow.GetComponentsInChildren<Button>(),
            _CurrentRow.GetComponentsInChildren<Button>(),
            _NextRow.GetComponentsInChildren<Button>(),
        };
        ChangeBaseShapeRow(0);
        UpdateUi();
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    void HandleKeyboardInput()
    {
        bool uiDirty = false;
        bool polyDirty = false;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _StackIndex -= 1;
            _StackIndex = Mathf.Clamp(_StackIndex, -1, _Stack.Count - 1);
            while (GetLabelText(_StackIndex, _ColumnIndex) == "" && _ColumnIndex > 0)
            {
                _ColumnIndex -= 1;
            }
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _StackIndex += 1;
            _StackIndex = Mathf.Clamp(_StackIndex, -1, _Stack.Count - 1);
            while (GetLabelText(_StackIndex, _ColumnIndex) == "" && _ColumnIndex > 0)
            {
                _ColumnIndex -= 1;
            }
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (GetLabelText(_StackIndex, _ColumnIndex - 1) != "")
            {
                _ColumnIndex -= 1;
            }
            else if (GetLabelText(_StackIndex, _ColumnIndex - 2) != "")
            {
                _ColumnIndex -= 2;
            }
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, 3);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (GetLabelText(_StackIndex, _ColumnIndex + 1) != "")
            {
                _ColumnIndex += 1;
            }
            else if (GetLabelText(_StackIndex, _ColumnIndex + 2) != "")
            {
                _ColumnIndex += 2;
            }
            _ColumnIndex = Mathf.Clamp(_ColumnIndex, 0, 3);
            uiDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            _Stack.RemoveAt(_StackIndex);
            if (_StackIndex > _Stack.Count - 1) _StackIndex = _Stack.Count - 1;
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            _Stack.Insert(_StackIndex + 1, new PolyHydra.ConwayOperator());
            _StackIndex += 1;
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (_StackIndex == -1)
            {
                ChangeBaseShapeRow(-1);
            }
            else
            {
                ChangeOpRow(-1);

            }
            uiDirty = true;
            polyDirty = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            if (_StackIndex == -1)
            {
                ChangeBaseShapeRow(1);
            }
            else
            {
                ChangeOpRow(1);

            }
            uiDirty = true;
            polyDirty = true;
        }

        if (uiDirty) UpdateUi();
        if (polyDirty)
        {
            _Poly.Validate();
            _Poly.Rebuild();
        }
    }

    private void ChangeOpRow(int direction)
    {
        switch (_ColumnIndex)
        {
            case 0:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
                _Stack[_StackIndex] = _Stack[_StackIndex].ChangeOpType(direction);
                break;
            case 1:
                _Stack[_StackIndex] = _Stack[_StackIndex].ChangeAmount(direction * 0.025f);
                break;
            case 2:
                _Stack[_StackIndex] = _Stack[_StackIndex].ChangeAmount2(direction * 0.025f);
                break;
            case 3:
                // GetKey brought us here but we only want GetKeyDown in this case
                if (!(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) return;
                _Stack[_StackIndex] = _Stack[_StackIndex].ChangeFaceSelection(direction);
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
            // case 1:
            //     switch (_Poly.ShapeType)
            //     {
            //         case PolyHydra.ShapeTypes.Uniform:
            //             break;
            //         case PolyHydra.ShapeTypes.Johnson:
            //             break;
            //         case PolyHydra.ShapeTypes.Grid:
            //             break;
            //     }
            //     break;
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
        }

    }

    void UpdateUi()
    {
        for (int row=0; row<=2; row++)
        {
            int rowOffset = row - 1;
            int rowStackIndex = _StackIndex + rowOffset;
            int btnCount = 4;
            if (rowStackIndex <= -1)
            {
                btnCount = 2;
                buttons[row][2].gameObject.SetActive(false);
                buttons[row][3].gameObject.SetActive(false);
            }
            else
            {
                buttons[row][2].gameObject.SetActive(true);
                buttons[row][3].gameObject.SetActive(true);
            }
            for (int col = 0; col < btnCount; col++)
            {
                var btn = buttons[row][col];
                string text = GetLabelText(rowStackIndex, col);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
                if (text == "")
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.interactable = true;
                }

                var colors = btn.colors;

                if (row == 1 && _ColumnIndex == col)
                {
                    colors.normalColor = Color.cyan;
                }
                else
                {
                    colors.normalColor = Color.white;

                }

                btn.colors = colors;
            }
        }
    }

    string GetLabelText(int stackIndex, int col)
    {
        string stackLabel = "";
        if (stackIndex == -1)  //
        {
            switch (col)
            {
                case 0:
                    stackLabel = _Poly.ShapeType.ToString(); break;
                case 1:
                    switch (_Poly.ShapeType)
                    {
                        case PolyHydra.ShapeTypes.Uniform:
                            stackLabel = _Poly.UniformPolyType.ToString().Replace("_", " "); break;
                        case PolyHydra.ShapeTypes.Grid:
                            stackLabel = _Poly.GridType.ToString(); break;
                        case PolyHydra.ShapeTypes.Johnson:
                            stackLabel = _Poly.JohnsonPolyType.ToString(); break;
                        case PolyHydra.ShapeTypes.Other:
                            stackLabel = _Poly.OtherPolyType.ToString(); break;

                    }
                    break;
                case 2:
                    stackLabel = ""; break;
                case 3:
                    stackLabel = ""; break;
            }
        }
        else if (stackIndex >= 0 && stackIndex < _Stack.Count)
        {
            switch (col)
            {
                case 0:
                    stackLabel =  $"{_Stack[stackIndex].opType}";
                    break;
                case 1:
                    if (_Poly.opconfigs[_Stack[stackIndex].opType].usesAmount)
                    {
                        stackLabel = $"{_Stack[stackIndex].amount}";
                    }
                    break;
                case 2:
                    if (_Poly.opconfigs[_Stack[stackIndex].opType].usesAmount2)
                    {
                        stackLabel = $"{_Stack[stackIndex].amount2}";
                    }
                    break;
                case 3:
                    if (_Poly.opconfigs[_Stack[stackIndex].opType].usesFaces)
                    {
                        stackLabel =  $"{_Stack[stackIndex].faceSelections}";

                    }
                    break;
            }
        }

        return stackLabel;
    }

}
