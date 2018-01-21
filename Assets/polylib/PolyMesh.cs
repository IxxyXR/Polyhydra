using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolyMesh {
	
	public Polyhedron polyhedron;

	public List<Vector3> vertices;
	public List<int> triangles;
	public List<Color> colors;
	
	public Mesh mesh;
	public Color[] vertexPallette;
	
	public PolyMesh(int polyType, bool dual) {
		
		// TODO generate mesh for duals
		
		polyhedron = new Polyhedron(polyType);
		vertices = new List<Vector3>();
		triangles = new List<int>();
		colors = new List<Color>();
		mesh = new Mesh();
		
		vertexPallette = new Color[] {
			Color.red,
			Color.yellow,
			Color.green,
			Color.cyan,
			Color.blue,
			Color.magenta
		};

		// TODO Fix duals
		
		List<Face> faces = new List<Face>();
		
		if (dual) {

//			// Swap everything around
//
//			int[,] tmpE = polyhedron.DualEdges;
//			polyhedron.DualEdges = polyhedron.Edges;
//			polyhedron.Edges = tmpE;
//
//			Vector[] tmpV = polyhedron.Vertices;
//			polyhedron.Vertices = polyhedron.Faces;
//			polyhedron.Faces = tmpV;
//
//			int tmpC = polyhedron.FaceCount;
//			polyhedron.FaceCount = polyhedron.EdgeCount;
//			polyhedron.EdgeCount = tmpC;
//
//			int[,] tmpI = polyhedron.VertexFaceIncidence;
//			for (int valency = 0; valency < polyhedron.Valency; valency++) {
//				for (int vertexIndex = 0; vertexIndex < polyhedron.VertexCount; vertexIndex++) {
//					int faceIndex = tmpI[valency, vertexIndex];
//					polyhedron.VertexFaceIncidence[valency, faceIndex] = vertexIndex;
//				}
//			}
			
			for (int vIndex = 0; vIndex < polyhedron.VertexCount; vIndex++) {
			
				int f1 = SeekFace(vIndex);
				Face face = new Face(
					polyhedron.Vertices[vIndex],
					polyhedron.Faces[f1],
					polyhedron.FaceSidesByType[polyhedron.FaceTypes[f1]]  // TODO
				);
				
				face.SetPoint(f1);
				
				bool end = false;
				
				while (!end) {
					int f2;
					for (int ii = 0; ii < polyhedron.Valency; ii++) {
						f2 = polyhedron.VertexAdjacency[ii, f1];
						bool found = false;
						for (int j = 0; j < polyhedron.Valency; j++) {
							if (polyhedron.VertexFaceIncidence[j, f2] == vIndex) {
								if (!face.VertexExists(f2)) {
									f1 = f2;
									found = true;
									break;
								}
							}
						}
						if (found) {
							face.SetPoint(f2);
						}
						if (face.GetCorners() == face.points.Count) {
							end = true;
						}
					}
				}
				faces.Add(face);
			}
			
			bool auxiliaryNeeded = false;
			
			foreach (double c in polyhedron.FaceSidesByType) {
				Fraction frax = new Fraction(c);
				if (frax.d > 1 && frax.d != frax.n - 1) {
					auxiliaryNeeded = true;
				}
			}
			
			if (auxiliaryNeeded) { // TODO this is awful
				var tempVerts = new List<Vector>();
				tempVerts = polyhedron.Vertices.ToList();
				foreach (Face f in faces) {
					if (f.frax.d > 1 && f.frax.d != f.frax.n - 1) {
						tempVerts.Add(f.center);
						f.SetPoint(tempVerts.Count - 1);
						f.centerPoint = tempVerts.Count - 1;
					}
				}
				polyhedron.Vertices = tempVerts.ToArray();
			}
			
			// Build Faces
	
			int meshVertexIndex = 0;
			
			for (int faceType = 0; faceType < polyhedron.FaceTypeCount; faceType++) {
				foreach (Face face in faces) {
					if (face.configuration == polyhedron.FaceSidesByType[faceType]) {
						var faceTriangles = face.CalcTriangles();
						Color faceColor = vertexPallette[(int) (face.configuration % vertexPallette.Length)];
						// Vertices
						for (int i = 0; i < faceTriangles.Count; i++) {
							Vector v = polyhedron.Vertices[faceTriangles[i]];
							vertices.Add(v.getVector3());
							colors.Add(faceColor);
							triangles.Add(meshVertexIndex);
							meshVertexIndex++;
						}	
					}
				}
			}
			
		
			
		} else {
			
			for (int faceIndex = 0; faceIndex < polyhedron.FaceCount; faceIndex++) {
			
				int v1 = SeekVertex(faceIndex);
				
				Face face = new Face(
					polyhedron.Faces[faceIndex],
					polyhedron.Vertices[v1],
					polyhedron.FaceSidesByType[polyhedron.FaceTypes[faceIndex]]
				);
				
				face.SetPoint(v1);
				
				bool end = false;
				
				while (!end) {
					int v2;
					for (int ii = 0; ii < polyhedron.Valency; ii++) {
						v2 = polyhedron.VertexAdjacency[ii, v1];
						bool found = false;
						for (int j = 0; j < polyhedron.Valency; j++) {
							if (polyhedron.VertexFaceIncidence[j, v2] == faceIndex) {
								if (!face.VertexExists(v2)) {
									v1 = v2;
									found = true;
									break;
								}
							}
						}
						if (found) {
							face.SetPoint(v2);
						}
						if (face.GetCorners() == face.points.Count) {
							end = true;
						}
					}
				}
				faces.Add(face);
			}
			
			bool auxiliaryNeeded = false;
			
			foreach (double c in polyhedron.FaceSidesByType) {
				Fraction frax = new Fraction(c);
				if (frax.d > 1 && frax.d != frax.n - 1) {
					auxiliaryNeeded = true;
				}
			}
			
			if (auxiliaryNeeded) { // TODO this is awful
				var tempVerts = new List<Vector>();
				tempVerts = polyhedron.Vertices.ToList();
				foreach (Face f in faces) {
					if (f.frax.d > 1 && f.frax.d != f.frax.n - 1) {
						tempVerts.Add(f.center);
						f.SetPoint(tempVerts.Count - 1);
						f.centerPoint = tempVerts.Count - 1;
					}
				}
				polyhedron.Vertices = tempVerts.ToArray();
			}
			
			// Build Faces
	
			int meshVertexIndex = 0;
			
			for (int faceType = 0; faceType < polyhedron.FaceTypeCount; faceType++) {
				foreach (Face face in faces) {
					if (face.configuration == polyhedron.FaceSidesByType[faceType]) {
						var faceTriangles = face.CalcTriangles();
						Color faceColor = vertexPallette[(int) (face.configuration % vertexPallette.Length)];
						// Vertices
						for (int i = 0; i < faceTriangles.Count; i++) {
							Vector v = polyhedron.Vertices[faceTriangles[i]];
							vertices.Add(v.getVector3());
							colors.Add(faceColor);
							triangles.Add(meshVertexIndex);
							meshVertexIndex++;
						}	
					}
				}
			}
			
		}
		
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.colors = colors.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
	}
	
	private int SeekVertex(int face) {
		for (int vc = 0; vc < polyhedron.VertexCount; vc++) {
			for (int vv = 0; vv < polyhedron.Valency; vv++) {
				int incid = polyhedron.VertexFaceIncidence[vv, vc];
				if (incid == face) {
					return vc;
				}
			}
		}
		throw new SystemException("SeekVertex failed on face: " + face);
	}
	
	private int SeekFace(int vertex) {
		return polyhedron.VertexFaceIncidence[0, vertex];
	}

	public Mesh Explode() {
		
		var newMesh = new Mesh();
		var newVerts = new Vector3[mesh.vertexCount];
		
		foreach (var t in mesh.triangles) {
			var direction = mesh.normals[t];
			newVerts[t] = mesh.vertices[t] + direction.normalized * 1.1f;
		}
		
		newMesh.vertices = newVerts.ToArray();
		newMesh.triangles = mesh.triangles;
		newMesh.colors = mesh.colors;
		newMesh.RecalculateNormals();
		newMesh.RecalculateTangents();
		newMesh.RecalculateBounds();
		return newMesh;
	}

//	public Mesh _OldBuildMesh() {
//		
//		faceVertices = new List<List<int>>();
//		faceEdges = new List<List<int>>();
//		
//		for (int faceNum = 0; faceNum < polyhedron.FaceCount; faceNum++) {
//			faceVertices.Add(new List<int>());
//			faceEdges.Add(new List<int>());
//		}
//		
//		// Here be monsters
//		for (int vertexNum = 0; vertexNum < polyhedron.VertexCount; vertexNum++) { 
//			for (int valence = 0; valence < polyhedron.Valency; valence++) {
//				int faceNum = polyhedron.VertexFaceIncidence[valence, vertexNum];
//				faceVertices[faceNum].Add(vertexNum);
//				for (int edgeNum = 0; edgeNum < polyhedron.EdgeCount; edgeNum++) {
//					if (vertexNum==polyhedron.Edges[0, edgeNum] || vertexNum==polyhedron.Edges[1, edgeNum]) {
//						faceEdges[faceNum].Add(edgeNum);
//					}
//				}
//			}
//		}
//		
//		var meshVertices = new List<Vector3>();
//		var meshTriangles = new List<int>();
//		var meshColors = new List<Color>();
//		int vIndex = 0;
//		
//		for (int i = 0; i < faceEdges.Count; i++) {
//
//			Color faceColor = pallette[polyhedron.FaceTypes[i]];
//			
//			for (int j = 0; j < faceEdges[i].Count; j++) {
//				
//				var points = new List<Vector3>();
//				
//				foreach (var vertex in faceVertices[i]) {
//					points.Add(polyhedron.Vertices[vertex].getVector3());
//				}
//				
//				var centrePoint = FindCenterPoint(points.ToArray());
//				int edgeNum = faceEdges[i][j];
//
//				//meshVertices.Add(f[i].getVector3());
//
//				// Normal direction
//				meshVertices.Add(centrePoint);
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				meshVertices.Add(polyhedron.Vertices[polyhedron.Edges[0, edgeNum]].getVector3());
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				meshVertices.Add(polyhedron.Vertices[polyhedron.Edges[1, edgeNum]].getVector3());
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				
//				// Reverse direction
//				meshVertices.Add(centrePoint);
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				meshVertices.Add(polyhedron.Vertices[polyhedron.Edges[1, edgeNum]].getVector3());
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				meshVertices.Add(polyhedron.Vertices[polyhedron.Edges[0, edgeNum]].getVector3());
//				meshColors.Add(faceColor);
//				meshTriangles.Add(vIndex);
//				vIndex++;
//				
//			}
//		}
//		
//		mesh.vertices = meshVertices.ToArray();
//		mesh.triangles = meshTriangles.ToArray();
//		mesh.colors = meshColors.ToArray();
//		mesh.RecalculateNormals();
//		mesh.name = polyhedron.PolyName;
//
//		return mesh;
//
//	}
//	
//	Vector3 FindCenterPoint(Vector3[] points) {
//		
//		if (points.Length == 0) {return Vector3.zero;}
//		if (points.Length == 1) {return points[0];}
//		var bounds = new Bounds(points[0], Vector3.zero);
//		for (var i = 1; i < points.Length; i++)
//			bounds.Encapsulate(points[i]); 
//		return bounds.center;
//	}
}