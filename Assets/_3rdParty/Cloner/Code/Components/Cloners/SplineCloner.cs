using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class SplineCloner : Cloner
	{
		public int count;
		public bool align = true;
		public BezierSpline spline;

		protected override int PointCount { get { return count; } }

		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			if (spline == null)
				return;

			var stepSize = 1f / count;
			for (int i = 0; i < points.Count; i++)
			{
				var t = i * stepSize;
				points[i] = Matrix4x4.TRS (spline.GetPoint (t), align ? Quaternion.LookRotation (spline.GetDirection (t)) : Quaternion.identity, Vector3.one);
			}
		}
	}
}