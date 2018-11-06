using System;
using System.Collections.Generic;
using System.Linq;
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

        public ConwayPoly(Polyhedron source) : this() {

            // Add vertices
            Vertices.Capacity = source.VertexCount;
            foreach (var p in source.Vertices) {
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
                    Faces.Add(v);
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

        /// <summary>
        /// Constructor to build a custom mesh from Unity's mesh type
        /// </summary>
        /// <param name="source">the Rhino mesh</param>
        public ConwayPoly(UnityEngine.Mesh source) : this() {

            // Add vertices
            Vertices.Capacity = source.vertexCount;
            foreach (var p in source.vertices) {
                Vertices.Add(new Vertex(p));
            }

            // Add faces (and construct halfedges and store in hash table)
            for (int i = 0; i < source.triangles.Length; i += 3) {
                var v = new List<Vertex> {
                    Vertices[source.triangles[i]],
                    Vertices[source.triangles[i + 1]],
                    Vertices[source.triangles[i + 2]]
                };
                Faces.Add(v);
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }

        private ConwayPoly(IEnumerable<Vector3> verticesByPoints,  IEnumerable<IEnumerable<int>> facesByVertexIndices) : this() {
            InitIndexed(verticesByPoints, facesByVertexIndices);
            Vertices.CullUnused();
        }

        private void InitIndexed(IEnumerable<Vector3> verticesByPoints, IEnumerable<IEnumerable<int>> facesByVertexIndices) {
            
            // Add vertices
            foreach (var p in verticesByPoints) {
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
        //                foreach (var v in this.Vertices) {
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

        public ConwayPoly Foo(float offset) {
            
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            
            for (int i = 0; i < Faces.Count; i++) {
                
                var face = Faces[i];
                
                int c = vertexPoints.Count;
                //vertexPoints.AddRange(face.GetVertices().Select(v => v.Position));
				var faceIndex = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++) {
                //    faceIndex.Add(c+ii);
                }
                
                c = vertexPoints.Count;
                vertexPoints.AddRange(face.GetVertices().Select(v => v.Position +  face.Centroid - face.Centroid * offset));
                faceIndex = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++) {
                    faceIndex.Add(c+ii);
                }
                faceIndices.Add(faceIndex);
                
            }
                
            return new ConwayPoly(vertexPoints, faceIndices);
        }
        
        public ConwayPoly KisN(float offset, int sides) {

            // vertices and faces to vertices
            var newVerts = Faces.Select(f => f.Centroid + f.Normal * offset);
            var vertexPoints = Enumerable.Concat(Vertices.Select(v => v.Position), newVerts);
                
            // vertex lookup
            var vlookup = new Dictionary<string, int>();
            int originalVerticesCount = Vertices.Count;
            for (int i = 0; i < originalVerticesCount; i++) {
                vlookup.Add(Vertices[i].Name, i);
            }
    
            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int i = 0; i < Faces.Count; i++) {
                if (Faces[i].Sides != sides) {
                    faceIndices.Add(ListFacesByVertexIndices()[i]);
                } else {
                    foreach (var edge in Faces[i].GetHalfedges()) {
                        // create new face from edge start, edge end and centroid
                        faceIndices.Add(
                            new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + originalVerticesCount}
                        );
                    }
                }
            }
                
            return new ConwayPoly(vertexPoints, faceIndices);
        }

        // TODO
        // See http://elfnor.com/conway-polyhedron-operators-in-sverchok.html
        // and https://en.wikipedia.org/wiki/Conway_polyhedron_notation
        //
        // chamfer
        // gyro
        // whirl
        // propellor
        // snub (=gyro(dual))

        /// <summary>
        /// Conway's dual operator
        /// </summary>
        /// <returns>the dual as a new mesh</returns>
        public ConwayPoly Dual() {
            
            // Create vertices from faces
            List<Vector3> vertexPoints = new List<Vector3>(Faces.Count);
            foreach (var f in Faces) {vertexPoints.Add(f.Centroid);}

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

                foreach (var f in v.GetVertexFaces()) {
                    fIndex.Add(flookup[f.Name]);
                }
                    
                if (naked[v.Name]) {  // Handle boundary vertices...
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
        
        public ConwayPoly Kebab(float r = 0.3333f, float h = 0.2f)  // A kebab is a mixed up gyro... Geddit?
        {
        
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
                
            // Loop through old faces
            for (int i = 0; i < Faces.Count; i++)
            {
                var oldFace = Faces[i];                
                var thisFaceIndices = new List<int>();
                
                // Loop through each vertex on old face and create a new face for each
                for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
                {

                    var edges = oldFace.GetHalfedges();

                    vertexPoints.Add(oldFace.Centroid);
                    int centroidIndex = vertexPoints.Count - 1;

                    var seedVertex = edges[j].Vertex;
                    vertexPoints.Add(seedVertex.Position);
                    var seedVertexIndex = vertexPoints.Count - 1;
                    
                    var oneThirdVertex = edges[j].Prev.OneThirdPoint;
                    vertexPoints.Add(oneThirdVertex);
                    int oneThirdIndex = vertexPoints.Count - 1;
                    
                    var prevThirdVertex = edges[j].OneThirdPoint;
                    vertexPoints.Add(prevThirdVertex);
                    int prevThirdIndex = vertexPoints.Count - 1;
                    
                    var prevTwoThirdVertex = edges[j].Prev.TwoThirdsPoint;
                    vertexPoints.Add(prevTwoThirdVertex);
                    int prevTwoThirdIndex = vertexPoints.Count - 1;
                    
                    thisFaceIndices.Add(centroidIndex);
                    thisFaceIndices.Add(oneThirdIndex);
                    thisFaceIndices.Add(seedVertexIndex);
                    thisFaceIndices.Add(prevTwoThirdIndex);
                    thisFaceIndices.Add(prevThirdIndex);
                }
            
            faceIndices.Add(thisFaceIndices);

            }
            return new ConwayPoly(vertexPoints, faceIndices);
        }

        public ConwayPoly Gyro(float r = 0.3333f, float scale = 0.8f)
        {
            
            // Happy accidents - skip n new faces - offset just the centroid?
        
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
                
            // Loop through old faces
            for (int i = 0; i < Faces.Count; i++)
            {
                var oldFace = Faces[i];                
                
                // Loop through each vertex on old face and create a new face for each
                for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
                {

                    var thisFaceIndices = new List<int>();

                    var edges = oldFace.GetHalfedges();

                    //vertexPoints.Add(oldFace.Centroid * (1 + scale));
                    vertexPoints.Add(oldFace.Centroid);
                    int centroidIndex = vertexPoints.Count - 1;

                    var seedVertex = edges[j].Vertex;
                    vertexPoints.Add(seedVertex.Position);
                    var seedVertexIndex = vertexPoints.Count - 1;
                    
                    var oneThirdVertex = edges[j].OneThirdPoint;
                    vertexPoints.Add(oneThirdVertex);
                    int oneThirdIndex = vertexPoints.Count - 1;
                    
                    var prevThirdVertex = edges[j].Next.TwoThirdsPoint;
                    vertexPoints.Add(prevThirdVertex);
                    int prevThirdIndex = vertexPoints.Count - 1;
                    
                    var prevTwoThirdVertex = edges[j].Next.OneThirdPoint;
                    vertexPoints.Add(prevTwoThirdVertex);
                    int PrevTwoThirdIndex = vertexPoints.Count - 1;
                    
                    thisFaceIndices.Add(centroidIndex);
                    thisFaceIndices.Add(oneThirdIndex);
                    thisFaceIndices.Add(seedVertexIndex);
                    thisFaceIndices.Add(prevThirdIndex);
                    thisFaceIndices.Add(PrevTwoThirdIndex);
                    
                    faceIndices.Add(thisFaceIndices);
                }
            }

            var poly = new ConwayPoly(vertexPoints, faceIndices);
            //AdjustXYZ(3);
            return poly;
        }


        /// <summary>
        /// Conway's ambo operator
        /// </summary>
        /// <returns>the ambo as a new mesh</returns>
        public ConwayPoly Ambo() {
            
            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            var vertexPoints = new List<Vector3>();  // vertices as points
            var hlookup = new Dictionary<string, int>();
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







	    /**
	    * Computes the "medial" polyhedron of this polyhedron. Adds vertices at the
	    * face centroids and edge midpoints. Each face is split into 2n triangles,
	    * where n is the number of vertices in the face. These triangles share a
	    * vertex at the face's centroid.
	    * 
	    * @return The medial polyhedron.
	    */
//	    public ConwayPoly medial() {
//		    return this.medial(2);
//	    }
	
		//	    public ConwayPoly exalt() {
		//		    return this.needle().needle();
		//	    }
		//	    
		//	    public ConwayPoly yank() {
		//		    return this.zip().zip();
		//	    }
	
	    /**
	    * Computes the "chamfer" polyhedron of this polyhedron. Truncates edges
	    * and replaces them with hexagonal faces.
	    * 
	    * @return The chamfer polyhedron.
	    */
	
	    public ConwayPoly Chamfer() {
		    return Dual().Subdivide().Dual();
	    }

        	
		/**
		* Computes the "subdivide" polyhedron of this polyhedron. Adds vertices at
		* the midpoints of edges, and creates new triangular faces around original
		* vertices. Equivalent to ambo without removing the original vertices.
		* 
		* @return The subdivide polyhedron.
		*/
		public ConwayPoly Subdivide() {
			
		    var vertexPoints = Enumerable.Concat(
		        Vertices.Select(v => v.Position),
                Halfedges.Select(e => e.Midpoint)
            ).ToList();
		    
		    var vlookup = new Dictionary<Vector3, int>();
		    for (int i = 0; i < vertexPoints.Count; i++) {
		        vlookup[vertexPoints[i]] = i;
		    }
		    
		    var faceIndices = new List<IEnumerable<int>>();
		    foreach (var face in Faces)
		    {

		        var newFace = new int[face.GetHalfedges().Count];

		        var list = face.GetHalfedges();
		        for (var index = 0; index < list.Count; index++)
		        {
		            var edge = list[index];
		            faceIndices.Add(
		                new[]
		                {
		                    // Happy accident: reverse order of these
		                    vlookup[edge.Midpoint], // Happy accident - try adding Prev instead
		                    vlookup[edge.Vertex.Position],
		                    vlookup[edge.Next.Midpoint]
		                }
		            );

		            var v = edge.Midpoint;
		            vertexPoints.Add(v);
		            newFace[index] = vertexPoints.Count - 1;
		        }

//		        faceIndices.Add(
//		            face.GetHalfedges().Select(e => vlookup[e.Midpoint])  // Happy accident: miss out the middle face or vertex faces
//		        );
		        
		        faceIndices.Add(newFace);
		        
		    }
		    
		    return new ConwayPoly(vertexPoints, faceIndices);
		}
	
		/**
		* Computes the "loft" polyhedron of this polyhedron. Adds a smaller
		* version of this face, with n trapezoidal faces connecting the inner
		* smaller version and the outer original version, where n is the number
		* of vertices the face has.
		* 
		* @return The loft polyhedron.
		*/
//		public ConwayPoly loft()
//		{
//			return loft(-1, true);
//		}

		/**
		* Computes the "loft" polyhedron of this polyhedron, except only faces
		* with the specified number of sides are lofted.
		*
		* @param n The number of sides a face needs to have loft applied to it.
		* @return The polyhedron with loft applied to faces with n sides.
		*/
//		public ConwayPoly loft(int n)
//		{
//			return loft(n, false);
//		}

		/**
		* A helper method which implements the loft operation, both the version
		* parametrized on the number of sides of affected faces and the one
		* without the parameter. If the "ignore" flag is set to true, every face
		* is modified.
		*
		* @param n     The number of sides a face needs to have loft applied
		*              to it.
		* @param ignore True if we want to ignore the parameter n.
		* @return The loft polyhedron.
		*/
//		private ConwayPoly loft(int n, bool ignore) {
//		    ConwayPoly loftPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				loftPolyhedron.Vertices.Add(vertexPos);
//			}
//
//			// Generate new vertices
//			var newVertices = new Dictionary<Face, int[]>();
//			int vertexIndex = loftPolyhedron.Vertices.Count;
//			foreach (var face in Faces) {
//				if (ignore || face.GetVertices().Count == n) {
//					Face shrunk = new Face(face.GetVertices().Count);
//					int[] newFaceVertices = new int[face.GetVertices().Count];
//
//					Vector3 centroid = face.Centroid;
//					for (int i = 0; i < face.GetVertices().Count; i++) {
//						int index = face.getVertexIndex(i);
//						var vertex = Vertices[index];
//						var newVertex = VectorMath.interpolate(vertex, centroid, 0.3);
//
//						loftPolyhedron.Vertices.Add(newVertex);
//						newFaceVertices[i] = vertexIndex;
//						shrunk.setVertexIndex(i, vertexIndex);
//						vertexIndex++;
//					}
//
//					newVertices[face] = newFaceVertices;
//					loftPolyhedron.Faces.Add(shrunk);
//				}
//			}
//
//			// Generate new faces
//			foreach (var face in Faces) {
//				if (newVertices.ContainsKey(face)) {
//					int[] newFaceVertices = newVertices[face];
//					int prevIndex = face.getVertexIndex(face.GetVertices().Count - 1);
//					int newPrevIndex = newFaceVertices[face.GetVertices().Count - 1];
//					for (int i = 0; i < face.GetVertices().Count; i++) {
//						int currIndex = face.getVertexIndex(i);
//						int newCurrIndex = newFaceVertices[i];
//
//						Face trapezoid = new Face(4);
//						trapezoid.setAllVertexIndices(prevIndex, currIndex, newCurrIndex, newPrevIndex);
//						loftPolyhedron.Faces.Add(trapezoid);
//
//						prevIndex = currIndex;
//						newPrevIndex = newCurrIndex;
//					}
//				} else {
//					// Keep original face
//					loftPolyhedron.Faces.Add(face);
//				}
//			}
//
//			loftPolyhedron.setVertexNormalsToFaceNormals();
//			return loftPolyhedron;
//		}

	    /**
		* Compute the "quinto" polyhedron of this polyhedron. Equivalent to an
		* ortho but truncating the vertex at the center of original faces. This
		* creates a small copy of the original face (but rotated).
		* 
		* @return The quinto polyhedron.
		*/
//		public ConwayPoly quinto() {
//		    ConwayPoly quintoPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				quintoPolyhedron.Vertices.Add(vertexPos);
//			}
//		
//			// Create new vertices at the midpoint of each edge and toward the
//			// face's centroid
//			var edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, quintoPolyhedron);
//
//			int vertexIndex = quintoPolyhedron.Vertices.Count;
//			var midptVertices = new Dictionary<Halfedge, int>();
//			foreach (var edge in this.Halfedges) {
//				quintoPolyhedron.Vertices.Add(new Vertex(edge.Midpoint));
//				midptVertices[edge] = vertexIndex++;
//			}
//		
//			// Generate new faces
//			foreach (var face in Faces) {
//				Face centralFace = new Face(face.GetVertices().Count);
//				var edges = face.GetHalfedges();
//			
//				int[] prevEnds = edges[edges.Count - 1].getEnds();
//				int prevVertex = edgeToVertex[prevEnds[0]][prevEnds[1]];
//				int prevMidpt = midptVertices[edges[edges.Count - 1]];
//				int centralIndex = 0;
//				foreach (var currEdge in edges) {
//					int[] currEnds = currEdge.getEnds();
//					int currVertex = edgeToVertex[currEnds[0]][currEnds[1]];
//					int currMidpt = midptVertices[currEdge];
//				
//					Face pentagon = new Face(5);
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
//			quintoPolyhedron.setVertexNormalsToFaceNormals();
//			return quintoPolyhedron;
//		}

	    /**
		* Computes the "joined-lace" polyhedron of this polyhedron. Like lace, but
		* old edges are replaced by quadrilateral faces instead of two triangular
		* faces.
		*
		* @return The joined-lace polyhedron.
		*/
//		public ConwayPoly joinedLace() {
//			return this.lace(-1, true, true);
//		}

		/**
		* Computes the "lace" polyhedron of this polyhedron. Like loft, but has
		* on each face an antiprism of the original face instead of a prism.
		* 
		* @return The lace polyhedron.
		*/
//		public ConwayPoly lace() {
//			return this.lace(-1, true, false);
//		}

		/**
		* Computes the "lace" polyhedron of this polyhedron, except the operation
		* is only applied to faces with the specified number of sides.
		*
		* @param n The number of sides a face needs to have lace applied to it.
		* @return The polyhedron with lace applied to faces with n sides.
		*/
//		public ConwayPoly lace(int n) {
//			return this.lace(n, false, false);
//		}

		/**
		* A helper method for implementing lace, parametrized lace, and
		* joined-lace.
		*
		* @param n      The number of sides a face needs to have lace applied
		*               to it.
		* @param ignore True if we want to ignore the parameter n.
		* @param joined True if we want to compute joined-lace.
		* @return The lace polyhedron.
		*/
//		private ConwayPoly lace(int n, bool ignore, bool joined) {
//		    ConwayPoly lacePolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				lacePolyhedron.Vertices.Add(vertexPos);
//			}
//
//			// Generate new vertices
//			var edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, lacePolyhedron);
//
//			if (joined) {
//				PolyhedraUtils.addRhombicFacesAtEdges(this, lacePolyhedron, edgeToVertex);
//			}
//
//			// Generate new faces
//			foreach (var face in Faces) {
//				if (ignore || face.GetVertices().Count == n) {
//					Face twist = new Face(face.GetVertices().Count);
//					var edges = face.GetHalfedges();
//
//					for (int i = 0; i < edges.Count; i++) {
//						// Build face at center of each original face
//						int[] ends = edges[i].getEnds();
//						int newVertex = edgeToVertex[ends[0]][ends[1]];
//						twist.setVertexIndex(i, newVertex);
//
//						// Always generate triangles from vertices to central face
//						int nextInd = (i + 1) % edges.Count;
//						int[] nextEnds = edges[nextInd].getEnds();
//						int nextNewVertex = edgeToVertex[nextEnds[0]][nextEnds[1]];
//
//						Face smallTriangle = new Face(3);
//						smallTriangle.setAllVertexIndices(nextNewVertex, newVertex, ends[1]);
//						lacePolyhedron.Faces.Add(smallTriangle);
//					}
//
//					lacePolyhedron.Faces.Add(twist);
//
//					if (!joined) {
//						// If not joined, generate triangle faces
//						foreach (var edge in edges) {
//							int[] ends = edge.getEnds();
//							int currVertex = edgeToVertex[ends[0]][ends[1]];
//
//							Face largeTriangle = new Face(3);
//							largeTriangle.setAllVertexIndices(currVertex,
//								ends[0], ends[1]);
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
//			lacePolyhedron.setVertexNormalsToFaceNormals();
//			return lacePolyhedron;
//		}

		/**
		* Computes the "stake" polyhedron of this polyhedron. Like lace, but
		* instead of having a central face, there is a central vertex and 
		* quadrilaterals around the center.
		* 
		* @return The stake polyhedron.
		*/
//		public ConwayPoly stake() {
//			return this.stake(-1, true);
//		}

		/**
		* Computes the "stake" polyhedron of this polyhedron, but only performs
		* the operation on faces with n sides.
		*
		* @param n The number of sides a face needs to have stake applied to it.
		* @return The polyhedron with stake applied to faces with n sides.
		*/
//		public ConwayPoly stake(int n) {
//			return this.stake(n, false);
//		}

		/**
		* A helper method for implementing stake and parametrized stake.
		*
		* @param n      The number of sides a face needs to have stake applied
		*               to it.
		* @param ignore True if we want to ignore the parameter n.
		* @return The stake polyhedron.
		*/
//		private ConwayPoly stake(int n, bool ignore) {
//		    ConwayPoly stakePolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				stakePolyhedron.Vertices.Add(vertexPos);
//			}
//
//			// Generate new vertices
//			var edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, stakePolyhedron);
//
//			int vertexIndex = stakePolyhedron.Vertices.Count;
//			foreach (var face in Faces) {
//				if (ignore || face.GetVertices().Count == n) {
//					Vector3 centroid = face.Centroid;
//					stakePolyhedron.Vertices.Add(new Vertex(centroid));
//					int centroidIndex = vertexIndex++;
//
//					var edges = face.GetHalfedges();
//
//					// Generate the quads and triangles on this face
//					for (int i = 0; i < edges.Count; i++) {
//						int[] ends = edges[i].getEnds();
//						int currVertex = edgeToVertex[ends[0]][ends[1]];
//						int[] nextEnds = edges[(i + 1) % edges.Count].getEnds();
//						int nextVertex = edgeToVertex[nextEnds[0]][nextEnds[1]];
//
//						Face triangle = new Face(3);
//						Face quad = new Face(4);
//						triangle.setAllVertexIndices(currVertex, ends[0], ends[1]);
//						quad.setAllVertexIndices(nextVertex, centroidIndex,
//							currVertex, ends[1]);
//
//						stakePolyhedron.Faces.Adds(triangle, quad);
//					}
//				} else {
//					// Keep original face
//					stakePolyhedron.Faces.Add(face);
//				}
//			}
//
//			stakePolyhedron.setVertexNormalsToFaceNormals();
//			return stakePolyhedron;
//		}

		/**
		* Computes the "edge-medial" polyhedron of this polyhedron. Places a
		* vertex at the centroid of every face, and subdivides each edge into
		* n segments, with edges from these subdivision points to the centroid.
		*
		* For example, the "edge-medial-3" operator corresponds to n = 3.
		*
		* @param n The number of subdivisions on each edge.
		* @return The edge-medial polyhedron with n subdivisions per edge.
		*/
//		public ConwayPoly edgeMedial(int n) {
//			return medial(n, true);
//		}

		/**
		* Computes the "joined-medial" polyhedron of this polyhedron. The same as
		* medial, but with rhombic faces im place of original edges.
		*
		* @return The joined-medial polyhedron.
		*/
//		public ConwayPoly joinedMedial() {
//		    ConwayPoly medialPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				medialPolyhedron.Vertices.Add(vertexPos);
//			}
//
//			// Generate new vertices and rhombic faces on original edges
//			var edgeToVertex = PolyhedraUtils.addEdgeToCentroidVertices(this, medialPolyhedron);
//			PolyhedraUtils.addRhombicFacesAtEdges(this, medialPolyhedron, edgeToVertex);
//
//			// Generate triangular faces
//			int vertexIndex = medialPolyhedron.Vertices.Count;
//			foreach (var face in faces) {
//				Vector3 centroid = face.Centroid;
//				medialPolyhedron.Vertices.Add(new Vertex(centroid));
//
//				var edges = face.GetHalfedges();
//				int[] prevEnds = edges[edges.Length - 1].getEnds();
//				int prevVertex = edgeToVertex[prevEnds[0]][prevEnds[1]];
//				foreach (var edge in edges) {
//					int[] ends = edge.getEnds();
//					int currVertex = edgeToVertex[ends[0]][ends[1]];
//
//					Face triangle1 = new Face(3);
//					Face triangle2 = new Face(3);
//					triangle1.setAllVertexIndices(vertexIndex, ends[0], currVertex);
//					triangle2.setAllVertexIndices(vertexIndex, prevVertex, ends[0]);
//
//					medialPolyhedron.Faces.Adds(triangle1, triangle2);
//
//					prevVertex = currVertex;
//				}
//
//				vertexIndex++;
//			}
//
//			medialPolyhedron.setVertexNormalsToFaceNormals();
//			return medialPolyhedron;
//		}

		/**
		* Generalized medial, parametrized on the number of subdivisions on each
		* edge. The regular medial operation corresponds to n = 2 subdivisions.
		*
		* @param n The number of subdivisions on each edge.
		* @return The medial polyhedron with n subdivisions per edge.
		*/
//		public ConwayPoly medial(int n) {
//			return medial(n, false);
//		}

		/**
		* A helper method for computing edge-medial and medial (parametrized).
		*
		* @param n    The number of subdivisions per edge.
		* @param edge True if computing edge-medial, false if regular medial.
		* @return The medial polyhedron subjected to the input constraints.
		*/
//		private ConwayPoly medial(int n, bool edge) {
//		    ConwayPoly medialPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				medialPolyhedron.Vertices.Add(vertexPos);
//			}
//
//			// Create new vertices on edges
//			var newVertices = PolyhedraUtils.subdivideEdges(this, medialPolyhedron, n);
//
//			int vertexIndex = medialPolyhedron.Vertices.Count;
//			foreach (var face in Faces) {
//				Vector3 centroid = face.Centroid;
//				var faceEdges = face.GetHalfedges();
//
//				var prevEdge = faceEdges[faceEdges.Count - 1];
//				int[] prevEnds = prevEdge.getEnds();
//				foreach (var currEdge in faceEdges) {
//					int[] currEnds = currEdge.getEnds();
//					int[] currNewVerts = newVertices[currEnds[0]][currEnds[1]];
//
//					int prevLastVert = newVertices[prevEnds[0]][prevEnds[1]][n - 2];
//					if (edge) {
//						// One quadrilateral face
//						Face quad = new Face(4);
//						quad.setAllVertexIndices(currEnds[0], currNewVerts[0], vertexIndex, prevLastVert);
//
//						medialPolyhedron.Faces.Add(quad);
//					} else {
//						// Two triangular faces
//						Face triangle1 = new Face(3);
//						Face triangle2 = new Face(3);
//						triangle1.setAllVertexIndices(currEnds[0], currNewVerts[0], vertexIndex);
//						triangle2.setAllVertexIndices(vertexIndex, prevLastVert, currEnds[0]);
//						medialPolyhedron.Faces.Adds(triangle1, triangle2);
//					}
//
//					// Create new triangular faces at edges
//					for (int i = 0 ; i < currNewVerts.Length - 1 ; i++) {
//						Face edgeTriangle = new Face(3);
//						edgeTriangle.setAllVertexIndices(vertexIndex, currNewVerts[i], currNewVerts[i + 1]);
//
//						medialPolyhedron.Faces.Add(edgeTriangle);
//					}
//				}
//
//				medialPolyhedron.Vertices.Add(new Vertex(centroid));
//				vertexIndex++;
//			}
//
//			medialPolyhedron.setVertexNormalsToFaceNormals();
//			return medialPolyhedron;
//		}

	    /**
		* Computes the "propellor" polyhedron of this polyhedron. It is like gyro,
		* but instead of having a central vertex we have a central face. This
		* creates quadrilateral faces instead of pentagonal faces.
		* 
		* @return The propellor polyhedron.
		*/
//		public ConwayPoly propellor() {
//		    ConwayPoly propellorPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				propellorPolyhedron.Vertices.Add(vertexPos);
//			}
//		
//			// Create new vertices on edges
//			var newVertices = PolyhedraUtils.subdivideEdges(this, propellorPolyhedron, 3);
//		
//			// Create quadrilateral faces and one central face on each face
//			foreach (var face in Faces) {
//				var faceEdges = face.GetHalfedges();
//			
//				Face centralFace = new Face(face.GetVertices().Count);
//				int[] prevEnds = faceEdges[faceEdges.Count - 1].getEnds();
//				int[] prevEdgeVertices = newVertices[prevEnds[0]][prevEnds[1]];
//				for (int i = 0 ; i < faceEdges.Count ; i++) {
//					int[] ends = faceEdges[i].getEnds();
//					int[] newEdgeVertices = newVertices[ends[0]][ends[1]];
//				
//					Face quad = new Face(4);
//					quad.setAllVertexIndices(ends[0], newEdgeVertices[0],
//						prevEdgeVertices[0], prevEdgeVertices[1]);
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
//			propellorPolyhedron.setVertexNormalsToFaceNormals();
//			return propellorPolyhedron;
//		}
	
		/**
		* Computes the "whirl" polyhedron of this polyhedron. Forms hexagon
		* faces at each edge, with a small copy of the original face at the
		* center of the original face.
		* 
		* @return The whirl polyhedron.
		*/
//		public ConwayPoly whirl() {
//		    ConwayPoly whirlPolyhedron = new ConwayPoly();
//			foreach (var vertexPos in Vertices) {
//				whirlPolyhedron.Vertices.Add(vertexPos);
//			}
//		
//			// Create new vertices on edges
//			var newVertices = PolyhedraUtils.subdivideEdges(this, whirlPolyhedron, 3);
//		
//			// Generate vertices near the center of each face
//			var centerVertices = new Dictionary<Face, int[]>();
//			int vertexIndex = whirlPolyhedron.Vertices.Count;
//			foreach (var face in Faces) {
//				int[] newCenterIndices = new int[face.GetVertices().Count];
//				Vector3 centroid = face.Centroid;
//				int i = 0;
//				foreach (var edge in face.GetHalfedges()) {
//					int[] ends = edge.getEnds();
//					int[] edgeVertices = newVertices[ends[0]][ends[1]];
//					var edgePoint = whirlPolyhedron.Vertices[edgeVertices[1]];
//					Vector3 diff = new Vector3();
//					diff.sub(edgePoint, centroid);
//					diff.scale(0.3); // 0 < arbitrary scale factor < 1
//				
//					Vector3 newFacePoint = new Vector3();
//					newFacePoint.add(centroid, diff);
//				
//					whirlPolyhedron.Vertices.Add(new Vertex(newFacePoint));
//					newCenterIndices[i++] = vertexIndex++;
//				}
//			
//				centerVertices[face] = newCenterIndices;
//			}
//		
//			// Generate hexagonal faces and central face
//			foreach (var face in Faces) {
//				Face centralFace = new Face(face.GetVertices().Count);
//			
//				var faceEdges = face.GetHalfedges();
//				int[] centralVertices = centerVertices[face];
//				int[] pEnds = faceEdges[faceEdges.Count - 1].getEnds();
//				int[] prevEdgeVertices = newVertices[pEnds[0]][pEnds[1]];
//				int prevCenterIndex = centralVertices[centralVertices.Length - 1];
//				for (int i = 0 ; i < face.GetVertices().Count ; i++) {
//					int[] ends = faceEdges[i].getEnds();
//					int[] edgeVertices = newVertices[ends[0]][ends[1]];
//					int currCenterIndex = centralVertices[i];
//				
//					Face hexagon = new Face(6);
//					hexagon.setAllVertexIndices(ends[0], edgeVertices[0],
//						edgeVertices[1], currCenterIndex, prevCenterIndex,
//						prevEdgeVertices[1]);
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
//			whirlPolyhedron.setVertexNormalsToFaceNormals();
//			return whirlPolyhedron;
//		}

		/**
		* Computes the "volute" polyhedron of this polyhedron. Equivalent to a
		* snub operation followed by kis on the original faces. This is the dual
		* of whirl.
		* 
		* @return The volute polyhedron.
		*/
//		public ConwayPoly volute() {
//			return this.whirl().Dual();
//		}

        #endregion

        #region geometry methods

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
                var origFaces = ribbon.Faces.ToArray();
    
                List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();
    
                // create new "vertex" faces
                List<List<Vertex>> allNewVertices = new List<List<Vertex>>();
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
    
                    foreach (var edge in halfedges) {
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
                    allNewVertices.Add(new_vertices);
                }
    
                // change edges to reference new vertices
                for (int k = 0; k < Vertices.Count; k++) {
                    Vertex v = ribbon.Vertices[k];
                    if (allNewVertices[k].Count < 1) {
                        continue;
                    }
    
                    int c = 0;
                    foreach (var edge in incidentEdges[k]) {
                        if (!ribbon.Halfedges.SetVertex(edge, allNewVertices[k][c++]))
                            edge.Vertex = allNewVertices[k][c];
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
    
                foreach (var halfedge in items) {
                    if (halfedge.Pair != null) {
                        // insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
                        if (smooth > 0.0) {
                            if (smooth > 0.5) {
                                smooth = 0.5f;
                            }
    
                            Vertex[] newVertices = {
                                new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
                                new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
                                new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
                                new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
                            };
                            ribbon.Vertices.AddRange(newVertices);
                            Vertex[] new_vertices1 = {
                                halfedge.Vertex,
                                newVertices[0],
                                newVertices[3],
                                halfedge.Pair.Prev.Vertex
                            };
                            Vertex[] new_vertices2 = {
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
                            Vertex[] newVertices = {
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
                foreach (var item in origFaces) {
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
        
        // Next 3 methods taken from http://www.georgehart.com/virtual-polyhedra/conway_notation.html
        
        public ConwayPoly AdjustXYZ(int iterations)
        {
            var poly = Duplicate();
            var dpoly = Dual();   // v's of dual are in order or arg's f's
            
            for (var count=0; count<iterations; count++) {    // iteration:
                dpoly.Vertices = ReciprocalC(poly);             // reciprocate face centers
                poly.Vertices = ReciprocalC(dpoly);            // reciprocate face centers
                Debug.Log(Vertices);
            }
            
            return poly;
        }
        
        // Returns array of reciprocals of face centers
        public MeshVertexList ReciprocalC(ConwayPoly poly)
        {
            
            MeshVertexList centers = FaceCenters(poly);
            
            for (int i=0; i<poly.Faces.Count; i++)
            {
                var m2 =
                    centers[i].Position.x * centers[i].Position.x +
                    centers[i].Position.y * centers[i].Position.y +
                    centers[i].Position.z * centers[i].Position.z;
                // Divide each coord by magnitude squared
                centers[i] = new Vertex(new Vector3(
                    centers[i].Position.x / m2,
                    centers[i].Position.y / m2,
                    centers[i].Position.z / m2
                ));
            }
            return centers;
        }
        
        // Returns array of Face centers
        public MeshVertexList FaceCenters(ConwayPoly poly)
        {
            var ans = new MeshVertexList();
            for (int i=0; i<poly.Faces.Count; i++)
            {
                ans.Add(new Vertex(poly.Faces[i].Centroid));
            }
            return ans;
        }

        #endregion

        #region methods

            /// <summary>
            /// A string representation of the mesh, mimicking Grasshopper's mesh class.
            /// </summary>
            /// <returns>a string representation of the mesh</returns>
            public override string ToString() {
                return base.ToString() + string.Format(" (V:{0} F:{1})", Vertices.Count, Faces.Count);
            }
    
            /// <summary>
            /// Gets the positions of all mesh vertices. Note that points are duplicated.
            /// </summary>
            /// <returns>a list of vertex positions</returns>
            private Vector3[] ListVerticesByPoints() {
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
            private List<int>[] ListFacesByVertexIndices() {
                
                var fIndex = new List<int>[Faces.Count];
                var vlookup = new Dictionary<String, int>();
    
                for (int i = 0; i < Vertices.Count; i++) {
                    vlookup.Add(Vertices[i].Name, i);
                }
                
                for (int i = 0; i < Faces.Count; i++) {
                    List<int> vertIdx = new List<int>();
                    foreach (var v in Faces[i].GetVertices()) {
                        vertIdx.Add(vlookup[v.Name]);
                    }
                    fIndex[i] = vertIdx;
                }
    
                return fIndex;
            }
    
            /// <summary>
            /// Convert to Unity mesh type.
            /// Recursively triangulates until only tri-faces remain.
            /// </summary>
            /// <returns>A Rhino mesh.</returns>
            public UnityEngine.Mesh ToUnityMesh(bool forceTwosided=false) {

                bool hasNaked = Halfedges.Select((item, ii) => ii).Where(i => Halfedges[i].Pair == null).ToList().Count > 0;
                
                var target = new UnityEngine.Mesh();
                var meshTriangles = new List<int>();
                var meshVertices = new List<Vector3>();
                var meshNormals = new List<Vector3>();
    
                // TODO: duplicate mesh and triangulate
                ConwayPoly source = Duplicate(); //.Triangulate();
//                for (int i = 0; i < source.Faces.Count; i++) {
//                    if (source.Faces[i].Sides > 3) {
//                        source.Faces.Triangulate(i, false);
//                    }
//                }
    
                // Strip down to Face-Vertex structure
                Vector3[] points = source.ListVerticesByPoints();
                List<int>[] faceIndices = source.ListFacesByVertexIndices();
    
                // Add faces
                int index = 0;

                for (var i = 0; i < faceIndices.Length; i++) {
                    List<int> f = faceIndices[i];
                    if (f.Count == 3) {
                        
                        var faceNormal = source.Faces[i].Normal;
                        
                        meshNormals.Add(faceNormal);
                        meshNormals.Add(faceNormal);
                        meshNormals.Add(faceNormal);
                        
                        meshVertices.Add(points[f[0]]);
                        meshTriangles.Add(index++);
                        meshVertices.Add(points[f[1]]);
                        meshTriangles.Add(index++);
                        meshVertices.Add(points[f[2]]);
                        meshTriangles.Add(index++);

                        if (hasNaked || forceTwosided) {
                            
                            meshNormals.Add(-faceNormal);
                            meshNormals.Add(-faceNormal);
                            meshNormals.Add(-faceNormal);
                        
                            meshVertices.Add(points[f[0]]);
                            meshTriangles.Add(index++);
                            meshVertices.Add(points[f[2]]);
                            meshTriangles.Add(index++);
                            meshVertices.Add(points[f[1]]);
                            meshTriangles.Add(index++);
                        }
                        
                    }
                    else {
                        Debug.Log("Non-triangular face found");
                    }
                }

                target.vertices = meshVertices.ToArray();
                target.normals = meshNormals.ToArray();
                target.triangles = meshTriangles.ToArray();
                
                if (hasNaked || forceTwosided) {
                    target.RecalculateNormals();
                }
                target.RecalculateNormals();

                return target;
            }
        
            public void ScaleToUnitSphere() {
                
                // Find the furthest vertex
                Vertex max = Vertices.OrderByDescending(x => x.Position.magnitude).FirstOrDefault();
                float scale = 1.0f/max.Position.magnitude;
                
                // TODO Ideal use case for Linq if I could get my head round the type-wrangling needed
                foreach (var v in Vertices) {
                    v.Position = v.Position * scale;
                }
                
            }
    
    //        TODO
    //        public List<Polyline> ToClosedPolylines() {
    //            List<Polyline> polylines = new List<Polyline>(Faces.Count);
    //            foreach (var f in Faces) {
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
            foreach (var edge in dup.Halfedges) {
                Halfedges.Add(edge);
            }

            foreach (var face in dup.Faces) {
                Faces.Add(face);
            }
        }
        
        


        #endregion
        
    }
}