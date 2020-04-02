using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class LinearCloner : Cloner
	{
		public int count;
		public float padding = 0f;
		public bool useBoundsInPadding = true;
		public float offset;

		private Vector3 spacing;

		protected override int PointCount { get { return count; } }

		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			spacing = Vector3.forward * (((useBoundsInPadding) ? mesh.bounds.size.z : 0f) + padding);
			for (int i = 0; i < points.Count; i++)
				points[i] = Matrix4x4.TRS (transform.position + transform.rotation * (spacing * i + Vector3.forward * offset), transform.rotation, transform.localScale);
		}
	}
}