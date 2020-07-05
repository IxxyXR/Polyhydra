using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wythoff;
using Debug = UnityEngine.Debug;
using Random = System.Random;


namespace Conway
{

	/// <summary>
	/// A class for manifold meshes which uses the Halfedge data structure.
	/// </summary>

	public class ConwayPoly
	{

		#region properties

		private Random random;
		private const float TOLERANCE = 0.02f;
		private PointOctree<Vertex> octree;

		public struct BasePolyhedraInfo
		{
			public int P;
			public int Q;
		}

		public enum TagType
		{
			Introvert,
			Extrovert
		}

		public BasePolyhedraInfo basePolyhedraInfo = new BasePolyhedraInfo();

		public List<Roles> FaceRoles;
		public List<Roles> VertexRoles;
		public List<HashSet<Tuple<string, TagType>>> FaceTags;


		public MeshHalfedgeList Halfedges { get; private set; }
		public MeshVertexList Vertices { get; set; }
		public MeshFaceList Faces { get; private set; }

		public enum Roles
		{
			Ignored,
			Existing,
			New,
			NewAlt,
			ExistingAlt,
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

		#region constructors

		public ConwayPoly()
		{
			Halfedges = new MeshHalfedgeList(this);
			Vertices = new MeshVertexList(this);
			Faces = new MeshFaceList(this);
			FaceRoles = new List<Roles>();
			VertexRoles = new List<Roles>();
			random = new Random();
			InitTags();
		}

		public ConwayPoly(WythoffPoly source, bool abortOnFailure=true) : this()
		{
			FaceRoles = new List<Roles>();
			VertexRoles = new List<Roles>();

			// Add vertices
			Vertices.Capacity = source.VertexCount;
			for (var i = 0; i < source.Vertices.Length; i++)
			{
				Vector p = source.Vertices[i];
				Vertices.Add(new Vertex(p.getVector3()));
				VertexRoles.Add(Roles.Existing);
			}

			// Add faces (and construct halfedges and store in hash table)
			for (var faceIndex = 0; faceIndex < source.faces.Count; faceIndex++)
			{
				var face = source.faces[faceIndex];
				var v = new Vertex[face.points.Count];

				for (int i = 0; i < face.points.Count; i++)
				{
					v[i] = Vertices[face.points[i]];
				}

				FaceRoles.Add((Roles) ((int) face.configuration % 5));


				if (!Faces.Add(v))
				{
					// Failed. Let's try flipping the face
					Array.Reverse(v);
					if (!Faces.Add(v))
					{
						if (abortOnFailure)
						{
							throw new InvalidOperationException("Failed even after flipping.");
						}
						else
						{
							Debug.LogWarning($"Failed even after flipping. ({v.Length} verts)");
							continue;
						}
					}
				}
			}

			// Find and link halfedge pairs
			Halfedges.MatchPairs();
			InitTags();
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
			InitTags();
		}

		private ConwayPoly(
			IEnumerable<Vector3> verticesByPoints,
			IEnumerable<IEnumerable<int>> facesByVertexIndices,
			IEnumerable<Roles> faceRoles,
			IEnumerable<Roles> vertexRoles,
			List<HashSet<Tuple<string, TagType>>> newFaceTags
		) : this(verticesByPoints, facesByVertexIndices, faceRoles, vertexRoles)
		{
			FaceTags = newFaceTags;
		}

		#endregion

		#region tag methods

		public void InitTags()
		{
			FaceTags = new List<HashSet<Tuple<string, TagType>>>();
			for (var i = 0; i < Faces.Count; i++)
			{
				FaceTags.Add(new HashSet<Tuple<string, TagType>>());
			}
		}

		public void TagFaces(string tags, FaceSelections facesel)
		{
			var tagList = StringToTagList(tags, true);
			if (FaceTags == null || FaceTags.Count==0)
			{
				InitTags();
			}

			for (var i = 0; i < Faces.Count; i++)
			{
				var tagset = FaceTags[i];
				if (IncludeFace(i, facesel))
				{
					tagset.UnionWith(tagList);
				}
			}
		}

		#endregion

		#region conway methods

		/// <summary>
		/// Conway's dual operator
		/// </summary>
		/// <returns>the dual as a new mesh</returns>
		public ConwayPoly Dual()
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Create vertices from faces
			var vertexPoints = new List<Vector3>(Faces.Count);
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var f = Faces[faceIndex];
				vertexPoints.Add(f.Centroid);
				vertexRoles.Add(Roles.New);
			}

			// Create sublist of non-boundary vertices
			var naked = new Dictionary<Guid, bool>(Vertices.Count); // vertices (name, boundary?)
			// boundary halfedges (name, index of point in new mesh)
			var hlookup = new Dictionary<(Guid, Guid)?, int>(Halfedges.Count);

