using System;
using System.Collections.Generic;
using UnityEngine;

public class PolyPresetManager : MonoBehaviour {
	
	private PolyComponent poly;

	[Serializable]
	public struct PolyPreset {
		public string name;
		public string data;
	}

	public List<PolyPreset> PolyPresets;
	
	public void Start () {
		PolyPresets = new List<PolyPreset>();
		poly = GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
	}

	[ContextMenu("Test Save")]
	public void TestSave() {
		AddPreset("test" + PolyPresets.Count, poly);
	}
	
	[ContextMenu("Test Load")]
	public void TestLoad() {
		var poly =  GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
		LoadPreset("test1", poly);
		poly.MakePolyhedron();
	}
	
	public void AddPreset(string name, PolyComponent poly) {
		var preset = new PolyPreset();
		preset.name = name;
		preset.data = JsonUtility.ToJson(poly);
		PolyPresets.Add(preset);
	}
	
	public void LoadPreset(string name, PolyComponent poly) {
		var preset = PolyPresets.Find(x => x.name.Equals(name));
		JsonUtility.FromJsonOverwrite(preset.data, poly);
	}
    
	public void ChangeBypassConway(bool val) {
		poly.BypassConway = val;
		poly.MakePolyhedron();
	}
}
