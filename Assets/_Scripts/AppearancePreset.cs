using System;
using System.Collections.Generic;
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
    public Transform LightingPrefabHDRP;
    public Transform LightingPrefabURP;
    public VolumeProfile ActiveVolumeProfileHDRP;
    public VolumeProfile ActiveVolumeProfileURP;
    public HDAdditionalCameraData.ClearColorMode CameraClearColorMode;
    public Color CameraBackgroundColor;
    public Material SkyBoxURP;
}