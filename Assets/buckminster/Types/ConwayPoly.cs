using System;
using System.Collections.Generic;
using System.Linq;
using Buckminster.Types.Collections;
using Polylib;
using UnityEditor;
using UnityEngine;

namespace Buckminster.Types {

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
            foreach (Vector p in source.Vertices) {
                Vertices.Add(new Vertex(p.getVector3()));
            }

            // Add faces (and construct halfedges and store in hash table)
            foreach (Polylib.Face face in source.faces) {
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

        /// <summary>
        /// Constructor to build a custom mesh from Unity's mesh type
        /// </summary>
        /// <param name="source">the Rhino mesh</param>
        public ConwayPoly(UnityEngine.Mesh source) : this() {

            // Add vertices
            Vertices.Capacity = source.vertexCount;
            foreach (Vector3 p in source.vertices) {
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

        private ConwayPoly(IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices) : this() {
            //InitIndexed(verticesByPoints, facesByVertexIndices);

            // Add vertices
            foreach (Vector3 p in verticesByPoints) {
                Vertices.Add(new Vertex(p));
            }

            foreach (IEnumerable<int> indices in facesByVertexIndices) {
                Faces.Add(indices.Select(i => Vertices[i]));
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();

            //Vertices.CullUnused();
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
        public MeshVertexList Vertices { get; private set; }
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
                //faceIndices.Add(faceIndex);
                
                c = vertexPoints.Count;
                vertexPoints.AddRange(face.GetVertices().Select(v => v.Position +  face.Centroid - ( face.Centroid * offset)));
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
            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++) {
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
                            new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
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
        
        public ConwayPoly Gyro(float r=0.3333f, float h=0.2f) {
        
//            // Create sublist of non-boundary vertices
//            var naked = new Dictionary<string, bool>(Vertices.Count); // vertices (name, boundary?)
//            var hlookup =
//                new Dictionary<string, int>(Halfedges.Count); // boundary halfedges (name, index of point in new mesh)
//            foreach (var he in Halfedges) {
//                if (!naked.ContainsKey(he.Vertex.Name)) // if not in dict, add (boundary == true)
//                    naked.Add(he.Vertex.Name, he.Pair == null);
//                else if (he.Pair == null) // if in dict and belongs to boundary halfedge, set true
//                    naked[he.Vertex.Name] = true;
//                if (he.Pair == null) {
//                    // if boundary halfedge, add mid-point to vertices and add to lookup
//                    hlookup.Add(he.Name, vertexPoints.Count);
//                    vertexPoints.Add(he.Midpoint);
//                }
//            }
//
//            // List new faces by their vertex indices
//            // (i.e. old vertices by their face indices)
//            Dictionary<string, int> flookup = new Dictionary<string, int>();
//            for (int i = 0; i < Faces.Count; i++)
//                flookup.Add(Faces[i].Name, i);
//
//            var faceIndices = new List<List<int>>(Vertices.Count);
//            foreach (var v in Vertices) {
//                List<int> fIndex = new List<int>();
//                foreach (Face f in v.GetVertexFaces())
//                    fIndex.Add(flookup[f.Name]);
//                if (naked[v.Name]) // Handle boundary vertices...
//                {
//                    var h = v.Halfedges;
//                    if (h.Count > 0) {
//                        // Add points on naked edges and the naked vertex
//                        fIndex.Add(hlookup[h.Last().Name]);
//                        fIndex.Add(vertexPoints.Count);
//                        fIndex.Add(hlookup[h.First().Next.Name]);
//                        vertexPoints.Add(v.Position);
//                    }
//                }
//
//                faceIndices.Add(fIndex);
//            }
            
            // Create vertices from faces
            List<Vector3> vertexPoints = new List<Vector3>(Faces.Count);
            foreach (Face f in Faces) {
                vertexPoints.Add(f.Centroid);
            }
            
            var faceIndices = new List<List<int>>(Vertices.Count);
    

            
                return new ConwayPoly(vertexPoints, faceIndices.ToArray());
            
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
                
                //top.Halfedges.Flip();
    
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
                    foreach (Vertex v in Faces[i].GetVertices()) {
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
                foreach (Vertex v in Vertices) {
                    v.Position = v.Position * scale;
                }
                
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