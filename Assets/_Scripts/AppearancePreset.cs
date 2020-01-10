using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

[Serializable]
public class AppearancePreset
{
    public string Name;
    [FormerlySerializedAs("PolyhedronMaterial")] public Material PolyhedronMaterialHDRP;
    public Material PolyhedronMaterialURP;
    public PolyHydra.ColorMethods PolyhedronColorMethod;
    public List<GameObject> ActiveProps;
    public List<Light> ActiveLights;
    [FormerlySerializedAs("ActiveVolumeProfile")] public VolumeProfile ActiveVolumeProfileHDRP;
    public VolumeProfile ActiveVolumeProfileURP;
    public HDAdditionalCameraData.ClearColorMode CameraClearColorMode;
    public Color CameraBackgroundColor;

    public void ApplyToPoly(ref PolyHydra poly, GameObject LightsParent, GameObject PropsParent, Volume activeVolume, Camera CurrentCamera, PolyhydraSceneSetup.RenderingPipelines pipeline)
    {
        
        var hdCamData = CurrentCamera.gameObject.GetComponent<HDAdditionalCameraData>();
        
        poly.APresetName = Name;
        if (pipeline == PolyhydraSceneSetup.RenderingPipelines.HDRP)
        {
            poly.gameObject.GetComponent<MeshRenderer>().material = PolyhedronMaterialHDRP;
            activeVolume.profile = ActiveVolumeProfileHDRP;
        }
        else
        {
            poly.gameObject.GetComponent<MeshRenderer>().material = PolyhedronMaterialURP;
            activeVolume.profile = ActiveVolumeProfileURP;
        }
        poly.ColorMethod = PolyhedronColorMethod;
        hdCamData.clearColorMode = CameraClearColorMode;
        hdCamData.backgroundColorHDR = CameraBackgroundColor;

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