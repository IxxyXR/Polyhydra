using UnityEngine;
using System.Collections;
using UnityEditor;


[CustomEditor(typeof(PolyComponent))]
public class PolyComponentDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = EditorApplication.isPlaying;
        PolyComponent ui = (PolyComponent)target;
        if(GUILayout.Button("Sync UI to Inspector"))
        {
            ui.polyUI.InitUI();
        }

        GUI.enabled = true;
        DrawDefaultInspector();
    }
}
