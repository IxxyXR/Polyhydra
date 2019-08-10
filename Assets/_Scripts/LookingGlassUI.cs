using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class LookingGlassUI : MonoBehaviour {

    [Header("Press physical buttons on Looking Glass for effect")]

    public PolyHydra poly;
    public PolyPresets presets;
    public AppearancePresets aPresets;

    private int presetIndex;

    void Start()
    {
        gameObject.GetComponent<CanvasGroup>().alpha = 0;
    }
    
    void Update ()
    {

        if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.SQUARE))
            ClearOps();
        if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.LEFT))
            PrevPreset();
        else if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.RIGHT))
            NextPreset();
        if (LookingGlass.ButtonManager.GetButtonDown(LookingGlass.ButtonType.CIRCLE))
            RandomizePolyhedra();
        if (Input.GetKey ("escape"))
            Application.Quit();
        
    }


    public void NextPreset()
    {
        presetIndex = (presetIndex + 1) % presets.Items.Count;
        presets.Items[presetIndex].ApplyToPoly(poly, aPresets, true);
        poly.Rebuild();
    }
    
    public void PrevPreset()
    {
        presetIndex = (presetIndex - 1) % presets.Items.Count;
        presets.Items[presetIndex].ApplyToPoly(poly, aPresets, true);
        poly.Rebuild();
    }

    public void ClearOps()
    {
        poly.ConwayOperators.Clear();
        poly.Rebuild();
    }

    public void RandomizePolyhedra()
    {
        poly.UniformPolyType = (PolyTypes)Random.Range(1, Enum.GetValues(typeof(PolyTypes)).Length);
        poly.ConwayOperators.Clear();
        if (!((IList) poly.NonOrientablePolyTypes).Contains((int) poly.UniformPolyType)) // Don't add Conway ops to non-orientable polys
        {
            if (Random.value > 0.1)
            {
                poly.AddRandomOp();
                if (Random.value > 0.2)
                {
                    poly.AddRandomOp();
                    if (Random.value > 0.3)
                    {
                        poly.AddRandomOp();
                    }
                }
            }
        }
        poly.Rebuild();
        aPresets.ApplyPresetToPoly(Random.Range(0, aPresets.Items.Count));
    }   
}
