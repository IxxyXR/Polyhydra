using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class ScaleModifier : PointModifier
	{
		public Vector3 scale = Vector3.one;
		public float uniform = 1f;

		public override List<Matrix4x4> Modify (List<Matrix4x4> points)
		{
			var s = scale * uniform;
			for (int i = 0; i < points.Count; i++)
			{
				points[i] *= Matrix4x4.Scale (s);
				// points[i] *= Matrix4x4.Scale (s * points[i].GetColumn(3).x);
			}

			return points;
		}
	}
}