using System;
using System.Collections.Generic;
using Polylib;
using UnityEngine;

namespace Buckminster.Types {
    
    public class Face {
        
        #region constructors

            public Face(Halfedge edge) {
                Halfedge = edge;
                Name = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

        #endregion

        #region properties

            public Halfedge Halfedge { get; set; }
            public String Name { get; private set; }
    
            public Vector3 Centroid {
                get {
                    Vector3 avg = new Vector3();
                    List<Vertex> vertices = GetVertices();
                    foreach (Vertex v in vertices) {
                        avg.x += v.Position.x;
                        avg.y += v.Position.y;
                        avg.z += v.Position.z;
                    }
    
                    avg.x /= vertices.Count;
                    avg.y /= vertices.Count;
                    avg.z /= vertices.Count;
    
                    return avg;
                }
            }
    
            /// <summary>
            /// Get the face normal (unit vector).
            /// </summary>
            public Vector3 Normal {
                get {
                    Vector normal = new Vector(0, 0, 0);
                    Halfedge edge = Halfedge;
                    do {
                        Vector3 crossTmp = Vector3.Cross(edge.Vector, edge.Next.Vector);
                        normal = normal.sum(new Vector(crossTmp.x, crossTmp.y, crossTmp.z));
                        edge = edge.Next;  // move on to next halfedge
                    } while (edge != Halfedge);
                    return new Vector3((float)normal.x, (float)normal.y, (float)normal.z).normalized;
                }
            }
    
            public int Sides {
                get { return GetVertices().Count; }
            }

        #endregion

        #region methods
    
            public List<Vertex> GetVertices() {
                List<Vertex> vertices = new List<Vertex>();
                Halfedge edge = Halfedge;
                do {
                    vertices.Add(edge.Vertex); // add vertex to list
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);
    
                return vertices;
            }
    
            public List<Halfedge> GetHalfedges() {
                List<Halfedge> halfedges = new List<Halfedge>();
                Halfedge edge = Halfedge;
                do {
                    halfedges.Add(edge); // add halfedge to list
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);
    
                return halfedges;
            }
    
            /// <summary>
            /// Constructs a close polyline which follows the edges bordering the face
            /// </summary>
            /// <returns>a closed polyline representing the face</returns>
            /// TODO
    //        public Polyline ToClosedPolyline() {
    //            Polyline polyline = new Polyline();
    //            foreach (BVertex v in GetVertices()) {
    //                polyline.Add(v.Position);
    //            }
    //
    //            polyline.Add(polyline.First); // close polyline
    //            return polyline;
    //        }
    
            public void Split(Vertex v1, Vertex v2, out Face f_new, out Halfedge he_new, out Halfedge he_new_pair) {
                Halfedge e1 = Halfedge;
                while (e1.Vertex != v1) {
                    e1 = e1.Next;
                }
    
                if (v2 == e1.Next.Vertex) {
                    throw new Exception("Vertices adjacent");
                }
    
                if (v2 == e1.Prev.Vertex) {
                    throw new Exception("Vertices adjacent");
                }
    
                f_new = new Face(e1.Next);
    
                Halfedge e2 = e1;
                while (e2.Vertex != v2) {
                    e2 = e2.Next;
                    e2.Face = f_new;
                }
    
                he_new = new Halfedge(v1, e1.Next, e2, f_new);
                he_new_pair = new Halfedge(v2, e2.Next, e1, this, he_new);
                he_new.Pair = he_new_pair;
    
                e1.Next.Prev = he_new;
                e1.Next = he_new_pair;
                e2.Next.Prev = he_new_pair;
                e2.Next = he_new;
            }

        #endregion
    }
}