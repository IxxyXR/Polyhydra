using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class PolyPresets : MonoBehaviour {
	
	public PolyHydra _poly;
	private const string PresetFileNamePrefix = "PolyPreset-";
	public AppearancePresets APresets; 
	public List<PolyPreset> Items;

	public PolyPreset ApplyPresetToPoly(int presetIndex)
	{
		var preset = Items[presetIndex];
		ApplyPresetToPoly(preset);
		return preset;
	}

	public void ApplyPresetToPoly(PolyPreset preset)
	{
		preset.ApplyToPoly(_poly, APresets);
	}

	public void AddPresetFromPoly(string presetName)
	{
		var existingPreset = Items.Find(x => x.Name.Equals(presetName));
		Items.Remove(existingPreset);
		var preset = new PolyPreset();
		preset.CreateFromPoly(presetName, _poly);
		Items.Add(preset);
	}

	[ContextMenu("Copy to clipboard")]
	public void CopyPresetToClipboard()
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly("Temp", _poly);
		var polyJson = JsonConvert.SerializeObject(preset, Formatting.Indented);
		GUIUtility.systemCopyBuffer = polyJson;
	}

	[ContextMenu("Paste from clipboard")]
	public void AddPresetFromClipboard()
	{
		name = GenerateUniquePresetName();
		AddPresetFromString(name, GUIUtility.systemCopyBuffer);
	}

	private string GenerateUniquePresetName()
	{
		var existingPresets = Items.Select(x => x.Name);
		int index = existingPresets.Count();
		name = $"New Preset {index}";
		while (existingPresets.Contains(name))
		{
			index++;
			name = $"New Preset {index}";
		}
		return name;
	}

	public void AddPresetFromString(string name, string data)
	{
		var preset = new PolyPreset();
		preset.Name = name;
		preset = JsonConvert.DeserializeObject<PolyPreset>(data);
		Items.Add(preset);
	}

	public void AddPresetsFromPath(string path, bool overwrite)
	{
		var existingPresets = Items.Select(x => x.Name);
		var dirInfo = new DirectoryInfo(path);
		var fileInfo = dirInfo.GetFiles(PresetFileNamePrefix + "*.json");
		foreach (var file in fileInfo) {
			var preset = new PolyPreset();
			preset = JsonConvert.DeserializeObject<PolyPreset>(File.ReadAllText(file.FullName));
			if (string.IsNullOrEmpty(preset.Name))
			{
				preset.Name = file.Name.Replace(PresetFileNamePrefix, "").Replace(".json", "");
			}
			if (!existingPresets.Contains(preset.Name) || overwrite)
			{
				Items.Add(preset);
			}
		}
	}

	public void AddPresetsFromResources()
	{
		var existingPresets = Items.Select(x => x.Name);
		var initialPresets = Resources.LoadAll("InitialPresets", typeof(TextAsset));
		foreach (var presetResource in initialPresets) {
			var preset = new PolyPreset();
			preset = JsonConvert.DeserializeObject<PolyPreset>(presetResource.ToString());
			if (string.IsNullOrEmpty(preset.Name))
			{
				preset.Name = presetResource.name.Replace(PresetFileNamePrefix, "").Replace(".json", "");
			}
			if (!existingPresets.Contains(preset.Name))
			{
				Items.Add(preset);
			}
		}
	}
	
	public void LoadAllPresets()
	{
		Items.Clear();
		AddPresetsFromPath(Application.persistentDataPath, overwrite: false);
		AddPresetsFromResources();
		SaveAllPresets();
	}
	
	public void SaveAllPresets()
	{
		foreach (var preset in Items) {
			var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + preset.Name + ".json");
			var polyJson = JsonConvert.SerializeObject(preset, Formatting.Indented);
			File.WriteAllText(fileName, polyJson);
		}
	}

	public void ResetPresets()
	{
		Items.Clear();
		AddPresetsFromResources();
		AddPresetsFromPath(Application.persistentDataPath, overwrite:true);
		SaveAllPresets();
	}

}
