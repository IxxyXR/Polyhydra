using System;
using UnityEngine;


namespace Buckminster.Types {
    
    public class Halfedge {
        
        #region constructors
    
            public Halfedge(Vertex vertex) {
                Vertex = vertex;
            }
    
            public Halfedge(Vertex vertex, Halfedge next, Halfedge prev, Face face) : this(vertex) {
                Next = next;
                Prev = prev;
                Face = face;
            }
    
            public Halfedge(Vertex vertex, Halfedge next, Halfedge prev, Face face, Halfedge pair)  : this(vertex, next, prev, face) {
                Pair = pair;
            }

        #endregion

        #region properties

            public Halfedge Next { get; set; }
            public Halfedge Prev { get; set; }
            public Halfedge Pair { get; set; }
            public Vertex Vertex { get; set; }
            public Face Face { get; set; }
    
            public String Name {
                get {
                    if (Vertex == null || Prev == null) {
                        return null;
                    }
    
                    if (Prev.Vertex == null) {
                        return null;
                    }
    
                    return Vertex.Name + Prev.Vertex.Name;
                }
            }
    
            public Vector3 Midpoint {
                get { return Vertex.Position + -0.5f * (Vertex.Position - Prev.Vertex.Position); }
            }
    
            public Vector3 Vector {
                get { return Vertex.Position - Prev.Vertex.Position; }
            }

        #endregion
    }
}