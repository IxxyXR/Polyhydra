using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class PolyPresets : MonoBehaviour {
	
	private PolyComponent _poly;
	private const string PresetFileNamePrefix = "PolyPreset-";
	private const string PresetFileName = "PolyPresets";
	private bool SINGLE_PRESET_PER_FILE = true;  // Until I solve encoding lists as JSON...
	
	public List<PolyPreset> Items = new List<PolyPreset>();
	
	public void Start () {
		Items = new List<PolyPreset>();
		_poly = GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
	}

	public void RebuildPoly() {
		_poly.MakePolyhedron();
	}
	
	public void AddPreset(string presetName) {
		_poly.preset.Name = presetName;
		Items.Add(_poly.preset);
		SavePresetsToDisk();
	}
	
	public void LoadPreset(string presetName) {
		var preset = Items.Find(x => x.Name.Equals(presetName));
		// TODO must be a better way to overwrite a component instance
		_poly.preset = preset;
		RebuildPoly();
	}
	

	public void LoadPresetsFromDisk() {
		Debug.Log("LoadPresetsFromDisk");
		Items.Clear();
		if (SINGLE_PRESET_PER_FILE) {
			var info = new DirectoryInfo(Application.persistentDataPath);
			var fileInfo = info.GetFiles();
			foreach (var file in fileInfo) {
				if (file.Name.StartsWith(PresetFileNamePrefix)) {
					Debug.Log(file.Name);
					var preset = new PolyPreset();
					JsonUtility.FromJsonOverwrite(File.ReadAllText(file.FullName), preset);
					if (string.IsNullOrEmpty(preset.Name)) {
						preset.Name = file.Name.Replace(PresetFileNamePrefix, "").Replace(".json", "");
					}
					Items.Add(preset);
					Debug.Log(Items.Count);
				}
			}
		} else {
			var path = Path.Combine(Application.persistentDataPath, PresetFileName + ".json");
			JsonUtility.FromJsonOverwrite(File.ReadAllText(path), Items);
		}
	}

	public void SavePresetsToDisk() {
		Debug.Log("SavePresetsToDisk");
		if (SINGLE_PRESET_PER_FILE) {
			foreach (var preset in Items) {
				var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + preset.Name + ".json");
				var polyJson = JsonUtility.ToJson(preset);
				File.WriteAllText(fileName, polyJson);
			}
		} else {
			var fileName = Path.Combine(Application.persistentDataPath, PresetFileName + ".json");
			var polyJson = JsonUtility.ToJson(Items);
			File.WriteAllText(fileName, polyJson);
		}
	}
	
	public void CreateInitialPresets() {
		Debug.Log("CreateInitialPresets");
		Items.Clear();
		var initialPresetFile = Resources.Load("initialPresets") as TextAsset;
		if (SINGLE_PRESET_PER_FILE) {
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
		} else {
			JsonUtility.FromJsonOverwrite(initialPresetFile.text, Items);
		}
	}
}
