using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class SkinnedMeshCloner : Cloner
	{
		[Range (0f, 1f)]
		public float amount = 0.5f;
		public float normalOffset;
		public bool alignWithNormals;
		public SkinnedMeshRenderer target;

		public Mesh bakedMesh;
		private Vector3[] vertices;
		private Vector3[] normals;

		protected override int PointCount { get { return (target == null) ? 0 : (int)(amount * target.sharedMesh.vertexCount); } }


		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			if (target == null)
				return;

			target.BakeMesh (bakedMesh);
			vertices = bakedMesh.vertices;
			normals = bakedMesh.normals;

			for (int i = 0; i < points.Count; i++)
			{
				var meshIndex = (int)Mathf.Lerp (0, target.sharedMesh.vertexCount, (float)i / PointCount);
				var position = target.transform.localToWorldMatrix.MultiplyPoint3x4 (vertices[meshIndex] + (normals[meshIndex] * normalOffset));
				var rotation = (alignWithNormals ? (Quaternion.LookRotation (normals[meshIndex])) : Quaternion.identity);
				points[i] = Matrix4x4.TRS (position, rotation, Vector3.one);
			}
		}
	}
}