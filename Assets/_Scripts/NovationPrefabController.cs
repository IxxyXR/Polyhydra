using System;
using System.Collections.Generic;
using UnityEngine;
using ADBannerView = UnityEngine.iOS.ADBannerView;


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

    [NonSerialized] public List<Transform> Sliders = new List<Transform>();
    [NonSerialized] public List<Transform> WideButtons = new List<Transform>();
    [NonSerialized] public List<Transform> ShiftButtons = new List<Transform>();
    [NonSerialized] public List<Transform> SelectButtons = new List<Transform>();
    [NonSerialized] public List<Transform> Dials = new List<Transform>();

    private Color[] ButtonColors = {Color.gray, Color.green, Color.yellow, Color.red};

    void Start()
    {
        Layout();
    }

    public void Layout()
    {
//        var shiftButton = Instantiate(ShiftButtonPrefab, transform);
//        shiftButton.transform.localPosition = ShiftButtonOrigin;

        for (float i = 0; i < 8; i++)
        {
            for (float j = 0; j < 2; j++)
            {
                var wideButton = Instantiate(WideButtonPrefab, transform);
                var wideButtonPos = new Vector3(
                    WideButtonOrigin.x + i * WideButtonOffset.x,
                    WideButtonOrigin.z + j * WideButtonOffset.y,
                    WideButtonOrigin.y
                );
                wideButton.transform.localPosition = wideButtonPos;
                WideButtons.Add(wideButton);
            }
        }

        for (float i = 0; i < 8; i++)
        {
            for (float j = 0; j < 3; j++)
            {
                var dial = Instantiate(DialPrefab, transform);
                var dialPos = new Vector3(
                    DialOrigin.x + i * DialOffset.x,
                    DialOrigin.z + j * DialOffset.y,
                    DialOrigin.y
                );
                dial.transform.localPosition = dialPos;
                Dials.Add(dial);
            }
        }

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
        }

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
            }
        }

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
        }
    }

    private void SetButtonColor(Transform button, int colorIndex)
    {
        var mat = button.gameObject.GetComponent<MeshRenderer>().material;
        mat.SetColor("_BaseColor", ButtonColors[colorIndex + 1]);
        mat.SetColor("_EmissiveColor", ButtonColors[colorIndex + 1]);
    }

//    public void SetSjiftButton(int column, int colorIndex)
//    {
//        SetButtonColor(ColumnButtons[column], colorIndex);
//    }

//    public void SetGridButtonIcon(int column, int row, PolyHydra.Ops opType)
//    {
//        var btn = WideButtons[column * 8 + row];
//        var img = btn.GetComponentInChildren<Image>();
//        img.sprite = Resources.Load<Sprite>("Icons/" + opType);
//    }
//
//    public void SetGridButtonLED(int column, int row, int colorIndex)
//    {
//        var btn = WideButtons[column * 8 + row];
//        SetButtonColor(btn, colorIndex);
//    }

    public void SetSlider(int slider, byte value)
    {
        var pos = Sliders[slider].localPosition;
        pos.z = SliderOrigin.y + (value / 3500f) - 0.135f;
        Sliders[slider].localPosition = pos;
    }

    private void OnValidate()
    {
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
