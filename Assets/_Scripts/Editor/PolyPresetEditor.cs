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
    
    public override void  OnInspectorGUI () {
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        if(GUILayout.Button("Save")) {
            _presets.AddPreset("test" + _presets.Items.Count);
        }
    }
    
}