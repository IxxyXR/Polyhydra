using System;
using System.Collections.Generic;
using System.Linq;
using Wythoff;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Conway
{

	/// <summary>
	/// A class for manifold meshes which uses the Halfedge data structure.
	/// </summary>

	public class ConwayPoly
	{

		#region constructors

		public ConwayPoly()
		{
			Halfedges = new MeshHalfedgeList(this);
			Vertices = new MeshVertexList(this);
			Faces = new MeshFaceList(this);
		}

		public ConwayPoly(WythoffPoly source) : this()
		{
			FaceRoles = new List<Roles>();
			VertexRoles = new List<Roles>();

			// Add vertices
			Vertices.Capacity = source.VertexCount;
			foreach (Vector p in source.Vertices)
			{
				Vertices.Add(new Vertex(p.getVector3()));
				VertexRoles.Add(Roles.Existing);
			}

			// Add faces (and construct halfedges and store in hash table)
			foreach (var face in source.faces)
			{
				var v = new Vertex[face.points.Count];
				for (int i = 0; i < face.points.Count; i++)
				{
					v[i] = Vertices[face.points[i]];
				}

				if (!Faces.Add(v))
				{
					// Failed. Let's try flipping the face
					Array.Reverse(v);
					if (!Faces.Add(v))
					{
						Debug.LogError("Failed even after flipping.");
					}
				}

				FaceRoles.Add(Roles.Existing);
			}

			// Find and link halfedge pairs
			Halfedges.MatchPairs();
		}

		public ConwayPoly(
			IEnumerable<Vector3> verticesByPoints,
			IEnumerable<IEnumerable<int>> facesByVertexIndices,
			IEnumerable<Roles> faceRoles,
			IEnumerable<Roles> vertexRoles
		) : this()
		{
			if (faceRoles.Count() != facesByVertexIndices.Count())
			{
				throw new ArgumentException(
					$"Incorrect FaceRole array: {faceRoles.Count()} instead of {facesByVertexIndices.Count()}",
					"faceRoles"
				);
			}

			InitIndexed(verticesByPoints, facesByVertexIndices);

			FaceRoles = faceRoles.ToList();
			VertexRoles = vertexRoles.ToList();

			Vertices.CullUnused();
		}

		private void InitIndexed(IEnumerable<Vector3> verticesByPoints,
			IEnumerable<IEnumerable<int>> facesByVertexIndices)
		{
			// Add vertices
			foreach (Vector3 p in verticesByPoints)
			{
				Vertices.Add(new Vertex(p));
			}

			// Add faces
			foreach (IEnumerable<int> indices in facesByVertexIndices)
			{
				Faces.Add(indices.Select(i => Vertices[i]));
			}

			// Find and link halfedge pairs
			Halfedges.MatchPairs();
		}

		public ConwayPoly Duplicate()
		{
			// Export to face/vertex and rebuild
			return new ConwayPoly(ListVerticesByPoints(), ListFacesByVertexIndices(), FaceRoles, VertexRoles);
		}

		#endregion

		#region properties

		public List<Roles> FaceRoles;
		public List<Roles> VertexRoles;

		public MeshHalfedgeList Halfedges { get; private set; }
		public MeshVertexList Vertices { get; set; }
		public MeshFaceList Faces { get; private set; }

		public enum Roles
		{
			Ignored,
			Existing,
			New,
			NewAlt
		}

		public enum FaceSelections
		{
			All,
			ThreeSided,
			FourSided,
			FiveSided,
			SixSided,
			SevenSided,
			EightSided,
			Ignored,
			Existing,
			New,
			NewAlt,
			AllNew
		}

		public bool IsValid
		{
			get
			{
				if (Halfedges.Count == 0)
				{
					return false;
				}

				if (Vertices.Count == 0)
				{
					return false;
				}

				if (Faces.Count == 0)
				{
					return false;
				}

				// TODO: beef this up (check for a valid mesh)

				return true;
			}
		}
		// TODO
		//        public BoundingBox BoundingBox {
		//            get {
		//                if (!IsValid) {
		//                    return BoundingBox.Empty;
		//                }
		//
		//                List<Vector> points = new List<Vector>();
		//                foreach (BVertex v in this.Vertices) {
		//                    points.Add(v.Position);
		//                }
		//
		//                BoundingBox result = new BoundingBox(points);
		//                result.MakeValid();
		//                return result;
		//            }
		//        }

		#endregion

		#region conway methods

		/// <summary>
		/// Conway's dual operator
		/// </summary>
		/// <returns>the dual as a new mesh</returns>
		public ConwayPoly Dual()
		{

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Create vertices from faces
			var vertexPoints = new List<Vector3>(Faces.Count);
			foreach (var f in Faces)
			{
				vertexPoints.Add(f.Centroid);
				vertexRoles.Add(Roles.New);
			}

			// Create sublist of non-boundary vertices
			var naked = new Dictionary<string, bool>(Vertices.Count); // vertices (name, boundary?)
			var hlookup =
				new Dictionary<string, int>(Halfedges.Count); // boundary halfedges (name, index of point in new mesh)

			foreach (var he in Halfedges)
			{
				if (!naked.ContainsKey(he.Vertex.Name))
				{
					// if not in dict, add (boundary == true)
					naked.Add(he.Vertex.Name, he.Pair == null);
				}
				else if (he.Pair == null)
				{
					// if in dict and belongs to boundary halfedge, set true
					naked[he.Vertex.Name] = true;
				}

				if (he.Pair == null)
				{
					// if boundary halfedge, add mid-point to vertices and add to lookup
					hlookup.Add(he.Name, vertexPoints.Count);
					vertexPoints.Add(he.Midpoint);
					vertexRoles.Add(Roles.New);
				}
			}

			// List new faces by their vertex indices
			// (i.e. old vertices by their face indices)
			var flookup = new Dictionary<string, int>();

			for (int i = 0; i < Faces.Count; i++)
			{
				flookup.Add(Faces[i].Name, i);
			}

			var faceIndices = new List<List<int>>(Vertices.Count);

			for (var i = 0; i < Vertices.Count; i++)
			{
				var v = Vertices[i];
				var fIndex = new List<int>();

				foreach (Face f in v.GetVertexFaces())
				{
					fIndex.Add(flookup[f.Name]);
				}

				if (naked.ContainsKey(v.Name) && naked[v.Name])
				{
					// Handle boundary vertices...
					var h = v.Halfedges;
					if (h.Count > 0)
					{
						// Add points on naked edges and the naked vertex
						fIndex.Add(hlookup[h.Last().Name]);
						fIndex.Add(vertexPoints.Count);
						fIndex.Add(hlookup[h.First().Next.Name]);
						vertexPoints.Add(v.Position);
						vertexRoles.Add(Roles.New);
					}
				}

				faceIndices.Add(fIndex);
				faceRoles.Add(VertexRoles[i]);
			}

			return new ConwayPoly(vertexPoints, faceIndices.ToArray(), faceRoles, vertexRoles);
		}

		public ConwayPoly AddDual(float scale = 1f)
		{
			var oldPoly = Duplicate();
			var newPoly = Dual();
			oldPoly.ScalePolyhedra();
			newPoly.ScalePolyhedra(scale);
			oldPoly.Append(newPoly);
			return oldPoly;
		}

		/// <summary>
		/// Conway's ambo operator
		/// </summary>
		/// <returns>the ambo as a new mesh</returns>
		public ConwayPoly Ambo()
		{

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
			var vertexPoints = new List<Vector3>(); // vertices as points
			var hlookup = new Dictionary<string, int>();
			int count = 0;

			foreach (var edge in Halfedges)
			{
				// if halfedge's pair is already in the table, give it the same index
				if (edge.Pair != null && hlookup.ContainsKey(edge.Pair.Name))
				{
					hlookup.Add(edge.Name, hlookup[edge.Pair.Name]);
				}
				else
				{
					// otherwise create a new vertex and increment the index
					hlookup.Add(edge.Name, count++);
					vertexPoints.Add(edge.Midpoint);
					vertexRoles.Add(Roles.New);
				}
			}

			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
			// faces to faces
			foreach (var face in Faces)
			{
				faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
				faceRoles.Add(Roles.Existing);
			}

			// vertices to faces
			foreach (var vertex in Vertices)
			{
				var he = vertex.Halfedges;
				if (he.Count == 0) continue; // no halfedges (naked vertex, ignore)
				var list = he.Select(edge => hlookup[edge.Name]); // halfedge indices for vertex-loop
				if (he[0].Next.Pair == null)
				{
					// Handle boundary vertex, add itself and missing boundary halfedge
					list = list.Concat(new[] {vertexPoints.Count, hlookup[he[0].Next.Name]});
					vertexPoints.Add(vertex.Position);
				}

				faceIndices.Add(list);
				faceRoles.Add(Roles.New);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		// Not currently used as it results in non-coplanar faces and it's cheaper to do ambo > dual than to follow this with canonicalize
		public ConwayPoly Join(float offset)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newCentroidVertices = new Dictionary<string, int>();
			var rhombusFlags = new Dictionary<string, bool>(); // Track if we've created a face for joined edges

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				existingVertices[vertexPoints[i]] = i;
				vertexRoles.Add(Roles.Existing);
			}

			int vertexIndex = vertexPoints.Count();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				vertexPoints.Add(face.Centroid + face.Normal * offset);
				newCentroidVertices[face.Name] = vertexIndex++;
				vertexRoles.Add(Roles.New);
			}

			foreach (var edge in Halfedges)
			{
				if (!rhombusFlags.ContainsKey(edge.PairedName))
				{
					var rhombus = new[]
					{
						existingVertices[edge.Prev.Vertex.Position],
						newCentroidVertices[edge.Face.Name],
						existingVertices[edge.Vertex.Position],
						newCentroidVertices[edge.Pair.Face.Name]
					};
					faceIndices.Add(rhombus);
					faceRoles.Add(Roles.New);
					rhombusFlags[edge.PairedName] = true;
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Kis(float offset, FaceSelections facesel)
		{

			// vertices and faces to vertices
			var newVerts = Faces.Select(f => f.Centroid + f.Normal * offset);
			var vertexPoints = Vertices.Select(v => v.Position).Concat(newVerts);
			var vertexRoles = Enumerable.Repeat(Roles.Existing, vertexPoints.Count());
			vertexRoles.Concat(Enumerable.Repeat(Roles.New, newVerts.Count()));

			var faceRoles = new List<Roles>();

			// vertex lookup
			var vlookup = new Dictionary<string, int>();
			int n = Vertices.Count;
			for (int i = 0; i < n; i++)
			{
				vlookup.Add(Vertices[i].Name, i);
			}

			// create new tri-faces (like a fan)
			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
			for (int i = 0; i < Faces.Count; i++)
			{
				if (IncludeFace(i, facesel))
				{
					var list = Faces[i].GetHalfedges();
					for (var edgeIndex = 0; edgeIndex < list.Count; edgeIndex++)
					{
						var edge = list[edgeIndex];
						// Create new face from edge start, edge end and centroid
						faceIndices.Add(
							new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
						);
						// Alternate roles but only for faces with an even number of sides
						if (edgeIndex % 2 == 0 || Faces[i].Sides % 2 != 0)
						{faceRoles.Add(Roles.New);}
						else {faceRoles.Add(Roles.NewAlt);}
					}
				}
				else
				{
					faceIndices.Add(ListFacesByVertexIndices()[i]);
					faceRoles.Add(Roles.Ignored);
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Gyro(float ratio = 0.3333f)
		{

			// Happy accidents - skip n new faces - offset just the centroid?

			var existingVerts = new Dictionary<string, int>();
			var newVerts = new Dictionary<string, int>();
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Loop through old faces
			for (int i = 0; i < Faces.Count; i++)
			{
				var oldFace = Faces[i];

				vertexPoints.Add(oldFace.Centroid);
				vertexRoles.Add(Roles.New);
				int centroidIndex = vertexPoints.Count - 1;

				// Loop through each vertex on old face and create a new face for each
				for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
				{

					int seedVertexIndex;
					int OneThirdIndex;
					int PairOneThirdIndex;
					int PrevThirdIndex;

					string keyName;

					var thisFaceIndices = new List<int>();
					var edges = oldFace.GetHalfedges();

					var seedVertex = edges[j].Vertex;
					keyName = seedVertex.Name;
					if (existingVerts.ContainsKey(keyName))
					{
						seedVertexIndex = existingVerts[keyName];
					}
					else
					{
						vertexPoints.Add(seedVertex.Position);
						vertexRoles.Add(Roles.Existing);
						seedVertexIndex = vertexPoints.Count - 1;
						existingVerts[keyName] = seedVertexIndex;
					}

					var OneThirdVertex = edges[j].PointAlongEdge(ratio);
					keyName = edges[j].Name;
					if (newVerts.ContainsKey(keyName))
					{
						OneThirdIndex = newVerts[keyName];
					}
					else
					{
						vertexPoints.Add(OneThirdVertex);
						vertexRoles.Add(Roles.NewAlt);
						OneThirdIndex = vertexPoints.Count - 1;
						newVerts[keyName] = OneThirdIndex;
					}

					Vector3 PrevThirdVertex;
					if (edges[j].Next.Pair != null)
					{
						PrevThirdVertex = edges[j].Next.Pair.PointAlongEdge(ratio);
						keyName = edges[j].Next.Pair.Name;						
					}
					else
					{
						PrevThirdVertex = edges[j].Next.PointAlongEdge(1 - ratio);
						keyName = edges[j].Next.Name + "-Pair";					
					}
					if (newVerts.ContainsKey(keyName))
					{
						PrevThirdIndex = newVerts[keyName];
					}
					else
					{
						vertexPoints.Add(PrevThirdVertex);
						vertexRoles.Add(Roles.NewAlt);
						PrevThirdIndex = vertexPoints.Count - 1;
						newVerts[keyName] = PrevThirdIndex;
					}

					Vector3 PairOneThird;
					if (edges[j].Pair != null)
					{
						PairOneThird = edges[j].Pair.PointAlongEdge(ratio);
						keyName = edges[j].Pair.Name;
					}
					else
					{
						PairOneThird = edges[j].PointAlongEdge(1 - ratio);
						keyName = edges[j].Name + "-Pair";					
					}
					if (newVerts.ContainsKey(keyName))
					{
						PairOneThirdIndex = newVerts[keyName];
					}
					else
					{
						vertexPoints.Add(PairOneThird);
						vertexRoles.Add(Roles.NewAlt);
						PairOneThirdIndex = vertexPoints.Count - 1;
						newVerts[keyName] = PairOneThirdIndex;
					}

					thisFaceIndices.Add(centroidIndex);
					thisFaceIndices.Add(PairOneThirdIndex);
					thisFaceIndices.Add(OneThirdIndex);
					thisFaceIndices.Add(seedVertexIndex);
					thisFaceIndices.Add(PrevThirdIndex);

					faceIndices.Add(thisFaceIndices);
					// Alternate roles but only for faces with an even number of sides
					if (j % 2 == 0 || Faces[j].Sides % 2 != 0){faceRoles.Add(Roles.New);}
					else {faceRoles.Add(Roles.NewAlt);}
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			return poly;
		}

		#endregion

		#region extended conway methods

		// Add Vertices at edge midpoints and new faces around each vertex
		// Equivalent to ambo without removing vertices
		public ConwayPoly Subdivide()
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = Vertices.Select(x => x.Position).ToList(); // Existing vertices
			var vertexRoles = Enumerable.Repeat(Roles.Existing, vertexPoints.Count).ToList();

			var faceRoles = new List<Roles>();

			// Create new vertices, one at the midpoint of each edge

			var newVertices = new Dictionary<string, int>();
			int vertexIndex = vertexPoints.Count();

			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.Midpoint);
				vertexRoles.Add(Roles.New);
				newVertices[edge.PairedName] = vertexIndex++;
			}

			foreach (var face in Faces)
			{
				// Create a new face for each existing face
				var newFace = new int[face.Sides];
				var edge = face.Halfedge;

				for (int i = 0; i < face.Sides; i++)
				{
					newFace[i] = newVertices[edge.PairedName];
					edge = edge.Next;
				}

				faceIndices.Add(newFace);
				faceRoles.Add(Roles.Existing);
			}

			// Create new faces for each vertex

			for (int i = 0; i < Vertices.Count; i++)
			{

				var adjacentFaces = Vertices[i].GetVertexFaces();

				foreach (Face face in adjacentFaces)
				{
					var edge = face.GetHalfedges().Find(x => x.Vertex == Vertices[i]);
					int currVertex = newVertices[edge.PairedName];
					int prevVertex = newVertices[edge.Next.PairedName];
					var triangle = new[] {i, prevVertex, currVertex};
					faceIndices.Add(triangle);
					// Alternate roles but only for faces with an even number of sides
					if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
					else {faceRoles.Add(Roles.NewAlt);}
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			return poly;
		}

		// Interesting accident
		public ConwayPoly BrokenLoft(float ratio = 0.33333333f, int sides = 0)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new vertices

			foreach (var face in Faces)
			{
				if (sides == 0 || face.Sides == sides)
				{
					var edge = face.Halfedge;
					var centroid = face.Centroid;

					// Create a new face for each existing face
					var newInsetFace = new int[face.Sides];

					for (int i = 0; i < face.Sides; i++)
					{
						var vertex = edge.Vertex.Position;
						var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);

						vertexPoints.Add(newVertex);
						vertexRoles.Add(Roles.New);
						newInsetFace[i] = vertexIndex;
						newVertices[edge.Name] = vertexIndex++;

						// Generate new faces

						var newFace = new[]
						{
							existingVertices[edge.Vertex.Position],
							(existingVertices[edge.Next.Vertex.Position] + 1),
							newVertices[edge.Name],
//						    //newVertices[edge.Prev.PairedName],

						};
						faceIndices.Add(newFace);

						edge = edge.Next;
					}

					faceIndices.Add(newInsetFace);
				}
				else
				{
					// Keep original face
					//faceIndices.Add(face);
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Loft(float ratio = 0.33333333f, FaceSelections facesel = FaceSelections.All)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new vertices

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				if (IncludeFace(faceIndex, facesel))
				{
					var edge = face.Halfedge;
					var centroid = face.Centroid;

					// Create a new face for each existing face
					var newInsetFace = new int[face.Sides];
					int newV = -1;
					int prevNewV = -1;

					for (int i = 0; i < face.Sides; i++)
					{
						var vertex = edge.Vertex.Position;
						var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);

						vertexPoints.Add(newVertex);
						vertexRoles.Add(Roles.New);
						newInsetFace[i] = vertexIndex;
						newVertices[edge.Name] = vertexIndex++;

						// Generate new faces
						newV = newVertices[edge.Name];
						if (i > 0)
						{
							var newEdgeFace = new[]
							{
								newV,
								prevNewV,
								existingVertices[edge.Prev.Vertex.Position],
								existingVertices[edge.Vertex.Position]
							};
							faceIndices.Add(newEdgeFace);
							// Alternate roles but only for faces with an even number of sides
							if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
							else {faceRoles.Add(Roles.NewAlt);}
						}

						prevNewV = newV;
						edge = edge.Next;
					}

					faceIndices.Add(newInsetFace);
					faceRoles.Add(Roles.Existing);

					// Add the final missing new edge face

					var lastEdge = face.Halfedge.Prev;
					var finalFace = new[]
					{
						existingVertices[lastEdge.Vertex.Position],
						existingVertices[lastEdge.Next.Vertex.Position],
						newVertices[lastEdge.Next.Name],
						newVertices[lastEdge.Name]
					};
					faceIndices.Add(finalFace);
					faceRoles.Add(Roles.New);
				}
				else
				{
					faceIndices.Add(
						face.GetHalfedges().Select(
							x => existingVertices[x.Vertex.Position]
						).ToArray());
					faceRoles.Add(Roles.Ignored);
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			return poly;
		}

		public ConwayPoly Quinto(float ratio = 0.33333333f)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<string, int>();
			var newInnerVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new edge vertices
			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.Midpoint);
				vertexRoles.Add(Roles.New);
				newEdgeVertices[edge.PairedName] = vertexIndex++;
			}

			foreach (var face in Faces)
			{
				var edge = face.Halfedge;
				var centroid = face.Centroid;

				// Create a new face for each existing face
				var newInsetFace = new int[face.Sides];
				int prevNewEdgeVertex = -1;
				int prevNewInnerVertex = -1;

				for (int i = 0; i < face.Sides; i++)
				{
					var newEdgeVertex = vertexPoints[newEdgeVertices[edge.PairedName]];
					var newInnerVertex = Vector3.LerpUnclamped(newEdgeVertex, centroid, ratio);

					vertexPoints.Add(newInnerVertex);
					vertexRoles.Add(Roles.NewAlt);
					newInsetFace[i] = vertexIndex;
					newInnerVertices[edge.Name] = vertexIndex++;


					// Generate new faces
					if (i > 0)
					{
						var newEdgeFace = new[]
						{
							prevNewInnerVertex,
							prevNewEdgeVertex,
							existingVertices[edge.Prev.Vertex.Position],
							newEdgeVertices[edge.PairedName],
							newInnerVertices[edge.Name]
						};
						faceIndices.Add(newEdgeFace);
						// Alternate roles but only for faces with an even number of sides
						if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
						else {faceRoles.Add(Roles.NewAlt);}
					}

					prevNewEdgeVertex = newEdgeVertices[edge.PairedName];
					prevNewInnerVertex = newInnerVertices[edge.Name];
					edge = edge.Next;
				}

				faceIndices.Add(newInsetFace);
				faceRoles.Add(Roles.Existing);

				// Add the final missing new edge face

				var lastEdge = face.Halfedge.Prev;
				var finalFace = new[]
				{
					prevNewInnerVertex,
					prevNewEdgeVertex,
					existingVertices[edge.Prev.Vertex.Position],
					newEdgeVertices[edge.PairedName],
					newInnerVertices[edge.Name]
				};
				faceIndices.Add(finalFace);
				// Alternate roles for final face
				if (face.Sides % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
				else {faceRoles.Add(Roles.NewAlt);}

			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			return poly;
		}

		public ConwayPoly JoinedLace(float ratio = 0.33333333f)
		{
			return this._Lace(0, true, ratio);
		}

		public ConwayPoly Lace(float ratio = 0.33333333f, FaceSelections facesel = FaceSelections.All)
		{
			return this._Lace(facesel, false, ratio);
		}

		private ConwayPoly _Lace(FaceSelections facesel, bool joined, float ratio = 0.3333333f)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newInnerVertices = new Dictionary<string, int>();
			var rhombusFlags = new Dictionary<string, bool>(); // Track if we've created a face for joined edges

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				if (joined || IncludeFace(faceIndex, facesel))
				{
					var edge = face.Halfedge;
					var centroid = face.Centroid;
					var innerFace = new int[face.Sides];

					for (int i = 0; i < face.Sides; i++)
					{
						var newVertex = Vector3.LerpUnclamped(
							edge.Midpoint,
							centroid,
							ratio
						);

						// Build face at center of each original face
						vertexPoints.Add(newVertex);
						vertexRoles.Add(Roles.New);
						newInnerVertices[edge.Name] = vertexIndex;
						innerFace[i] = vertexIndex++;

						edge = edge.Next;
					}

					edge = face.Halfedge;

					for (int i = 0; i < face.Sides; i++)
					{
						var largeTriangle = new int[]
						{
							newInnerVertices[edge.Next.Name],
							newInnerVertices[edge.Name],
							existingVertices[edge.Vertex.Position]
						};
						faceIndices.Add(largeTriangle);
						faceRoles.Add(Roles.NewAlt);

						if (!joined)
						{
							var smallTriangle = new int[]
							{
								existingVertices[edge.Prev.Vertex.Position],
								existingVertices[edge.Vertex.Position],
								newInnerVertices[edge.Name]
							};
							faceIndices.Add(smallTriangle);
							faceRoles.Add(Roles.New);
						}

						edge = edge.Next;
					}

					faceIndices.Add(innerFace);
					faceRoles.Add(Roles.Existing);
				}
				else
				{
					faceIndices.Add(
						face.GetHalfedges().Select(
							x => existingVertices[x.Vertex.Position]
						).ToArray());
					faceRoles.Add(Roles.Ignored);
				}
			}

			// Create Rhombus faces
			// TODO Make planar
			if (joined)
			{
				foreach (var edge in Halfedges)
				{
					if (!rhombusFlags.ContainsKey(edge.PairedName))
					{
						var rhombus = new int[]
						{
							existingVertices[edge.Prev.Vertex.Position],
							newInnerVertices[edge.Pair.Name],
							existingVertices[edge.Vertex.Position],
							newInnerVertices[edge.Name]
						};
						faceIndices.Add(rhombus);
						faceRoles.Add(Roles.New);
						rhombusFlags[edge.PairedName] = true;
					}
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Stake(float ratio = 0.3333333f, FaceSelections facesel = FaceSelections.All)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newInnerVertices = new Dictionary<string, int>();
			var newCentroidVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				if (IncludeFace(faceIndex, facesel))
				{
					var edge = face.Halfedge;
					var centroid = face.Centroid;

					vertexPoints.Add(centroid);
					newCentroidVertices[face.Name] = vertexIndex++;
					vertexRoles.Add(Roles.New);

					// Generate the quads and triangles on this face
					for (int i = 0; i < face.Sides; i++)
					{
						var newVertex = Vector3.LerpUnclamped(
							edge.Midpoint,
							centroid,
							ratio
						);

						vertexPoints.Add(newVertex);
						vertexRoles.Add(Roles.NewAlt);
						newInnerVertices[edge.Name] = vertexIndex++;

						edge = edge.Next;
					}

					edge = face.Halfedge;

					for (int i = 0; i < face.Sides; i++)
					{
						var triangle = new[]
						{
							newInnerVertices[edge.Name],
							existingVertices[edge.Prev.Vertex.Position],
							existingVertices[edge.Vertex.Position]
						};
						faceIndices.Add(triangle);
						// Alternate roles but only for faces with an even number of sides
						if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
						else {faceRoles.Add(Roles.NewAlt);}

						var quad = new[]
						{
							existingVertices[edge.Vertex.Position],
							newInnerVertices[edge.Next.Name],
							newCentroidVertices[face.Name],
							newInnerVertices[edge.Name],
						};
						faceIndices.Add(quad);
						faceRoles.Add(Roles.Existing);

						edge = edge.Next;
					}
				}
				else
				{
					faceIndices.Add(
						face.GetHalfedges().Select(
							x => existingVertices[x.Vertex.Position]
						).ToArray()
					);
					faceRoles.Add(Roles.Ignored);
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		// TODO Fix
		public ConwayPoly Medial(int subdivisions)
		{
			return _Medial(subdivisions);
		}

		// TODO Fix
		public ConwayPoly EdgeMedial(int subdivisions)
		{
			return _Medial(subdivisions, true);
		}

		// TODO Fix
		private ConwayPoly _Medial(int subdivisions, bool edgeMedial = false)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<string, int[]>();
			var newCentroidVertices = new Dictionary<string, int>();
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();


			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new edge vertices
			foreach (var edge in Halfedges)
			{
				for (int i = 0; i < subdivisions; i++)
				{
					vertexPoints.Add(edge.Midpoint);
					vertexRoles.Add(Roles.New);
					newEdgeVertices[edge.Name][i] = vertexIndex++;
				}
			}

			foreach (var face in Faces)
			{

				vertexPoints.Add(face.Centroid);
				vertexRoles.Add(Roles.New);
				newCentroidVertices[face.Name] = vertexIndex++;

				var edge = face.Halfedge;
				var centroid = face.Centroid;

				vertexPoints.Add(centroid);
				vertexRoles.Add(Roles.New);
				int centroidIndex = vertexIndex++;

				for (int i = 0; i < face.Sides; i++)
				{

					var currNewVerts = newEdgeVertices[edge.Name];
					var prevLastVert = newEdgeVertices[edge.Name][subdivisions - 2];

					if (edgeMedial)
					{
						// One quadrilateral face
						var quad = new[]
						{
							existingVertices[edge.Vertex.Position],
							currNewVerts[0],
							centroidIndex,
							prevLastVert
						};
						//faceIndices.Add(quad);
						faceRoles.Add(Roles.New);
					}
					else
					{
						// Two triangular faces

						var triangle1 = new[]
						{
							existingVertices[edge.Vertex.Position],
							currNewVerts[0],
							centroidIndex
						};
						//faceIndices.Add(triangle1);
						faceRoles.Add(Roles.New);

						var triangle2 = new[]
						{
							centroidIndex,
							prevLastVert,
							existingVertices[edge.Vertex.Position]
						};
						//faceIndices.Add(triangle2);
						faceRoles.Add(Roles.New);
					}

					// Create new triangular faces at edges
					for (int j = 0; j < currNewVerts.Count() - 1; j++)
					{
						var edgeTriangle = new[]
						{
							centroidIndex,
							currNewVerts[j],
							currNewVerts[j + 1]
						};
						//faceIndices.Add(edgeTriangle);
						faceRoles.Add(Roles.New);
					}

					edge = edge.Next;

				}
			}

			//medialPolyhedron.setVertexNormalsToFaceNormals();
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, VertexRoles);
		}

		// TODO Fix
		public ConwayPoly JoinedMedial()
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create rhombic faces
			foreach (var edge in Halfedges)
			{
				int v0 = newEdgeVertices[edge.PairedName];
				int v2 = newEdgeVertices[edge.PairedName];
				var rhombus = new[]
				{
					v0,
					existingVertices[edge.Vertex.Position],
					v2,
					existingVertices[edge.Next.Vertex.Position]
				};
				faceIndices.Add(rhombus);
				faceRoles.Add(Roles.New);
			}

			// Generate triangular faces
			foreach (var face in Faces)
			{
				var centroid = face.Centroid;
				vertexPoints.Add(centroid);
				vertexRoles.Add(Roles.New);

				var edges = face.GetHalfedges();
				var prevEnds = edges[face.Sides - 1].getEnds();
				int prevVertex = newEdgeVertices[edges[0].PairedName];

				for (var i = 0; i < edges.Count; i++)
				{
					Halfedge edge = edges[i];
					var ends = edge.getEnds();
					int currVertex = newEdgeVertices[edge.PairedName];

					var triangle1 = new int[]
					{
						vertexIndex,
						existingVertices[edges[i].Vertex.Position],
						currVertex
					};
					var triangle2 = new int[]
					{
						vertexIndex,
						prevVertex,
						existingVertices[edges[i].Vertex.Position]
					};

					faceIndices.Add(triangle1);
					faceRoles.Add(Roles.New);
					faceIndices.Add(triangle2);
					faceRoles.Add(Roles.New);

					prevVertex = currVertex;
					edge = edge.Next;
				}

				vertexIndex++;
			}

			//medialPolyhedron.setVertexNormalsToFaceNormals();
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Propeller(float ratio = 0.33333333f)
		{
			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new edge vertices
			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.PointAlongEdge(ratio));
				newEdgeVertices[edge.Name] = vertexIndex++;
				vertexRoles.Add(Roles.New);

				if (edge.Pair != null)
				{
					vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));
					newEdgeVertices[edge.Pair.Name] = vertexIndex++;
				}
				else
				{
					vertexPoints.Add(edge.PointAlongEdge(1 - ratio));
					newEdgeVertices[edge.Name + "-Pair"] = vertexIndex++;
				}
				vertexRoles.Add(Roles.New);
			}

			// Create quadrilateral faces and one central face on each face
			foreach (var face in Faces)
			{
				var edge = face.Halfedge;
				var centralFace = new int[face.Sides];

				for (int i = 0; i < face.Sides; i++)
				{
					string edgePairName;
					if (edge.Pair !=null)
					{
						edgePairName = edge.Pair.Name;
					}
					else
					{
						edgePairName = edge.Name + "-Pair";
					}
					
					string edgeNextPairName;
					if (edge.Next.Pair !=null)
					{
						edgeNextPairName = edge.Next.Pair.Name;
					}
					else
					{
						edgeNextPairName = edge.Next.Name + "-Pair";
					}

					var quad = new[]
					{
						newEdgeVertices[edge.Next.Name],
						newEdgeVertices[edgeNextPairName],
						newEdgeVertices[edgePairName],
						existingVertices[edge.Vertex.Position],
					};
					faceIndices.Add(quad);
					// Alternate roles but only for faces with an even number of sides
					if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
					else {faceRoles.Add(Roles.NewAlt);}

					centralFace[i] = newEdgeVertices[edgePairName];
					edge = edge.Next;
				}

				faceIndices.Add(centralFace);
				faceRoles.Add(Roles.Existing);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Whirl(float ratio = 0.3333333f)
		{

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<string, int>();
			var newInnerVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			int vertexIndex = vertexPoints.Count();

			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.PointAlongEdge(ratio));
				vertexRoles.Add(Roles.New);
				newEdgeVertices[edge.Name] = vertexIndex++;
				if (edge.Pair != null)
				{
					vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));				
					newEdgeVertices[edge.Pair.Name] = vertexIndex++;
				}
				else
				{
					vertexPoints.Add(edge.PointAlongEdge(1 - ratio));	
					newEdgeVertices[edge.Name + "-Pair"] = vertexIndex++;
				}
				vertexRoles.Add(Roles.New);
			}

			foreach (var face in Faces)
			{
				var edges = face.GetHalfedges();
				for (var i = 0; i < edges.Count; i++)
				{
					var edge = edges[i];
					var direction = (face.Centroid - edge.Midpoint) * 2;
					var pointOnEdge = vertexPoints[newEdgeVertices[edge.Name]];
					vertexPoints.Add(Vector3.LerpUnclamped(pointOnEdge, pointOnEdge + direction, ratio));
					vertexRoles.Add(Roles.NewAlt);
					newInnerVertices[edge.Name] = vertexIndex++;
				}
			}

			// Generate hexagonal faces and central face
			foreach (var face in Faces)
			{

				var centralFace = new int[face.Sides];
				var edge = face.Halfedge;

				for (var i = 0; i < face.Sides; i++)
				{
					
					string edgeNextPairName;
					if (edge.Next.Pair != null) {edgeNextPairName = edge.Next.Pair.Name;}
					else {edgeNextPairName = edge.Next.Name + "-Pair";}
					
					var hexagon = new[]
					{
						existingVertices[edge.Vertex.Position],
						newEdgeVertices[edgeNextPairName],
						newEdgeVertices[edge.Next.Name],
						newInnerVertices[edge.Next.Name],
						newInnerVertices[edge.Name],
						newEdgeVertices[edge.Name],
					};
					faceIndices.Add(hexagon);
					
					// Alternate roles but only for faces with an even number of sides
					if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.New);}
					else {faceRoles.Add(Roles.NewAlt);}
					
					centralFace[i] = newInnerVertices[edge.Name];
					edge = edge.Next;
				}

				faceIndices.Add(centralFace);
				faceRoles.Add(Roles.Existing);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		public ConwayPoly Volute(float ratio = 0.33333333f)
		{
			return Whirl(ratio).Dual();
		}

		#endregion

		#region geometry methods

		public ConwayPoly VertexScale(float scale, FaceSelections vertexsel)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = ListFacesByVertexIndices();

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var vertex = Vertices[vertexIndex];
				var includeVertex = IncludeVertex(vertexIndex, vertexsel);
				vertexPoints.Add(includeVertex ? vertex.Position * (scale + 1) : vertex.Position);
			}

			return new ConwayPoly(vertexPoints, faceIndices, FaceRoles, VertexRoles);
		}


		public ConwayPoly FaceScale(float scale, FaceSelections facesel)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var includeFace = IncludeFace(faceIndex, facesel);
				int c = vertexPoints.Count;

				vertexPoints.AddRange(face.GetVertices()
					.Select(v =>
						includeFace ? Vector3.LerpUnclamped(face.Centroid, v.Position, scale + 1) : v.Position));
				var faceVerts = new List<int>();
				for (int ii = 0; ii < face.GetVertices().Count; ii++)
				{
					faceVerts.Add(c + ii);
				}

				faceIndices.Add(faceVerts);
				faceRoles.Add(includeFace ? Roles.Existing : Roles.Ignored);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, VertexRoles);
		}

		public ConwayPoly FaceRotate(float angle, FaceSelections facesel, int axis = 0)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var includeFace = IncludeFace(faceIndex, facesel);

				int c = vertexPoints.Count;
				var faceVertices = new List<int>();

				c = vertexPoints.Count;

				var pivot = face.Centroid;
				Vector3 direction = face.Normal;
				switch (axis)
				{
					case 1:
						direction = Vector3.Cross(face.Normal, Vector3.up);
						break;
					case 2:
						direction = Vector3.Cross(face.Normal, Vector3.forward);
						break;
				}

				var rot = Quaternion.AngleAxis(angle, direction);

				vertexPoints.AddRange(
					face.GetVertices().Select(
						v => includeFace ? pivot + rot * (v.Position - pivot) : v.Position
					)
				);
				faceVertices = new List<int>();
				for (int ii = 0; ii < face.GetVertices().Count; ii++)
				{
					faceVertices.Add(c + ii);
				}

				faceIndices.Add(faceVertices);
				faceRoles.Add(faceRoles[faceIndex]);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, VertexRoles);
		}

		public ConwayPoly FaceRemove(FaceSelections facesel, bool invertLogic)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var includeFace = IncludeFace(faceIndex, facesel);
				includeFace = invertLogic ? includeFace : !includeFace;
				if (includeFace)
				{
					int c = vertexPoints.Count;
					var faceVertices = new List<int>();

					c = vertexPoints.Count;
					vertexPoints.AddRange(face.GetVertices().Select(v => v.Position));
					faceVertices = new List<int>();
					for (int ii = 0; ii < face.GetVertices().Count; ii++)
					{
						faceVertices.Add(c + ii);
						vertexRoles.Add(Roles.Existing);
					}

					faceIndices.Add(faceVertices);
					faceRoles.Add(FaceRoles[faceIndex]);
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		/// <summary>
		/// Offsets a mesh by moving each vertex by the specified distance along its normal vector.
		/// </summary>
		/// <param name="offset">Offset distance</param>
		/// <returns>The offset mesh</returns>
		public ConwayPoly Offset(double offset)
		{
			var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => offset).ToList();
			return Offset(offsetList);
		}

		public ConwayPoly Offset(double offset, FaceSelections facesel)
		{

			// This will only work if the faces are split and don't share vertices

			var offsetList = new List<double>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var vertexOffset = IncludeFace(faceIndex, facesel) ? offset : 0;
				foreach (var vertex in Faces[faceIndex].GetVertices())
				{
					offsetList.Add(vertexOffset);
				}
			}

			return Offset(offsetList);
		}

		public ConwayPoly Offset(List<double> offset)
		{

			Vector3[] points = new Vector3[Vertices.Count];

			for (int i = 0; i < Vertices.Count && i < offset.Count; i++)
			{
				points[i] = Vertices[i].Position + Vertices[i].Normal * (float) offset[i];
			}

			return new ConwayPoly(points, ListFacesByVertexIndices(), FaceRoles, VertexRoles);
		}

		/// <summary>
		/// Thickens each mesh edge in the plane of the mesh surface.
		/// </summary>
		/// <param name="offset">Distance to offset edges in plane of adjacent faces</param>
		/// <param name="boundaries">If true, attempt to ribbon boundary edges</param>
		/// <returns>The ribbon mesh</returns>
		public ConwayPoly Ribbon(float offset, Boolean boundaries, float smooth)
		{

			ConwayPoly ribbon = Duplicate();
			var orig_faces = ribbon.Faces.ToArray();

			List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();

			// create new "vertex" faces
			List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
			for (int k = 0; k < Vertices.Count; k++)
			{
				Vertex v = ribbon.Vertices[k];
				List<Vertex> new_vertices = new List<Vertex>();
				List<Halfedge> halfedges = incidentEdges[k];
				Boolean boundary = halfedges[0].Next.Pair != halfedges[halfedges.Count - 1];

				// if the edge loop around this vertex is open, close it with 'temporary edges'
				if (boundaries && boundary)
				{
					Halfedge a, b;
					a = halfedges[0].Next;
					b = halfedges[halfedges.Count - 1];
					if (a.Pair == null)
					{
						a.Pair = new Halfedge(a.Prev.Vertex) {Pair = a};
					}

					if (b.Pair == null)
					{
						b.Pair = new Halfedge(b.Prev.Vertex) {Pair = b};
					}

					a.Pair.Next = b.Pair;
					b.Pair.Prev = a.Pair;
					a.Pair.Prev = a.Pair.Prev ?? a; // temporary - to allow access to a.Pair's start/end vertices
					halfedges.Add(a.Pair);
				}

				foreach (Halfedge edge in halfedges)
				{
					if (halfedges.Count < 2)
					{
						continue;
					}

					Vector3 normal = edge.Face != null ? edge.Face.Normal : Vertices[k].Normal;
					Halfedge edge2 = edge.Next;

					var o1 = new Vertex(Vector3.Cross(normal, edge.Vector).normalized * offset);
					var o2 = new Vertex(Vector3.Cross(normal, edge2.Vector).normalized * offset);

					if (edge.Face == null)
					{
						// boundary condition: create two new vertices in the plane defined by the vertex normal
						Vertex v1 = new Vertex(v.Position + (edge.Vector * (1 / edge.Vector.magnitude) * -offset) +
						                       o1.Position);
						Vertex v2 = new Vertex(v.Position + (edge2.Vector * (1 / edge2.Vector.magnitude) * offset) +
						                       o2.Position);
						ribbon.Vertices.Add(v2);
						ribbon.Vertices.Add(v1);
						new_vertices.Add(v2);
						new_vertices.Add(v1);
						Halfedge c = new Halfedge(v2, edge2, edge, null);
						edge.Next = c;
						edge2.Prev = c;
					}
					else
					{
						// internal condition: offset each edge in the plane of the shared face and create a new vertex where they intersect eachother
						// TODO
						Line l1 = new Line(new Vertex(edge.Vertex.Position + o1.Position),
							new Vertex(edge.Prev.Vertex.Position + o1.Position));
						Line l2 = new Line(new Vertex(edge2.Vertex.Position + o2.Position),
							new Vertex(edge2.Prev.Vertex.Position + o2.Position));
						Vector3 intersection;
						l1.Intersect(out intersection, l2);
						ribbon.Vertices.Add(new Vertex(intersection));
						new_vertices.Add(new Vertex(intersection));
					}
				}

				if ((!boundaries && boundary) == false) // only draw boundary node-faces in 'boundaries' mode
					ribbon.Faces.Add(new_vertices);
				all_new_vertices.Add(new_vertices);
			}

			// change edges to reference new vertices
			for (int k = 0; k < Vertices.Count; k++)
			{
				Vertex v = ribbon.Vertices[k];
				if (all_new_vertices[k].Count < 1)
				{
					continue;
				}

				int c = 0;
				foreach (Halfedge edge in incidentEdges[k])
				{
					if (!ribbon.Halfedges.SetVertex(edge, all_new_vertices[k][c++]))
						edge.Vertex = all_new_vertices[k][c];
				}

				//v.Halfedge = null; // unlink from halfedge as no longer in use (culled later)
				// note: new vertices don't link to any halfedges in the mesh until later
			}

			// cull old vertices
			ribbon.Vertices.RemoveRange(0, Vertices.Count);

			// use existing edges to create 'ribbon' faces
			MeshHalfedgeList temp = new MeshHalfedgeList();
			for (int i = 0; i < Halfedges.Count; i++)
			{
				temp.Add(ribbon.Halfedges[i]);
			}

			List<Halfedge> items = temp.GetUnique();

			foreach (Halfedge halfedge in items)
			{
				if (halfedge.Pair != null)
				{
					// insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
					if (smooth > 0.0)
					{
						if (smooth > 0.5)
						{
							smooth = 0.5f;
						}

						Vertex[] newVertices = new Vertex[]
						{
							new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
							new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
							new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
							new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
						};
						ribbon.Vertices.AddRange(newVertices);
						Vertex[] new_vertices1 = new Vertex[]
						{
							halfedge.Vertex,
							newVertices[0],
							newVertices[3],
							halfedge.Pair.Prev.Vertex
						};
						Vertex[] new_vertices2 = new Vertex[]
						{
							newVertices[1],
							halfedge.Prev.Vertex,
							halfedge.Pair.Vertex,
							newVertices[2]
						};
						ribbon.Faces.Add(newVertices);
						ribbon.Faces.Add(new_vertices1);
						ribbon.Faces.Add(new_vertices2);
					}
					else
					{
						Vertex[] newVertices = new Vertex[]
						{
							halfedge.Vertex,
							halfedge.Prev.Vertex,
							halfedge.Pair.Vertex,
							halfedge.Pair.Prev.Vertex
						};

						ribbon.Faces.Add(newVertices);
					}
				}
			}

			// remove original faces, leaving just the ribbon
			//var orig_faces = Enumerable.Range(0, Faces.Count).Select(i => ribbon.Faces[i]);
			foreach (Face item in orig_faces)
			{
				ribbon.Faces.Remove(item);
			}

			// search and link pairs
			ribbon.Halfedges.MatchPairs();

			return ribbon;
		}

		/// <summary>
		/// Gives thickness to mesh faces by offsetting the mesh and connecting naked edges with new faces.
		/// </summary>
		/// <param name="distance">Distance to offset the mesh (thickness)</param>
		/// <param name="symmetric">Whether to extrude in both (-ve and +ve) directions</param>
		/// <returns>The extruded mesh (always closed)</returns>
		public ConwayPoly Extrude(double distance, bool symmetric)
		{
			var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => distance).ToList();
			return Extrude(offsetList, symmetric);
		}

		//TODO Use list parameter to implement face selection
		public ConwayPoly Extrude(List<double> distance, bool symmetric)
		{

			ConwayPoly result, top;

			if (symmetric)
			{
				result = Offset(distance.Select(d => 0.5 * d).ToList());
				top = Offset(distance.Select(d => -0.5 * d).ToList());
			}
			else
			{
				result = Duplicate();
				top = Offset(distance);
			}

			result.Halfedges.Flip();

			// append top to ext (can't use Append() because copy would reverse face loops)
			foreach (var v in top.Vertices) result.Vertices.Add(v);
			foreach (var h in top.Halfedges) result.Halfedges.Add(h);
			foreach (var f in top.Faces)
			{
				result.Faces.Add(f);
				result.FaceRoles.Add(Roles.New);
			}


			// get indices of naked halfedges in source mesh
			var naked = Halfedges.Select((item, index) => index).Where(i => Halfedges[i].Pair == null).ToList();

			if (naked.Count > 0)
			{
				int n = Halfedges.Count;
				int failed = 0;
				foreach (var i in naked)
				{
					Vertex[] vertices =
					{
						result.Halfedges[i].Vertex,
						result.Halfedges[i].Prev.Vertex,
						result.Halfedges[i + n].Vertex,
						result.Halfedges[i + n].Prev.Vertex
					};
					if (result.Faces.Add(vertices) == false)
					{
						failed++;
					}
					else
					{
						result.FaceRoles.Add(Roles.NewAlt);
					}
				}
			}

			result.Halfedges.MatchPairs();

			return result;
		}

		public void ScalePolyhedra(float scale = 1)
		{

			if (Vertices.Count > 0)
			{

				// Find the furthest vertex
				Vertex max = Vertices.OrderByDescending(x => x.Position.magnitude).FirstOrDefault();
				float unitScale = 1.0f / max.Position.magnitude;

				// TODO Ideal use case for Linq if I could get my head round the type-wrangling needed
				foreach (Vertex v in Vertices)
				{
					v.Position = v.Position * unitScale * scale;
				}
			}

		}

		public static ConwayPoly MakeGrid(int rows = 5, int cols = 5, float rowScale = .3f, float colScale = .3f)
		{
			float rowOffset = rows * rowScale * 0.5f;
			float colOffset = cols * colScale * 0.5f;

			// Count fences not fence poles
			rows++;
			cols++;
			
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<List<int>>();


			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < cols; col++)
				{
					var pos = new Vector3(-rowOffset + row * rowScale, 0, -colOffset + col * colScale);
					vertexPoints.Add(pos);
				}
			}
			
			for (int row = 1; row < rows; row++)
			{
				for (int col = 1; col < cols; col++)
				{
					int corner = (row * rows) + col;
					var face = new List<int>()
					{
						corner,
						corner - 1,
						corner - rows - 1,
						corner - rows
					};
					faceIndices.Add(face);
				}

			}

			var faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count);
			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

		#endregion

		#region canonicalize

		public void SetVertexPositions(List<Vector3> newPositions)
		{
			for (var i = 0; i < Vertices.Count; i++)
			{
				Vertices[i].Position = newPositions[i];
			}
		}

		private static double MAX_VERTEX_CHANGE = 1.0;

		/**
		 * A port of the "reciprocalN" function written by George Hart.
		 *
		 * @param poly The polyhedron to apply this canonicalization to.
		 * @return A list of the new vertices of the dual polyhedron.
		 */
		private static List<Vector3> ReciprocalVertices(ConwayPoly poly)
		{

			var newVertices = new List<Vector3>();

			foreach (var face in poly.Faces)
			{
				// Initialize values which will be updated in the loop below
				var centroid = face.Centroid;
				var normalSum = new Vector3();
				double avgEdgeDistance = 0.0;

				// Retrieve the indices of the vertices defining this face
				var faceVertices = face.GetVertices();

				// Keep track of the "previous" two vertices in CCW order
				var lastlastVertex = faceVertices[faceVertices.Count - 2];
				var lastVertex = faceVertices[faceVertices.Count - 1];

				foreach (var vertex in faceVertices)
				{

					// Compute the normal of the plane defined by this vertex and
					// the previous two
					var v1 = lastlastVertex.Position;
					v1 -= lastVertex.Position;
					var v2 = vertex.Position;
					v2 -= lastVertex.Position;
					var normal = Vector3.Cross(v1, v2);
					normalSum += normal;

					// Compute distance from edge to origin
					avgEdgeDistance += PointLineDist(new Vector3(), lastlastVertex.Position, lastVertex.Position);

					// Update the previous vertices for the next iteration
					lastlastVertex = lastVertex;
					lastVertex = vertex;
				}

				normalSum = normalSum.normalized;
				avgEdgeDistance /= faceVertices.Count;

				var resultingVector = new Vector3();
				resultingVector = Vector3.Dot(centroid, normalSum) * normalSum;
				resultingVector *= Mathf.Pow(1.0f / resultingVector.magnitude, 2);
				resultingVector *= (1.0f + (float) avgEdgeDistance) / 2.0f;
				newVertices.Add(resultingVector);
			}

			return newVertices;
		}

		private static double PointLineDist(Vector3 lineDir, Vector3 linePnt, Vector3 pnt)
		{
			lineDir.Normalize(); //this needs to be a unit vector
			var v = pnt - linePnt;
			var d = Vector3.Dot(v, lineDir);
			return (linePnt + lineDir * d).magnitude;
		}

		/**
		 * Modifies a polyhedron's vertices such that faces are closer to planar.
		 * The more iterations, the closer the faces are to planar. If a vertex
		 * moves by an unexpectedly large amount, or if the new vertex position
		 * has an NaN component, the algorithm automatically terminates.
		 *
		 * @param poly          The polyhedron whose faces to planarize.
		 * @param numIterations The number of iterations to planarize for.
		 */
		public static void Planarize(ConwayPoly poly, int numIterations)
		{

			var dual = poly.Dual();

			for (int i = 0; i < numIterations; i++)
			{

				var newDualPositions = ReciprocalVertices(poly);
				dual.SetVertexPositions(newDualPositions);
				var newPositions = ReciprocalVertices(dual);

				double maxChange = 0.0;

				for (int j = 0; j < poly.Vertices.Count; j++)
				{
					var newPos = poly.Vertices[j].Position;
					var diff = newPos - poly.Vertices[j].Position;
					maxChange = Math.Max(maxChange, diff.magnitude);
				}

				// Check if an error occurred in computation. If so, terminate
				// immediately. This likely occurs when faces are already planar.
				if (Double.IsNaN(newPositions[0].x) || Double.IsNaN(newPositions[0].y) ||
				    Double.IsNaN(newPositions[0].z))
				{
					break;
				}

				// Check if the position changed by a significant amount so as to
				// be erroneous. If so, terminate immediately
				if (maxChange > MAX_VERTEX_CHANGE)
				{
					break;
				}

				poly.SetVertexPositions(newPositions);
			}

		}

		/**
		 * Modifies a polyhedron's vertices such that faces are closer to planar.
		 * When no vertex moves more than the given threshold, the algorithm
		 * terminates.
		 *
		 * @param poly      The polyhedron to canonicalize.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @return The number of iterations that were executed.
		 */
		public static int Planarize(ConwayPoly poly, double threshold)
		{
			return _Canonicalize(poly, threshold, true);
		}

		/**
		 * A port of the "reciprocalC" function written by George Hart. Reflects
		 * the centers of faces across the unit sphere.
		 * 
		 * @param poly The polyhedron whose centers to invert.
		 * @return The list of inverted face centers.
		 */
		private static List<Vector3> ReciprocalCenters(ConwayPoly poly)
		{

			var faceCenters = new List<Vector3>();

			foreach (Face face in poly.Faces)
			{
				var newCenter = face.Centroid;
				newCenter *= 1.0f / Mathf.Pow(newCenter.magnitude, 2);
				faceCenters.Add(newCenter);
			}

			return faceCenters;
		}

		/**
		 * Canonicalizes a polyhedron by adjusting its vertices iteratively.
		 * 
		 * @param poly          The polyhedron whose vertices to adjust.
		 * @param numIterations The number of iterations to adjust for.
		 */
		public static void Adjust(ConwayPoly poly, int numIterations)
		{

			var dual = poly.Dual();

			for (int i = 0; i < numIterations; i++)
			{
				var newDualPositions = ReciprocalCenters(poly);
				dual.SetVertexPositions(newDualPositions);
				var newPositions = ReciprocalCenters(dual);
				poly.SetVertexPositions(newPositions);
			}

		}

		/**
		 * Canonicalizes a polyhedron by adjusting its vertices iteratively. When
		 * no vertex moves more than the given threshold, the algorithm terminates.
		 * 
		 * @param poly      The polyhedron whose vertices to adjust.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @return The number of iterations that were executed.
		 */
		public static int Adjust(ConwayPoly poly, double threshold)
		{
			return _Canonicalize(poly, threshold, false);
		}

		/**
		 * A helper method for threshold-based termination in both planarizing and
		 * adjusting. If a vertex moves by an unexpectedly large amount, or if the
		 * new vertex position has an NaN component, the algorithm automatically
		 * terminates.
		 *
		 * @param poly      The polyhedron to canonicalize.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @param planarize True if we are planarizing, false if we are adjusting.
		 * @return The number of iterations that were executed.
		 */
		private static int _Canonicalize(ConwayPoly poly, double threshold, bool planarize)
		{

			var dual = poly.Dual();
			var currentPositions = poly.Vertices.Select(x => x.Position).ToList();

			int iterations = 0;

			while (true)
			{

				var newDualPositions = planarize ? ReciprocalVertices(poly) : ReciprocalCenters(poly);
				dual.SetVertexPositions(newDualPositions);
				var newPositions = planarize ? ReciprocalVertices(dual) : ReciprocalCenters(dual);

				double maxChange = 0.0;
				for (int i = 0; i < currentPositions.Count; i++)
				{
					var newPos = poly.Vertices[i].Position;
					var diff = newPos - currentPositions[i];
					maxChange = Math.Max(maxChange, diff.magnitude);
				}

				// Check if an error occurred in computation. If so, terminate
				// immediately
				if (Double.IsNaN(newPositions[0].x) || Double.IsNaN(newPositions[0].y) ||
				    Double.IsNaN(newPositions[0].z))
				{
					break;
				}

				// Check if the position changed by a significant amount so as to
				// be erroneous. If so, terminate immediately
				if (planarize && maxChange > MAX_VERTEX_CHANGE)
				{
					break;
				}

				poly.SetVertexPositions(newPositions);

				if (maxChange < threshold)
				{
					break;
				}

				currentPositions = poly.Vertices.Select(x => x.Position).ToList();
				iterations++;
			}

			return iterations;
		}


		/**
		* Canonicalizes this polyhedron for the given number of iterations.
		* See util.Canonicalize for more details. Performs "adjust" followed
		* by "planarize".
		* 
		* @param iterationsAdjust    The number of iterations to "adjust" for.
		* @param iterationsPlanarize The number of iterations to "planarize" for.
		* @return The canonicalized version of this polyhedron.
		*/
		public ConwayPoly Canonicalize(int iterationsAdjust, int iterationsPlanarize)
		{
			var previousFaceRoles = FaceRoles;
			var canonicalized = Duplicate();
			if (iterationsAdjust > 0) Adjust(canonicalized, iterationsAdjust);
			if (iterationsPlanarize > 0) Planarize(canonicalized, iterationsPlanarize);
			canonicalized.FaceRoles = previousFaceRoles;
			return canonicalized;
		}

		/**
		 * Canonicalizes this polyhedron until the change in position does not
		 * exceed the given threshold. That is, the algorithm terminates when no vertex
		 * moves more than the threshold after one iteration.
		 * 
		 * @param thresholdAdjust    The threshold for change in one "adjust"
		 *                           iteration.
		 * @param thresholdPlanarize The threshold for change in one "planarize"
		 *                           iteration.
		 * @return The canonicalized version of this polyhedron.
		 */
		public ConwayPoly Canonicalize(double thresholdAdjust, double thresholdPlanarize)
		{
			var previousFaceRoles = FaceRoles;
			ConwayPoly canonicalized = Duplicate();
			if (thresholdAdjust > 0) Adjust(canonicalized, thresholdAdjust);
			if (thresholdPlanarize > 0) Planarize(canonicalized, thresholdPlanarize);
			canonicalized.FaceRoles = previousFaceRoles;
			return canonicalized;
		}

		#endregion

		#region methods

		/// <summary>
		/// A string representation of the mesh.
		/// </summary>
		/// <returns>a string representation of the mesh</returns>
		public override string ToString()
		{
			return base.ToString() + String.Format(" (V:{0} F:{1})", Vertices.Count, Faces.Count);
		}

		/// <summary>
		/// Gets the positions of all mesh vertices. Note that points are duplicated.
		/// </summary>
		/// <returns>a list of vertex positions</returns>
		public Vector3[] ListVerticesByPoints()
		{
			Vector3[] points = new Vector3[Vertices.Count];
			for (int i = 0; i < Vertices.Count; i++)
			{
				Vector3 pos = Vertices[i].Position;
				points[i] = new Vector3(pos.x, pos.y, pos.z);
			}

			return points;
		}

		/// <summary>
		/// Gets the indices of vertices in each face loop (i.e. index face-vertex data structure).
		/// Used for duplication and conversion to other mesh types, such as Rhino's.
		/// </summary>
		/// <returns>An array of lists of vertex indices.</returns>
		public List<int>[] ListFacesByVertexIndices()
		{

			var fIndex = new List<int>[Faces.Count];
			var vlookup = new Dictionary<string, int>();

			for (int i = 0; i < Vertices.Count; i++)
			{
				vlookup.Add(Vertices[i].Name, i);
			}

			for (int i = 0; i < Faces.Count; i++)
			{
				List<int> vertIdx = new List<int>();
				foreach (Vertex v in Faces[i].GetVertices())
				{
					vertIdx.Add(vlookup[v.Name]);
				}

				fIndex[i] = vertIdx;
			}

			return fIndex;
		}

		public bool HasNaked()
		{
			return Halfedges.Select((item, ii) => ii).Where(i => Halfedges[i].Pair == null).ToList().Count > 0;
		}

