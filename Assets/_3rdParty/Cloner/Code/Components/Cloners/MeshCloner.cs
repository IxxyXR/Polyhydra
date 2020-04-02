using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class MeshCloner : Cloner
	{
		public float normalOffset;
		public bool alignWithNormals;
		public MeshFilter target;

		private Vector3[] vertices;
		private Vector3[] normals;

		protected override int PointCount { get { return (target == null) ? 0 : target.sharedMesh.vertexCount; } }


		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			if (target == null)
				return;

			vertices = target.sharedMesh.vertices;
			normals = target.sharedMesh.normals;

			for (int i = 0; i < points.Count; i++)
			{
				var position = target.transform.localToWorldMatrix.MultiplyPoint3x4 (vertices[i] + (normals[i] * normalOffset));
				var rotation = (alignWithNormals ? (Quaternion.LookRotation (normals[i])) : Quaternion.identity);
				points[i] = Matrix4x4.TRS (position, rotation, Vector3.one);
			}
		}
	}
}