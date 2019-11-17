using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PolyPresets))]
public class PolyPresetEditor : CustomEditorBase {
    
    PolyPresets _presets;

    protected override void OnEnable() {
        _presets = (PolyPresets) target;
        base.OnEnable();
    }
    
    public override void  OnInspectorGUI ()
    {
        string newPresetName = "New Preset " + _presets.Items.Count;
        
        base.OnInspectorGUI();
        GUILayout.TextField(newPresetName);
        if(GUILayout.Button("Save")) {
            _presets.AddOrUpdateFromPoly(newPresetName);
        }
    }
    
}