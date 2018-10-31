using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class PolyPresets : MonoBehaviour {
	
	public PolyComponent _poly;
	private const string PresetFileNamePrefix = "PolyPreset-";
	
	public List<PolyPreset> Items;

	public void ApplyPresetToPoly(string presetName)
	{
		ApplyPresetToPoly(Items.Find(x => x.Name.Equals(presetName)));
	}

	public void ApplyPresetToPoly(PolyPreset preset)
	{
		preset.ApplyToPoly(ref _poly);
	}

	public void AddPresetFromPoly(string presetName)
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly(presetName, _poly);
		Items.Add(preset);
	}
	
	public void LoadAllPresets()
	{
		bool presetsExist = false;
		Items.Clear();
		var info = new DirectoryInfo(Application.persistentDataPath);
		var fileInfo = info.GetFiles();
		foreach (var file in fileInfo) {
			if (file.Name.StartsWith(PresetFileNamePrefix))
			{
				presetsExist = true;
				var preset = new PolyPreset();
				preset = JsonConvert.DeserializeObject<PolyPreset>(File.ReadAllText(file.FullName));
				if (string.IsNullOrEmpty(preset.Name)) {
					preset.Name = file.Name.Replace(PresetFileNamePrefix, "").Replace(".json", "");
				}
				Items.Add(preset);
			}
		}

		if (!presetsExist)
		{
			// If we don't have any presets then create the default set.
			ResetInitialPresets();
			SaveAllPresets();
		}
	}
	
	public void SaveAllPresets()
	{
		foreach (var preset in Items) {
			var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + preset.Name + ".json");
			var polyJson = JsonConvert.SerializeObject(preset);
			File.WriteAllText(fileName, polyJson);
		}
	}

	public void ResetInitialPresets()
	{
		Items.Clear();
		var initialPresetFile = Resources.Load("initialPresets") as TextAsset;
		if (initialPresetFile != null)
		{
			var presets = JsonConvert.DeserializeObject<List<PolyPreset>>(initialPresetFile.text);
			foreach (var preset in presets) {
				Items.Add(preset);
			}
		} else {
			Debug.Log("No initial presets found");
		}
	}

}
