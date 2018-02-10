using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PolyPresetManager))]
public class PolyPresetEditor : CustomEditorBase {
    
    PolyPresetManager _presetManager;
    PolyComponent _poly;

    protected override void OnEnable() {
        _presetManager = (PolyPresetManager) target;
        _poly = GameObject.Find("Polyhedron").GetComponent<PolyComponent>();
        base.OnEnable();
    }
    
    public override void  OnInspectorGUI () {
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        if(GUILayout.Button("Save")) {
            _presetManager.AddPreset("test" + _presetManager.PolyPresets.Count, _poly);
        }
    }
    
}