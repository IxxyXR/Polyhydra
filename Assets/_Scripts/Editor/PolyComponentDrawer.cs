using UnityEngine;
using System.Collections;
using UnityEditor;


[CustomEditor(typeof(PolyHydra))]
public class PolyComponentDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.enabled = EditorApplication.isPlaying;
        PolyHydra ui = (PolyHydra)target;
        if(GUILayout.Button("Sync UI to Inspector"))
        {
            ui.polyUI.InitPolySpecificUI();
        }

        GUI.enabled = true;
        DrawDefaultInspector();
    }
}
