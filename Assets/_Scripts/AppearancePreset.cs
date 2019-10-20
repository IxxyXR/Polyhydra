using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public class AppearancePreset
{
    public string Name;
    public Material PolyhedronMaterial;
    public PolyHydra.ColorMethods PolyhedronColorMethod;
    public List<GameObject> ActiveProps;
    public List<Light> ActiveLights;
    public VolumeProfile ActiveVolumeProfile;
    public HDAdditionalCameraData.ClearColorMode CameraClearColorMode;
    public Color CameraBackgroundColor;

    public void ApplyToPoly(ref PolyHydra poly, GameObject LightsParent, GameObject PropsParent, Volume activeVolume, Camera CurrentCamera)
    {
        
        var hdCamData = CurrentCamera.gameObject.GetComponent<HDAdditionalCameraData>();
        
        poly.APresetName = Name;
        poly.gameObject.GetComponent<MeshRenderer>().material = PolyhedronMaterial;
        poly.ColorMethod = PolyhedronColorMethod;
        hdCamData.clearColorMode = CameraClearColorMode;
        hdCamData.backgroundColorHDR = CameraBackgroundColor;
        activeVolume.profile = ActiveVolumeProfile;

        var lights = LightsParent.GetComponentsInChildren<Light>(includeInactive: true);
        foreach (var light in lights)
        {
            if (ActiveLights.Contains(light)) {light.gameObject.SetActive(true);}
            else {light.gameObject.SetActive(false);}
        }

        var props = PropsParent.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var prop in props)
        {
            if (ActiveProps.Contains(prop.gameObject)) {prop.gameObject.SetActive(true);}
            else {prop.gameObject.SetActive(false);}
        }

    }
}