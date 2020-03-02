using System;
using System.Collections.Generic;
using RtMidi.LowLevel;
using UnityEngine;


public class NovationPrefabController : MonoBehaviour
{
    [Header("Prefabs")]
    public Transform WideButtonPrefab;
    public Transform SquareButtonPrefab;
    public Transform SliderPrefab;
    public Transform DialPrefab;

    [Header("Scale")]
    public float SquareButtonScale;

    [Header("Rotations")]
    public Vector3 SquareButtonRotation;

    [Header("Origins")]
    public Vector3 WideButtonOrigin = new Vector3(0.2969f, 0.0079f, 0.0585f);
    public Vector3 ShiftButtonOrigin = new Vector3(0.2961f, 0.022f, 0.05787034f);
    public Vector3 SelectButtonOrigin = new Vector3(0.298f, 0.022f, 0.05787034f);
    public Vector3 SliderOrigin = new Vector3(0.2961f, 0.05870106f, -0.075f);
    public Vector3 DialOrigin = new Vector3(0.4980748f, 0.02149916f, 0.05770105f);

    [Header("Offsets")]
    public Vector2 WideButtonOffset = new Vector2(0.0248f, 0.02f);
    public Vector2 DialOffset = new Vector2(0.0248f, 0.02f);
    public float SliderOffset = 0.0248f;
    public float ShiftButtonOffset = 0.0248f;
    public Vector2 SelectButtonOffset = new Vector2(0.0248f, 0.02f);

    [NonSerialized]  public MidiOutPort MidiOut;

    [NonSerialized] public List<Transform> Sliders = new List<Transform>();
    [NonSerialized] public List<Transform> WideButtons = new List<Transform>();
    [NonSerialized] public List<Transform> ShiftButtons = new List<Transform>();
    [NonSerialized] public List<Transform> SelectButtons = new List<Transform>();
    [NonSerialized] public List<Transform> Dials = new List<Transform>();

    public List<byte> SliderStates;
    public List<int> WideButtonStates;
    public List<int> ShiftButtonStates;
    public List<int> SelectButtonStates;
    public List<byte> DialStates;

    public int[] ButtonIds =
    {
        41, 42, 43, 44,
        57, 58, 59, 60,
        73, 74, 75, 76,
        89, 90, 91, 92,
    };

    public int[] ButtonColorCodes = {12, 13, 15, 28, 60, 29, 62, 63};

    private Color[] ButtonColors =
    {
    //    12 Off Off
    //    13 Red Low
    //    15 Red Full
    //    28 Green Low
    //    60 Green Full
    //    29 Amber Low
    //    63 Amber Full
    //    62 Yellow Full
        Color.gray * 0.4f,
        Color.red * 0.4f,
        Color.red,
        Color.green * 0.4f,
        Color.green,
        Color.yellow * 0.4f,
        Color.yellow * 0.6f,
        Color.yellow,
    };



    void Start()
    {
        Layout();
    }

