using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Wythoff;
using UnityEngine;

namespace Conway {

    /// <summary>
    /// A class for manifold meshes which uses the Halfedge data structure.
    /// </summary>

    public class ConwayPoly {

        #region constructors

        public ConwayPoly() {
            Halfedges = new MeshHalfedgeList(this);
            Vertices = new MeshVertexList(this);
            Faces = new MeshFaceList(this);
        }

        public ConwayPoly(WythoffPoly source) : this() {
            
            // Add vertices
            Vertices.Capacity = source.VertexCount;
            foreach (Vector p in source.Vertices) {
                Vertices.Add(new Vertex(p.getVector3()));
            }

            // Add faces (and construct halfedges and store in hash table)
            foreach (Wythoff.Face face in source.faces) {
                var v = new Vertex[face.points.Count];
                for (int i = 0; i < face.points.Count; i++) {
                    v[i] = Vertices[face.points[i]];
                }

                if (!Faces.Add(v)) {
                    // Failed. Let's try flipping the face
                    Array.Reverse(v);
                    if (!Faces.Add(v))
                    {
                        Debug.LogError("Failed even after flipping.");
                    };
                }
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }
        
        public ConwayPoly(List<Vector3> positions, List<List<int>> faces) {
            
            // Add vertices
            Vertices.Capacity = positions.Count;
            foreach (var p in positions) {
                Vertices.Add(new Vertex(p));
            }

            // Add faces (and construct halfedges and store in hash table)
            foreach (var face in faces) {
                var v = new Vertex[face.Count];
                for (int i = 0; i < face.Count; i++) {
                    v[i] = Vertices[face[i]];
                }

                if (!Faces.Add(v)) {
                    // Failed. Let's try flipping the face
                    Array.Reverse(v);
                    Faces.Add(v);
                }
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }

        private ConwayPoly(IEnumerable<Vector3> verticesByPoints, IEnumerable<IEnumerable<int>> facesByVertexIndices) : this()
        {
            InitIndexed(verticesByPoints, facesByVertexIndices);

            Vertices.CullUnused();
        }

        private void InitIndexed(IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices) {
            // Add vertices
            foreach (Vector3 p in verticesByPoints) {
                Vertices.Add(new Vertex(p));
            }

            // Add faces
            foreach (IEnumerable<int> indices in facesByVertexIndices) {
                Faces.Add(indices.Select(i => Vertices[i]));
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }

        public ConwayPoly Duplicate() {
            // Export to face/vertex and rebuild
            return new ConwayPoly(ListVerticesByPoints(), ListFacesByVertexIndices());
        }

        #endregion

        #region properties

        public MeshHalfedgeList Halfedges { get; private set; }
        public MeshVertexList Vertices { get; set; }
        public MeshFaceList Faces { get; private set; }

        public bool IsValid {
            get {
                if (Halfedges.Count == 0) {
                    return false;
                }

                if (Vertices.Count == 0) {
                    return false;
                }

                if (Faces.Count == 0) {
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
        public ConwayPoly Dual() {
            
            // Create vertices from faces
            List<Vector3> vertexPoints = new List<Vector3>(Faces.Count);
            foreach (Face f in Faces) {vertexPoints.Add(f.Centroid);}

            // Create sublist of non-boundary vertices
            var naked = new Dictionary<string, bool>(Vertices.Count);  // vertices (name, boundary?)
            var hlookup = new Dictionary<string, int>(Halfedges.Count);  // boundary halfedges (name, index of point in new mesh)
            
            foreach (var he in Halfedges) {
                if (!naked.ContainsKey(he.Vertex.Name)) {  // if not in dict, add (boundary == true)
                    naked.Add(he.Vertex.Name, he.Pair == null);
                } else if (he.Pair == null) {  // if in dict and belongs to boundary halfedge, set true
                    naked[he.Vertex.Name] = true;
                }
                    
                if (he.Pair == null) {
                    // if boundary halfedge, add mid-point to vertices and add to lookup
                    hlookup.Add(he.Name, vertexPoints.Count);
                    vertexPoints.Add(he.Midpoint);
                }
            }

            // List new faces by their vertex indices
            // (i.e. old vertices by their face indices)
            Dictionary<string, int> flookup = new Dictionary<string, int>();

            for (int i = 0; i < Faces.Count; i++) {
                flookup.Add(Faces[i].Name, i);
            }

            var faceIndices = new List<List<int>>(Vertices.Count);
            
            foreach (var v in Vertices) {
                
                List<int> fIndex = new List<int>();

                foreach (Face f in v.GetVertexFaces()) {
                    fIndex.Add(flookup[f.Name]);
                }
                    
                if (naked.ContainsKey(v.Name) && naked[v.Name]) {  // Handle boundary vertices...
                    var h = v.Halfedges;
                    if (h.Count > 0) {
                        // Add points on naked edges and the naked vertex
                        fIndex.Add(hlookup[h.Last().Name]);
                        fIndex.Add(vertexPoints.Count);
                        fIndex.Add(hlookup[h.First().Next.Name]);
                        vertexPoints.Add(v.Position);
                    }
                }

                faceIndices.Add(fIndex);
            }

            return new ConwayPoly(vertexPoints, faceIndices.ToArray());
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
        public ConwayPoly Ambo() {
            
            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            List<Vector3> vertexPoints = new List<Vector3>();  // vertices as points
            Dictionary<string, int> hlookup = new Dictionary<string, int>();
            int count = 0;
            
            foreach (var edge in Halfedges) {
                // if halfedge's pair is already in the table, give it the same index
                if (edge.Pair != null && hlookup.ContainsKey(edge.Pair.Name)) {
                    hlookup.Add(edge.Name, hlookup[edge.Pair.Name]);
                } else {  // otherwise create a new vertex and increment the index
                    hlookup.Add(edge.Name, count++);
                    vertexPoints.Add(edge.Midpoint);
                }
            }

            var faceIndices = new List<IEnumerable<int>>();  // faces as vertex indices
            // faces to faces
            foreach (var face in Faces) {
                faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
            }

            // vertices to faces
            foreach (var vertex in Vertices) {
                var he = vertex.Halfedges;
                if (he.Count == 0) continue;  // no halfedges (naked vertex, ignore)
                var list = he.Select(edge => hlookup[edge.Name]);  // halfedge indices for vertex-loop
                if (he[0].Next.Pair == null) {
                    // Handle boundary vertex, add itself and missing boundary halfedge
                    list = list.Concat(new[] {vertexPoints.Count, hlookup[he[0].Next.Name]});
                    vertexPoints.Add(vertex.Position);
                }

                faceIndices.Add(list);
            }

            return new ConwayPoly(vertexPoints, faceIndices);
        }

        /// <summary>
        /// Conway's kis operator
        /// </summary>
        /// <returns>the kis as a new mesh</returns>
        public ConwayPoly Kis(float offset=0, bool excludeTriangles=false) {
            
            // vertices and faces to vertices
            var newVerts = Faces.Select(f => f.Centroid + f.Normal * offset);
            var vertexPoints = Enumerable.Concat(Vertices.Select(v => v.Position), newVerts);
            
            // vertex lookup
            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++) {
                vlookup.Add(Vertices[i].Name, i);
            }

            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int i = 0; i < Faces.Count; i++) {
                if (Faces[i].Sides <= 3 && excludeTriangles) {
                    faceIndices.Add(ListFacesByVertexIndices()[i]);
                } else {
                    foreach (var edge in Faces[i].GetHalfedges()) {
                        // create new face from edge start, edge end and centroid
                        faceIndices.Add(
                            new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
                        );
                    }
                }
            }
            
            return new ConwayPoly(vertexPoints, faceIndices);
        }

        public ConwayPoly KisN(float offset, int sides) {

            // vertices and faces to vertices
            var newVerts = Faces.Select(f => f.Centroid + f.Normal * offset);
            var vertexPoints = Enumerable.Concat(Vertices.Select(v => v.Position), newVerts);
                
            // vertex lookup
            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++) {
                vlookup.Add(Vertices[i].Name, i);
            }
    
            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int i = 0; i < Faces.Count; i++) {
                if (sides > 2 && Faces[i].Sides != sides) {
                    faceIndices.Add(ListFacesByVertexIndices()[i]);
                } else {
                    foreach (var edge in Faces[i].GetHalfedges()) {
                        // create new face from edge start, edge end and centroid
                        faceIndices.Add(
                            new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
                        );
                    }
                }
            }
                
            return new ConwayPoly(vertexPoints, faceIndices);
        }

        public ConwayPoly Gyro(float ratio = 0.3333f)
        {
            
            // Happy accidents - skip n new faces - offset just the centroid?

            var existingVerts = new Dictionary<string, int>();
            var newVerts = new Dictionary<string, int>();
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
                
            // Loop through old faces
            for (int i = 0; i < Faces.Count; i++)
            {
                var oldFace = Faces[i];                
                
                vertexPoints.Add(oldFace.Centroid);
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
                        OneThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = OneThirdIndex;
                    }
                    
                    
                    var PrevThirdVertex = edges[j].Next.Pair.PointAlongEdge(ratio);
                    keyName = edges[j].Next.Pair.Name;
                    if (newVerts.ContainsKey(keyName))
                    {
                        PrevThirdIndex = newVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(PrevThirdVertex);
                        PrevThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = PrevThirdIndex;
                    }
                    
                    var PairOneThird = edges[j].Pair.PointAlongEdge(ratio);
                    keyName = edges[j].Pair.Name;
                    if (newVerts.ContainsKey(keyName))
                    {
                        PairOneThirdIndex = newVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(PairOneThird);
                        PairOneThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = PairOneThirdIndex;
                    }
                    
                    thisFaceIndices.Add(centroidIndex);
                    thisFaceIndices.Add(PairOneThirdIndex);
                    thisFaceIndices.Add(OneThirdIndex);
                    thisFaceIndices.Add(seedVertexIndex);
                    thisFaceIndices.Add(PrevThirdIndex);
                    
                    faceIndices.Add(thisFaceIndices);
                }
            }

            var poly = new ConwayPoly(vertexPoints, faceIndices);
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
			
			// Create new vertices, one at the midpoint of each edge
		    
			var newVertices = new Dictionary<string, int>();
			int vertexIndex = vertexPoints.Count();
		    
			foreach (var face in Faces)
			{
				// Create a new face for each existing face
				var newFace = new int[face.Sides];
			    var edge = face.Halfedge;
			    
				for (int i=0; i<face.Sides; i++)
				{
				    if (!newVertices.ContainsKey(edge.PairedName))
				    {
				        vertexPoints.Add(edge.Midpoint);
				        newVertices[edge.PairedName] = vertexIndex++;
				    }
				    newFace[i] = newVertices[edge.PairedName];
				    edge = edge.Next;
				}
			    faceIndices.Add(newFace);
			}
			
			// Create new faces for each vertex
		    		    
			for (int i = 0 ; i < Vertices.Count; i++)
			{
			    
				var adjacentFaces = Vertices[i].GetVertexFaces();
			    
				foreach (Face face in adjacentFaces)
				{
				    var edge = face.GetHalfedges().Find(x => x.Vertex == Vertices[i]);
					int currVertex = newVertices[edge.PairedName];
				    int prevVertex = newVertices[edge.Next.PairedName];
					var triangle = new int[]{i, prevVertex, currVertex};
					faceIndices.Add(triangle);
				}
			}
			
			return new ConwayPoly(vertexPoints, faceIndices);
		}
	
        // Interesting accident
        public ConwayPoly BrokenLoft(float ratio=0.33333333f, int sides=0)
        {

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newVertices = new Dictionary<string, int>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
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
                        var newVertex = Vector3.Lerp(vertex, centroid, ratio);

                        vertexPoints.Add(newVertex);
                        newInsetFace[i] = vertexIndex;
                        newVertices[edge.Name] = vertexIndex++;
	
                        // Generate new faces

                        var newFace = new[]{
                            existingVertices[edge.Vertex.Position],
                            (existingVertices[edge.Next.Vertex.Position] + 1),
                            newVertices[edge.Name],
//						    //newVertices[edge.Prev.PairedName],

                        };
                        faceIndices.Add(newFace);
	
                        edge = edge.Next;
                    }
                    faceIndices.Add(newInsetFace);
                } else {
                    // Keep original face
                    //faceIndices.Add(face);
                }
            }
	
