using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class AppearancePresets : MonoBehaviour {
	
	public PolyHydra _poly;
	public Camera CurrentCamera;
	public GameObject PropsParent;
	public Volume ActiveVolume;
	public Transform LightingPrefab;
	public List<AppearancePreset> Items;
	public int editorPresetIndex;

	private PolyhydraSceneSetup _setup;

	private const string PresetFileNamePrefix = "AppearancePreset-";
	
	public AppearancePreset ApplyPresetToPoly(string presetName)
	{
		AppearancePreset preset = null;
		if (presetName != null)
		{
			int index = Items.FindIndex(x => x.Name == presetName);
			if (index >= 0)
			{
				preset = Items[index];
				ApplyPresetToPoly(preset);
			}
		}
		return preset;
	}
	
	public AppearancePreset ApplyPresetToPoly(int presetIndex)
	{
		var preset = Items[presetIndex];
		ApplyPresetToPoly(preset);
		return preset;
	}

	public void ApplyPresetToPoly(AppearancePreset preset)
	{
		if (_setup == null)
		{
			_setup = FindObjectOfType<PolyhydraSceneSetup>();
		}

		_poly.APresetName = preset.Name;
		
		if (LightingPrefab != null)
		{
			if (Application.isPlaying)
			{
				Destroy(LightingPrefab.gameObject);
			}
			else
			{
				DestroyImmediate(LightingPrefab.gameObject);
			}
		}
        
		if (_setup.RenderingPipeline == PolyhydraSceneSetup.RenderingPipelines.HDRP)
		{
			_poly.gameObject.GetComponent<MeshRenderer>().material = preset.PolyhedronMaterialHDRP;
			if (preset.LightingPrefabHDRP != null) LightingPrefab = Instantiate(preset.LightingPrefabHDRP);
			ActiveVolume.profile = preset.ActiveVolumeProfileHDRP;
			var hdCamData = CurrentCamera.gameObject.GetComponent<HDAdditionalCameraData>();
			hdCamData.clearColorMode = preset.CameraClearColorMode;
			hdCamData.backgroundColorHDR = preset.CameraBackgroundColor;
		}
		else
		{
			_poly.gameObject.GetComponent<MeshRenderer>().material = preset.PolyhedronMaterialURP;
			if (preset.LightingPrefabURP != null) LightingPrefab = Instantiate(preset.LightingPrefabURP);
			ActiveVolume.profile = preset.ActiveVolumeProfileURP;
			CurrentCamera.clearFlags = ConvertClearFlags(preset.CameraClearColorMode);
			CurrentCamera.backgroundColor = preset.CameraBackgroundColor;
			RenderSettings.skybox = preset.SkyBoxURP;
		}
		_poly.ColorMethod = preset.PolyhedronColorMethod;


		var props = PropsParent.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (var prop in props)
		{
			if (prop.gameObject == PropsParent.gameObject) continue;
			if (preset.ActiveProps.Contains(prop.gameObject)) {prop.gameObject.SetActive(true);}
			else {prop.gameObject.SetActive(false);}
		}
	}
	
	[ContextMenu("Current preset")]
	public void ApplyPresetAtRuntime()
	{
		ApplyPresetToPoly(editorPresetIndex);
	}
	
	[ContextMenu("Next preset")]
	public void CyclePresetAtRuntime()
	{
		ApplyPresetToPoly(editorPresetIndex);
		editorPresetIndex++;
		editorPresetIndex %= Items.Count;
	}
	
	private CameraClearFlags ConvertClearFlags(HDAdditionalCameraData.ClearColorMode cameraClearColorMode)
    {
        switch (cameraClearColorMode)
        {
            case HDAdditionalCameraData.ClearColorMode.Color:
                return CameraClearFlags.Color;
            case HDAdditionalCameraData.ClearColorMode.None:
                return CameraClearFlags.Nothing;
            case HDAdditionalCameraData.ClearColorMode.Sky:
                return CameraClearFlags.Skybox;
            default:
                return CameraClearFlags.Nothing;
        }
    }
}
