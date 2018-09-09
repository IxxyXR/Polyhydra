using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PolyPresets : MonoBehaviour {
	
	public PolyComponent _poly;
	private const string PresetFileNamePrefix = "PolyPreset-";
	
	public List<PolyPreset> Items;
	
	public void Start () {
		Items = new List<PolyPreset>();
		LoadAllPresets();
	}

	public void ApplyPresetToPoly(string presetName)
	{
		ApplyPresetToPoly(Items.Find(x => x.Name.Equals(presetName)));
	}

	public void ApplyPresetToPoly(PolyPreset preset)
	{
		preset.ApplyToPoly(ref _poly);
		RebuildPoly();
	}

	public void RebuildPoly()
	{
		_poly.MakePolyhedron();
	}
	
	public void AddPresetFromPoly(string presetName)
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly(presetName, _poly);
		Items.Add(preset);
	}
	
	public void LoadAllPresets()
	{
		Items.Clear();
		var info = new DirectoryInfo(Application.persistentDataPath);
		var fileInfo = info.GetFiles();
		foreach (var file in fileInfo) {
			if (file.Name.StartsWith(PresetFileNamePrefix)) {
				var preset = new PolyPreset();
				JsonUtility.FromJsonOverwrite(File.ReadAllText(file.FullName), preset);
				if (string.IsNullOrEmpty(preset.Name)) {
					preset.Name = file.Name.Replace(PresetFileNamePrefix, "").Replace(".json", "");
				}
				Items.Add(preset);
				Debug.Log(preset.Name);
			}
		}
	}
	
	public void SaveAllPresets()
	{
		foreach (var preset in Items) {
			var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + preset.Name + ".json");
			var polyJson = JsonUtility.ToJson(preset);
			File.WriteAllText(fileName, polyJson);
		}
	}

	public void ResetInitialPresets()
	{
		Items.Clear();
		var initialPresetFile = Resources.Load("initialPresets") as TextAsset;
		if (initialPresetFile != null) {
			var polys = JsonHelper.FromJson<string>(initialPresetFile.text);
			for (var index = 0; index < polys.Length; index++) {
				var preset = new PolyPreset();
				JsonUtility.FromJsonOverwrite(polys[index], preset);
				Items.Add(preset);
			}
		} else {
			Debug.Log("No initial presets found");
		}
	}

}