            return new ConwayPoly(vertexPoints, faceIndices);
        }
        
		/**
		* Adds smaller version of each face, with n trapezoidal faces connecting the inner
		* it to the original version, where n is the number of vertices of the face.
		*/
		public ConwayPoly Loft(float ratio=0.33333333f, int sides=0)
		{

		    var faceIndices = new List<int[]>();
		    var vertexPoints = new List<Vector3>();
		    var existingVertices = new Dictionary<Vector3, int>();
		    var newVertices = new Dictionary<string, int>();

		    for (var i = 0; i < Vertices.Count; i++)
		    {
		        vertexPoints.Add(Vertices[i].Position);
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
			        int newV = -1;
			        int prevNewV = -1;

			        for (int i = 0; i < face.Sides; i++)
			        {
			            var vertex = edge.Vertex.Position;
			            var newVertex = Vector3.Lerp(vertex, centroid, ratio);

			            vertexPoints.Add(newVertex);
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
			            }
			            prevNewV = newV;	
					    edge = edge.Next;
					}
			        faceIndices.Add(newInsetFace);
				} else {
			        
				    faceIndices.Add(
				        face.GetHalfedges().Select(
				            x => existingVertices[x.Vertex.Position]
                    ).ToArray());
				}

			    var lastEdge = face.Halfedge.Prev;
			    var finalFace = new[]
			    {
			        existingVertices[lastEdge.Vertex.Position],
			        existingVertices[lastEdge.Next.Vertex.Position],
			        newVertices[lastEdge.Next.Name],
			        newVertices[lastEdge.Name]
			    };
			    faceIndices.Add(finalFace);
			}
		    
		    Debug.Log("Outer: " + faceIndices.Count);
		    foreach (var x in faceIndices)
		    {
		        Debug.Log("Inner: " + x.Count());
                
		    }
	
			return new ConwayPoly(vertexPoints, faceIndices);
		}