//        TODO
//        public List<Polyline> ToClosedPolylines() {
//            List<Polyline> polylines = new List<Polyline>(Faces.Count);
//            foreach (Face f in Faces) {
//                polylines.Add(f.ToClosedPolyline());
//            }
//
//            return polylines;
//        }
//
//        public List<Line> ToLines() {
//            return Halfedges.GetUnique().Select(h => new Rhino.Geometry.Line(h.Prev.BVertex.Position, h.BVertex.Position))
//                .ToList();
//        }

		/// <summary>
		/// Appends a copy of another mesh to this one.
		/// </summary>
		/// <param name="other">Mesh to append to this one.</param>
		public void Append(ConwayPoly other)
		{
			ConwayPoly dup = other.Duplicate();

			Vertices.AddRange(dup.Vertices);
			foreach (Halfedge edge in dup.Halfedges)
			{
				Halfedges.Add(edge);
			}

			foreach (Face face in dup.Faces)
			{
				Faces.Add(face);
			}
			VertexRoles.AddRange(Enumerable.Repeat(Roles.NewAlt, dup.VertexRoles.Count));
			FaceRoles.AddRange(Enumerable.Repeat(Roles.NewAlt, dup.FaceRoles.Count));
		}

		private int FaceSelectionToSides(FaceSelections facesel)
		{
			switch (facesel)
			{
				case FaceSelections.ThreeSided:
					return 3;
				case FaceSelections.FourSided:
					return 4;
				case FaceSelections.FiveSided:
					return 5;
				case FaceSelections.SixSided:
					return 6;
				case FaceSelections.SevenSided:
					return 7;
				case FaceSelections.EightSided:
					return 8;
			}

			return 0;
		}

		public bool IncludeFace(int faceIndex, FaceSelections facesel)
		{
			switch (facesel)
			{
				case FaceSelections.All:
					return true;
				case FaceSelections.Existing:
					return FaceRoles[faceIndex] == Roles.Existing;
				case FaceSelections.Ignored:
					return FaceRoles[faceIndex] == Roles.Ignored;
				case FaceSelections.New:
					return FaceRoles[faceIndex] == Roles.New;
				case FaceSelections.NewAlt:
					return FaceRoles[faceIndex] == Roles.NewAlt;
				case FaceSelections.AllNew:
					return FaceRoles[faceIndex] == Roles.New || FaceRoles[faceIndex] == Roles.NewAlt;
			}

			return Faces[faceIndex].Sides == FaceSelectionToSides(facesel);
		}

		public bool IncludeVertex(int vertexIndex, FaceSelections vertexsel)
		{
			switch (vertexsel)
			{
				case FaceSelections.All:
					return true;
				// TODO
				case FaceSelections.Existing:
					return VertexRoles[vertexIndex] == Roles.Existing;
				case FaceSelections.Ignored:
					return VertexRoles[vertexIndex] == Roles.Ignored;
				case FaceSelections.New:
					return VertexRoles[vertexIndex] == Roles.New;
				case FaceSelections.NewAlt:
					return VertexRoles[vertexIndex] == Roles.NewAlt;
			}

			return Vertices[vertexIndex].Halfedges.Count == FaceSelectionToSides(vertexsel);
		}

		#endregion

	}
}