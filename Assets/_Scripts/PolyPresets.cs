using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class PolyPresets : MonoBehaviour {
	
	public PolyComponent _poly;
	private const string PresetFileNamePrefix = "PolyPreset-";
	
	public List<PolyPreset> Items;

	public PolyPreset ApplyPresetToPoly(int presetIndex)
	{
		var preset = Items[presetIndex];
		ApplyPresetToPoly(preset);
		return preset;
	}

	public void ApplyPresetToPoly(PolyPreset preset)
	{
		preset.ApplyToPoly(ref _poly);
	}

	public void AddPresetFromPoly(string presetName)
	{
		var existingPreset = Items.Find(x => x.Name.Equals(presetName));
		Items.Remove(existingPreset);
		var preset = new PolyPreset();
		preset.CreateFromPoly(presetName, _poly);
		Items.Add(preset);
	}

	public void AddPresetsFromPath(string path)
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
			if (!existingPresets.Contains(preset.Name))
			{
				Items.Add(preset);						
			}
		}

	}
	
	public void LoadAllPresets()
	{
		Items.Clear();
		AddPresetsFromPath(Application.persistentDataPath);
		AddPresetsFromPath(Path.Combine(Application.streamingAssetsPath, "InitialPresets"));
		SaveAllPresets();
	}
	
	public void SaveAllPresets()
	{
		foreach (var preset in Items) {
			var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + preset.Name + ".json");
			var polyJson = JsonConvert.SerializeObject(preset);
			File.WriteAllText(fileName, polyJson);
		}
	}

}
