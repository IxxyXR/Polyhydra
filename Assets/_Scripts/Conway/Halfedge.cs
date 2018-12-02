using System;
using System.Collections.Generic;
using UnityEngine;


namespace Conway {
    
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
                    if (Vertex == null || Prev == null || Prev.Vertex == null) return null;
                    return Vertex.Name + Prev.Vertex.Name;
                }
            }
        
            public String PairedName {
                // A unique name for the half-edge pair
                get
                {
                    if (Vertex == null || Prev == null || Prev.Vertex == null) return null;
                    var names = new List<string> {Vertex.Name, Prev.Vertex.Name};
                    names.Sort();
                    return String.Join(",", names);
            }
            }
    
            public Vector3 Midpoint {
                get { return Vertex.Position + -0.5f * (Vertex.Position - Prev.Vertex.Position); }
            }
        
            public Vector3 OneThirdPoint {
                get { return Vertex.Position + -0.333333f * (Vertex.Position - Prev.Vertex.Position); }
            }
    
            public Vector3 TwoThirdsPoint {
                get { return Vertex.Position + -0.6666666f * (Vertex.Position - Prev.Vertex.Position); }
            }
        
            public Vector3 Vector {
                get { return Vertex.Position - Prev.Vertex.Position; }
            }

        public Vector3 PointAlongEdge(float n)
        {
            return Vertex.Position + -n * (Vertex.Position - Prev.Vertex.Position);
        }

        #endregion

        public string[] getEnds()
        {
            return new string[2] { Vertex.Name, Pair.Vertex.Name};
        }
    }
}