			for (var i = 0; i < Halfedges.Count; i++)
			{
				var he = Halfedges[i];
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

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var vertex = Vertices[vertexIndex];
				var fIndex = new List<int>();

				var vertexFaces = vertex.GetVertexFaces();
				for (var faceIndex = 0; faceIndex < vertexFaces.Count; faceIndex++)
				{
					Face f = vertexFaces[faceIndex];
					fIndex.Add(flookup[f.Name]);
				}

				if (naked.ContainsKey(vertex.Name) && naked[vertex.Name])
				{
					// Handle boundary vertices...
					var h = vertex.Halfedges;
					if (h.Count > 0)
					{
						// Add points on naked edges and the naked vertex
						fIndex.Add(hlookup[h.Last().Name]);
						fIndex.Add(vertexPoints.Count);
						fIndex.Add(hlookup[h.First().Next.Name]);
						vertexPoints.Add(vertex.Position);
						vertexRoles.Add(Roles.New);
					}
				}

				faceIndices.Add(fIndex);
				try
				{
					faceRoles.Add(VertexRoles[vertexIndex]);
				}
				catch(Exception e)
				{
					Debug.LogWarning($"Dual op failed to set face role based on existing vertex role. Faces.Count: {Faces.Count} Verts: {Vertices.Count} old VertexRoles.Count: {VertexRoles.Count} i: {vertexIndex}");
//					throw;
				}


				var vertexFaceIndices = vertexFaces.Select(f => Faces.IndexOf(f));
				var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi].Where(t=>t.Item2==TagType.Extrovert));
				var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) => {rs.UnionWith(i); return rs;});
				newFaceTags.Add(newFaceTagSet);

			}

			// If we're ended up with an invalid number of roles then just set them all to 'New'
			if (faceRoles.Count!=faceIndices.Count) faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count).ToList();
			if (vertexRoles.Count!=vertexPoints.Count) vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count).ToList();

			return new ConwayPoly(vertexPoints, faceIndices.ToArray(), faceRoles, vertexRoles, newFaceTags);
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

		// public ConwayPoly ExperimentalZipThing(float amount) // Todo give this a name and fill in missing faces
		// {
		// 	var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();
		//
		// 	var faceRoles = new List<Roles>();
		// 	var vertexRoles = new List<Roles>();
		//
		// 	// Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
		// 	var vertexPoints = new List<Vector3>(); // vertices as points
		// 	var newInnerVerts = new Dictionary<string, int>();
		// 	int count = 0;
		//
		// 	var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
		// 	// faces to faces
		// 	foreach (var face in Faces)
		// 	{
		// 		var centroid = face.Centroid;
		// 		foreach (var edge in face.GetHalfedges())
		// 		{
		// 			vertexPoints.Add(Vector3.Lerp(edge.Midpoint, centroid, amount));
		// 			vertexRoles.Add(Roles.New);
		// 			newInnerVerts.Add(edge.Name, count++);
		// 		}
		// 		faceIndices.Add(face.GetHalfedges().Select(edge => newInnerVerts[edge.Name]));
		// 		faceRoles.Add(Roles.Existing);
		// 		newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
		// 	}
		//
		// 	// vertices to faces
		// 	foreach (var vertex in Vertices)
		// 	{
		// 		var he = vertex.Halfedges;
		// 		if (he.Count == 0) continue; // no halfedges (naked vertex, ignore)
		// 		var list = he.Select(edge => newInnerVerts[edge.Name]); // halfedge indices for vertex-loop
		// 		if (he[0].Next.Pair == null)
		// 		{
		// 			// Handle boundary vertex, add itself and missing boundary halfedge
		// 			list = list.Concat(new[] {vertexPoints.Count, newInnerVerts[he[0].Next.Name]});
		// 			vertexPoints.Add(vertex.Position);
		// 			vertexRoles.Add(Roles.NewAlt);
		// 		}
		//
		// 		faceIndices.Add(list);
		// 		faceRoles.Add(Roles.New);
		// 		newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
		// 	}
		//
		// 	return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		// }

		public ConwayPoly Zip(float amount)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
			var vertexPoints = new List<Vector3>(); // vertices as points
			var newInnerVerts = new Dictionary<(Guid, Guid)?, int>();
			int count = 0;

			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
			// faces to faces
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var prevFaceTagSet = FaceTags[faceIndex];
				var centroid = face.Centroid;
				foreach (var edge in face.GetHalfedges())
				{
					vertexPoints.Add(Vector3.Lerp(edge.Midpoint, centroid, amount));
					vertexRoles.Add(Roles.New);
					newInnerVerts.Add(edge.Name, count++);
				}

				faceIndices.Add(face.GetHalfedges().Select(edge => newInnerVerts[edge.Name]));
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			// vertices to faces
			for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
			{
				var vertex = Vertices[vertIndex];
				var halfedges = vertex.Halfedges;
				if (halfedges.Count < 3) continue;
				var newVertexFace = new List<int>();
				foreach (var edge in halfedges)
				{
					newVertexFace.Add(newInnerVerts[edge.Name]);
					if (edge.Pair != null)
					{
						newVertexFace.Add(newInnerVerts[edge.Pair.Name]);
					}
					else
					{
						vertexPoints.Add(edge.Midpoint);
						vertexRoles.Add(Roles.NewAlt);
						newVertexFace.Add(vertexPoints.Count - 1);
					}
				}

				faceIndices.Add(newVertexFace);
				faceRoles.Add(Roles.New);

				var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
				var existingTagSets =
					vertexFaceIndices.Select(fi => FaceTags[fi].Where(t => t.Item2 == TagType.Extrovert));
				var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) =>
				{
					rs.UnionWith(i);
					return rs;
				});
				newFaceTags.Add(newFaceTagSet);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		/// <summary>
		/// Conway's ambo operator
		/// </summary>
		/// <returns>the ambo as a new mesh</returns>
		public ConwayPoly Ambo()
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
			var vertexPoints = new List<Vector3>(); // vertices as points
			var hlookup = new Dictionary<(Guid, Guid)?, int>();
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
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			// vertices to faces
			for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
			{
				var vertex = Vertices[vertIndex];
				var halfedges = vertex.Halfedges;
				if (halfedges.Count == 0) continue; // no halfedges (naked vertex, ignore)
				var newHalfedges = halfedges.Select(edge => hlookup[edge.Name]); // halfedge indices for vertex-loop
				if (halfedges[0].Next.Pair == null)
				{
					// Handle boundary vertex, add itself and missing boundary halfedge
					newHalfedges = newHalfedges.Concat(new[] {vertexPoints.Count, hlookup[halfedges[0].Next.Name]});
					vertexPoints.Add(vertex.Position);
					vertexRoles.Add(Roles.NewAlt);
				}

				faceIndices.Add(newHalfedges);
				faceRoles.Add(Roles.New);

				var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
				var existingTagSets =
					vertexFaceIndices.Select(fi => FaceTags[fi].Where(t => t.Item2 == TagType.Extrovert));
				var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) =>
				{
					rs.UnionWith(i);
					return rs;
				});
				newFaceTags.Add(newFaceTagSet);
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public static int ActualMod(int x, int m) // Fuck C# deciding that mod isn't actually mod
		{
			return (x % m + m) % m;
		}

		public ConwayPoly Truncate(float amount, FaceSelections vertexsel, bool randomize = false)
		{

			int GetVertID(Vertex v)
			{
				return Vertices.FindIndex(a => a == v);
			}

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			var vertexPoints = new List<Vector3>(); // vertices as points
			var ignoredVerts = new Dictionary<Guid, int>();
			var newVerts = new Dictionary<Vector3, int>();
			int count = 0;

			// TODO support random
			//if (randomize) amount = 1 - UnityEngine.Random.value/2f;


			for (var i = 0; i < Vertices.Count; i++)
			{
				var v = Vertices[i];
				if (IncludeVertex(i, vertexsel))
				{
					foreach (var edge in v.Halfedges)
					{

						Vector3 pos = edge.PointAlongEdge(amount);
						vertexPoints.Add(pos);
						vertexRoles.Add(Roles.New);
						newVerts[pos] = vertexPoints.Count - 1;

						if (edge.Pair == null)
						{
							Vector3 pos2 = edge.PointAlongEdge(1 - amount);
							vertexPoints.Add(pos2);
							vertexRoles.Add(Roles.New);
							newVerts[pos2] = vertexPoints.Count - 1;
						}
					}
				}
				else
				{
					vertexPoints.Add(v.Position);
					vertexRoles.Add(Roles.Ignored);
					ignoredVerts[v.Name] = vertexPoints.Count - 1;
				}
			}

			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices

			// faces to faces
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var centerFace = new List<int>();
				var faceEdges = face.GetHalfedges();
				for (var i = 0; i < faceEdges.Count; i++)
				{
					var edge = faceEdges[i];
					Vector3 pos1 = edge.PointAlongEdge(amount);
					var nextEdge = faceEdges[ActualMod((i + 1), faceEdges.Count)].Pair;
					Vector3 pos2;
					if (nextEdge != null)
					{
						pos2 = nextEdge.PointAlongEdge(amount);
					}
					else
					{
						pos2 = faceEdges[ActualMod((i + 1), faceEdges.Count)].PointAlongEdge(1 - amount);
					}

					if (IncludeVertex(GetVertID(edge.Vertex), vertexsel))
					{
						centerFace.Add(newVerts[pos1]);
						centerFace.Add(newVerts[pos2]);
					}
					else
					{
						centerFace.Add(ignoredVerts[edge.Vertex.Name]);
					}
				}

				if (centerFace.Count >= 3)
				{
					faceIndices.Add(centerFace);
					faceRoles.Add(Roles.Existing);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			// vertices to faces
			for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
			{
				var vertex = Vertices[vertIndex];
				if (!IncludeVertex(GetVertID(vertex), vertexsel)) continue;
				bool boundary = false;
				var edges = vertex.Halfedges;

				var vertexFace = new List<int>();
				for (var i = 0; i < edges.Count; i++)
				{
					var edge = edges[i];
					Vector3 pos;
					if (edge.Pair != null)
					{
						pos = edge.PointAlongEdge(amount);
					}
					else
					{
						// It's a reverse edge
						boundary = true;
						break;
						// pos = edge.PointAlongEdge(1 - amount);
					}

					vertexFace.Add(newVerts[pos]);
				}

				if (vertexFace.Count >= 3 && !boundary)
				{
					faceIndices.Add(vertexFace);
					faceRoles.Add(Roles.New);

					var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
					var existingTagSets =
						vertexFaceIndices.Select(fi => FaceTags[fi].Where(t => t.Item2 == TagType.Extrovert));
					var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) =>
					{
						rs.UnionWith(i);
						return rs;
					});
					newFaceTags.Add(newFaceTagSet);
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		public ConwayPoly Bevel(float amount, bool randomize=false)
		{
			return Bevel(amount, amount * 0.5f, 0, randomize);
		}
		
		public ConwayPoly Bevel(float amountP, float amountQ, float offset=0, bool randomize=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			var newVertexPoints = new List<Vector3>();
			var newInnerVertsL = new Dictionary<(Guid, Guid)?, int>();
			var newInnerVertsR = new Dictionary<(Guid, Guid)?, int>();

			foreach (var face in Faces)
			{
				var centroid = face.Centroid;

				foreach (var edge in face.GetHalfedges())
				{
					if (randomize)
					{
						amountP = 1 - UnityEngine.Random.value/2f;
						amountQ = 1 - UnityEngine.Random.value/2f;
					}

					var edgePointL = edge.PointAlongEdge(amountP);
					var innerPointL = Vector3.LerpUnclamped(edgePointL, centroid, amountQ);
					innerPointL += face.Normal * offset;
					newVertexPoints.Add(innerPointL);
					vertexRoles.Add(Roles.New);
					newInnerVertsL.Add(edge.Name, newVertexPoints.Count - 1);
					
					var edgePointR = edge.PointAlongEdge(1 - amountP);
					var innerPointR = Vector3.LerpUnclamped(edgePointR, centroid, amountQ);
					innerPointR += face.Normal * offset;
					newVertexPoints.Add(innerPointR);
					vertexRoles.Add(Roles.New);
					newInnerVertsR.Add(edge.Name, newVertexPoints.Count - 1);
				}
			}

			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices

			// faces to faces
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var newFace = new List<int>();
				foreach (var edge in face.GetHalfedges())
				{
					newFace.Add(newInnerVertsR[edge.Name]);
					newFace.Add(newInnerVertsL[edge.Name]);
				}

				faceIndices.Add(newFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
			{
				bool skip = false;
				var edges = Vertices[vertIndex].Halfedges;
				var list = new List<int>();
				foreach (var edge in edges)
				{
					if (edge.Pair == null)
					{
						skip = true;
						break;
					}

					list.Add(newInnerVertsL[edge.Name]);
					list.Add(newInnerVertsR[edge.Pair.Name]);
				}

				if (skip) continue;
				// list.Reverse();
				faceIndices.Add(list);
				faceRoles.Add(Roles.New);

				var vertexFaceIndices = Vertices[vertIndex].GetVertexFaces().Select(f => Faces.IndexOf(f));
				var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi].Where(t => t.Item2 == TagType.Extrovert));
				var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) =>
				{
					rs.UnionWith(i);
					return rs;
				});
				newFaceTags.Add(newFaceTagSet);
			}

			var edgeFlags = new HashSet<(Guid, Guid)?>();
			foreach (var edge in Halfedges)
			{
				if (edge.Pair == null) continue;
				if (edgeFlags.Contains(edge.PairedName)) continue;
				var list = new List<int>
				{
					newInnerVertsL[edge.Name],
					newInnerVertsR[edge.Name],
					newInnerVertsL[edge.Pair.Name],
					newInnerVertsR[edge.Pair.Name],
				};
				faceIndices.Add(list);
				faceRoles.Add(Roles.NewAlt);
				edgeFlags.Add(edge.PairedName);

				var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
				newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
				newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
				newFaceTags.Add(newFaceTagSet);
			}

			return new ConwayPoly(newVertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Ortho(float offset, bool randomize=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var existingVerts = new Dictionary<string, int>();
			var newVerts = new Dictionary<string, int>();
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Loop through old faces
			for (int i = 0; i < Faces.Count; i++)
			{
				var prevFaceTagSet = FaceTags[i];
				var oldFace = Faces[i];
				float offsetVal = (float) (offset * (randomize ? random.NextDouble() : 1));
				vertexPoints.Add(oldFace.Centroid + oldFace.Normal * offsetVal);
				vertexRoles.Add(Roles.New);
				int centroidIndex = vertexPoints.Count - 1;

				// Loop through each vertex on old face and create a new face for each
				for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
				{

					int seedVertexIndex;
					int midpointIndex;
					int prevMidpointIndex;

					string keyName;

					var thisFaceIndices = new List<int>();
					var edges = oldFace.GetHalfedges();

					var seedVertex = edges[j].Vertex;
					keyName = seedVertex.Name.ToString();
					if (existingVerts.ContainsKey(keyName))
					{
						seedVertexIndex = existingVerts[keyName];
					}
					else
					{
						vertexPoints.Add(seedVertex.Position - seedVertex.Normal * offsetVal);
						vertexRoles.Add(Roles.Existing);
						seedVertexIndex = vertexPoints.Count - 1;
						existingVerts[keyName] = seedVertexIndex;
					}

					
					
					
					var midpointVertex = edges[j].Midpoint;
					keyName = edges[j].PairedName.ToString();
					if (newVerts.ContainsKey(keyName))
					{
						midpointIndex = newVerts[keyName];
					}
					else
					{
						vertexPoints.Add(midpointVertex);
						vertexRoles.Add(Roles.NewAlt);
						midpointIndex = vertexPoints.Count - 1;
						newVerts[keyName] = midpointIndex;
					}

					
					
					
					var prevMidpointVertex = edges[j].Next.Midpoint;
					keyName = edges[j].Next.PairedName.ToString();

					if (newVerts.ContainsKey(keyName))
					{
						prevMidpointIndex = newVerts[keyName];
					}
					else
					{
						vertexPoints.Add(prevMidpointVertex);
						vertexRoles.Add(Roles.NewAlt);
						prevMidpointIndex = vertexPoints.Count - 1;
						newVerts[keyName] = prevMidpointIndex;
					}
					
					thisFaceIndices.Add(centroidIndex);
					thisFaceIndices.Add(midpointIndex);
					thisFaceIndices.Add(seedVertexIndex);
					thisFaceIndices.Add(prevMidpointIndex);

					faceIndices.Add(thisFaceIndices);
					// Alternate roles but only for faces with an even number of sides
					if (j % 2 == 0 || (j < Faces.Count && Faces[j].Sides % 2 != 0)){faceRoles.Add(Roles.New);}
					else {faceRoles.Add(Roles.NewAlt);}
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		public ConwayPoly Expand(float ratio = 0.33333333f)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var newVertices = new Dictionary<(Guid, Guid)?, int>();
			var edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			int vertexIndex = 0;

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				
				var edge = face.Halfedge;
				var centroid = face.Centroid;

				// Create a new face for each existing face
				var newInsetFace = new int[face.Sides];

				for (int i = 0; i < face.Sides; i++)
				{
					var vertex = edge.Vertex.Position;
					var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);
					vertexPoints.Add(newVertex);
					vertexRoles.Add(Roles.Existing);
					newInsetFace[i] = vertexIndex;
					newVertices[edge.Name] = vertexIndex++;
					edge = edge.Next;
				}

				faceIndices.Add(newInsetFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

			}
			
			// Add edge faces
			foreach (var edge in Halfedges)
			{
				if (!edgeFaceFlags.ContainsKey(edge.PairedName))
				{
					if (edge.Pair != null)
					{
						var edgeFace = new[]
						{
							newVertices[edge.Name],
							newVertices[edge.Prev.Name],
							newVertices[edge.Pair.Name],
							newVertices[edge.Pair.Prev.Name],
						};
						faceIndices.Add(edgeFace);
						faceRoles.Add(Roles.New);

						var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);
					}
					edgeFaceFlags[edge.PairedName] = true;
				}
			}

			for (var idx = 0; idx < Vertices.Count; idx++)
			{
				var vertex = Vertices[idx];
				var vertexFace = new List<int>();
				for (var j = 0; j < vertex.Halfedges.Count; j++)
				{
					var edge = vertex.Halfedges[j];
					vertexFace.Add(newVertices[edge.Name]);
				}

				if (vertexFace.Count >= 3)
				{
					faceIndices.Add(vertexFace.ToArray());
					faceRoles.Add(Roles.NewAlt);

					var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
					var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi].Where(t=>t.Item2==TagType.Extrovert));
					var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) => {rs.UnionWith(i); return rs;});
					newFaceTags.Add(newFaceTagSet);
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}
		
		public ConwayPoly Chamfer(float ratio = 0.33333333f)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newVertices = new Dictionary<(Guid, Guid)?, int>();
			var edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();
		
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();
			
			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}
			
			int vertexIndex = existingVertices.Count;

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				
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
					edge = edge.Next;
				}

				faceIndices.Add(newInsetFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

			}
			
			// Add edge faces
			foreach (var edge in Halfedges)
			{
				var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
				if (!edgeFaceFlags.ContainsKey(edge.PairedName))
				{
					edgeFaceFlags[edge.PairedName] = true;
					if (edge.Pair != null)
					{
						var edgeFace = new[]
						{
							existingVertices[edge.Vertex.Position],
							newVertices[edge.Name],
							newVertices[edge.Prev.Name],
							existingVertices[edge.Pair.Vertex.Position],
							newVertices[edge.Pair.Name],
							newVertices[edge.Pair.Prev.Name],
						};
						faceIndices.Add(edgeFace);
						faceRoles.Add(Roles.New);
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);
					}
				}
			}
			
			// Planarize new edge faces
			// TODO not perfect - we need an iterative algorithm
			edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();
			foreach (var edge in Halfedges)
			{
				if (edge.Pair==null) continue;

				if (!edgeFaceFlags.ContainsKey(edge.PairedName))
				{
					edgeFaceFlags[edge.PairedName] = true;

					float distance;

					var plane = new Plane();
					plane.Set3Points(
						vertexPoints[newVertices[edge.Name]],
						vertexPoints[newVertices[edge.Prev.Name]],
						vertexPoints[newVertices[edge.Pair.Name]]
					);

					var ray1 = new Ray(edge.Vertex.Position, edge.Vertex.Normal);
					plane.Raycast(ray1, out distance);
					vertexPoints[existingVertices[edge.Vertex.Position]] = ray1.GetPoint(distance);

					var ray2 = new Ray(edge.Pair.Vertex.Position, edge.Pair.Vertex.Normal);
					plane.Raycast(ray2, out distance);
					vertexPoints[existingVertices[edge.Pair.Vertex.Position]] = ray2.GetPoint(distance);
				}
			}
			
			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}
		
		public ConwayPoly Join(float offset)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newCentroidVertices = new Dictionary<string, int>();
			var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				existingVertices[vertexPoints[i]] = i;
				vertexRoles.Add(Roles.New);
			}

			int vertexIndex = vertexPoints.Count();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				vertexPoints.Add(face.Centroid + face.Normal * offset);
				newCentroidVertices[face.Name] = vertexIndex++;
				vertexRoles.Add(Roles.New);
			}

			for (var i = 0; i < Halfedges.Count; i++)
			{
				var edge = Halfedges[i];
				if (!rhombusFlags.ContainsKey(edge.PairedName))
				{
					if (edge.Pair != null)
					{
						var rhombus = new[]
						{
							newCentroidVertices[edge.Pair.Face.Name],
							existingVertices[edge.Vertex.Position],
							newCentroidVertices[edge.Face.Name],
							existingVertices[edge.Prev.Vertex.Position]
						};
						faceIndices.Add(rhombus);
						faceRoles.Add(i % 2 == 0 ? Roles.New : Roles.NewAlt);

						var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);

					}

					rhombusFlags[edge.PairedName] = true;
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Needle(float offset, bool randomize)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newCentroidVertices = new Dictionary<string, int>();
			var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

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
				vertexPoints.Add(face.Centroid + face.Normal * (float)(offset * (randomize?random.NextDouble():1)));
				newCentroidVertices[face.Name] = vertexIndex++;
				vertexRoles.Add(Roles.New);
			}

			for (var i = 0; i < Halfedges.Count; i++)
			{
				var edge = Halfedges[i];
				if (!rhombusFlags.ContainsKey(edge.Name))
				{
					if (edge.Pair != null)
					{
						var rhombus = new[]
						{
							newCentroidVertices[edge.Pair.Face.Name],
							existingVertices[edge.Vertex.Position],
							newCentroidVertices[edge.Face.Name],
						};
						faceIndices.Add(rhombus);
						faceRoles.Add(i % 2 == 0?Roles.New:Roles.NewAlt);

						var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);

					}

					rhombusFlags[edge.Name] = true;
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Kis(float offset, FaceSelections facesel, string tags = "", bool randomize=false, List<int> selectedFaces=null, bool scalebyArea=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var tagList = StringToTagList(tags);
			// vertices and faces to vertices
			var vertexRoles = Enumerable.Repeat(Roles.Existing, Vertices.Count());
			IEnumerable<Vector3> newVerts;
			if (scalebyArea)
			{
				newVerts = Faces.Select(f => f.Centroid + f.Normal * (float)((offset*f.GetArea()) * (randomize?random.NextDouble():1)));
			}
			else
			{
				newVerts = Faces.Select(f => f.Centroid + f.Normal * (float)(offset * (randomize?random.NextDouble():1)));
			}
			vertexRoles = vertexRoles.Concat(Enumerable.Repeat(Roles.New, newVerts.Count()));
			var vertexPoints = Vertices.Select(v => v.Position).Concat(newVerts);

			var faceRoles = new List<Roles>();

			// vertex lookup
			var vlookup = new Dictionary<Guid, int>();
			int n = Vertices.Count;
			for (int i = 0; i < n; i++)
			{
				vlookup.Add(Vertices[i].Name, i);
			}

			// create new tri-faces (like a fan)
			var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
			for (int i = 0; i < Faces.Count; i++)
			{
				var prevFaceTagSet = FaceTags[i];
				bool includeFace = selectedFaces==null || selectedFaces.Contains(i);
				includeFace &= IncludeFace(i, facesel, tagList);
				if (includeFace)
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
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
					}
				}
				else
				{
					faceIndices.Add(ListFacesByVertexIndices()[i]);
					faceRoles.Add(Roles.Ignored);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Gyro(float ratio = 0.3333f, float offset=0, bool randomize=false)
		{

			// Happy accidents - skip n new faces - offset just the centroid?

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var existingVerts = new Dictionary<string, int>();
			var newVerts = new Dictionary<string, int>();
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			// Loop through old faces
			for (int i = 0; i < Faces.Count; i++)
			{
				var prevFaceTagSet = FaceTags[i];
				var oldFace = Faces[i];
				float offsetVal = (float) (offset * (randomize ? random.NextDouble() : 1));
				vertexPoints.Add(oldFace.Centroid + oldFace.Normal * offsetVal);
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
					keyName = seedVertex.Name.ToString();
					if (existingVerts.ContainsKey(keyName))
					{
						seedVertexIndex = existingVerts[keyName];
					}
					else
					{
						vertexPoints.Add(seedVertex.Position - seedVertex.Normal * offsetVal);
						vertexRoles.Add(Roles.Existing);
						seedVertexIndex = vertexPoints.Count - 1;
						existingVerts[keyName] = seedVertexIndex;
					}

					var OneThirdVertex = edges[j].PointAlongEdge(ratio);
					keyName = edges[j].Name.ToString();
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
						keyName = edges[j].Next.Pair.Name.ToString();
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
						keyName = edges[j].Pair.Name.ToString();
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
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		#endregion

		#region extended conway methods

		// Add Vertices at edge midpoints and new faces around each vertex
		// Equivalent to ambo without removing vertices
		public ConwayPoly Subdivide(float offset)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = Vertices.Select(x => x.Position + x.Normal * offset).ToList(); // Existing vertices
			var vertexRoles = Enumerable.Repeat(Roles.Existing, vertexPoints.Count).ToList();

			var faceRoles = new List<Roles>();

			// Create new vertices, one at the midpoint of each edge

			var newVertices = new Dictionary<(Guid, Guid)?, int>();
			int vertexIndex = vertexPoints.Count();

			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.Midpoint);
				vertexRoles.Add(Roles.New);
				newVertices[edge.PairedName] = vertexIndex++;
			}

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var prevFaceTagSet = FaceTags[faceIndex];
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
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			// Create new faces for each vertex
			for (int idx = 0; idx < Vertices.Count; idx++)
			{
				var vertex = Vertices[idx];
				var adjacentFaces = vertex.GetVertexFaces();

				for (var faceIndex = 0; faceIndex < adjacentFaces.Count; faceIndex++)
				{
					Face face = adjacentFaces[faceIndex];
					var edge = face.GetHalfedges().Find(x => x.Vertex == vertex);
					int currVertex = newVertices[edge.PairedName];
					int prevVertex = newVertices[edge.Next.PairedName];
					var triangle = new[] {idx, prevVertex, currVertex};
					faceIndices.Add(triangle);
					faceRoles.Add(Roles.New);

					var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
					var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi].Where(t=>t.Item2==TagType.Extrovert));
					var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) => {rs.UnionWith(i); return rs;});
					newFaceTags.Add(newFaceTagSet);
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		// Interesting accident
		public ConwayPoly BrokenLoft(float ratio = 0.33333333f, int sides = 0)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newVertices = new Dictionary<(Guid, Guid)?, int>();

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

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		private static IEnumerable<Tuple<string, TagType>> StringToTagList(string tagString, bool extrovertOnly = false)
		{
			var tagList = new List<Tuple<string, TagType>>();
			if (tagString != null && tagString != "")
			{
				var substrings = tagString.Split(',');
				if (substrings.Length == 0) substrings = new [] {tagString};
				tagList = substrings.Select(item => new Tuple<string, TagType>(item, TagType.Extrovert)).ToList();
				if (!extrovertOnly)
				{
					tagList.Concat(substrings.Select(item => new Tuple<string, TagType>(item, TagType.Introvert)));
				}
			}

			return tagList;
		}

		public ConwayPoly Loft(float ratio = 0.33333333f, float offset = 0, FaceSelections facesel = FaceSelections.All, string tags = "", bool randomize = false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();
			var tagList = StringToTagList(tags);
			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newVertices = new Dictionary<(Guid, Guid)?, int>();

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
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var offsetVector = face.Normal * (float) (offset * (randomize ? random.NextDouble() : 1));

				if (IncludeFace(faceIndex, facesel, tagList))
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
						newVertex += offsetVector;
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
							newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
						}

						prevNewV = newV;
						edge = edge.Next;
					}

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
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					// Inner face
					faceIndices.Add(newInsetFace);
					faceRoles.Add(Roles.Existing);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

				}
				else
				{
					faceIndices.Add(
						face.GetHalfedges().Select(
							x => existingVertices[x.Vertex.Position]
						).ToArray());
					faceRoles.Add(Roles.Ignored);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		public ConwayPoly Quinto(float ratio = 0.33333333f, float offset=0, bool randomize=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
			var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				var offsetVal = (float) (offset * (randomize ? random.NextDouble() : 1));
				var pos = Vertices[i].Position;
				vertexPoints.Add(pos - Vertices[i].Normal * offsetVal);
				vertexRoles.Add(Roles.Existing);
				existingVertices[pos] = i;
			}

			int vertexIndex = vertexPoints.Count();

			// Create new edge vertices
			foreach (var edge in Halfedges)
			{
				vertexPoints.Add(edge.Midpoint);
				vertexRoles.Add(Roles.New);
				newEdgeVertices[edge.PairedName] = vertexIndex++;
			}

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var edge = face.Halfedge;
				var centroid = face.Centroid;
				var offsetVal = (float) (offset * (randomize ? random.NextDouble() : 1));

				// Create a new face for each existing face
				var newInsetFace = new int[face.Sides];
				int prevNewEdgeVertex = -1;
				int prevNewInnerVertex = -1;

				for (int i = 0; i < face.Sides; i++)
				{
					var newEdgeVertex = vertexPoints[newEdgeVertices[edge.PairedName]];
					var newInnerVertex = Vector3.LerpUnclamped(newEdgeVertex, centroid, ratio);

					vertexPoints.Add(newInnerVertex + face.Normal * offsetVal);
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
						if (i % 2 == 0 || face.Sides % 2 != 0)
						{
							faceRoles.Add(Roles.New);
						}
						else
						{
							faceRoles.Add(Roles.NewAlt);
						}
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
					}

					prevNewEdgeVertex = newEdgeVertices[edge.PairedName];
					prevNewInnerVertex = newInnerVertices[edge.Name];
					edge = edge.Next;
				}

				faceIndices.Add(newInsetFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

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
				if (face.Sides % 2 == 0 || face.Sides % 2 != 0)
				{
					faceRoles.Add(Roles.New);
				}
				else
				{
					faceRoles.Add(Roles.NewAlt);
				}
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
			return poly;
		}

		public ConwayPoly JoinedLace(float ratio = 0.33333333f, float offset=0, bool randomize=false)
		{
			return this._Lace(0, "", true, false, ratio, offset, randomize);
		}

		public ConwayPoly OppositeLace(float ratio = 0.33333333f, float offset=0, bool randomize=false)
		{
			return this._Lace(0, "", false, true, ratio, offset, randomize);
		}

		public ConwayPoly Lace(float ratio = 0.33333333f, FaceSelections facesel = FaceSelections.All, string tags = "", float offset=0, bool randomize=false)
		{
			return this._Lace(facesel, tags, false, false, ratio,  offset, randomize);
		}

		private ConwayPoly _Lace(FaceSelections facesel, string tags="", bool joined=false, bool opposite=false, float ratio=0.3f, float offset=0, bool randomize=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var tagList = StringToTagList(tags);
			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
			var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				var pos = Vertices[i].Position;
				vertexPoints.Add(pos);
				vertexRoles.Add(Roles.Existing);
				existingVertices[pos] = i;
			}

			int vertexIndex = vertexPoints.Count();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var offsetVal = (float) (offset * (randomize ? random.NextDouble() : 1));
				var offsetVector = face.Normal * offsetVal;
				if (joined || opposite || IncludeFace(faceIndex, facesel, tagList))
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
						vertexPoints.Add(newVertex + offsetVector);
						vertexRoles.Add(Roles.New);
						newInnerVertices[edge.Name] = vertexIndex;
						innerFace[i] = vertexIndex++;

						edge = edge.Next;
					}

					edge = face.Halfedge;

					for (int i = 0; i < face.Sides; i++)
					{
						var largeTriangle = new []
						{
							newInnerVertices[edge.Next.Name],
							newInnerVertices[edge.Name],
							existingVertices[edge.Vertex.Position]
						};
						faceIndices.Add(largeTriangle);
						faceRoles.Add(Roles.NewAlt);
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

						if (!joined && !opposite)
						{
							var smallTriangle = new int[]
							{
								existingVertices[edge.Prev.Vertex.Position],
								existingVertices[edge.Vertex.Position],
								newInnerVertices[edge.Name]
							};
							faceIndices.Add(smallTriangle);
							faceRoles.Add(Roles.New);
							newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
						}

						edge = edge.Next;
					}

					faceIndices.Add(innerFace);
					faceRoles.Add(Roles.Existing);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
				else
				{
					faceIndices.Add(
						face.GetHalfedges().Select(
							x => existingVertices[x.Vertex.Position]
						).ToArray());
					faceRoles.Add(Roles.Ignored);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
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
						if (edge.Pair != null)
						{
							var rhombus = new[]
							{
								existingVertices[edge.Prev.Vertex.Position],
								newInnerVertices[edge.Pair.Name],
								existingVertices[edge.Vertex.Position],
								newInnerVertices[edge.Name]
							};
							faceIndices.Add(rhombus);
							faceRoles.Add(Roles.New);

							var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
							newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTags.Add(newFaceTagSet);
						}
						rhombusFlags[edge.PairedName] = true;
					}
				}
			}

			if (opposite)
			{
				foreach (var edge in Halfedges)
				{
					if (!rhombusFlags.ContainsKey(edge.PairedName))
					{
						if (edge.Pair != null)
						{
							var tri1 = new[]
							{
								existingVertices[edge.Prev.Vertex.Position],
								newInnerVertices[edge.Pair.Name],
								newInnerVertices[edge.Name]
							};
							faceIndices.Add(tri1);
							faceRoles.Add(Roles.New);

							var newFaceTagSet1 = new HashSet<Tuple<string, TagType>>();
							newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTags.Add(newFaceTagSet1);

							var tri2 = new[]
							{
								newInnerVertices[edge.Pair.Name],
								existingVertices[edge.Vertex.Position],
								newInnerVertices[edge.Name]
							};
							faceIndices.Add(tri2);
							faceRoles.Add(Roles.New);

							var newFaceTagSet2 = new HashSet<Tuple<string, TagType>>();
							newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
							newFaceTags.Add(newFaceTagSet2);

						}
						rhombusFlags[edge.PairedName] = true;
					}
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Stake(float ratio = 0.3333333f, FaceSelections facesel = FaceSelections.All, string tags = "", bool join=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var tagList = StringToTagList(tags);
			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
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
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				if (join || IncludeFace(faceIndex, facesel, tagList))
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

						if (!join)
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
							newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

						}

						var quad = new[]
						{
							existingVertices[edge.Vertex.Position],
							newInnerVertices[edge.Next.Name],
							newCentroidVertices[face.Name],
							newInnerVertices[edge.Name],
						};
						faceIndices.Add(quad);
						// Alternate roles but only for faces with an even number of sides
						if (i % 2 == 0 || face.Sides % 2 != 0){faceRoles.Add(Roles.Existing);}
						else {faceRoles.Add(Roles.ExistingAlt);}
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

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
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

 			if (join)
			{
				var edgeFlags = new  HashSet<(Guid, Guid)?>();
				foreach (var edge in Halfedges)
				{
					if (edge.Pair == null) continue;

					if (!edgeFlags.Contains(edge.PairedName))
					{
						var quad = new[]
						{
							existingVertices[edge.Vertex.Position],
							newInnerVertices[edge.Name],
							existingVertices[edge.Pair.Vertex.Position],
							newInnerVertices[edge.Pair.Name],
						};
						faceIndices.Add(quad);
						faceRoles.Add(Roles.New);
						edgeFlags.Add(edge.PairedName);

						var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);
					}
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly JoinKisKis(float ratio, float offset)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
			var newCentroidVertices = new Dictionary<string, int>();
			var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				var vert = Vertices[i];
				vertexPoints.Add(vert.Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vert.Position] = i;
			}

			for (var i = 0; i < Faces.Count; i++)
			{
				var face = Faces[i];
				var centroid = face.Centroid;
				vertexPoints.Add(centroid + face.Normal * offset);
				vertexRoles.Add(Roles.Existing);
				newCentroidVertices[face.Name] = vertexPoints.Count - 1;
			}

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];
				var face = Faces[faceIndex];
				var centroid = face.Centroid;
				var edges = face.GetHalfedges();

				for (int i = 0; i < edges.Count; i++)
				{
					var edge = edges[i];

					var newVertex = Vector3.LerpUnclamped(
						edge.Midpoint,
						centroid,
						ratio
					);
					vertexPoints.Add(newVertex + face.Normal * offset);
					vertexRoles.Add(Roles.New);
					newInnerVertices[edge.Name] = vertexPoints.Count - 1;

					var triangle1 = new[]
					{
						newCentroidVertices[edge.Face.Name],
						newInnerVertices[edge.Name],
						existingVertices[edge.Vertex.Position]
					};
					faceIndices.Add(triangle1);
					faceRoles.Add(Roles.New);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					var triangle2 = new[]
					{
						newInnerVertices[edge.Name],
						newCentroidVertices[edge.Face.Name],
						existingVertices[edge.Prev.Vertex.Position]
					};
					faceIndices.Add(triangle2);
					faceRoles.Add(Roles.NewAlt);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				}
			}

			// Create Rhombus faces
			// TODO Make planar

			foreach (var edge in Halfedges)
			{
				if (!rhombusFlags.ContainsKey(edge.PairedName))
				{
					if (edge.Pair != null)
					{
						var rhombus = new[]
						{
							existingVertices[edge.Prev.Vertex.Position],
							newInnerVertices[edge.Pair.Name],
							existingVertices[edge.Vertex.Position],
							newInnerVertices[edge.Name]
						};
						faceIndices.Add(rhombus);
						faceRoles.Add(Roles.Existing);

						var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)].Where(t=>t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);
					}
					rhombusFlags[edge.PairedName] = true;
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Medial(int subdivisions, float offset)
		{
			return _Medial(subdivisions, offset);
		}

		public ConwayPoly EdgeMedial(int subdivisions, float offset)
		{
			return _Medial(subdivisions, offset, true);
		}

		private ConwayPoly _Medial(int subdivisions, float offset, bool edgeMedial = false)
		{

			subdivisions = subdivisions < 1 ? 1 : subdivisions;
			
			// Some nasty hacks in here
			// due to face.GetHalfedges seemingly returning edges in an
			// inconsistent order. I might be missing something obvious.

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var faceIndices = new List<int[]>();
			var vertexPoints = new List<Vector3>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<(Guid, Guid)?, int[]>();
			var newCentroidVertices = new Dictionary<string, int>();
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();
			
			for (var i = 0; i < Vertices.Count; i++)
			{
				var vert = Vertices[i];
				vertexPoints.Add(vert.Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vert.Position] = i;
			}

			int vertexIndex = vertexPoints.Count;

			// Create new edge vertices
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var prevFaceTagSet = FaceTags[faceIndex];
				vertexPoints.Add(face.Centroid + face.Normal * offset);
				vertexRoles.Add(Roles.New);
				int centroidIndex = vertexIndex;
				newCentroidVertices[face.Name] = vertexIndex++;

				foreach (var edge in face.GetHalfedges())
				{
					if (!newEdgeVertices.ContainsKey(edge.PairedName))
					{
						newEdgeVertices[edge.PairedName] = new int[subdivisions];
						for (int i = 0; i < subdivisions; i++)
						{
							vertexPoints.Add(edge.PointAlongEdge((1f / (subdivisions + 1)) * (i + 1)));
							vertexRoles.Add(Roles.NewAlt);
							newEdgeVertices[edge.PairedName][i] = vertexIndex++;
						}
					}
				}

				var halfedges = face.GetHalfedges();
				for (var i = 0; i < halfedges.Count; i++)
				{
					var edge = halfedges[i];
					var currNewVerts = newEdgeVertices[edge.PairedName];

					//Figure out which point is nearest to our existing vert.
					// Another symptom of inconsistent edge order.
					// If we fix that we can remove this and the normal flipping later on.
					int nearestVertexIndex;
					int furthestVertexIndex;
					var distance1 = (vertexPoints[currNewVerts[0]] - edge.Vertex.Position).sqrMagnitude;
					var distance2 = (vertexPoints[currNewVerts.Last()] - edge.Vertex.Position).sqrMagnitude;
					if (distance1 < distance2)
					{
						nearestVertexIndex = currNewVerts[0];
						furthestVertexIndex = currNewVerts.Last();
					}
					else
					{
						nearestVertexIndex = currNewVerts.Last();
						furthestVertexIndex = currNewVerts[0];
					}

					if (edgeMedial)
					{
						int otherNearestVertexIndex;
						var otherNewVerts = newEdgeVertices[edge.Next.PairedName];

						var d1 = (vertexPoints[otherNewVerts[0]] - edge.Vertex.Position).sqrMagnitude;
						var d2 = (vertexPoints[otherNewVerts.Last()] - edge.Vertex.Position).sqrMagnitude;
						otherNearestVertexIndex = d1 < d2 ? otherNewVerts[0] : otherNewVerts.Last();

						// One quadrilateral face
						var quad = new[]
						{
							centroidIndex,
							nearestVertexIndex,
							existingVertices[edge.Vertex.Position],
							otherNearestVertexIndex
						};
						faceIndices.Add(quad);
						faceRoles.Add(Roles.Existing);

						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
					}
					else
					{
						// Two triangular faces
						var triangle1 = new[]
						{
							centroidIndex,
							nearestVertexIndex,
							existingVertices[edge.Vertex.Position]
						};
						faceIndices.Add(triangle1);
						faceRoles.Add(Roles.Existing);
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

//						if (edge.Pair != null)
//						{
						var triangle2 = new[]
						{
							centroidIndex,
							existingVertices[edge.Prev.Vertex.Position],
							furthestVertexIndex
						};
						faceIndices.Add(triangle2);
						faceRoles.Add(Roles.ExistingAlt);
						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
//						}
					}

					// Create new triangular faces at edges
					for (int j = 0; j < subdivisions - 1; j++)
					{
						int edgeVertIndex;
						int edgeNextVertIndex;

						// Flip new vertex array if this isn't the primary edge
						if (edge.PairedName.Value.Item1==edge.Vertex.Name)
						{
							edgeVertIndex = j;
							edgeNextVertIndex = j + 1;
						}
						else
						{
							edgeVertIndex = currNewVerts.Length - j - 1;
							edgeNextVertIndex = currNewVerts.Length - j - 2;
						}

						var edgeTriangle = new[]
						{
							centroidIndex,
							currNewVerts[edgeVertIndex],
							currNewVerts[edgeNextVertIndex]
						};

						// Can't seem to get the edge points to be in a consistent order
						// So need to fix normals by comparing with face normal and flipping if different
						var side1 = vertexPoints[edgeTriangle[1]] - vertexPoints[edgeTriangle[0]];
						var side2 = vertexPoints[edgeTriangle[2]] - vertexPoints[edgeTriangle[1]];
						var normal = Vector3.Cross(side1, side2).normalized;
						if (face.Normal != normal)
						{
							var temp = edgeTriangle[1];
							edgeTriangle[1] = edgeTriangle[2];
							edgeTriangle[2] = temp;
						}

						faceIndices.Add(edgeTriangle);
						if (j % 2 == 0)
						{
							faceRoles.Add(Roles.New);
						}
						else
						{
							faceRoles.Add(Roles.NewAlt);
						}

						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
					}
				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}
		
		// public ConwayPoly JoinedMedial(int subdivisions, float offset)
		// {
		//
		// var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();
		//
		// 	var faceIndices = new List<int[]>();
		// 	var vertexPoints = new List<Vector3>();
		// 	var existingVertices = new Dictionary<Vector3, int>();
		// 	var newEdgeVertices = new Dictionary<string, int[]>();
		//
		// 	var faceRoles = new List<Roles>();
		// 	var vertexRoles = new List<Roles>();
		//
		// 	for (var i = 0; i < Vertices.Count; i++)
		// 	{
		// 		vertexPoints.Add(Vertices[i].Position);
		// 		vertexRoles.Add(Roles.Existing);
		// 		existingVertices[vertexPoints[i]] = i;
		// 	}
		//
		// 	int vertexIndex = vertexPoints.Count();
		//
		// 	foreach (var face in Faces)
		// 	{
		// 		foreach (var edge in face.GetHalfedges())
		// 		{
		// 			if (!newEdgeVertices.ContainsKey(edge.PairedName))
		// 			{
		// 				newEdgeVertices[edge.PairedName] = new int[subdivisions];
		// 				for (int i = 0; i < subdivisions; i++)
		// 				{
		// 					vertexPoints.Add(edge.PointAlongEdge((1f / (subdivisions + 1)) * (i + 1)));
		// 					vertexRoles.Add(Roles.New);
		// 					newEdgeVertices[edge.PairedName][i] = vertexIndex++;
		// 				}
		// 			}
		// 		}
		// 	}
		//
		// 	// Create rhombic faces
		// 	foreach (var edge in Halfedges)
		// 	{
		// 		for (int i=0; i < subdivisions; i++)
		// 		{
		// 			int v0 = newEdgeVertices[edge.PairedName][i];
		// 			int v2 = newEdgeVertices[edge.PairedName][i];
		// 			var rhombus = new[]
		// 			{
		// 				v0,
		// 				existingVertices[edge.Vertex.Position],
		// 				v2,
		// 				existingVertices[edge.Next.Vertex.Position]
		// 			};
		// 			faceIndices.Add(rhombus);
		// 			faceRoles.Add(Roles.New);
		//			newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
		// 		}
		// 	}
		//
		// 	// Generate triangular faces
		// 	foreach (var face in Faces)
		// 	{
		// 		var centroid = face.Centroid;
		// 		vertexPoints.Add(centroid);
		// 		vertexRoles.Add(Roles.New);
		//
		// 		var edges = face.GetHalfedges();
		// 		var prevEnds = edges[face.Sides - 1].getEnds();
		// 		for (int i = 0; i < subdivisions; i++)
		// 		{
		// 			int prevVertex = newEdgeVertices[edges[0].PairedName][i];
		//
		// 			for (var j = 0; i < edges.Count; j++)
		// 			{
		// 				Halfedge edge = edges[j];
		// 				var ends = edge.getEnds();
		// 				int currVertex = newEdgeVertices[edge.PairedName][i];
		//
		// 				var triangle1 = new int[]
		// 				{
		// 					vertexIndex,
		// 					existingVertices[edges[j].Vertex.Position],
		// 					currVertex
		// 				};
		// 				var triangle2 = new int[]
		// 				{
		// 					vertexIndex,
		// 					prevVertex,
		// 					existingVertices[edges[j].Vertex.Position]
		// 				};
		//
		// 				faceIndices.Add(triangle1);
		// 				faceRoles.Add(Roles.New);
		//				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
		// 				faceIndices.Add(triangle2);
		// 				faceRoles.Add(Roles.New);
		//				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
		//
		// 				prevVertex = currVertex;
		// 				edge = edge.Next;
		// 			}
		// 			vertexIndex++;
		// 		}
		// 	}
		//
		// 	//medialPolyhedron.setVertexNormalsToFaceNormals();
		// 	return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		// }

		public ConwayPoly Propeller(float ratio = 0.33333333f)
		{
			ratio = 1 - ratio;

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

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
			for (var i = 0; i < Halfedges.Count; i++)
			{
				var edge = Halfedges[i];
				vertexPoints.Add(edge.PointAlongEdge(ratio));
				newEdgeVertices[edge.Name.ToString()] = vertexIndex++;
				vertexRoles.Add(Roles.New);

				if (edge.Pair != null)
				{
					vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));
					newEdgeVertices[edge.Pair.Name.ToString()] = vertexIndex++;
				}
				else
				{
					vertexPoints.Add(edge.PointAlongEdge(1 - ratio));
					newEdgeVertices[edge.Name.ToString() + "-Pair"] = vertexIndex++;
				}

				vertexRoles.Add(Roles.New);
			}

			// Create quadrilateral faces and one central face on each face
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var prevFaceTagSet = FaceTags[faceIndex];

				var face = Faces[faceIndex];
				var edge = face.Halfedge;
				var centralFace = new int[face.Sides];

				for (int i = 0; i < face.Sides; i++)
				{
					string edgePairName;
					if (edge.Pair != null)
					{
						edgePairName = edge.Pair.Name.ToString();
					}
					else
					{
						edgePairName = edge.Name + "-Pair";
					}

					string edgeNextPairName;
					if (edge.Next.Pair != null)
					{
						edgeNextPairName = edge.Next.Pair.Name.ToString();
					}
					else
					{
						edgeNextPairName = edge.Next.Name.ToString() + "-Pair";
					}

					var quad = new[]
					{
						newEdgeVertices[edge.Next.Name.ToString()],
						newEdgeVertices[edgeNextPairName],
						newEdgeVertices[edgePairName],
						existingVertices[edge.Vertex.Position],
					};
					faceIndices.Add(quad);
					// Alternate roles but only for faces with an even number of sides
					if (i % 2 == 0 || face.Sides % 2 != 0)
					{
						faceRoles.Add(Roles.New);
					}
					else
					{
						faceRoles.Add(Roles.NewAlt);
					}
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					centralFace[i] = newEdgeVertices[edgePairName];
					edge = edge.Next;
				}

				faceIndices.Add(centralFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Whirl(float ratio = 0.3333333f)
		{

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

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

			for (var i = 0; i < Halfedges.Count; i++)
			{
				var edge = Halfedges[i];
				vertexPoints.Add(edge.PointAlongEdge(ratio));
				vertexRoles.Add(Roles.New);
				newEdgeVertices[edge.Name.ToString()] = vertexIndex++;
				if (edge.Pair != null)
				{
					vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));
					newEdgeVertices[edge.Pair.Name.ToString()] = vertexIndex++;
				}
				else
				{
					vertexPoints.Add(edge.PointAlongEdge(1 - ratio));
					newEdgeVertices[edge.Name.ToString() + "-Pair"] = vertexIndex++;
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
					var pointOnEdge = vertexPoints[newEdgeVertices[edge.Name.ToString()]];
					vertexPoints.Add(Vector3.LerpUnclamped(pointOnEdge, pointOnEdge + direction, ratio));
					vertexRoles.Add(Roles.NewAlt);
					newInnerVertices[edge.Name.ToString()] = vertexIndex++;
				}
			}

			// Generate hexagonal faces and central face
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				var prevFaceTagSet = FaceTags[faceIndex];
				var centralFace = new int[face.Sides];
				var edge = face.Halfedge;

				for (var i = 0; i < face.Sides; i++)
				{
					string edgeNextPairName;
					if (edge.Next.Pair != null)
					{
						edgeNextPairName = edge.Next.Pair.Name.ToString();
					}
					else
					{
						edgeNextPairName = edge.Next.Name + "-Pair";
					}

					var hexagon = new[]
					{
						existingVertices[edge.Vertex.Position],
						newEdgeVertices[edgeNextPairName],
						newEdgeVertices[edge.Next.Name.ToString()],
						newInnerVertices[edge.Next.Name.ToString()],
						newInnerVertices[edge.Name.ToString()],
						newEdgeVertices[edge.Name.ToString()],
					};
					faceIndices.Add(hexagon);

					// Alternate roles but only for faces with an even number of sides
					if (i % 2 == 0 || face.Sides % 2 != 0)
					{
						faceRoles.Add(Roles.New);
					}
					else
					{
						faceRoles.Add(Roles.NewAlt);
					}

					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					centralFace[i] = newInnerVertices[edge.Name.ToString()];
					edge = edge.Next;
				}

				faceIndices.Add(centralFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Volute(float ratio = 0.33333333f)
		{
			return Whirl(ratio).Dual();
		}

		public ConwayPoly Meta(float offset, float offset2, bool randomize=false)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
			var newCenterVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				var vert = Vertices[i];
				vertexPoints.Add(vert.Position + vert.Normal * offset2);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vert.Position] = i;
			}

			for (var i = 0; i < Faces.Count; i++)
			{
				var prevFaceTagSet = FaceTags[i];

				var face = Faces[i];
				var centroid = face.Centroid;

				vertexPoints.Add(centroid + face.Normal * (float) (offset * (randomize ? random.NextDouble() : 1)));
				vertexRoles.Add(Roles.Existing);
				newCenterVertices[face.Name] = vertexPoints.Count - 1;

				var edges = face.GetHalfedges();

				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];

					if (!newEdgeVertices.ContainsKey(edge.PairedName))
					{
						vertexPoints.Add(edge.Midpoint);
						vertexRoles.Add(Roles.New);
						newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
					}
				}

				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];

					var edgeFace1 = new List<int>
					{
						existingVertices[edge.Vertex.Position],
						newEdgeVertices[edge.PairedName],
						newCenterVertices[face.Name],
					};
					faceIndices.Add(edgeFace1);
					faceRoles.Add(Roles.New);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					var edgeFace2 = new List<int>
					{
						newEdgeVertices[edge.PairedName],
						existingVertices[edge.Prev.Vertex.Position],
						newCenterVertices[face.Name],
					};
					faceIndices.Add(edgeFace2);
					faceRoles.Add(Roles.NewAlt);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Cross(float amount)
		{
			amount = amount * 0.5f + 0.5f;

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
			var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
			var newCentroidVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var i = 0; i < Vertices.Count; i++)
			{
				vertexPoints.Add(Vertices[i].Position);
				vertexRoles.Add(Roles.Existing);
				existingVertices[vertexPoints[i]] = i;
			}

			for (var i = 0; i < Faces.Count; i++)
			{
				var face = Faces[i];
				var prevFaceTagSet = FaceTags[i];
				var centroid = face.Centroid;

				vertexPoints.Add(centroid);
				vertexRoles.Add(Roles.Existing);
				newCentroidVertices[face.Name] = vertexPoints.Count - 1;

				var edges = face.GetHalfedges();
				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];

					vertexPoints.Add(centroid - (centroid - edge.Vertex.Position) * amount);
					vertexRoles.Add(Roles.NewAlt);
					newInnerVertices[edge.Name] = vertexPoints.Count - 1;

					if (!newEdgeVertices.ContainsKey(edge.PairedName))
					{
						vertexPoints.Add(edge.Midpoint);
						vertexRoles.Add(Roles.New);
						newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
					}
				}

				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];

					var innerFace = new List<int>
					{
						newCentroidVertices[face.Name],
						newInnerVertices[edge.Name],
						newEdgeVertices[edge.PairedName],
						newInnerVertices[edge.Prev.Name],
					};
					faceIndices.Add(innerFace);
					faceRoles.Add(j % 2 == 0 ? Roles.Existing : Roles.ExistingAlt);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					var edgeFace1 = new List<int>
					{
						existingVertices[edge.Vertex.Position],
						newEdgeVertices[edge.PairedName],
						newInnerVertices[edge.Name],
					};
					faceIndices.Add(edgeFace1);
					faceRoles.Add(Roles.New);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					var edgeFace2 = new List<int>
					{
						newEdgeVertices[edge.PairedName],
						existingVertices[edge.Prev.Vertex.Position],
						newInnerVertices[edge.Prev.Name],
					};
					faceIndices.Add(edgeFace2);
					faceRoles.Add(Roles.NewAlt);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

				}
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		public ConwayPoly Squall(float amount, bool join=true)
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();
			var existingVertices = new Dictionary<Vector3, int>();
			var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
			var newInnerVertices = new Dictionary<string, int>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			if (!join)
			{
				for (var i = 0; i < Vertices.Count; i++)
				{
					vertexPoints.Add(Vertices[i].Position);
					vertexRoles.Add(Roles.Existing);
					existingVertices[vertexPoints[i]] = i;
				}
			}

			for (var i = 0; i < Faces.Count; i++)
			{
				var face = Faces[i];
				var prevFaceTagSet = FaceTags[i];
				var centroid = face.Centroid;
				
				var edges = face.GetHalfedges();
				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];
					
					vertexPoints.Add(Vector3.LerpUnclamped(centroid, edge.Vertex.Position, amount / 2f));
					vertexRoles.Add(Roles.NewAlt);
					newInnerVertices[face.Name + edge.Vertex.Name] = vertexPoints.Count - 1;

					if (!newEdgeVertices.ContainsKey(edge.PairedName))
					{
						vertexPoints.Add(edge.Midpoint);
						vertexRoles.Add(Roles.New);
						newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
					}
				}

				for (int j=0; j < edges.Count; j++)
				{
					var edge = edges[j];

					var innerFace = new List<int>
					{
						newInnerVertices[face.Name + edge.Vertex.Name],
						newEdgeVertices[edge.PairedName],
						newInnerVertices[face.Name + edge.Prev.Vertex.Name],
					};
					faceIndices.Add(innerFace);
					faceRoles.Add(Roles.New);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));

					if (!join)
					{	
						var vertexFace = new List<int>
						{
							existingVertices[edge.Vertex.Position],
							newEdgeVertices[edge.PairedName],
							newInnerVertices[face.Name + edge.Vertex.Name],
							newEdgeVertices[edge.Next.PairedName],
						};
						faceIndices.Add(vertexFace);
						faceRoles.Add(Roles.NewAlt);

						newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
					}
				}
				
				var existingFace = new List<int>();
				for (int j = edges.Count - 1; j >= 0; j--)
				{
					var edge = edges[j];
					existingFace.Add(newInnerVertices[face.Name + edge.Vertex.Name]);
				}
				faceIndices.Add(existingFace);
				faceRoles.Add(Roles.Existing);
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(prevFaceTagSet));
				
			}

			if (join)
			{
				for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
				{
					var vertex = Vertices[vertIndex];
					var vertexFace = new List<int>();
					for (int j = vertex.Halfedges.Count - 1; j >= 0; j--)
					{
						var edge = vertex.Halfedges[j];
						vertexFace.Add(newEdgeVertices[edge.PairedName]);
						vertexFace.Add(newInnerVertices[edge.Face.Name + vertex.Name]);
					}

					faceIndices.Add(vertexFace);
					faceRoles.Add(Roles.NewAlt);

					var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
					var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]
						.Where(t => t.Item2 == TagType.Extrovert));
					var newFaceTagSet = existingTagSets.Aggregate(new HashSet<Tuple<string, TagType>>(), (rs, i) =>
					{
						rs.UnionWith(i);
						return rs;
					});
					newFaceTags.Add(newFaceTagSet);
				}
			}
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
		}

		#endregion

		#region geometry methods

		public ConwayPoly AddMirrored(Vector3 axis, float amount, FaceSelections facesel = FaceSelections.All, string tags="")
		{
			var original = FaceKeep(facesel, tags);
			var mirror = original.Duplicate();
			mirror.Mirror(axis, amount);
			mirror = mirror.FaceKeep(facesel);
			mirror.Halfedges.Flip();
			original.Append(mirror);
			return original;
		}

		public void Mirror(Vector3 axis, float offset)
		{
			Vector3 offsetVector = offset * axis;
			for (var i = 0; i < Vertices.Count; i++)
			{
				Vertices[i].Position -= offsetVector;
			}

			for (var i = 0; i < Vertices.Count; i++)
			{
				var v = Vertices[i];
				v.Position = Vector3.Reflect(v.Position, axis);
			}
		}

		public ConwayPoly AddCopy(Vector3 axis, float amount, FaceSelections facesel = FaceSelections.All, string tags="")
		{
			amount /= 2.0f;
			var original = Duplicate(axis * -amount, Quaternion.identity, 1.0f);
			var copy = Duplicate(axis * amount, Quaternion.identity, 1.0f);
			copy = copy.FaceKeep(facesel);
			original.Append(copy);
			return original;
		}

		public ConwayPoly Stack(Vector3 axis, float offset, float scale, float limit = 0.1f,
			FaceSelections facesel = FaceSelections.All, string tags = "")
		{
			scale = Mathf.Abs(scale);
			scale = Mathf.Clamp(scale, 0.0001f, 0.99f);
			var original = Duplicate();
			Vector3 offsetVector = axis * offset;
			var copy = Duplicate();
			copy = copy.FaceKeep(facesel, tags);
			int copies = 0;
			while (scale > limit && copies < 64)  // TODO make copies configurable
			{
				original.Append(copy.Duplicate(offsetVector, Quaternion.identity, scale));
				scale *= scale;
				offsetVector += axis * offset;
				offset *= Mathf.Sqrt(scale);  // Not sure why but sqrt *looks* right.
				copies++;
			}
			return original;
		}

		public ConwayPoly Rotate(Vector3 axis, float amount)
		{
			var copy = Duplicate();
			for (var i = 0; i < copy.Vertices.Count; i++)
			{
				var v = copy.Vertices[i];
				v.Position = Quaternion.AngleAxis(amount, axis) * v.Position;
			}
			return copy;
		}

		public Vector3 GetCentroid()
		{
			if (Vertices.Count == 0) return Vector3.zero;

			return new Vector3(
				Vertices.Average(x=>x.Position.x),
				Vertices.Average(x=>x.Position.y),
				Vertices.Average(x=>x.Position.z)
			);

		}

		public void Recenter()
		{
			Vector3 newCenter = GetCentroid();
			for (var i = 0; i < Vertices.Count; i++)
			{
				Vertices[i].Position -= newCenter;
			}
		}

		public ConwayPoly SitLevel(float faceFactor = 0)
		{
			int faceIndex = Mathf.FloorToInt(Faces.Count * faceFactor);
			faceIndex = Mathf.Clamp(faceIndex, 0, 1);
			var vertexPoints = new List<Vector3>();
			var faceIndices = ListFacesByVertexIndices();

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var rot = Quaternion.LookRotation(Faces[faceIndex].Normal);
				var rotForwardToDown = Quaternion.FromToRotation(Vector3.down, Vector3.forward);
				vertexPoints.Add(Quaternion.Inverse(rot * rotForwardToDown) * Vertices[vertexIndex].Position);
			}

			var conway = new ConwayPoly(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
			return conway;
		}

		public ConwayPoly Stretch(float amount)
		{
			var vertexPoints = new List<Vector3>();
			var faceIndices = ListFacesByVertexIndices();

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var vertex = Vertices[vertexIndex];
				float y;
				if (vertex.Position.y < 0.1)
				{
					y = vertex.Position.y - amount;
				}
				else if (vertex.Position.y > -0.1)
				{
					y = vertex.Position.y + amount;
				}
				else
				{
					y = vertex.Position.y;
				}


				var newPos = new Vector3(vertex.Position.x, y, vertex.Position.z);
				vertexPoints.Add(newPos);
			}

			var conway = new ConwayPoly(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
			return conway;
		}

		public ConwayPoly FaceSlide(float amount, float direction, FaceSelections facesel, string tags="", bool randomize=false)
		{
			var tagList = StringToTagList(tags);
			var poly = Duplicate();
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = poly.Faces[faceIndex];
				if (!IncludeFace(faceIndex, facesel, tagList)) continue;
				var faceNormal = face.Normal;
				//var amount = amount * (float) (randomize ? random.NextDouble() : 1);
				var faceVerts = face.GetVertices();
				for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
				{
					var vertexPos = faceVerts[vertexIndex].Position;

					Vector3 tangent, tangentLeft, tangentUp, t1, t2;

					t1 = Vector3.Cross(faceNormal, Vector3.forward);
					t2 = Vector3.Cross(faceNormal, Vector3.left);
					if(t1.magnitude > t2.magnitude) {tangentUp = t1;}
					else {tangentUp = t2;}

					t2 = Vector3.Cross(faceNormal, Vector3.up);
					if(t1.magnitude > t2.magnitude) {tangentLeft = t1;}
					else {tangentLeft = t2;}

					tangent = Vector3.SlerpUnclamped(tangentUp, tangentLeft, direction);

					var vector = tangent * (amount * (float)(randomize ? random.NextDouble():1));
					var newPos = vertexPos + vector;
					faceVerts[vertexIndex].Position = newPos;
				}
			}

			return poly;

		}


		public ConwayPoly VertexScale(float scale, FaceSelections vertexsel, bool randomize)
		{
			var vertexPoints = new List<Vector3>();
			var faceIndices = ListFacesByVertexIndices();

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var _scale = scale * (randomize?random.NextDouble():1) + 1;
				var vertex = Vertices[vertexIndex];
				var includeVertex = IncludeVertex(vertexIndex, vertexsel);
				vertexPoints.Add(includeVertex ? vertex.Position * (float)_scale : vertex.Position);
			}

			return new ConwayPoly(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
		}

		public ConwayPoly VertexFlex(float scale, FaceSelections facesel, string tags = "", bool randomize = false)
		{
			var tagList = StringToTagList(tags);
			var poly = Duplicate();
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = poly.Faces[faceIndex];
				if (!IncludeFace(faceIndex, facesel, tagList)) continue;
				var faceCentroid = face.Centroid;
				var faceVerts = face.GetVertices();
				for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
				{
					var vertexPos = faceVerts[vertexIndex].Position;
					float _scale = scale * (randomize ? (float)random.NextDouble() : 1f) + 1f;
					var newPos = vertexPos + (vertexPos - faceCentroid) * _scale;
					faceVerts[vertexIndex].Position = newPos;
				}
			}

			return poly;
		}

		public ConwayPoly VertexRotate(float amount, FaceSelections facesel, string tags = "", bool randomize = false)
		{
			var tagList = StringToTagList(tags);
			var poly = Duplicate();
			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = poly.Faces[faceIndex];
				if (!IncludeFace(faceIndex, facesel, tagList)) continue;
				var faceCentroid = face.Centroid;
				var direction = face.Normal;
				amount = amount * (float) (randomize ? random.NextDouble() : 1);
				var _angle = (360f / face.Sides) * amount;
				var faceVerts = face.GetVertices();
				for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
				{
					var vertexPos = faceVerts[vertexIndex].Position;
					var rot = Quaternion.AngleAxis(_angle, direction);
					var newPos = faceCentroid + rot * (vertexPos - faceCentroid);
					faceVerts[vertexIndex].Position = newPos;
				}
			}

			return poly;
		}


		public ConwayPoly FaceScale(float scale, FaceSelections facesel, string tags = "", bool randomize=false)
		{
			var tagList = StringToTagList(tags);
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var _scale = scale * (randomize?random.NextDouble():1) + 1;
				var face = Faces[faceIndex];
				var includeFace = IncludeFace(faceIndex, facesel, tagList);
				int c = vertexPoints.Count;

				vertexPoints.AddRange(face.GetVertices()
					.Select(v =>
						includeFace ? Vector3.LerpUnclamped(face.Centroid, v.Position, (float)_scale) : v.Position));
				var faceVerts = new List<int>();
				for (int ii = 0; ii < face.GetVertices().Count; ii++)
				{
					faceVerts.Add(c + ii);
				}

				faceIndices.Add(faceVerts);
				faceRoles.Add(includeFace ? FaceRoles[faceIndex] : Roles.Ignored);
				var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
				vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVerts.Count));
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
		}

		public ConwayPoly FaceRotate(float amount, FaceSelections facesel, string tags = "", int axis = 1, bool randomize=false)
		{
			var tagList = StringToTagList(tags);
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();

			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();


			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];

				amount = amount * (float)(randomize ? random.NextDouble() : 1);
				var _angle = (360f / face.Sides) * amount;

				var includeFace = IncludeFace(faceIndex, facesel, tagList);

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

				var rot = Quaternion.AngleAxis((float)_angle, direction);

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
				faceRoles.Add(includeFace ? FaceRoles[faceIndex]: Roles.Ignored);
				var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
				vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVertices.Count));
			}

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
		}

		public ConwayPoly VertexRemove(FaceSelections vertexsel, bool invertLogic)
		{

			var allFaceIndices = new List<List<int>>();
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();
			
			int vertexCount = 0;

			var faces = ListFacesByVertexIndices();
			for (var i = 0; i < faces.Length; i++)
			{
				var oldFaceIndices = faces[i];
				var newFaceIndices = new List<int>();
				for (var idx = 0; idx < oldFaceIndices.Count; idx++)
				{
					var vertexIndex = oldFaceIndices[idx];
					bool keep = IncludeVertex(vertexIndex, vertexsel);
					keep = invertLogic ? !keep : keep;
					if (!keep)
					{
						newFaceIndices.Add(vertexIndex);
						vertexCount++;
					}
				}

				if (newFaceIndices.Count > 2)
				{
					allFaceIndices.Add(newFaceIndices);
				}
			}
			faceRoles.AddRange(Enumerable.Repeat(Roles.Existing, allFaceIndices.Count));
			vertexRoles.AddRange(Enumerable.Repeat(Roles.Existing, vertexCount));
			return new ConwayPoly(Vertices.Select(x => x.Position), allFaceIndices, faceRoles, vertexRoles, FaceTags);
		}
		
		public ConwayPoly Collapse(FaceSelections vertexsel, bool invertLogic)
		{
			var poly = VertexRemove(vertexsel, invertLogic);
			poly.FillHoles();
			return poly;
		}

		public ConwayPoly Layer(int layers, float scale, float offset, FaceSelections facesel, string tags = "")
		{
			var poly = Duplicate();
			var layer = Duplicate();
			for (int i=0; i <= layers; i++)
			{
				var newLayer = layer.Duplicate();
				newLayer = newLayer.FaceScale(scale, facesel, tags);
				newLayer = newLayer.Offset(offset, facesel);
				poly.Append(newLayer);
				layer = newLayer;
			}
			return poly;
		}

		public ConwayPoly FaceRemove(FaceSelections facesel, string tags="")
		{
			return _FaceRemove(facesel, tags);
		}

		public ConwayPoly FaceKeep(FaceSelections facesel, string tags="")
		{
			return _FaceRemove(facesel, tags, true);
		}

		public ConwayPoly _FaceRemove(FaceSelections facesel=FaceSelections.All, string tags = "", bool invertLogic=false, Func<Face, bool> filter=null)
		{
			var tagList = StringToTagList(tags);
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();
			var facesToRemove = new List<Face>();
			var newPoly = Duplicate();
			var faceIndices = ListFacesByVertexIndices();
			var existingFaceRoles = new Dictionary<Vector3, Roles>();
			var existingVertexRoles = new Dictionary<Vector3, Roles>();

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				var face = Faces[faceIndex];
				bool removeFace;
				if (filter != null)
				{
					removeFace = filter(face);
				}
				else
				{
					removeFace = IncludeFace(faceIndex, facesel, tagList);
				}
				removeFace = invertLogic ? !removeFace : removeFace;
				if (removeFace)
				{
					facesToRemove.Add(newPoly.Faces[faceIndex]);
				}
				else
				{
					existingFaceRoles[face.Centroid] = FaceRoles[faceIndex];
					var verts = face.GetVertices();
					for (var vertIndex = 0; vertIndex < verts.Count; vertIndex++)
					{
						var vert = verts[vertIndex];
						existingVertexRoles[vert.Position] = VertexRoles[faceIndices[faceIndex][vertIndex]];
					}
				}

			}

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();
			for (var i = 0; i < facesToRemove.Count; i++)
			{
				var face = facesToRemove[i];
				newPoly.Faces.Remove(face);
			}

			newPoly.Vertices.CullUnused();

			for (var faceIndex = 0; faceIndex < newPoly.Faces.Count; faceIndex++)
			{
				var face = newPoly.Faces[faceIndex];
				faceRoles.Add(existingFaceRoles[face.Centroid]);
				newFaceTags.Add(FaceTags[faceIndex]);
			}

			newPoly.FaceRoles = faceRoles;

			for (var i = 0; i < newPoly.Vertices.Count; i++)
			{
				var vert = newPoly.Vertices[i];
				vertexRoles.Add(existingVertexRoles[vert.Position]);
			}

			newPoly.VertexRoles = vertexRoles;

			return newPoly;
		}

		/// <summary>
		/// Offsets a mesh by moving each vertex by the specified distance along its normal vector.
		/// </summary>
		/// <param name="offset">Offset distance</param>
		/// <returns>The offset mesh</returns>
		public ConwayPoly Offset(double offset, bool randomize)
		{
			var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => offset).ToList();
			return Offset(offsetList, randomize);
		}

		public ConwayPoly Offset(double offset, FaceSelections facesel, string tags = "", bool randomize=false)
		{
			// This will only work if the faces are split and don't share vertices

			var tagList = StringToTagList(tags);
			var offsetList = new List<double>();
			double _offset = offset;

			for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
			{
				if (randomize) _offset = random.NextDouble() * (float)offset;
				var vertexOffset = IncludeFace(faceIndex, facesel, tagList) ? _offset : 0;
				for (var i = 0; i < Faces[faceIndex].GetVertices().Count; i++)
				{
					offsetList.Add(vertexOffset);
				}
			}

			return Offset(offsetList, randomize);
		}

		public ConwayPoly FaceMerge(FaceSelections facesel)
		{
			// TODO Breaks if the poly already has holes.
			var newPoly = Duplicate();
			newPoly = newPoly.FaceRemove(facesel);
			// Why do we do this?
			newPoly = newPoly.FaceRemove(FaceSelections.Outer);
			newPoly.FillHoles();
			return newPoly;
		}

		public ConwayPoly Offset(List<double> offset, bool randomize)
		{
			Vector3[] points = new Vector3[Vertices.Count];
			double _offset;
			var faceOffsets = new Dictionary<string, double>();
			
			for (int i = 0; i < Vertices.Count && i < offset.Count; i++)
			{
				var vert = Vertices[i];
				if (randomize)
				{
					if (faceOffsets.ContainsKey(vert.Halfedge.Face.Name))
					{
						_offset = faceOffsets[vert.Halfedge.Face.Name];
					}
					else
					{
						_offset = random.NextDouble() * (float) offset[i];
						faceOffsets[vert.Halfedge.Face.Name] = _offset;
					}
				}
				else
				{
					_offset = offset[i];
				}
				points[i] = vert.Position + Vertices[i].Normal * (float)_offset;
			}

			return new ConwayPoly(points, ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
		}

		/// <summary>
		/// Thickens each mesh edge in the plane of the mesh surface.
		/// </summary>
		/// <param name="offset">Distance to offset edges in plane of adjacent faces</param>
		/// <param name="boundaries">If true, attempt to ribbon boundary edges</param>
		/// <returns>The ribbon mesh</returns>
		// public ConwayPoly Ribbon(float offset, Boolean boundaries, float smooth)
		// {
		//
		// 	ConwayPoly ribbon = Duplicate();
		// 	var orig_faces = ribbon.Faces.ToArray();
		//
		// 	List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();
		//
		// 	// create new "vertex" faces
		// 	List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
		// 	for (int k = 0; k < Vertices.Count; k++)
		// 	{
		// 		Vertex v = ribbon.Vertices[k];
		// 		List<Vertex> new_vertices = new List<Vertex>();
		// 		List<Halfedge> halfedges = incidentEdges[k];
		// 		Boolean boundary = halfedges[0].Next.Pair != halfedges[halfedges.Count - 1];
		//
		// 		// if the edge loop around this vertex is open, close it with 'temporary edges'
		// 		if (boundaries && boundary)
		// 		{
		// 			Halfedge a, b;
		// 			a = halfedges[0].Next;
		// 			b = halfedges[halfedges.Count - 1];
		// 			if (a.Pair == null)
		// 			{
		// 				a.Pair = new Halfedge(a.Prev.Vertex) {Pair = a};
		// 			}
		//
		// 			if (b.Pair == null)
		// 			{
		// 				b.Pair = new Halfedge(b.Prev.Vertex) {Pair = b};
		// 			}
		//
		// 			a.Pair.Next = b.Pair;
		// 			b.Pair.Prev = a.Pair;
		// 			a.Pair.Prev = a.Pair.Prev ?? a; // temporary - to allow access to a.Pair's start/end vertices
		// 			halfedges.Add(a.Pair);
		// 		}
		//
		// 		foreach (Halfedge edge in halfedges)
		// 		{
		// 			if (halfedges.Count < 2)
		// 			{
		// 				continue;
		// 			}
		//
		// 			Vector3 normal = edge.Face != null ? edge.Face.Normal : Vertices[k].Normal;
		// 			Halfedge edge2 = edge.Next;
		//
		// 			var o1 = new Vertex(Vector3.Cross(normal, edge.Vector).normalized * offset);
		// 			var o2 = new Vertex(Vector3.Cross(normal, edge2.Vector).normalized * offset);
		//
		// 			if (edge.Face == null)
		// 			{
		// 				// boundary condition: create two new vertices in the plane defined by the vertex normal
		// 				Vertex v1 = new Vertex(v.Position + (edge.Vector * (1 / edge.Vector.magnitude) * -offset) +
		// 				                       o1.Position);
		// 				Vertex v2 = new Vertex(v.Position + (edge2.Vector * (1 / edge2.Vector.magnitude) * offset) +
		// 				                       o2.Position);
		// 				ribbon.Vertices.Add(v2);
		// 				ribbon.Vertices.Add(v1);
		// 				new_vertices.Add(v2);
		// 				new_vertices.Add(v1);
		// 				Halfedge c = new Halfedge(v2, edge2, edge, null);
		// 				edge.Next = c;
		// 				edge2.Prev = c;
		// 			}
		// 			else
		// 			{
		// 				// internal condition: offset each edge in the plane of the shared face and create a new vertex where they intersect eachother
		//
		// 				Vector3 start1 = edge.Vertex.Position + o1.Position;
		// 				Vector3 end1 = edge.Prev.Vertex.Position + o1.Position;
		// 				Line l1 = new Line(start1, end1);
		//
		// 				Vector3 start2 = edge2.Vertex.Position + o2.Position;
		// 				Vector3 end2 = edge2.Prev.Vertex.Position + o2.Position;
		// 				Line l2 = new Line(start2, end2);
		//
		// 				Vector3 intersection;
		// 				l1.Intersect(out intersection, l2);
		// 				ribbon.Vertices.Add(new Vertex(intersection));
		// 				new_vertices.Add(new Vertex(intersection));
		// 			}
		// 		}
		//
		// 		if ((!boundaries && boundary) == false) // only draw boundary node-faces in 'boundaries' mode
		// 			ribbon.Faces.Add(new_vertices);
		// 		all_new_vertices.Add(new_vertices);
		// 	}
		//
		// 	// change edges to reference new vertices
		// 	for (int k = 0; k < Vertices.Count; k++)
		// 	{
		// 		Vertex v = ribbon.Vertices[k];
		// 		if (all_new_vertices[k].Count < 1)
		// 		{
		// 			continue;
		// 		}
		//
		// 		int c = 0;
		// 		foreach (Halfedge edge in incidentEdges[k])
		// 		{
		// 			if (!ribbon.Halfedges.SetVertex(edge, all_new_vertices[k][c++]))
		// 				edge.Vertex = all_new_vertices[k][c];
		// 		}
		//
		// 		//v.Halfedge = null; // unlink from halfedge as no longer in use (culled later)
		// 		// note: new vertices don't link to any halfedges in the mesh until later
		// 	}
		//
		// 	// cull old vertices
		// 	ribbon.Vertices.RemoveRange(0, Vertices.Count);
		//
		// 	// use existing edges to create 'ribbon' faces
		// 	MeshHalfedgeList temp = new MeshHalfedgeList();
		// 	for (int i = 0; i < Halfedges.Count; i++)
		// 	{
		// 		temp.Add(ribbon.Halfedges[i]);
		// 	}
		//
		// 	List<Halfedge> items = temp.GetUnique();
		//
		// 	foreach (Halfedge halfedge in items)
		// 	{
		// 		if (halfedge.Pair != null)
		// 		{
		// 			// insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
		// 			if (smooth > 0.0)
		// 			{
		// 				if (smooth > 0.5)
		// 				{
		// 					smooth = 0.5f;
		// 				}
		//
		// 				Vertex[] newVertices = new Vertex[]
		// 				{
		// 					new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
		// 					new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
		// 					new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
		// 					new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
		// 				};
		// 				ribbon.Vertices.AddRange(newVertices);
		// 				Vertex[] new_vertices1 = new Vertex[]
		// 				{
		// 					halfedge.Vertex,
		// 					newVertices[0],
		// 					newVertices[3],
		// 					halfedge.Pair.Prev.Vertex
		// 				};
		// 				Vertex[] new_vertices2 = new Vertex[]
		// 				{
		// 					newVertices[1],
		// 					halfedge.Prev.Vertex,
		// 					halfedge.Pair.Vertex,
		// 					newVertices[2]
		// 				};
		// 				ribbon.Faces.Add(newVertices);
		// 				ribbon.Faces.Add(new_vertices1);
		// 				ribbon.Faces.Add(new_vertices2);
		// 			}
		// 			else
		// 			{
		// 				Vertex[] newVertices = new Vertex[]
		// 				{
		// 					halfedge.Vertex,
		// 					halfedge.Prev.Vertex,
		// 					halfedge.Pair.Vertex,
		// 					halfedge.Pair.Prev.Vertex
		// 				};
		//
		// 				ribbon.Faces.Add(newVertices);
		// 			}
		// 		}
		// 	}
		//
		// 	// remove original faces, leaving just the ribbon
		// 	//var orig_faces = Enumerable.Range(0, Faces.Count).Select(i => ribbon.Faces[i]);
		// 	foreach (Face item in orig_faces)
		// 	{
		// 		ribbon.Faces.Remove(item);
		// 	}
		//
		// 	// search and link pairs
		// 	ribbon.Halfedges.MatchPairs();
		//
		// 	return ribbon;
		// }

		/// <summary>
		/// Gives thickness to mesh faces by offsetting the mesh and connecting naked edges with new faces.
		/// </summary>
		/// <param name="distance">Distance to offset the mesh (thickness)</param>
		/// <param name="symmetric">Whether to extrude in both (-ve and +ve) directions</param>
		/// <returns>The extruded mesh (always closed)</returns>
		public ConwayPoly Extrude(double distance, bool symmetric, bool randomize)
		{
			var offsetList = Enumerable.Repeat(distance, Vertices.Count).ToList();
			return _Extrude(offsetList, symmetric, randomize);
		}

		public ConwayPoly Extrude(float amount, FaceSelections facesel, string tags = "", bool randomize=false)
		{
			var tagList = StringToTagList(tags);
			var debugFaces = new[] { 0, 1};

			ConwayPoly result;
			result = Duplicate();
			var affectedFaces = new List<Face>();
			for (var i = 0; i < result.Faces.Count; i++)
			{
				if (!debugFaces.Contains(i)) continue;
				var face = result.Faces[i];
				if (IncludeFace(i, facesel, tagList))
				{
					affectedFaces.Add(face);
				}
			}

			foreach (var face in affectedFaces)
			{
					var hole = result.Faces.Remove(face);
					var holeVerts = hole.Select(e => e.Vertex).ToList();
					var newVerts = hole.Select(e => new Vertex(e.Vertex.Position + face.Normal * amount)).ToList();
					result.Vertices.AddRange(newVerts);
					for (int j=0; j<newVerts.Count; j++)
					{
						var side = new List<Vertex>();
//						side.Add(result.Vertices[k - 1]);
						side.Add(newVerts[j]);
						side.Add(newVerts[(j+1) % newVerts.Count]);
						side.Add(holeVerts[j]);
						result.Faces.Add(side);
					}



//					foreach (var v in newVerts)
//					{
//						result.Vertices.Add(v);
//					}
//					result.Faces.Add(hole.Select(e=>e.Vertex));

//					var newTopVerts = new List<Vector3>();
//					var holeVerts = new List<Vector3>();
//
//					var extrusionFaces = new List<List<int>>();
//					var topFace = new List<int>();
//
//					var origFaceVerts = face.GetVertices();
//
//					for (var j = 0; j < origFaceVerts.Count; j++)
//					{
//						var v = origFaceVerts[j];
//						newTopVerts.Add(v.Position + face.Normal * amount);
//						topFace.Add(j);
////						holeVerts.Add(v.Position);
//						holeVerts.Add(v.Position + face.Normal * (amount/2));
//						extrusionFaces.Add(new List<int>
//						{
//							(j % origFaceVerts.Count) + origFaceVerts.Count,
//							((j + 1) % origFaceVerts.Count) + origFaceVerts.Count,
//							(j + 1) % origFaceVerts.Count,
//							j,
//						});
//					}
//
//					var allNewVerts = newTopVerts;
//					allNewVerts.AddRange(holeVerts);
//					extrusionFaces.Add(topFace);
//					var extrusionPoly = new ConwayPoly(
//						allNewVerts,
//						extrusionFaces,
//						Enumerable.Repeat(Roles.New, extrusionFaces.Count).ToList(),
//						Enumerable.Repeat(Roles.New, allNewVerts.Count).ToList()
//					);
//					result.Append(extrusionPoly);
//					result.Faces.Remove(result.Faces.First(f=>f.Centroid==face.Centroid));  // TODO implement without iteration
//				}

			}

//			for (var i = 0; i < Faces.Count; i++)
//			{
//				if (!IncludeFace(i, facesel, tagList))
//				{
//					result.Append(Faces[i].Detach());
//				}
//				else
//				{
//					//List<Halfedge> holeEdges = result.Faces.Remove(result.Faces[i]);
//				}
//			}

			result.Halfedges.MatchPairs();

			result.FaceRoles = Enumerable.Repeat(Roles.Existing, result.Faces.Count).ToList();
			result.VertexRoles = Enumerable.Repeat(Roles.Existing, result.Vertices.Count).ToList();

			return result;

		}

		private ConwayPoly _Extrude(List<double> distance, bool symmetric, bool randomize)
		{

			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();

			ConwayPoly result, top;

			if (symmetric)
			{
				result = Offset(distance.Select(d => 0.5 * d).ToList(), randomize);
				top = Offset(distance.Select(d => -0.5 * d).ToList(), randomize);
			}
			else
			{
				result = Duplicate();
				top = Offset(distance, randomize);
			}
			result.FaceRoles = Enumerable.Repeat(Roles.Existing, result.Faces.Count).ToList();
			result.VertexRoles = Enumerable.Repeat(Roles.Existing, result.Vertices.Count).ToList();
			newFaceTags.AddRange(FaceTags);

			result.Halfedges.Flip();

			// append top to ext (can't use Append() because copy would reverse face loops)
			foreach (var v in top.Vertices) result.Vertices.Add(v);
			foreach (var h in top.Halfedges) result.Halfedges.Add(h);
			for (var topFaceIndex = 0; topFaceIndex < top.Faces.Count; topFaceIndex++)
			{
				var f = top.Faces[topFaceIndex];
				result.Faces.Add(f);
				result.FaceRoles.Add(Roles.New);
				result.VertexRoles.AddRange(Enumerable.Repeat(Roles.New, f.Sides));
				newFaceTags.Add(new HashSet<Tuple<string, TagType>>(FaceTags[topFaceIndex]));
			}


			// get indices of naked halfedges in source mesh
			var naked = Halfedges.Select((item, index) => index).Where(i => Halfedges[i].Pair == null).ToList();

			if (naked.Count > 0)
			{
				int n = Halfedges.Count;
				int failed = 0;
				foreach (var i in naked)
				{
					var newFaceTagSet = new HashSet<Tuple<string, TagType>>();
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
						int prevFaceIndex = result.Faces.IndexOf(result.Halfedges[i].Face);
						var prevFaceTagSet = FaceTags[prevFaceIndex];
						newFaceTagSet.UnionWith(prevFaceTagSet.Where(t => t.Item2==TagType.Extrovert));
						newFaceTags.Add(newFaceTagSet);
					}
				}
			}

			result.FaceTags = newFaceTags;
			result.Halfedges.MatchPairs();

			return result;
		}

		public void ScalePolyhedra(float scale = 1)
		{

			if (Vertices.Count > 0)
			{
				// Find the furthest vertex
				Vertex max = Vertices.OrderByDescending(x => x.Position.sqrMagnitude).FirstOrDefault();
				float unitScale = 1.0f / max.Position.magnitude;
				for (var i = 0; i < Vertices.Count; i++)
				{
					Vertices[i].Position *= unitScale * scale;
				}
			}

		}

		public ConwayPoly Slice(float lower, float upper, string tags="")
		{

			if (lower > upper) (upper, lower) = (lower, upper);
			float yMax = Vertices.Max(v => v.Position.y);
			float yMin = Vertices.Min(v => v.Position.y);
			lower = Mathf.Lerp(yMin, yMax, lower);
			upper = Mathf.Lerp(yMin, yMax, upper);
			Func<Face, bool> slice = x => x.Centroid.y > lower && x.Centroid.y < upper;
			return _FaceRemove(FaceSelections.All, tags, true, slice);
		}

		#endregion

		#region Grids

		public static ConwayPoly MakeUnitileGrid(int pattern, int gridShape, int rows = 5, int cols = 5, bool weld=false)
		{
			var ut = new Unitile(pattern, rows, cols, true);

			switch (gridShape)
			{
				case 0:
					ut.plane();
					break;
				case 1:
					ut.torus();
					break;
				case 2:
					ut.conic_frust(1);
					break;
				case 3:
					ut.conic_frust(0.00001f);
					break;
				case 4:
					ut.conic_frust();
					break;
				case 5:
					ut.mobius();
					break;
				case 6:
					ut.torus_trefoil();
					break;
				case 7:
					ut.klein();
					break;
				case 8:
					ut.klein2();
					break;
				case 9:
					ut.roman();
					break;
				case 10:
					ut.roman_boy();
					break;
				case 11:
					ut.cross_cap();
					break;
				case 12:
					ut.cross_cap2();
					break;
			}
			var vertexRoles = Enumerable.Repeat(Roles.New, ut.raw_verts.Count);

			var faceRoles = new List<Roles>();
			int foo, isEven, width, height, coloringOffset;

			for (int i = 0; i < ut.raw_faces.Count; i++)
			{
				switch (pattern)
				{
					case 1:
						isEven = cols % 2==0 ? (Mathf.FloorToInt(i / (float) cols)) % 2 : 0;
						foo = ((i + isEven) % 2) + 2;
						faceRoles.Add((Roles)foo);
						break;
					case 2:
						// int width = Mathf.CeilToInt((rows / Mathf.Sqrt(3))) * 2 + 1;
						// int height = ut.raw_faces.Count / width;
						// isEven = 0;
						// foo = ((i/4/width) + isEven) % 2;
						foo = i < ut.raw_faces.Count / 2 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					case 3:
						width = Mathf.CeilToInt((rows / Mathf.Sqrt(3)));
						height = ut.raw_faces.Count / width;
						coloringOffset = i < ut.raw_faces.Count / 2 ? 0 : 1;
						foo = i / (height / 2);
						if (coloringOffset==1 && width % 3 == 0) coloringOffset += 1;
						if (coloringOffset==1 && width % 3 == 2) coloringOffset += 2;
						foo += coloringOffset;
						foo = (foo % 3) + 2;
						faceRoles.Add((Roles)foo);
						break;
					case 4:
						foo = ut.raw_faces[i].Count == 3 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					case 5:  // TODO
						width = rows;
						height = (Mathf.FloorToInt(cols / 4) + 1) * 4;
						foo = i < ut.raw_faces.Count / 2 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					case 6:  // TODO
						foo = ut.raw_faces[i].Count == 3 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					// case 7:
					// 	foo = i < ut.raw_faces.Count / 2 ? 0 : 1;
					// 	faceRoles.Add((Roles)foo);
					// 	break;
					case 8:  // TODO
						foo = ut.raw_faces[i].Count == 3 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					case 9:  // TODO
						foo = ut.raw_faces[i].Count == 8 ? 2 : 3;
						faceRoles.Add((Roles)foo);
						break;
					case 10:
						switch (ut.raw_faces[i].Count)
						{
							case 3: foo = 2; break;
							case 4: foo = 3; break;
							case 6: foo = 4; break;
							default: foo = 5; break;
						};
						faceRoles.Add((Roles)foo);
						break;
					case 11:
						switch (ut.raw_faces[i].Count)
						{
							case 4: foo = 2; break;
							case 6: foo = 3; break;
							case 12: foo = 4; break;
							default: foo = 5; break;
						};
						faceRoles.Add((Roles)foo);
						break;
					default:
						faceRoles.Add((Roles)((i % 2) + 2));
						break;
				}


			}

			for (var i = 0; i < ut.raw_faces[0].Count; i++)
			{
				var idx = ut.raw_faces[0][i];
				var v = ut.raw_verts[idx];
			}

			var poly = new ConwayPoly(ut.raw_verts, ut.raw_faces, faceRoles, vertexRoles);
			poly.Recenter();
			if (gridShape > 0 && weld) poly = poly.Weld(0.001f);
			return poly;
		}



		public static ConwayPoly MakeGrid(int rows = 5, int cols = 5, float rowScale = .3f, float colScale = .3f)
		{
			var faceRoles = new List<Roles>();

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
					int corner = (row * cols) + col;
					var face = new List<int>
					{
						corner,
						corner - 1,
						corner - cols - 1,
						corner - cols
					};
					faceIndices.Add(face);
					faceRoles.Add((row + col) % 2 == 0 ? Roles.New : Roles.NewAlt);
				}
			}

			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

 		public static ConwayPoly MakeIsoGrid(int cols = 5, int rows = 5)
		{

			float colScale = 1;
			float rowScale = Mathf.Sqrt(3)/2;
			
			float colOffset = rows * colScale * 0.5f;
			float rowOffset = cols * rowScale * 0.5f;

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<List<int>>();

			for (int row = 0; row <= rows; row++)
			{
				for (int col = 0; col <= cols; col++)
				{
					var pos = new Vector3(-colOffset + col * colScale, 0, -rowOffset + row * rowScale);
					if (row % 2 > 0)
					{
						pos.x -= colScale/2f;
					}
					vertexPoints.Add(pos);
				}
			}
			
			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < cols; col++)
				{
					int corner = row * (cols + 1) + col;

					if (row % 2 == 0)
					{
						var face1 = new List<int>
						{
							corner,
							corner + cols + 1,
							corner + cols + 2,
						};
						faceIndices.Add(face1);
				
						var face2 = new List<int>
						{
							corner,
							corner + cols + 2,
							corner + 1
						};
						faceIndices.Add(face2);
						
					}
					else
					{
						var face1 = new List<int>
						{
							corner,
							corner + cols + 1,
							corner + 1
						};
						faceIndices.Add(face1);
						
						var face2 = new List<int>
						{
							corner + 1,
							corner + cols + 1,
							corner + cols + 2
						};
						faceIndices.Add(face2);

					}
				}
			}

			var faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count);
			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			poly.Recenter();
			return poly;
		}
		
		public static ConwayPoly MakeHexGrid(int cols = 4, int rows = 4)
		{
			// Flag if the number of columns is odd.
			bool oddCols = cols % 2 == 1;

			// We measure rows by the 6 triangles in each hex
			cols = ((cols * 3) / 2) + (oddCols?3:1);
			rows = rows * 2 + 2;

			float colScale = 1f;
			float rowScale = Mathf.Sqrt(3)/2;
			
			float colOffset = rows * colScale * 0.5f;
			float rowOffset = cols * rowScale * 0.5f;

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<List<int>>();

			for (int row = 0; row <= rows + 2; row++)
			{
				for (int col = 0; col <= cols; col++)
				{
					var pos = new Vector3(-colOffset + col * colScale, 0, -rowOffset + row * rowScale);
					if (row % 2 > 0)
					{
						pos.x -= colScale/2f;
					}
					vertexPoints.Add(pos);
				}
			}
			
			for (int row = 0; row < rows - 3; row += 2)
			{
				for (int col = 0; col < cols - 3; col += 3)
				{
					int corner = row * (cols + 1) + col;

					var hex1 = new List<int>
					{
						corner,
						corner + cols + 1,
						corner + cols + cols + 2,
						corner + cols + cols + 3,
						corner + cols + 3,
						corner + 1
					};
					faceIndices.Add(hex1);

					if (oddCols && col == cols - 4) continue;
					corner += cols + 3;
					var hex2 = new List<int>
					{
						corner,
						corner + cols,
						corner + cols + cols + 2,
						corner + cols + cols + 3,
						corner + cols + 2,
						corner + 1
					};
					faceIndices.Add(hex2);
				}
			}

			var faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count);
			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			var poly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
			poly.Recenter();
			return poly;
		}

		public static ConwayPoly MakePolarGrid(int sides = 6, int divisions = 4)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<List<int>>();

			var faceRoles = new List<Roles>();

			float theta = Mathf.PI * 2 / sides;

			int start, end, inc;

			start = 0;
			end = sides;
			inc = 1;
			float radiusStep = 1f / divisions;

			vertexPoints.Add(Vector3.zero);

			for (float radius = radiusStep; radius <= 1; radius += radiusStep)
			{
				for (int i = start; i != end; i += inc)
				{
					float angle = theta * i + theta;
					vertexPoints.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
				}
			}

			for (int i = 0; i < sides; i++)
			{
				faceIndices.Add(new List<int>{0, (i + 1) % sides + 1, i + 1});
				if (sides % 2 == 0) // Even sides
				{
					faceRoles.Add((Roles)(i % 2) + 2);
				}
				else
				{
					int lastCellMod = (i == sides - 1 && sides % 3 == 1) ? 1 : 0;  //  Fudge the last cell to stop clashes in some cases
					faceRoles.Add((Roles)((i + lastCellMod) % 3) + 2);
				}
			}

			for (int d = 0; d < divisions - 1; d++)
			{
				for (int i = 0; i < sides; i++)
				{
					int rowStart = d * sides + 1;
					int nextRowStart = (d + 1) * sides + 1;
					faceIndices.Add(new List<int>
					{
						rowStart + i,
						rowStart + (i + 1) % sides,
						nextRowStart + (i + 1) % sides,
						nextRowStart + i
					});
					if (sides % 2 == 0) // Even sides
					{
						faceRoles.Add((Roles)((i + d) % 2) + 2);
					}
					else
					{
						int lastCellMod = (i == sides - 1 && sides % 3 == 1) ? 1 : 0;  //  Fudge the last cell to stop clashes in some cases
						faceRoles.Add((Roles)((i + d + lastCellMod + 1) % 3) + 2);
					}
				}
			}

			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);

		}

		#endregion

		#region Canonicalize

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

				for (var i = 0; i < faceVertices.Count; i++)
				{
					var vertex = faceVertices[i];
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

			for (var i = 0; i < poly.Faces.Count; i++)
			{
				var newCenter = poly.Faces[i].Centroid;
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
			var previousVertexRoles = VertexRoles;
			ConwayPoly canonicalized = Duplicate();
			if (thresholdAdjust > 0) Adjust(canonicalized, thresholdAdjust);
			if (thresholdPlanarize > 0) Planarize(canonicalized, thresholdPlanarize);
			canonicalized.FaceRoles = previousFaceRoles;
			canonicalized.VertexRoles = previousVertexRoles;
			return canonicalized;
		}

		public ConwayPoly Hinge(float amount)
		{
			
			// Rotate singly connected faces around the connected edge
			foreach (Face f in Faces)
			{
				Halfedge hinge = null;
				
				// Find a single connected edge
				foreach (Halfedge e in f.GetHalfedges())
				{
					if (e.Pair != null)  // This edge is connected
					{
						if (hinge == null)  // Our first connected edge
						{
							// Record the first connected edge and keep looking
							hinge = e;
							
						}
						else  // We already found a hinge for this face
						{
							// Therefore this Face has more than 1 connected edge
							hinge = null;
							break;
						}
					}
				}
				
				if (hinge != null)  // We found a single hinge for this face
				{
					Vector3 axis = hinge.Vector;
					Quaternion rotation = Quaternion.AngleAxis(amount, axis);

					var vs = f.GetVertices();
					for (var i = 0; i < vs.Count; i++)
					{
						Vertex v = vs[i];
						// Only rotate vertices that aren't part of the hinge
						if (v != hinge.Vertex && v != hinge.Pair.Vertex)
						{
							v.Position -= hinge.Vertex.Position;
							v.Position = rotation * v.Position;
							v.Position += hinge.Vertex.Position;
						}
					}
				}
			}

			return this;
		}
		
		public ConwayPoly Spherize(float amount, FaceSelections vertexsel)
		{
			
			// TODO - preserve planar faces

			var vertexPoints = new List<Vector3>();
			var faceIndices = ListFacesByVertexIndices();

			for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
			{
				var vertex = Vertices[vertexIndex];
				if (IncludeVertex(vertexIndex, vertexsel))
				{
					vertexPoints.Add(Vector3.LerpUnclamped(vertex.Position, vertex.Position.normalized, amount));
					VertexRoles[vertexIndex] = Roles.Existing;
				}
				else
				{
					vertexPoints.Add(vertex.Position);
					VertexRoles[vertexIndex] = Roles.Ignored;
				}
			}

			var conway = new ConwayPoly(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
			return conway;
		}

		#endregion

		#region General Methods

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

		public List<List<Halfedge>> FindBoundaries()
		{
			var looped = new HashSet<Halfedge>();
			var loops = new List<List<Halfedge>>();
			
			foreach (var startHalfedge in Halfedges)
			{
				// If it's not a bare edge or we've already checked it
				if (startHalfedge.Pair != null || looped.Contains(startHalfedge)) continue;

				var loop = new List<Halfedge>();
				var currLoopEdge = startHalfedge;
				int escapeClause = 0;
				do
				{
					loop.Add(currLoopEdge);
					looped.Add(currLoopEdge);
					Halfedge nextLoopEdge = null;
					var possibleEdges = currLoopEdge.Prev.Vertex.Halfedges;
					//possibleEdges.Reverse();
					foreach (var edgeToTest in possibleEdges)
					{
						if (currLoopEdge != edgeToTest && edgeToTest.Pair == null)
						{
							nextLoopEdge = edgeToTest;
							break;
						};
					}

					if (nextLoopEdge != null)
					{
						currLoopEdge = nextLoopEdge;
					}
					escapeClause++;
				} while (currLoopEdge != startHalfedge && escapeClause < 1000);

				if (loop.Count >= 3)
				{
					loops.Add(loop);
				}

				
			}

			return loops;
		}

		public void FillHoles()
		{
			var newFaceTags = new List<HashSet<Tuple<string, TagType>>>();
			var boundaries = FindBoundaries();
			foreach (var boundary in boundaries)
			{
				var success = Faces.Add(boundary.Select(x => x.Vertex));
				if (!success)
				{
					boundary.Reverse();
					success = Faces.Add(boundary.Select(x => x.Vertex));
				}

				if (success)
				{
					FaceRoles.Add(Roles.New);
					newFaceTags.Add(new HashSet<Tuple<string, TagType>>());
				}

				Halfedges.MatchPairs();
			}
		}
		
//		public ConwayPoly ElongateHoles()
//		{
//			
//		}		
//
//		public ConwayPoly GyroElongateHoles()
//		{
//			
//		}		
		
		/// <summary>
		/// Gets the indices of vertices in each face loop (i.e. index face-vertex data structure).
		/// Used for duplication and conversion to other mesh types, such as Rhino's.
		/// </summary>
		/// <returns>An array of lists of vertex indices.</returns>
		public List<int>[] ListFacesByVertexIndices()
		{

			var fIndex = new List<int>[Faces.Count];
			var vlookup = new Dictionary<Guid, int>();

			for (int i = 0; i < Vertices.Count; i++)
			{
				vlookup.Add(Vertices[i].Name, i);
			}

			for (int i = 0; i < Faces.Count; i++)
			{
				var vertIndices = new List<int>();
				var vs = Faces[i].GetVertices();
				for (var vertIndex = 0; vertIndex < vs.Count; vertIndex++)
				{
					Vertex v = vs[vertIndex];
					vertIndices.Add(vlookup[v.Name]);
				}
				fIndex[i] = vertIndices;
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
			Append(other, Vector3.zero, Quaternion.identity, 1.0f);
		}

		public void Append(ConwayPoly other, Vector3 transform, Quaternion rotation, float scale)
		{
			ConwayPoly dup = other.Duplicate(transform, rotation, scale);

			Vertices.AddRange(dup.Vertices);
			for (var i = 0; i < dup.Halfedges.Count; i++)
			{
				Halfedges.Add(dup.Halfedges[i]);
			}

			for (var i = 0; i < dup.Faces.Count; i++)
			{
				Faces.Add(dup.Faces[i]);
			}

			FaceRoles.AddRange(dup.FaceRoles);
			FaceTags.AddRange(dup.FaceTags);
			VertexRoles.AddRange(dup.VertexRoles);
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
				case FaceSelections.NineSided:
					return 9;
				case FaceSelections.TenSided:
					return 10;
				case FaceSelections.ElevenSided:
					return 11;
				case FaceSelections.TwelveSided:
					return 12;
			}

			return 0;
		}

		public bool IncludeFace(int faceIndex, FaceSelections facesel, IEnumerable<Tuple<string, TagType>> tagList = null)
		{
			bool include = true;

			// Return true if any tags match
			if (tagList != null && tagList.Any())
			{
				var matches = tagList.Intersect(FaceTags[faceIndex]);
				include = matches.Any();
			}

			float angle;
			switch (facesel)
			{
				case FaceSelections.All:
					return include && true;
				case FaceSelections.EvenSided:
					return include && Faces[faceIndex].Sides % 2 == 0;
				case FaceSelections.OddSided:
					return include && Faces[faceIndex].Sides % 2 != 0;
				case FaceSelections.PSided:
					return include && Faces[faceIndex].Sides == basePolyhedraInfo.P;
				case FaceSelections.QSided:
					return include && Faces[faceIndex].Sides == basePolyhedraInfo.Q;
				case FaceSelections.FacingUp:
					return include && Faces[faceIndex].Normal.y > TOLERANCE;
				case FaceSelections.FacingStraightUp:
					return include && Vector3.Angle(Vector3.up, Faces[faceIndex].Normal) < TOLERANCE;
				case FaceSelections.FacingForward:
					return include && Faces[faceIndex].Normal.z > TOLERANCE;
				case FaceSelections.FacingStraightForward:
					return include && Vector3.Angle(Vector3.forward, Faces[faceIndex].Normal) < TOLERANCE;
				case FaceSelections.FacingLevel:
					return include && Math.Abs(Faces[faceIndex].Normal.y) < TOLERANCE;
				case FaceSelections.FacingDown:
					return include && Faces[faceIndex].Normal.y < -TOLERANCE;
				case FaceSelections.FacingStraightDown:
					return include && Vector3.Angle(Vector3.down, Faces[faceIndex].Normal) < TOLERANCE;
				case FaceSelections.FacingCenter:
					angle = Vector3.Angle(-Faces[faceIndex].Normal, Faces[faceIndex].Centroid);
					return include && Math.Abs(angle) < TOLERANCE || Math.Abs(angle - 180) < TOLERANCE;
				case FaceSelections.FacingIn:
					return include && Vector3.Angle(-Faces[faceIndex].Normal, Faces[faceIndex].Centroid) > 90 - TOLERANCE;
				case FaceSelections.FacingOut:
					return include && Vector3.Angle(-Faces[faceIndex].Normal, Faces[faceIndex].Centroid) < 90 + TOLERANCE;
				case FaceSelections.TopHalf:
					return include && Faces[faceIndex].Centroid.y > 0;
				case FaceSelections.Existing:
					return include && FaceRoles[faceIndex] == Roles.Existing || FaceRoles[faceIndex] == Roles.ExistingAlt;
				case FaceSelections.Ignored:
					return include && FaceRoles[faceIndex] == Roles.Ignored;
				case FaceSelections.New:
					return include && FaceRoles[faceIndex] == Roles.New;
				case FaceSelections.NewAlt:
					return include && FaceRoles[faceIndex] == Roles.NewAlt;
				case FaceSelections.AllNew:
					return include && FaceRoles[faceIndex] == Roles.New || FaceRoles[faceIndex] == Roles.NewAlt;
				case FaceSelections.Odd:
					return include && faceIndex % 2 == 1;
				case FaceSelections.Even:
					return include && faceIndex % 2 == 0;
				case FaceSelections.OnlyFirst:
					return include && faceIndex == 0;
				case FaceSelections.ExceptFirst:
					return include && faceIndex != 0;
				case FaceSelections.Inner:
					return include && Faces[faceIndex].GetHalfedges().All(i=>i.Pair!=null);
				case FaceSelections.Outer:
					return include && Faces[faceIndex].GetHalfedges().Any(i=>i.Pair==null);
				case FaceSelections.Smaller:
					return include && Faces[faceIndex].GetArea() <= 0.05f;
				case FaceSelections.Larger:
					return include && Faces[faceIndex].GetArea() > 0.05f;
				case FaceSelections.Random:
					return include && random.NextDouble() < 0.5;
				case FaceSelections.None:
					return include && false;
			}

			return include && (Faces[faceIndex].Sides == FaceSelectionToSides(facesel));
		}

		public bool IncludeVertex(int vertexIndex, FaceSelections vertexsel)
		{
			float angle;
			switch (vertexsel)
			{
				case FaceSelections.All:
					return true;
				// TODO
				case FaceSelections.PSided:
					return Vertices[vertexIndex].Halfedges.Count == basePolyhedraInfo.P;
				case FaceSelections.QSided:
					return Vertices[vertexIndex].Halfedges.Count == basePolyhedraInfo.Q;
				case FaceSelections.ThreeSided:
					return Vertices[vertexIndex].Halfedges.Count <= 3; // Weird but it will do for now
				case FaceSelections.FourSided:
					return Vertices[vertexIndex].Halfedges.Count == 4;
				case FaceSelections.FiveSided:
					return Vertices[vertexIndex].Halfedges.Count == 5;
				case FaceSelections.SixSided:
					return Vertices[vertexIndex].Halfedges.Count == 6;
				case FaceSelections.SevenSided:
					return Vertices[vertexIndex].Halfedges.Count == 7;
				case FaceSelections.EightSided:
					return Vertices[vertexIndex].Halfedges.Count == 8;
				case FaceSelections.FacingUp:
					return Vertices[vertexIndex].Normal.y > TOLERANCE;
				case FaceSelections.FacingLevel:
					return Math.Abs(Vertices[vertexIndex].Normal.y) < TOLERANCE;
				case FaceSelections.FacingDown:
					return Vertices[vertexIndex].Normal.y < -TOLERANCE;
				case FaceSelections.FacingCenter:
					angle = Vector3.Angle(-Vertices[vertexIndex].Normal, Vertices[vertexIndex].Position);
					return Math.Abs(angle) < TOLERANCE || Math.Abs(angle - 180) < TOLERANCE;
				case FaceSelections.FacingIn:
					return Vector3.Angle(-Vertices[vertexIndex].Normal, Vertices[vertexIndex].Position) > 90 - TOLERANCE;
				case FaceSelections.FacingOut:
					return Vector3.Angle(-Vertices[vertexIndex].Normal, Vertices[vertexIndex].Position) < 90 + TOLERANCE;
				case FaceSelections.Existing:
					return VertexRoles[vertexIndex] == Roles.Existing;
				case FaceSelections.Ignored:
					return VertexRoles[vertexIndex] == Roles.Ignored;
				case FaceSelections.New:
					return VertexRoles[vertexIndex] == Roles.New;
				case FaceSelections.NewAlt:
					return VertexRoles[vertexIndex] == Roles.NewAlt;
				case FaceSelections.AllNew:
					return VertexRoles[vertexIndex] == Roles.New || VertexRoles[vertexIndex] == Roles.NewAlt;
				case FaceSelections.Odd:
					return vertexIndex % 2 == 1;
				case FaceSelections.Even:
					return vertexIndex % 2 == 0;
				case FaceSelections.OnlyFirst:
					return vertexIndex == 0;
				case FaceSelections.ExceptFirst:
					return vertexIndex != 0;
				case FaceSelections.Random:
					return random.NextDouble() < 0.5;
			}

			return Vertices[vertexIndex].Halfedges.Count == FaceSelectionToSides(vertexsel);
		}

		#endregion

		public void InitOctree()
		{
			octree = new PointOctree<Vertex>(1, Vector3.zero, 32);
			for (var i = 0; i < Vertices.Count; i++)
			{
				var v = Vertices[i];
				octree.Add(v, v.Position);
			}
		}

		public Vertex[] FindNeighbours(Vertex v, float distance)
		{
			return octree.GetNearby(v.Position, distance);
		}

		public ConwayPoly Weld(float distance)
		{
			if (distance < .00001f) distance = .00001f;  // We always weld by a very small amount. Disable the op if you don't want to weld at all.
			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<IEnumerable<int>>();
			var faceRoles = new List<Roles>();
			var vertexRoles = new List<Roles>();
			var reverseDict = new Dictionary<Vertex, int>();
			var vertexReplacementDict = new Dictionary<int, int>();

			var groups = new List<Vertex[]>();
			var checkedVerts = new HashSet<Vertex>();

			InitOctree();

			for (var i = 0; i < Vertices.Count; i++)
			{
				var v = Vertices[i];
				reverseDict[v] = i;
				if (checkedVerts.Contains(v)) continue;
				checkedVerts.Add(v);
				var neighbours = FindNeighbours(v, distance);
				if (neighbours.Length < 1) continue;
				groups.Add(neighbours);
				checkedVerts.UnionWith(neighbours);
			}

			foreach (var group in groups)
			{
				vertexPoints.Add(group[0].Position);
				int VertToKeep = -1;
				for (var i = 0; i < group.Length; i++)
				{
					var vertIndex = reverseDict[group[i]];
					if (i == 0)
					{
						VertToKeep = vertexPoints.Count - 1;
					}
					vertexReplacementDict[vertIndex] = VertToKeep;
				}
			}

			foreach (var faceVertIndices in ListFacesByVertexIndices())
			{
				var newFaceVertIndices = new List<int>();
				foreach (var vertIndex in faceVertIndices)
				{
					newFaceVertIndices.Add(vertexReplacementDict[vertIndex]);
				}
				faceIndices.Add(newFaceVertIndices);
			}

			faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count).ToList();
			vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count).ToList();

			return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
		}

		private void InitIndexed(IEnumerable<Vector3> verticesByPoints, IEnumerable<IEnumerable<int>> facesByVertexIndices)
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
			return new ConwayPoly(ListVerticesByPoints(), ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
		}

		public ConwayPoly Duplicate(Vector3 transform, Quaternion rotation, float scale)
		{
			IEnumerable<Vector3> verts;

			if (transform == Vector3.zero && rotation == Quaternion.identity && scale == 1.0f)
			{
				// Fast path
				 verts = ListVerticesByPoints();
			}
			else
			{
				verts = ListVerticesByPoints().Select(i => rotation * i * scale + transform);
			}

			return new ConwayPoly(verts, ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
		}

		public ConwayPoly AppendMany(ConwayPoly stashed, FaceSelections facesel, string tags = "", float scale = 1, float angle=0, float offset=0, bool toFaces=true)
		{
			var tagList = StringToTagList(tags);
			var result = Duplicate();

			if (toFaces)
			{
				for (var i = 0; i < Faces.Count; i++)
				{
					var face = Faces[i];
					if (IncludeFace(i, facesel, tagList))
					{
						Vector3 transform = face.Centroid + face.Normal * offset;
						var rot = Quaternion.AngleAxis(angle, face.Normal);
						result.Append(stashed, transform, rot, scale);
					}
				}
			}
			else
			{
				for (var i = 0; i < Vertices.Count; i++)
				{
					var vert = Vertices[i];
					if (IncludeVertex(i, facesel))
					{
						Vector3 transform = vert.Position + vert.Normal * offset;
						var rot = Quaternion.AngleAxis(angle, vert.Normal);
						result.Append(stashed, transform, rot, scale);
					}
				}
			}
			return result;
		}
	}
}