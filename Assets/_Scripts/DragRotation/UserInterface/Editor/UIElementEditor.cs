/*************************************************
 * Author: Jeremy Fournier                       *
 *************************************************/
using UnityEditor;
using UnityEngine;
using System.Collections;

namespace DragRotation.UIElement
{
	[CustomEditor ( typeof ( DragRotation.UIElement.UIElement ), true )]
	public class UIElementEditor : UnityEditor.Editor
	{
		#region Variables
		private SerializedProperty sp_bool_OnMouseOver;
		private SerializedProperty sp_event_OnMouseOver;

		private SerializedProperty sp_bool_OnMouseInput;
		private SerializedProperty sp_event_OnMouseInput;

		private bool bool_foldoutOnMouseOver;
		private bool bool_foldoutOnMouseInput;
		#endregion

		#region Initialization
		protected virtual void OnEnable ()
		{
			sp_bool_OnMouseOver = serializedObject.FindProperty ( "bool_OnMouseOver" );
			sp_event_OnMouseOver = serializedObject.FindProperty ( "event_OnMouseOver" );

			sp_bool_OnMouseInput = serializedObject.FindProperty ( "bool_OnMouseInput" );
			sp_event_OnMouseInput = serializedObject.FindProperty ( "event_OnMouseInput" );
		}
		#endregion

		#region Inspector
		public override void OnInspectorGUI ()
		{
			serializedObject.Update ();

			EditorGUILayout.Space ();

			EditorGUILayout.LabelField ( "Input Events", EditorStyles.boldLabel );

			EditorGUILayout.PropertyField ( sp_bool_OnMouseOver, new GUIContent ( "On Mouse Over", "Enable/Disable using events when the mouse Enters/Exits the UIElement" ) );
			if( sp_bool_OnMouseOver.boolValue == true )
			{
				bool_foldoutOnMouseOver = EditorGUILayout.Foldout ( bool_foldoutOnMouseOver, "Boolean Event" );
				if( bool_foldoutOnMouseOver )
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.PropertyField ( sp_event_OnMouseOver, new GUIContent ( "Mouse Over Event", "The event(s) to call when the mouse Enters/Exits UIElement" ) );
					EditorGUILayout.EndHorizontal ();
				}
			}

			EditorGUILayout.PropertyField ( sp_bool_OnMouseInput, new GUIContent ( "Use Input Events", "Enable/Disable using events when the Left Mouse Button is Pressed/Released" ) );
			if( sp_bool_OnMouseInput.boolValue == true )
			{
				bool_foldoutOnMouseInput = EditorGUILayout.Foldout ( bool_foldoutOnMouseInput, "Boolean Event" );
				if( bool_foldoutOnMouseInput )
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.PropertyField ( sp_event_OnMouseInput, new GUIContent ( "Input Event", "The event(s) to call when the mouse is Pressed/Released" ) );
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.Space ();

			serializedObject.ApplyModifiedProperties ();
		}
		#endregion
	}
}