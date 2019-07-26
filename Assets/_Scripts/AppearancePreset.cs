using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class AppearancePreset
{
    public enum ColorMethods
    {
        BySides,
        ByRole
    }

    public string Name;
    public Material PolyhedronMaterial;
    public ColorMethods PolyhedronColorMethod;
    public List<Light> ActiveLights;
    public VolumeProfile ActiveVolumeProfile;

    public void ApplyToPoly(ref PolyHydra poly, GameObject LightsParent, Volume activeVolume, Camera CurrentCamera)
    {
        poly.APresetName = Name;
        poly.gameObject.GetComponent<MeshRenderer>().material = PolyhedronMaterial;
        poly.ColorMethod = PolyhedronColorMethod;
        var lights = LightsParent.GetComponentsInChildren<Light>(includeInactive: true);
        foreach (var light in lights)
        {
            if (ActiveLights.Contains(light)) {light.gameObject.SetActive(true);}
            else {light.gameObject.SetActive(false);}
        }

        activeVolume.profile = ActiveVolumeProfile;


    }
}