    public void Layout()
    {
//        var shiftButton = Instantiate(ShiftButtonPrefab, transform);
//        shiftButton.transform.localPosition = ShiftButtonOrigin;

        for (float j = 0; j < 2; j++)
        {
            for (float i = 0; i < 8; i++)
            {
                var wideButton = Instantiate(WideButtonPrefab, transform);
                var wideButtonPos = new Vector3(
                    WideButtonOrigin.x + i * WideButtonOffset.x,
                    WideButtonOrigin.z + j * WideButtonOffset.y,
                    WideButtonOrigin.y
                );
                wideButton.transform.localPosition = wideButtonPos;
                WideButtons.Insert(0, wideButton);
                WideButtonStates.Add(0);
            }
        }

        for (float j = 0; j < 3; j++)
        {
            for (float i = 0; i < 8; i++)
            {
                var dial = Instantiate(DialPrefab, transform);
                var dialPos = new Vector3(
                    DialOrigin.x + i * DialOffset.x,
                    DialOrigin.z + j * DialOffset.y,
                    DialOrigin.y
                );
                dial.transform.localPosition = dialPos;
                Dials.Add(dial);
                DialStates.Add(0);
            }
        }
        Dials.Reverse();

        for (float i = 0; i < 4; i++)
        {
            var shiftButton = Instantiate(SquareButtonPrefab, transform);
            var shiftButtonPos = new Vector3(
                ShiftButtonOrigin.x,
                ShiftButtonOrigin.z + i * ShiftButtonOffset,
                ShiftButtonOrigin.y
            );
            shiftButton.transform.localScale *= SquareButtonScale;
            shiftButton.transform.localPosition = shiftButtonPos;
            shiftButton.rotation = Quaternion.Euler(SquareButtonRotation);
            ShiftButtons.Add(shiftButton);
            ShiftButtonStates.Add(0);
        }
        ShiftButtons.Reverse();

        for (float i = 0; i < 2; i++)
        {
            for (float j = 0; j < 2; j++)
            {
                var selectButton = Instantiate(SquareButtonPrefab, transform);
                var selectButtonPos = new Vector3(
                    SelectButtonOrigin.x + i * SelectButtonOffset.x,
                    SelectButtonOrigin.z + j * SelectButtonOffset.y,
                    SelectButtonOrigin.y
                );
                selectButton.transform.localScale *= SquareButtonScale;
                selectButton.transform.localPosition = selectButtonPos;
                selectButton.rotation = Quaternion.Euler(SquareButtonRotation);
                SelectButtons.Add(selectButton);
                SelectButtonStates.Add(0);
            }
        }
        SelectButtons.Reverse();

        for (float i = 0; i < 8; i++)
        {
            var slider = Instantiate(SliderPrefab, transform);
            var sliderPos = new Vector3(
                SliderOrigin.x + i * SliderOffset,
                SliderOrigin.y,
                SliderOrigin.z
            );
            slider.transform.localPosition = sliderPos;
            Sliders.Add(slider);
            SliderStates.Add(0);
        }
        Sliders.Reverse();
    }

    private void SetButtonColor(Transform button, int buttonId, int colorIndex)
    {
        MidiOut.SendNoteOn(8, ButtonIds[buttonId], ButtonColorCodes[colorIndex]);
        var mat = button.gameObject.GetComponentInChildren<MeshRenderer>().material;
        mat.SetColor("_BaseColor", ButtonColors[colorIndex]);
        mat.SetColor("_EmissiveColor", ButtonColors[colorIndex] * 2f);
    }

    public void SetShiftButton(int index, int colorIndex)
    {
        ShiftButtonStates[index] = 1 - ShiftButtonStates[index];
        SetButtonColor(ShiftButtons[index], index, colorIndex);
    }

    public void SetSelectButtonColor(int column, int row, int colorIndex)
    {
        int btnIndex = column * 2 + row;
        var btn = SelectButtons[btnIndex];
        ShiftButtonStates[btnIndex] = 1 - ShiftButtonStates[btnIndex];
        SetButtonColor(btn, btnIndex, colorIndex);
    }

    public void SetWideButton(int column, int row, int colorIndex)
    {
        int btnIndex = (row * 8) + column;
        WideButtonStates[btnIndex] = colorIndex;
        var btn = WideButtons[(btnIndex + 8) % 16]; // Fix the row order
        SetButtonColor(btn, btnIndex, colorIndex);
    }

    public void SetSlider(int slider, byte value)
    {
        SliderStates[slider] = value;
        var pos = Sliders[slider].localPosition;
        pos.y = SliderOrigin.y + (value * 0.004f);
        Sliders[slider].localPosition = pos;
    }

    public void SetDial(int row, int dial, byte value)
    {
        int index = (row * 8) + dial;
        DialStates[index] = value;
        var rot = Dials[index].rotation.eulerAngles;
        rot.y = DialOrigin.y + (value * 2.25f - 160f);
        Dials[index].rotation = Quaternion.Euler(rot);
    }

    private void OnValidate()
    {
        return;  // Only use this for tweaking layout
        if (!Application.isPlaying) return;
        foreach (var slider in Sliders)
        {
            Destroy(slider.gameObject);
        }
        Sliders.Clear();
        foreach (var dial in Dials)
        {
            Destroy(dial.gameObject);
        }
        Dials.Clear();
        foreach (var btn in WideButtons)
        {
            Destroy(btn.gameObject);
        }
        WideButtons.Clear();
        foreach (var btn in ShiftButtons)
        {
            Destroy(btn.gameObject);
        }
        ShiftButtons.Clear();
        foreach (var btn in SelectButtons)
        {
            Destroy(btn.gameObject);
        }
        SelectButtons.Clear();

        Layout();
    }
}
