using UnityEngine;
using UnityEditor;

namespace Cloner
{
	[CustomEditor (typeof (BezierCurve))]
	public class BezierCurveEditor : Editor
	{
		private const int DIRECTION_STEPS = 10;
		private const float DIRECTION_SCALE = 0.5f;
		private const float CURVE_WIDTH = 2f;

		private BezierCurve curve;
		private Transform handleTransform;
		private Quaternion handleRotation;

		private void OnSceneGUI ()
		{
			curve = target as BezierCurve;
			handleTransform = curve.transform;
			handleRotation = (Tools.pivotRotation == PivotRotation.Local) ? handleTransform.rotation : Quaternion.identity;

			var p0 = ShowPoint (0);
			var p1 = ShowPoint (1);
			var p2 = ShowPoint (2);
			var p3 = ShowPoint (3);

			Handles.color = Color.gray;
			Handles.DrawLine (p0, p1);
			Handles.DrawLine (p2, p3);

			Handles.DrawBezier (p0, p3, p1, p2, Color.white, null, CURVE_WIDTH);
			ShowDirections ();
		}

		private Vector3 ShowPoint (int index)
		{
			var point = handleTransform.TransformPoint (curve.points[index]);

			EditorGUI.BeginChangeCheck ();
			point = Handles.DoPositionHandle (point, handleRotation);
			if (EditorGUI.EndChangeCheck ())
			{
				Undo.RecordObject (curve, "Move Point");
				EditorUtility.SetDirty (curve);
				curve.points[index] = point;
			}

			return point;
		}

		private void ShowDirections ()
		{
			Handles.color = Color.green;
			var point = curve.GetPoint (0f);
			Handles.DrawLine (point, (point + curve.GetDirection (0f)) * DIRECTION_SCALE);
			for (int i = 1; i <= DIRECTION_STEPS; i++)
			{
				point = curve.GetPoint (i / (float)DIRECTION_STEPS);
				Handles.DrawLine (point, point + curve.GetDirection (i / (float)DIRECTION_STEPS) * DIRECTION_SCALE);
			}
		}
	}
}