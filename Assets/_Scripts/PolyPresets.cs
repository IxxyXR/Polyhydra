﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using UnityEngine;
using UnityEngine.Networking;


public class PolyPresets : MonoBehaviour {
	
	public PolyHydra _poly;
	public AppearancePresets APresets;
	public List<PolyPreset> Items;


	void Awake()
	{
		AotHelper.EnsureList<PolyPreset.Op>();
	}
	
	public PolyPreset ApplyPresetToPoly(int presetIndex, bool loadMatchingAppearance)
	{
		var preset = Items[presetIndex];
		ApplyPresetToPoly(preset, loadMatchingAppearance);
		return preset;
	}

	public void ApplyPresetToPoly(PolyPreset preset, bool loadMatchingAppearance)
	{
		preset.ApplyToPoly(_poly, APresets, loadMatchingAppearance);
	}

	public PolyPreset AddOrUpdateFromPoly(string presetName)
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly(presetName, _poly);

		for (var i = 0; i < Items.Count; i++)
		{
			var item = Items[i];
			if (item.Name == presetName)
			{
				Items[i] = preset;
				return Items[i];
			}
		}

		Items.Add(preset);
		return Items.Last();
	}

	[ContextMenu("Copy from poly")]
	public void CopyFromPoly()
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly("Temp", _poly);
		var polyJson = JsonConvert.SerializeObject(preset, Formatting.Indented);
		GUIUtility.systemCopyBuffer = polyJson;
	}

	[ContextMenu("Paste to poly")]
	public void PasteToPoly()
	{
		var preset = new PolyPreset();
		preset = JsonConvert.DeserializeObject<PolyPreset>(GUIUtility.systemCopyBuffer);
		preset.ApplyToPoly(_poly, APresets, false);
		_poly.Rebuild();
	}

	[ContextMenu("Create preset from clipboard")]
	public void CreatePresetFromClipboard()
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
		var fileInfo = dirInfo.GetFiles(PolyPreset.PresetFileNamePrefix + "*.json");
		foreach (var file in fileInfo)
		{
			string rawJson = File.ReadAllText(file.FullName);
			// Legacy Fixes
			rawJson = rawJson
			// Grid is no longer a uniform polytype. Set it to any valid value (Cube)
			.Replace(
				"PolyType\": \"Grid\"",
				"PolyType\": \"Cube\""
			)
			// We renamed prisms
			.Replace(
				"PolyType\": \"Penta",
				"PolyType\": \"Poly"
			)
			.Replace(
				"GridShape\": \"Cube",
				"GridShape\": \"Plane"
			)
			.Replace(
				"JohnsonPolyType\": \"ElongatedBicupola",
				"JohnsonPolyType\": \"ElongatedGyroBicupola"
			)
			.Replace(
				"OtherPolyType\": \"L1",
				"OtherPolyType\": \"L_Shape"
			)
			.Replace(
				"OtherPolyType\": \"L2",
				"OtherPolyType\": \"L_Alt_Shape"
			)
			.Replace(
				"FaceSelections\": \"Alternate",
				"FaceSelections\": \"Even"
			)
			.Replace(
				"JohnsonPolyType\": \"Bicupola",
				"JohnsonPolyType\": \"GyroBicupola"
			);
			PolyPreset preset = null;
			try
			{
				preset = JsonConvert.DeserializeObject<PolyPreset>(rawJson);
				Debug.Log($"{preset.Name}: {preset.Ops.Where(x=>!x.Disabled).Count()} ops");
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to load preset {file.FullName}");
			}

			if (preset == null) continue;
			if (string.IsNullOrEmpty(preset.Name))
			{
				preset.Name = file.Name.Replace(PolyPreset.PresetFileNamePrefix, "").Replace(".json", "");
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
			Debug.Log($"{preset.Name}: {preset.Ops.Where(x=>!x.Disabled).Count()} ops");
			if (string.IsNullOrEmpty(preset.Name))
			{
				preset.Name = presetResource.name.Replace(PolyPreset.PresetFileNamePrefix, "").Replace(".json", "");
			}
			if (!existingPresets.Contains(preset.Name))
			{
				Items.Add(preset);
			}
		}
	}
	
	[ContextMenu("Load All Presets")]
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
			preset.Save();
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
