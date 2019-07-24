﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AppearancePresets : MonoBehaviour {
	
	public PolyHydra _poly;
	public Camera CurrentCamera;
	public GameObject LightsParent;
	public GameObject VolumesParent;
	public List<AppearancePreset> Items;
	public int editorPresetIndex;
	
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
		preset.ApplyToPoly(ref _poly, LightsParent, VolumesParent, CurrentCamera);
	}

	[ContextMenu("Next preset")]
	public void CyclePresetAtRuntime()
	{
		ApplyPresetToPoly(editorPresetIndex);
		editorPresetIndex++;
		editorPresetIndex %= Items.Count;
	}
	
}
