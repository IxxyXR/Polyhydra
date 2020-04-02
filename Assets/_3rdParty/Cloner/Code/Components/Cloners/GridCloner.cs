using UnityEngine;
using System.Collections.Generic;

namespace Cloner
{
	public class GridCloner : Cloner
	{
		public Vector3Int count = new Vector3Int (3, 3, 3);
		public Vector3 padding = Vector3.zero;
		public bool useBoundsInPadding = true;

		protected override int PointCount { get { return count.x * count.y * count.z; } }

		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			if (count.x < 0 || count.y < 0 || count.z < 0)
				return;

			float xPadding = padding.x + ((useBoundsInPadding) ? mesh.bounds.size.x : 0f);
			float yPadding = padding.y + ((useBoundsInPadding) ? mesh.bounds.size.y : 0f);
			float zPadding = padding.z + ((useBoundsInPadding) ? mesh.bounds.size.z : 0f);

			for (int x = 0; x < count.x; x++)
			{
				for (int y = 0; y < count.y; y++)
				{
					for (int z = 0; z < count.z; z++)
					{
						var index = x + count.x * (y + count.y * z);
						var p = new Vector3 (x * xPadding, y * yPadding, z * zPadding);
						points[index] = Matrix4x4.TRS (transform.position + transform.rotation * p, transform.rotation, transform.localScale);
					}
				}
			}
		}
	}
}