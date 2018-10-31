using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PolyPresets))]
public class PolyPresetEditor : CustomEditorBase {
    
    PolyPresets _presets;
    PolyComponent _poly;

    protected override void OnEnable() {
        _presets = (PolyPresets) target;
        _poly = GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
        base.OnEnable();
    }
    
    public override void  OnInspectorGUI ()
    {
        string newPresetName = "New Preset " + _presets.Items.Count;
        
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        GUILayout.TextField(newPresetName);
        if(GUILayout.Button("Save")) {
            _presets.AddPresetFromPoly(newPresetName);
        }
    }
    
}