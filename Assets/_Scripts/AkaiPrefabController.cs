using System;
using System.Collections;
using System.Collections.Generic;
using RtMidi.LowLevel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AkaiPrefabController : MonoBehaviour
{
    public Transform SquareButtonPrefab;
    public Transform RoundButtonPrefab;
    public Transform ShiftButtonPrefab;
    public Transform SliderPrefab;
    public Transform IconPrefab;

    public Vector3 MainButtonOrigin = new Vector3(0.2969f, 0.0079f, 0.0585f);
    public Vector3 ColumnButtonOrigin = new Vector3(0.2961f, 0.022f, 0.05787034f);
    public Vector3 RowButtonOrigin = new Vector3(0.298f, 0.022f, 0.05787034f);
    public Vector3 SliderOrigin = new Vector3(0.2961f, 0.05870106f, -0.075f);
    public Vector3 ShiftButtonOrigin = new Vector3(0.4980748f, 0.02149916f, 0.05770105f);

    public Vector2 MainButtonOffset = new Vector2( 0.0248f, 0.02f);
    public float SliderOffset = 0.0248f;
    public float RowButtonOffset = 0.02f;
    public float ColumnButtonOffset = 0.0248f;

    [NonSerialized]  public MidiOutPort MidiOut;

    [NonSerialized] public List<Transform> Sliders = new List<Transform>();
    [NonSerialized] public List<Transform> MainButtons = new List<Transform>();
    [NonSerialized] public List<Transform> RowButtons = new List<Transform>();
    [NonSerialized] public List<Transform> ColumnButtons = new List<Transform>();

    private Color[] ButtonColors = {Color.gray, Color.green, Color.yellow, Color.red};

    void Start()
    {
        var shiftButton = Instantiate(ShiftButtonPrefab, transform);
        shiftButton.transform.localPosition = ShiftButtonOrigin;

        for (float i = 0; i < 8; i++)
        {
            for (float j = 0; j < 8; j++)
            {
                var mainButton = Instantiate(SquareButtonPrefab, transform);
                var mainButtonPos = new Vector3(
                    MainButtonOrigin.x + i * MainButtonOffset.x,
                    MainButtonOrigin.y,
                    MainButtonOrigin.z + j * MainButtonOffset.y
                );
                mainButton.transform.localPosition = mainButtonPos;
                MainButtons.Add(mainButton);
            }
        }

        for (float i = 0; i < 8; i++)
        {
            var rowButton = Instantiate(RoundButtonPrefab, transform);
            var rowButtonPos = new Vector3(
                RowButtonOrigin.x,
                RowButtonOrigin.y,
                RowButtonOrigin.z + i * RowButtonOffset
            );
            rowButton.transform.localPosition = rowButtonPos;
            RowButtons.Add(rowButton);
        }

        for (float i = 0; i < 8; i++)
        {
            var columnButton = Instantiate(RoundButtonPrefab, transform);
            var columnButtonPos = new Vector3(
                ColumnButtonOrigin.x + i * ColumnButtonOffset,
                ColumnButtonOrigin.y,
                ColumnButtonOrigin.z
            );
            columnButton.transform.localPosition = columnButtonPos;
            ColumnButtons.Add(columnButton);
        }

        for (float i = 0; i < 9; i++)
        {
            var slider = Instantiate(SliderPrefab, transform);
            var sliderPos = new Vector3(
                SliderOrigin.x + i * SliderOffset,
                SliderOrigin.y,
                SliderOrigin.z
            );
            slider.transform.localPosition = sliderPos;
            Sliders.Add(slider);
        }
    }

    private void SetButtonColor(Transform button, int colorIndex)
    {
        var mat = button.gameObject.GetComponent<MeshRenderer>().material;
        mat.SetColor("_BaseColor", ButtonColors[colorIndex + 1]);
        mat.SetColor("_EmissiveColor", ButtonColors[colorIndex + 1]);
    }

    public void SetColumnButton(int column, int colorIndex)
    {
        SetButtonColor(ColumnButtons[column], colorIndex);
    }

    public void SetGridButtonIcon(int column, int row, PolyHydra.Ops opType)
    {
        var btn = MainButtons[column * 8 + row];
        var img = btn.GetComponentInChildren<Image>();
        img.sprite = Resources.Load<Sprite>("Icons/" + opType);
    }

    public void SetGridButtonLED(int column, int row, int colorIndex)
    {
        var btn = MainButtons[column * 8 + row];
        SetButtonColor(btn, colorIndex);
    }

    public void SetSlider(int slider, byte value)
    {
        var pos = Sliders[slider].localPosition;
        pos.z = SliderOrigin.y + (value / 3500f) - 0.135f;
        Sliders[slider].localPosition = pos;
    }
}
