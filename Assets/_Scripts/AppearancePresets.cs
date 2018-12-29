using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class AppearancePresets : MonoBehaviour {
	
	public PolyHydra _poly;
	public GameObject LightsParent;
	public List<AppearancePreset> Items;
	
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
		preset.ApplyToPoly(ref _poly, LightsParent);
	}



}
