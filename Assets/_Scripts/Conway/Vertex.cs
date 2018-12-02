using System;
using System.Collections.Generic;
using Wythoff;
using UnityEngine;

namespace Conway {
    
    public class Vertex {
        
        #region constructors

        public Vertex(Vector3 point) {
            Position = point;
            Name = Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        public Vertex(Vector3 point, Halfedge edge)
            : this(point) {
            Halfedge = edge;
        }

        #endregion

        #region properties

        /// <summary>
        /// The coordinates of the vertex.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// One of the halfedges that points towards the vertex.
        /// Used as a starting point for traversal from the vertex.
        /// </summary>
        public Halfedge Halfedge { get; set; }

        public string Name { get; private set; }

        public Vector3 Normal {
            get {
                Vector normal = new Vector(0, 0, 0);
                foreach (Face f in GetVertexFaces()) {
                    normal = normal.sum(new Vector(f.Normal.x, f.Normal.y, f.Normal.z));
                }

                return new Vector3((float) normal.x, (float) normal.y, (float) normal.z).normalized;
            }
        }

        /// <summary>
        /// Gets a list of edges which are incident this vertex (ordered anticlockwise around the vertex).
        /// </summary>
        public List<Halfedge> Halfedges {
            get {
                List<Halfedge> edges = new List<Halfedge>();
                if (Halfedge == null) return edges;
                bool boundary = false;
                Halfedge edge = Halfedge;
                do {
                    edges.Add(edge);
                    if (edge.Pair == null) {
                        boundary = true; // boundary hit
                        break;
                    }

                    edge = edge.Pair.Prev;
                } while (edge != Halfedge);

                if (boundary) {
                    List<Halfedge> redges = new List<Halfedge>();
                    edge = Halfedge;
                    while (edge.Next.Pair != null) {
                        edge = edge.Next.Pair;
                        redges.Add(edge);
                    }

                    if (redges.Count > 1) {
                        redges.Reverse();
                    }

                    redges.AddRange(edges);
                    return redges;
                }

                return edges;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Finds the faces which share this vertex
        /// </summary>
        /// <returns>a list of incident faces, ordered counter-clockwise around the vertex</returns>
        public List<Face> GetVertexFaces() {
            List<Face> adjacent = new List<Face>();
            if (Halfedge == null) return adjacent;
            bool boundary = false;
            Halfedge edge = Halfedge;
            do {
                adjacent.Add(edge.Face);
                if (edge.Pair == null) {
                    boundary = true; // boundary hit
                    break;
                }

                edge = edge.Pair.Prev;
            } while (edge != Halfedge);

            if (boundary) {
                List<Face> rAdjacent = new List<Face>();
                edge = Halfedge;
                while (edge.Next.Pair != null) {
                    edge = edge.Next.Pair;
                    rAdjacent.Add(edge.Face);
                }

                if (rAdjacent.Count > 1) {
                    rAdjacent.Reverse();
                }

                rAdjacent.AddRange(adjacent);
                return rAdjacent;
            }

            return adjacent;
        }

        #endregion
    }
}