using UnityEngine;
using UnityEditor;

namespace Cloner
{
	[CustomEditor (typeof (BezierSpline))]
	public class BezierSplineEditor : Editor
	{
		private const int DIRECTION_STEPS = 10;
		private const float DIRECTION_SCALE = 0.5f;
		private const float CURVE_WIDTH = 2f;
		private const float HANDLE_SCALE = 0.04f;
		private const float HANDLE_PICK_SCALE = 0.06f;

		private BezierSpline spline;
		private Transform handleTransform;
		private Quaternion handleRotation;
		private int selectedIndex;

		public override void OnInspectorGUI ()
		{
			spline = target as BezierSpline;

			EditorGUI.BeginChangeCheck ();
			bool loop = EditorGUILayout.Toggle ("Loop", spline.Loop);
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (spline, "Toggle Loop");
				EditorUtility.SetDirty (spline);
				spline.Loop = loop;
			}

			if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
				DrawSelectedPointInspector ();

			if (GUILayout.Button ("Add Curve"))
			{
				Undo.RecordObject (spline, "Add Curve");
				spline.AddCurve ();
				EditorUtility.SetDirty (spline);
			}
		}

		private void OnSceneGUI ()
		{
			spline = target as BezierSpline;
			handleTransform = spline.transform;
			handleRotation = (Tools.pivotRotation == PivotRotation.Local) ? handleTransform.rotation : Quaternion.identity;

			var p0 = ShowPoint (0);
			for (int i = 1; i < spline.ControlPointCount; i += 3)
			{
				var p1 = ShowPoint (i);
				var p2 = ShowPoint (i + 1);
				var p3 = ShowPoint (i + 2);

				Handles.color = Color.gray;
				Handles.DrawLine (p0, p1);
				Handles.DrawLine (p2, p3);

				Handles.DrawBezier (p0, p3, p1, p2, Color.white, null, CURVE_WIDTH);

				p0 = p3;
			}

			ShowDirections ();
		}

		private void DrawSelectedPointInspector ()
		{
			GUILayout.Label ("Selected Point");
			EditorGUI.BeginChangeCheck ();
			var point = EditorGUILayout.Vector3Field ("Position", spline.GetControlPoint (selectedIndex));
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (spline, "Move Point");
				EditorUtility.SetDirty (spline);
				spline.SetControlPoint (selectedIndex, point);
			}

			EditorGUI.BeginChangeCheck ();
			BezierControlPointMode mode = (BezierControlPointMode)
			EditorGUILayout.EnumPopup (new GUIContent ("Control Mode", "Default: Mirrored\nAlt: Free\nShift: Aligned"), spline.GetControlPointMode (selectedIndex));
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (spline, "Change Point Mode");
				spline.SetControlPointMode (selectedIndex, mode);
				EditorUtility.SetDirty (spline);
			}
		}

		private Vector3 ShowPoint (int index)
		{
			var point = handleTransform.TransformPoint (spline.GetControlPoint (index));

			Handles.color = Color.white;
			var scale = HandleUtility.GetHandleSize (point);
			if (index == 0)
				scale *= 2f;
			var handleScale = scale * HANDLE_SCALE;
			var handlePickScale = scale * HANDLE_PICK_SCALE;
			if (Handles.Button (point, handleRotation, handleScale, handlePickScale, Handles.DotHandleCap))
			{
				selectedIndex = index;
				Repaint ();
			}

			if (selectedIndex == index)
			{
				EditorGUI.BeginChangeCheck ();
				point = Handles.DoPositionHandle (point, handleRotation);
				if (EditorGUI.EndChangeCheck ())
				{
					Undo.RecordObject (spline, "Move Point");
					EditorUtility.SetDirty (spline);

					var e = Event.current;
					if (e.alt)
						spline.SetControlPointMode (index, BezierControlPointMode.Free);
					else if (e.shift)
						spline.SetControlPointMode (index, BezierControlPointMode.Aligned);
					else
						spline.SetControlPointMode (index, BezierControlPointMode.Mirrored);

					spline.SetControlPoint (index, point);
				}
			}

			return point;
		}

		private void ShowDirections ()
		{
			Handles.color = Color.green;
			var point = spline.GetPoint (0f);
			var steps = DIRECTION_STEPS * spline.CurveCount;
			Handles.DrawLine (point, (point + spline.GetDirection (0f)) * DIRECTION_SCALE);
			for (int i = 1; i <= steps; i++)
			{
				point = spline.GetPoint (i / (float)steps);
				Handles.DrawLine (point, point + spline.GetDirection (i / (float)steps) * DIRECTION_SCALE);
			}
		}
	}
}