using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UnityEditor.UI
{
	[CustomEditor(typeof(UIAccordionElement), true)]
	public class UIAccordionElementEditor : ToggleEditor {
	
		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_MinHeight"));
			this.serializedObject.ApplyModifiedProperties();
			
			base.serializedObject.Update();
			EditorGUILayout.PropertyField(base.serializedObject.FindProperty("m_IsOn"));
			EditorGUILayout.PropertyField(base.serializedObject.FindProperty("m_Interactable"));
            EditorGUILayout.PropertyField(base.serializedObject.FindProperty("graphic"));
            base.serializedObject.ApplyModifiedProperties();
		}
	}
}