using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class AppearancePresets : MonoBehaviour {
	
	public PolyHydra _poly;
	public Camera CurrentCamera;
	public GameObject LightsParent;
	public GameObject PropsParent;
	public Volume Volume;
	public List<AppearancePreset> Items;
	public int editorPresetIndex;

	private PolyhydraSceneSetup _setup;

	private const string PresetFileNamePrefix = "AppearancePreset-";
	
	public AppearancePreset ApplyPresetToPoly(string presetName)
	{
		AppearancePreset preset;
		if (presetName != null)
		{
			preset = Items[Items.FindIndex(x => x.Name == presetName)];
			ApplyPresetToPoly(preset);
		}
		else
		{
			preset = null;
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
		preset.ApplyToPoly(ref _poly, LightsParent, PropsParent, Volume, CurrentCamera, _setup.RenderingPipeline);
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
}
