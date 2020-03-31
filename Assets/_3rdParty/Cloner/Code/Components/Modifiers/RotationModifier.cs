using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class RotationModifier : PointModifier
	{
		public Vector3 rotation;

		public override List<Matrix4x4> Modify (List<Matrix4x4> points)
		{
			var quaternion = Quaternion.Euler (rotation);

			for (int i = 0; i < points.Count; i++)
				points[i] *= Matrix4x4.Rotate (quaternion);

			return points;
		}
	}
}