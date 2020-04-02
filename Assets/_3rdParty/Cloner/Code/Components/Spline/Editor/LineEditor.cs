using UnityEngine;
using UnityEditor;

namespace Cloner
{
	[CustomEditor (typeof (Line))]
	public class LineEditor : Editor
	{
		private void OnSceneGUI ()
		{
			var line = target as Line;

			var transform = line.transform;
			var rotation = (Tools.pivotRotation == PivotRotation.Local) ? transform.rotation : Quaternion.identity;

			var p0 = transform.TransformPoint (line.p0);
			var p1 = transform.TransformPoint (line.p1);

			Handles.color = Color.white;
			Handles.DrawLine (p0, p1);

			EditorGUI.BeginChangeCheck ();
			p0 = Handles.DoPositionHandle (p0, rotation);
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (line, "Move Point");
				EditorUtility.SetDirty (line);
				line.p0 = transform.InverseTransformPoint (p0);
			}

			EditorGUI.BeginChangeCheck ();
			p1 = Handles.DoPositionHandle (p1, rotation);
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (line, "Move Point");
				EditorUtility.SetDirty (line);
				line.p1 = transform.InverseTransformPoint (p1);
			}
		}
	}
}