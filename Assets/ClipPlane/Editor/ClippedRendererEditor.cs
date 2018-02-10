using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClippedRenderer))]
public class ClippedRendererEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();

        ClippedRenderer cr = (ClippedRenderer)target;

        EditorGUI.BeginChangeCheck();
        cr.material = (Material)EditorGUILayout.ObjectField("Material", cr.material, typeof(Material), false);
        cr.clipMaterial = (Material)EditorGUILayout.ObjectField("Clip Cap Material", serializedObject.FindProperty("_clipMaterial").objectReferenceValue, typeof(Material), false);

        EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
        cr.shareMaterialProperties = EditorGUILayout.Toggle(new GUIContent("Share Properties", "Whether the script should set the properties on the original material"), cr.shareMaterialProperties);
        cr.useWorldSpace = EditorGUILayout.Toggle(new GUIContent("Use World Space", "Draw the clip plane in world space"), cr.useWorldSpace);

        EditorGUILayout.LabelField("Plane", EditorStyles.boldLabel);
        cr.planeNormal = EditorGUILayout.Vector3Field("Normal", cr.planeNormal);
        cr.planePoint = EditorGUILayout.Vector3Field("Point", cr.planePoint);
        cr.planeVector = EditorGUILayout.Vector4Field("Vector", cr.planeVector);
        
        if (EditorGUI.EndChangeCheck()) RepaintAll();
    }
    
    void RepaintAll() {
        // Repaint all views, including the game and scene editor view
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
