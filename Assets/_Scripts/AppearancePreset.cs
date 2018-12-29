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
    public Material SkyboxMaterial;
    public Color32 SkyboxColor;
    public Cubemap ReflectionCubemap;

    public void ApplyToPoly(ref PolyHydra poly, GameObject LightsParent)
    {
        poly.gameObject.GetComponent<MeshRenderer>().material = PolyhedronMaterial;
        poly.ColorMethod = PolyhedronColorMethod;
        poly.APresetName = Name;
        if (SkyboxMaterial != null)
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.skybox = SkyboxMaterial;
        }
        else
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.skybox = null;
        }

        RenderSettings.ambientSkyColor = SkyboxColor;
        if (ReflectionCubemap != null)
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflection = ReflectionCubemap;
        }
        else
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.customReflection = null;
        }
        RenderSettings.skybox = SkyboxMaterial;
        var lights = LightsParent.GetComponentsInChildren<Light>(includeInactive: true);
        foreach (var light in lights)
        {
            if (ActiveLights.Contains(light)) {light.gameObject.SetActive(true);}
            else {light.gameObject.SetActive(false);}
        }
        
        

    }
}