//				
//		/**
//		* Compute the "quinto" polyhedron of this polyhedron. Equivalent to an
//		* ortho but truncating the vertex at the center of original faces. This
//		* creates a small copy of the original face (but rotated).
//		* 
//		* @return The quinto polyhedron.
//		*/
//		public ConwayPoly quinto() {
//			
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(new Vertex(vertexPos.Position));
//			}
//			
//			// Create new vertices at the midpoint of each edge and toward the
//			// face's centroid
//			Dictionary<int, Dictionary<int, int>> edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, quintoPolyhedron);
//	
//			int vertexIndex = vertexPoints.Count;
//			var midptVertices = new Dictionary<Halfedge, int>();
//			foreach (Halfedge edge in this.Halfedges) {
//				vertexPoints.Add(edge.Midpoint);
//				midptVertices[edge] = vertexIndex++;
//			}
//			
//			// Generate new faces
//			foreach (Face face in Faces) {
//				Face centralFace = new Face();
//				List<Halfedge> edges = face.GetHalfedges();
//				
//				int[] prevEnds = edges[face.Sides - 1].getEnds();
//				int prevVertex = edgeToVertex[prevEnds[0]][prevEnds[1]];
//				int prevMidpt = midptVertices[edges[face.Sides - 1]];
//				int centralIndex = 0;
//				foreach (Halfedge currEdge in edges) {
//					int[] currEnds = currEdge.getEnds();
//					int currVertex = edgeToVertex[currEnds[0]][currEnds[1]];
//					int currMidpt = midptVertices[currEdge];
//					
//					Face pentagon = new Face();
//					pentagon.setAllVertexIndices(prevVertex, prevMidpt, currEnds[0], currMidpt, currVertex);
//					quintoPolyhedron.Faces.Add(pentagon);
//					
//					centralFace.setVertexIndex(centralIndex++, currVertex);
//					
//					// Update previous vertex indices
//					prevVertex = currVertex;
//					prevMidpt = currMidpt;
//				}
//				quintoPolyhedron.Faces.Add(centralFace);
//			}
//			
//			//quintoPolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//	
//		/**
//		* Computes the "joined-lace" polyhedron of this polyhedron. Like lace, but
//		* old edges are replaced by quadrilateral faces instead of two triangular
//		* faces.
//		*
//		* @return The joined-lace polyhedron.
//		*/
//		public ConwayPoly joinedLace() {
//			return this.lace(-1, true, true);
//		}
//	
//		/**
//		* Computes the "lace" polyhedron of this polyhedron. Like loft, but has
//		* on each face an antiprism of the original face instead of a prism.
//		* 
//		* @return The lace polyhedron.
//		*/
//		public ConwayPoly lace() {
//			return this.lace(-1, true, false);
//		}
//	
//		/**
//		* Computes the "lace" polyhedron of this polyhedron, except the operation
//		* is only applied to faces with the specified number of sides.
//		*
//		* @param n The number of sides a face needs to have lace applied to it.
//		* @return The polyhedron with lace applied to faces with n sides.
//		*/
//		public ConwayPoly lace(int n) {
//			return this.lace(n, false, false);
//		}
//	
//		/**
//		* A helper method for implementing lace, parametrized lace, and
//		* joined-lace.
//		*
//		* @param n      The number of sides a face needs to have lace applied
//		*               to it.
//		* @param ignore True if we want to ignore the parameter n.
//		* @param joined True if we want to compute joined-lace.
//		* @return The lace polyhedron.
//		*/
//		private ConwayPoly lace(int n, bool ignore, bool joined) {
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(new Vertex(vertexPos.Position));
//			}
//	
//			// Generate new vertices
//			Dictionary<int, Dictionary<int, int>> edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, lacePolyhedron);
//	
//			if (joined) {
//				PolyhedraUtils.addRhombicFacesAtEdges(this, lacePolyhedron, edgeToVertex);
//			}
//	
//			// Generate new faces
//			foreach (Face face in Faces) {
//				if (ignore || face.Sides == n) {
//					Face twist = new Face();
//					List<Halfedge> edges = face.GetHalfedges();
//	
//					for (int i = 0; i < face.Sides; i++) {
//						// Build face at center of each original face
//						int[] ends = edges[i].getEnds();
//						int newVertex = edgeToVertex[ends[0]][ends[1]];
//						twist.setVertexIndex(i, newVertex);
//	
//						// Always generate triangles from vertices to central face
//						int nextInd = (i + 1) % face.Sides;
//						int[] nextEnds = edges[nextInd].getEnds();
//						int nextNewVertex = edgeToVertex[nextEnds[0]][nextEnds[1]];
//	
//						Face smallTriangle = new Face();
//						smallTriangle.setAllVertexIndices(nextNewVertex, newVertex, ends[1]);
//	
//						lacePolyhedron.Faces.Add(smallTriangle);
//					}
//	
//					lacePolyhedron.Faces.Add(twist);
//	
//					if (!joined) {
//						// If not joined, generate triangle faces
//						foreach (Halfedge edge in edges) {
//							int[] ends = edge.getEnds();
//							int currVertex = edgeToVertex[ends[0]][ends[1]];
//	
//							Face largeTriangle = new Face();
//							largeTriangle.setAllVertexIndices(currVertex, ends[0], ends[1]);
//	
//							lacePolyhedron.Faces.Add(largeTriangle);
//						}
//					}
//				} else {
//					// Keep original face
//					lacePolyhedron.Faces.Add(face);
//				}
//			}
//	
//			//lacePolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//	
//		/**
//		* Computes the "stake" polyhedron of this polyhedron. Like lace, but
//		* instead of having a central face, there is a central vertex and 
//		* quadrilaterals around the center.
//		* 
//		* @return The stake polyhedron.
//		*/
//		public ConwayPoly stake() {
//			return this.stake(-1, true);
//		}
//	
//		/**
//		* Computes the "stake" polyhedron of this polyhedron, but only performs
//		* the operation on faces with n sides.
//		*
//		* @param n The number of sides a face needs to have stake applied to it.
//		* @return The polyhedron with stake applied to faces with n sides.
//		*/
//		public ConwayPoly stake(int n) {
//			return this.stake(n, false);
//		}
//	
//		/**
//		* A helper method for implementing stake and parametrized stake.
//		*
//		* @param n      The number of sides a face needs to have stake applied
//		*               to it.
//		* @param ignore True if we want to ignore the parameter n.
//		* @return The stake polyhedron.
//		*/
//		private ConwayPoly stake(int n, bool ignore) {
//			
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(new Vertex(vertexPos.Position));
//			}
//	
//			// Generate new vertices
//			Dictionary<int, Dictionary<int, int>> edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, stakePolyhedron);
//	
//			int vertexIndex = vertexPoints.Count;
//			foreach (Face face in Faces) {
//				if (ignore || face.Sides == n) {
//					Vector3 centroid = face.Centroid;
//					vertexPoints.Add(new Vertex(centroid));
//					int centroidIndex = vertexIndex++;
//	
//					List<Halfedge> edges = face.GetHalfedges();
//	
//					// Generate the quads and triangles on this face
//					for (int i = 0; i < face.Sides; i++) {
//						int[] ends = edges[i].getEnds();
//						int currVertex = edgeToVertex[ends[0]][ends[1]];
//						int[] nextEnds = edges[(i + 1) % face.Sides].getEnds();
//						int nextVertex = edgeToVertex[nextEnds[0]][nextEnds[1]];
//	
//						Face triangle = new Face();
//						Face quad = new Face();
//						triangle.setAllVertexIndices(currVertex, ends[0], ends[1]);
//						quad.setAllVertexIndices(nextVertex, centroidIndex, currVertex, ends[1]);
//	
//						stakePolyhedron.Faces.Add(triangle);
//						stakePolyhedron.Faces.Add(quad);
//					}
//				} else {
//					// Keep original face
//					stakePolyhedron.Faces.Add(face);
//				}
//			}
//	
//			//stakePolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
	    
	    
//		/**
//		* Computes the "medial" polyhedron of this polyhedron. Adds vertices at the
//		* face centroids and edge midpoints. Each face is split into 2n triangles,
//		* where n is the number of vertices in the face. These triangles share a
//		* vertex at the face's centroid.
//		* 
//		* @return The medial polyhedron.
//		*/
//		public ConwayPoly medial() {
//			return this.medial(2);
//		}
//	
//		/**
//		* Computes the "edge-medial" polyhedron of this polyhedron. Places a
//		* vertex at the centroid of every face, and subdivides each edge into
//		* n segments, with edges from these subdivision points to the centroid.
//		*
//		* For example, the "edge-medial-3" operator corresponds to n = 3.
//		*
//		* @param n The number of subdivisions on each edge.
//		* @return The edge-medial polyhedron with n subdivisions per edge.
//		*/
//		public ConwayPoly edgeMedial(int n) {
//			return medial(n, true);
//		}
//
//		/**
//		 * Generalized medial, parametrized on the number of subdivisions on each
//		 * edge. The regular medial operation corresponds to n = 2 subdivisions.
//		 *
//		 * @param n The number of subdivisions on each edge.
//		 * @return The medial polyhedron with n subdivisions per edge.
//		 */
//		public ConwayPoly medial(int n) {
//			return medial(n, false);
//		}
//	
//		/**
//		* Computes the "joined-medial" polyhedron of this polyhedron. The same as
//		* medial, but with rhombic faces im place of original edges.
//		*
//		* @return The joined-medial polyhedron.
//		*/
//		public ConwayPoly joinedMedial() {
//			
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(new Vertex(vertexPos.Position));
//			}
//	
//			// Generate new vertices and rhombic faces on original edges
//			Dictionary<int, Dictionary<int, int>> edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, medialPolyhedron);
//			PolyhedraUtils.addRhombicFacesAtEdges(this, medialPolyhedron, edgeToVertex);
//	
//			// Generate triangular faces
//			int vertexIndex = vertexPoints.Count;
//			foreach (Face face in Faces) {
//				Vector3 centroid = face.Centroid;
//				vertexPoints.Add(new Vertex(centroid));
//	
//				List<Halfedge> edges = face.GetHalfedges();
//				int[] prevEnds = edges[face.Sides - 1].getEnds();
//				int prevVertex = edgeToVertex[prevEnds[0]][prevEnds[1]];
//				foreach (Halfedge edge in edges) {
//					int[] ends = edge.getEnds();
//					int currVertex = edgeToVertex[ends[0]][ends[1]];
//	
//					Face triangle1 = new Face();
//					Face triangle2 = new Face();
//					triangle1.setAllVertexIndices(vertexIndex, ends[0], currVertex);
//					triangle2.setAllVertexIndices(vertexIndex, prevVertex, ends[0]);
//	
//					medialPolyhedron.Faces.Add(triangle1);
//					medialPolyhedron.Faces.Add(triangle2);
//	
//					prevVertex = currVertex;
//				}
//	
//				vertexIndex++;
//			}
//	
//			//medialPolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//	
//		/**
//		* A helper method for computing edge-medial and medial (parametrized).
//		*
//		* @param n    The number of subdivisions per edge.
//		* @param edge True if computing edge-medial, false if regular medial.
//		* @return The medial polyhedron subjected to the input constraints.
//		*/
//		private ConwayPoly medial(int n, bool edge) {
//			
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(vertexPos.Position);
//			}
//	
//			// Create new vertices on edges
//			Dictionary<int, Dictionary<int, int[]>> newVertices = PolyhedraUtils.subdivideEdges(this, medialPolyhedron, n);
//	
//			int vertexIndex = vertexPoints.Count();
//			foreach (Face face in Faces) {
//				Vector3 centroid = face.Centroid;
//	
//				List<Halfedge> faceEdges = face.GetHalfedges();
//	
//				Halfedge prevEdge = faceEdges[faceEdges.Count - 1];
//				int[] prevEnds = prevEdge.getEnds();
//				foreach (Halfedge currEdge in faceEdges) {
//					int[] currEnds = currEdge.getEnds();
//					int[] currNewVerts = newVertices[currEnds[0]][currEnds[1]];
//	
//					int prevLastVert = newVertices[prevEnds[0]][prevEnds[1]][n - 2];
//					if (edge) {
//						// One quadrilateral face
//						Face quad = new Face();
//						quad.setAllVertexIndices(currEnds[0], currNewVerts[0], vertexIndex, prevLastVert);
//						medialPolyhedron.Faces.Add(quad);
//					} else {
//						// Two triangular faces
//						Face triangle1 = new Face();
//						Face triangle2 = new Face();
//						triangle1.setAllVertexIndices(currEnds[0], currNewVerts[0], vertexIndex);
//						triangle2.setAllVertexIndices(vertexIndex, prevLastVert, currEnds[0]);
//	
//						medialPolyhedron.Faces.Add(triangle1);
//						medialPolyhedron.Faces.Add(triangle2);
//					}
//	
//					// Create new triangular faces at edges
//					for (int i = 0 ; i < currNewVerts.Count() - 1 ; i++) {
//						Face edgeTriangle = new Face();
//						edgeTriangle.setAllVertexIndices(vertexIndex, currNewVerts[i], currNewVerts[i + 1]);
//	
//						medialPolyhedron.Faces.Add(edgeTriangle);
//					}
//				}
//	
//				vertexPoints.Add(new Vertex(centroid));
//				vertexIndex++;
//			}
//	
//			//medialPolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//		
//		/**
//		* Computes the "propellor" polyhedron of this polyhedron. It is like gyro,
//		* but instead of having a central vertex we have a central face. This
//		* creates quadrilateral faces instead of pentagonal faces.
//		* 
//		* @return The propellor polyhedron.
//		*/
//		public ConwayPoly propellor() {
//			
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(vertexPos.Position);
//			}
//			
//			// Create new vertices on edges
//			Dictionary<int, Dictionary<int, int[]>> newVertices = PolyhedraUtils.subdivideEdges(this, propellorPolyhedron, 3);
//			
//			// Create quadrilateral faces and one central face on each face
//			foreach (Face face in Faces) {
//				List<Halfedge> faceEdges = face.GetHalfedges();
//				
//				Face centralFace = new Face();
//				int[] prevEnds = faceEdges[faceEdges.Count - 1].getEnds();
//				int[] prevEdgeVertices = newVertices[prevEnds[0]][prevEnds[1]];
//				for (int i = 0 ; i < face.Sides; i++) {
//					int[] ends = faceEdges[i].getEnds();
//					int[] newEdgeVertices = newVertices[ends[0]][ends[1]];
//					
//					Face quad = new Face();
//					quad.setAllVertexIndices(ends[0], newEdgeVertices[0], prevEdgeVertices[0], prevEdgeVertices[1]);
//					propellorPolyhedron.Faces.Add(quad);
//					
//					centralFace.setVertexIndex(i, newEdgeVertices[0]);
//					
//					prevEnds = ends;
//					prevEdgeVertices = newEdgeVertices;
//				}
//				
//				propellorPolyhedron.Faces.Add(centralFace);
//			}
//			
//			//propellorPolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//		
//		/**
//		* Computes the "whirl" polyhedron of this polyhedron. Forms hexagon
//		* faces at each edge, with a small copy of the original face at the
//		* center of the original face.
//		* 
//		* @return The whirl polyhedron.
//		*/
//		public ConwayPoly whirl() {
//			
//					
//			var vertexPoints = new List<Vector3>();
//			var faceIndices = new List<List<int>>();
//			
//			foreach (var vertexPos in Vertices) {
//				vertexPoints.Add(vertexPos.Position);
//			}
//			
//			// Create new vertices on edges
//			Dictionary<int, Dictionary<int, int[]>> newVertices = PolyhedraUtils.subdivideEdges(this, whirlPolyhedron, 3);
//			
//			// Generate vertices near the center of each face
//			var centerVertices = new Dictionary<Face, int[]>();
//			int vertexIndex = vertexPoints.Count();
//			foreach (Face face in Faces) {
//				int[] newCenterIndices = new int[face.Sides];
//				Vector3 centroid = face.Centroid;
//				int i = 0;
//				foreach (Halfedge edge in face.GetHalfedges()) {
//					int[] ends = edge.getEnds();
//					int[] edgeVertices = newVertices[ends[0]][ends[1]];
//					Vector3 edgePoint = vertexPoints[edgeVertices[1]];
//					Vector3 diff = new Vector3();
//					diff = edgePoint - centroid;
//					diff *= 0.3f; // 0 < arbitrary scale factor < 1
//					
//					Vector3 newFacePoint = new Vector3();
//					newFacePoint = centroid + diff;
//					
//					vertexPoints.Add(newFacePoint);
//					newCenterIndices[i++] = vertexIndex++;
//				}
//				
//				centerVertices[face] = newCenterIndices;
//			}
//			
//			// Generate hexagonal faces and central face
//			foreach (Face face in Faces) {
//				Face centralFace = new Face();
//				
//				List<Halfedge> faceEdges = face.GetHalfedges();
//				int[] centralVertices = centerVertices[face];
//				int[] pEnds = faceEdges[faceEdges.Count() - 1].getEnds();
//				int[] prevEdgeVertices = newVertices[pEnds[0]][pEnds[1]];
//				int prevCenterIndex = centralVertices[centralVertices.Count() - 1];
//				for (int i = 0 ; i < face.Sides ; i++) {
//					int[] ends = faceEdges[i].getEnds();
//					int[] edgeVertices = newVertices[ends[0]][ends[1]];
//					int currCenterIndex = centralVertices[i];
//					
//					Face hexagon = new Face();
//					hexagon.setAllVertexIndices(ends[0], edgeVertices[0], edgeVertices[1], currCenterIndex, prevCenterIndex, prevEdgeVertices[1]);
//					whirlPolyhedron.Faces.Add(hexagon);
//					
//					centralFace.setVertexIndex(i, currCenterIndex);
//					
//					prevEdgeVertices = edgeVertices;
//					prevCenterIndex = currCenterIndex;
//				}
//				
//				whirlPolyhedron.Faces.Add(centralFace);
//			}
//			
//			//whirlPolyhedron.setVertexNormalsToFaceNormals();
//			return new ConwayPoly(vertexPoints, faceIndices);
//		}
//	
//		/**
//		* Computes the "volute" polyhedron of this polyhedron. Equivalent to a
//		* snub operation followed by kis on the original faces. This is the dual
//		* of whirl.
//		* 
//		* @return The volute polyhedron.
//		*/
//		public ConwayPoly volute() {
//			return this.whirl().Dual();
//		}
        
        #endregion

        #region geometry methods

        public ConwayPoly FaceScale(float scale, int sides) {
            
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            
            foreach (var t in Faces)
            {
                var includeFace = t.Sides == sides || sides==0;
                
                var face = t;

                int c = vertexPoints.Count;
                var faceIndex = new List<int>();

                c = vertexPoints.Count;
                vertexPoints.AddRange(face.GetVertices()
                    .Select(v => includeFace?Vector3.LerpUnclamped(face.Centroid, v.Position, scale + 1):v.Position));
                faceIndex = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++)
                {
                    faceIndex.Add(c + ii);
                }

                faceIndices.Add(faceIndex);

            }
                
            return new ConwayPoly(vertexPoints, faceIndices);
        }
        
        public ConwayPoly FaceRotate(float angle, int sides) {
            
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            
            foreach (var t in Faces)
            {
                var includeFace = t.Sides == sides || sides==0;

                var face = t;

                int c = vertexPoints.Count;
                var faceIndex = new List<int>();

                c = vertexPoints.Count;

                var pivot = face.Centroid;
                var rot = Quaternion.AngleAxis(angle, face.Normal);

                vertexPoints.AddRange(
                    face.GetVertices().Select(
                        v => includeFace?pivot + rot * (v.Position - pivot):v.Position
                    )
                );
                faceIndex = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++)
                {
                    faceIndex.Add(c + ii);
                }

                faceIndices.Add(faceIndex);
                
            }
                
            return new ConwayPoly(vertexPoints, faceIndices);
        }
        
        public ConwayPoly FaceRemove(int sides, bool invertLogic) {
            
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            foreach (var t in Faces)
            {
                var includeFace = t.Sides == sides;
                includeFace = invertLogic ? includeFace : !includeFace;
                if (includeFace)
                {
                    var face = t;

                    int c = vertexPoints.Count;
                    var faceIndex = new List<int>();

                    c = vertexPoints.Count;
                    vertexPoints.AddRange(face.GetVertices().Select(v => v.Position));
                    faceIndex = new List<int>();
                    for (int ii = 0; ii < face.GetVertices().Count; ii++)
                    {
                        faceIndex.Add(c + ii);
                    }

                    faceIndices.Add(faceIndex);
                }
            }

            return new ConwayPoly(vertexPoints, faceIndices);                        
        }
        
        /// <summary>
        /// Offsets a mesh by moving each vertex by the specified distance along its normal vector.
        /// </summary>
        /// <param name="offset">Offset distance</param>
        /// <returns>The offset mesh</returns>
        public ConwayPoly Offset(double offset) {
            var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => offset).ToList();
            return Offset(offsetList);
        }
    
        public ConwayPoly Offset(List<double> offset) {
            
            Vector3[] points = new Vector3[Vertices.Count];
            
            for (int i = 0; i < Vertices.Count && i < offset.Count; i++) {
                points[i] = Vertices[i].Position + Vertices[i].Normal * (float)offset[i];
            }
            
            return new ConwayPoly(points, ListFacesByVertexIndices());
        }
               
        /// <summary>
        /// Thickens each mesh edge in the plane of the mesh surface.
        /// </summary>
        /// <param name="offset">Distance to offset edges in plane of adjacent faces</param>
        /// <param name="boundaries">If true, attempt to ribbon boundary edges</param>
        /// <returns>The ribbon mesh</returns>
        public ConwayPoly Ribbon(float offset, Boolean boundaries, float smooth) {
            
            ConwayPoly ribbon = Duplicate();
            var orig_faces = ribbon.Faces.ToArray();

            List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();

            // create new "vertex" faces
            List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
            for (int k = 0; k < Vertices.Count; k++) {
                Vertex v = ribbon.Vertices[k];
                List<Vertex> new_vertices = new List<Vertex>();
                List<Halfedge> halfedges = incidentEdges[k];
                Boolean boundary = halfedges[0].Next.Pair != halfedges[halfedges.Count - 1];

                // if the edge loop around this vertex is open, close it with 'temporary edges'
                if (boundaries && boundary) {
                    Halfedge a, b;
                    a = halfedges[0].Next;
                    b = halfedges[halfedges.Count - 1];
                    if (a.Pair == null) {
                        a.Pair = new Halfedge(a.Prev.Vertex) {Pair = a};
                    }

                    if (b.Pair == null) {
                        b.Pair = new Halfedge(b.Prev.Vertex) {Pair = b};
                    }

                    a.Pair.Next = b.Pair;
                    b.Pair.Prev = a.Pair;
                    a.Pair.Prev = a.Pair.Prev ?? a; // temporary - to allow access to a.Pair's start/end vertices
                    halfedges.Add(a.Pair);
                }

                foreach (Halfedge edge in halfedges) {
                    if (halfedges.Count < 2) {
                        continue;
                    }

                    Vector3 normal = edge.Face != null ? edge.Face.Normal : Vertices[k].Normal;
                    Halfedge edge2 = edge.Next;

                    var o1 = new Vertex(Vector3.Cross(normal, edge.Vector).normalized * offset);
                    var o2 = new Vertex(Vector3.Cross(normal, edge2.Vector).normalized * offset);

                    if (edge.Face == null) {
                        // boundary condition: create two new vertices in the plane defined by the vertex normal
                        Vertex v1 = new Vertex(v.Position + (edge.Vector * (1 / edge.Vector.magnitude) * -offset) + o1.Position);
                        Vertex v2 = new Vertex(v.Position + (edge2.Vector * (1 / edge2.Vector.magnitude) * offset) + o2.Position);
                        ribbon.Vertices.Add(v2);
                        ribbon.Vertices.Add(v1);
                        new_vertices.Add(v2);
                        new_vertices.Add(v1);
                        Halfedge c = new Halfedge(v2, edge2, edge, null);
                        edge.Next = c;
                        edge2.Prev = c;
                    }
                    else {
                        // internal condition: offset each edge in the plane of the shared face and create a new vertex where they intersect eachother
                        // TODO
                        Line l1 = new Line(new Vertex(edge.Vertex.Position + o1.Position), new Vertex(edge.Prev.Vertex.Position + o1.Position));
                        Line l2 = new Line(new Vertex(edge2.Vertex.Position + o2.Position), new Vertex(edge2.Prev.Vertex.Position + o2.Position));
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
            for (int k = 0; k < Vertices.Count; k++) {
                Vertex v = ribbon.Vertices[k];
                if (all_new_vertices[k].Count < 1) {
                    continue;
                }

                int c = 0;
                foreach (Halfedge edge in incidentEdges[k]) {
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
            for (int i = 0; i < Halfedges.Count; i++) {
                temp.Add(ribbon.Halfedges[i]);
            }

            List<Halfedge> items = temp.GetUnique();

            foreach (Halfedge halfedge in items) {
                if (halfedge.Pair != null) {
                    // insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
                    if (smooth > 0.0) {
                        if (smooth > 0.5) {
                            smooth = 0.5f;
                        }

                        Vertex[] newVertices = new Vertex[] {
                            new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
                            new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
                            new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
                            new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
                        };
                        ribbon.Vertices.AddRange(newVertices);
                        Vertex[] new_vertices1 = new Vertex[] {
                            halfedge.Vertex,
                            newVertices[0],
                            newVertices[3],
                            halfedge.Pair.Prev.Vertex
                        };
                        Vertex[] new_vertices2 = new Vertex[] {
                            newVertices[1],
                            halfedge.Prev.Vertex,
                            halfedge.Pair.Vertex,
                            newVertices[2]
                        };
                        ribbon.Faces.Add(newVertices);
                        ribbon.Faces.Add(new_vertices1);
                        ribbon.Faces.Add(new_vertices2);
                    }
                    else {
                        Vertex[] newVertices = new Vertex[] {
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
            foreach (Face item in orig_faces) {
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
        public ConwayPoly Extrude(double distance, bool symmetric) {
            var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => distance).ToList();
            return Extrude(offsetList, symmetric);
        }

        public ConwayPoly Extrude(List<double> distance, bool symmetric) {
            
            ConwayPoly ext, top;
            
            if (symmetric) {
                ext = Offset(distance.Select(d => 0.5 * d).ToList());
                top = Offset(distance.Select(d => -0.5 * d).ToList());
            } else {
                ext = Duplicate();
                top = Offset(distance);
            }
            
            ext.Halfedges.Flip();

            // append top to ext (can't use Append() because copy would reverse face loops)
            foreach (var v in top.Vertices) ext.Vertices.Add(v);
            foreach (var h in top.Halfedges) ext.Halfedges.Add(h);
            foreach (var f in top.Faces) ext.Faces.Add(f);

            // get indices of naked halfedges in source mesh
            var naked = Halfedges.Select((item, index) => index).Where(i => Halfedges[i].Pair == null).ToList();

            if (naked.Count > 0) {
                int n = Halfedges.Count;
                int failed = 0;
                foreach (var i in naked) {
                    Vertex[] vertices = {
                        ext.Halfedges[i].Vertex,
                        ext.Halfedges[i].Prev.Vertex,
                        ext.Halfedges[i + n].Vertex,
                        ext.Halfedges[i + n].Prev.Vertex
                    };
                    if (ext.Faces.Add(vertices) == false) {
                        failed++;
                    }
                }
            }

            ext.Halfedges.MatchPairs();

            return ext;
        }
    
        public void ScalePolyhedra(float scale=1) {
            
            if (Vertices.Count > 0)
            {
            
                // Find the furthest vertex
                Vertex max = Vertices.OrderByDescending(x => x.Position.magnitude).FirstOrDefault();
                float unitScale = 1.0f/max.Position.magnitude;
            
                // TODO Ideal use case for Linq if I could get my head round the type-wrangling needed
                foreach (Vertex v in Vertices)
                {
                    v.Position = v.Position * unitScale * scale;
                }                    
            }
            
        }

        #endregion
	    
        #region canonicalize
        
//		/**
//		* Canonicalizes this polyhedron for the given number of iterations.
//		* See util.Canonicalize for more details. Performs "adjust" followed
//		* by "planarize".
//		* 
//		* @param iterationsAdjust    The number of iterations to "adjust" for.
//		* @param iterationsPlanarize The number of iterations to "planarize" for.
//		* @return The canonicalized version of this polyhedron.
//		*/
//	    public ConwayPoly canonicalize(int iterationsAdjust,
//		    int iterationsPlanarize) {
//		    ConwayPoly canonicalized = this.Duplicate();
//		    Canonicalize.adjust(canonicalized, iterationsAdjust);
//		    Canonicalize.planarize(canonicalized, iterationsPlanarize);
//		    return canonicalized;
//	    }
//	
//	    /**
//	     * Canonicalizes this polyhedron until the change in position does not
//	     * exceed the given threshold. That is, the algorithm terminates when no vertex
//	     * moves more than the threshold after one iteration.
//	     * 
//	     * @param thresholdAdjust    The threshold for change in one "adjust"
//	     *                           iteration.
//	     * @param thresholdPlanarize The threshold for change in one "planarize"
//	     *                           iteration.
//	     * @return The canonicalized version of this polyhedron.
//	     */
//	    public ConwayPoly canonicalize(double thresholdAdjust,
//		    double thresholdPlanarize) {
//		    ConwayPoly canonicalized = Duplicate();
//		    Canonicalize.adjust(canonicalized, thresholdAdjust);
//		    Canonicalize.planarize(canonicalized, thresholdPlanarize);
//		    return canonicalized;
//	    }
	    
	    
        
        
        #endregion
        
        #region methods

        /// <summary>
        /// A string representation of the mesh.
        /// </summary>
        /// <returns>a string representation of the mesh</returns>
        public override string ToString() {
            return base.ToString() + string.Format(" (V:{0} F:{1})", Vertices.Count, Faces.Count);
        }
    
        /// <summary>
        /// Gets the positions of all mesh vertices. Note that points are duplicated.
        /// </summary>
        /// <returns>a list of vertex positions</returns>
        public Vector3[] ListVerticesByPoints() {
            Vector3[] points = new Vector3[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++) {
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
        public List<int>[] ListFacesByVertexIndices() {
            
            var fIndex = new List<int>[Faces.Count];
            var vlookup = new Dictionary<String, int>();
    
            for (int i = 0; i < Vertices.Count; i++) {
                vlookup.Add(Vertices[i].Name, i);
            }
            
            for (int i = 0; i < Faces.Count; i++) {
                List<int> vertIdx = new List<int>();
                foreach (Vertex v in Faces[i].GetVertices()) {
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
        public void Append(ConwayPoly other) {
            ConwayPoly dup = other.Duplicate();
        
            Vertices.AddRange(dup.Vertices);
            foreach (Halfedge edge in dup.Halfedges) {
                Halfedges.Add(edge);
            }
        
            foreach (Face face in dup.Faces) {
                Faces.Add(face);
            }
        }

#endregion
	    
	}
	
}