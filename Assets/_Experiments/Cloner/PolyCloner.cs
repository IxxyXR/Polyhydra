using System.Collections.Generic;
using System.Linq;
using Conway;
using Unity.Mathematics;
using UnityEngine;

namespace Cloner
{
	public class PolyCloner : Cloner
	{


		public enum CloneTypes
		{
			Vertex,
			Edge,
			Face
		}


		public CloneTypes CloneType;
		public float normalOffset;
		public bool alignWithNormals;
		public PolyHydra target;
		public Vector3 initialRotation = new Vector3(0, 90, 0);

		private Vector3[] targetPoints;
		private Vector3[] normals;

		protected override int PointCount
		{
			get
			{
				if (target == null) return 0;

				switch (CloneType)
				{
					case CloneTypes.Vertex:
						return target._conwayPoly.Vertices.Count;
					case CloneTypes.Edge:
						int boundaryEdgeCount = 0;
						int pairedEdgeCount = 0;
						foreach (var edge in target._conwayPoly.Halfedges)
						{
							if (edge.Pair == null)
							{
								boundaryEdgeCount++;
							}
							else
							{
								pairedEdgeCount++;
							}
						}
						return boundaryEdgeCount + (pairedEdgeCount / 2);
					case CloneTypes.Face:
						return target._conwayPoly.Faces.Count;
					default:
						return 0;
				}
			}
		}


		protected override void CalculatePoints (ref List<Matrix4x4> points)
		{
			if (target == null)
				return;

			switch (CloneType)
			{
				case CloneTypes.Vertex:
					targetPoints = target._conwayPoly.Vertices.Select(v => v.Position).ToArray();
					normals = target._conwayPoly.Vertices.Select(v => v.Normal).ToArray();
					break;
				case CloneTypes.Edge:
					targetPoints = new Vector3[PointCount];
					normals = new Vector3[PointCount];
					int boundaryEdgeCount = 0;
					int pairedEdgeCount = 0;
					var seenEdges = new HashSet<string>();
					int i = 0;
					foreach (var edge in target._conwayPoly.Halfedges)
					{
						if (edge.Pair == null || !seenEdges.Contains(edge.PairedName))
						{
							seenEdges.Add(edge.PairedName);
							targetPoints[i] = edge.Midpoint;
							normals[i] = edge.Face.Normal;
							if (edge.Pair != null)
							{
								normals[i] += edge.Pair.Face.Normal;
							}
							i++;
						}
					}
					break;
				case CloneTypes.Face:
					targetPoints = target._conwayPoly.Faces.Select(v => v.Centroid).ToArray();
					normals = target._conwayPoly.Faces.Select(v => v.Normal).ToArray();
					break;
				default:
					targetPoints = new Vector3[0];
					normals = new Vector3[0];
					break;
			}

			var initialTheta = initialRotation * Mathf.Deg2Rad;
			for (int i = 0; i < points.Count; i++)
			{
				var position = target.transform.localToWorldMatrix.MultiplyPoint3x4 (targetPoints[i] + (normals[i] * normalOffset));
				var rotation = (alignWithNormals ? (Quaternion.LookRotation (normals[i])) : Quaternion.identity);
				points[i] = Matrix4x4.TRS (position, rotation * quaternion.Euler(initialTheta), Vector3.one);
			}
		}
	}
}