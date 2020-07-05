using System;
using System.Collections.Generic;
using System.Linq;
using Wythoff;
using UnityEngine;


namespace Conway {
    
    public class Face {
        
        #region constructors

            public Face(Halfedge edge) {
                Halfedge = edge;
                Name = Guid.NewGuid().ToString("N").Substring(0, 8);
            }
        
            public Face() {
                Name = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

        #endregion

        #region properties

            public Halfedge Halfedge { get; set; }
            public String Name { get; private set; }
            private Vector3 _cachedNormal;
            // private bool _hasCachedNormal;

            public float GetArea()
            {
                float area = 0;
                var edges = GetHalfedges();

                if (edges.Count == 3)
                {
                    Vector3 v = Vector3.Cross(
                        edges[0].Vertex.Position - edges[1].Vertex.Position, 
                        edges[0].Vertex.Position - edges[2].Vertex.Position
                    );
                    area += v.magnitude * 0.5f;
                }
                else
                {
                    var centroid = Centroid;
                    for(int i = 0; i < edges.Count; i += 2)
                    {
                        Vector3 a = centroid;
                        Vector3 b = edges[i].Vertex.Position;
                        Vector3 c = edges[i+1].Vertex.Position;
                        Vector3 v = Vector3.Cross(a-b, a-c);
                        area += v.magnitude * 0.5f;
                    }
                }
                return area;
            }
            
            public Vector3 Centroid {
                get {
                    Vector3 avg = new Vector3();
                    List<Vertex> vertices = GetVertices();
                    var vcount = vertices.Count;
                    for (var i = 0; i < vcount; i++)
                    {
                        Vertex v = vertices[i];
                        avg.x += v.Position.x;
                        avg.y += v.Position.y;
                        avg.z += v.Position.z;
                    }

                    avg.x /= vcount;
                    avg.y /= vcount;
                    avg.z /= vcount;
    
                    return avg;
                }
            }
    
            /// <summary>
            /// Get the face normal (unit vector).
            /// </summary>
            public Vector3 Normal {
                get {
                    // TODO cache normals
                    // if (!_hasCachedNormal)
                    // {
                        Vector normal = new Vector(0, 0, 0);
                        var centroid = Centroid;
                        Halfedge edge = Halfedge;
                        do
                        {
                            Vector3 crossTmp = Vector3.Cross(edge.Vector - centroid, edge.Next.Vector - centroid);
                            normal = normal.sum(new Vector(crossTmp.x, crossTmp.y, crossTmp.z));
                            edge = edge.Next;  // move on to next halfedge
                        } while (edge != Halfedge);
                        _cachedNormal = new Vector3((float) normal.x, (float) normal.y, (float) normal.z).normalized;
                        // _hasCachedNormal = true;
                    // }
                    return _cachedNormal;
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

        public ConwayPoly DetachCopy()
        {

            IEnumerable<Vector3> verts = GetVertices().Select(i => i.Position);
            IEnumerable<IEnumerable<int>> faces = new List<List<int>>
            {Enumerable.Range(0, verts.Count()).ToList()};
            IEnumerable<ConwayPoly.Roles> faceRoles = new List<ConwayPoly.Roles> {ConwayPoly.Roles.New};
            IEnumerable<ConwayPoly.Roles> vertexRoles = new List<ConwayPoly.Roles> {ConwayPoly.Roles.New};
            return new ConwayPoly(verts, faces, faceRoles, vertexRoles);
        }

    }
}