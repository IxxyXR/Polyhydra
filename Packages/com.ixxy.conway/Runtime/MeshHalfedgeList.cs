using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Conway {
    /// <summary>
    /// A collection of mesh halfedges
    /// </summary>
    public class MeshHalfedgeList : KeyedCollection<(Guid, Guid)?, Halfedge> {

        private ConwayPoly _mConwayPoly;

        /// <summary>
        /// Creates a halfedge list that is aware of its parent mesh
        /// </summary>
        /// <param name="conwayPoly"></param>
        public MeshHalfedgeList(ConwayPoly conwayPoly)
            : base() {
            _mConwayPoly = conwayPoly;
        }

        /// <summary>
        /// Convenience constructor, for use outside of the mesh class
        /// </summary>
        public MeshHalfedgeList() {
            _mConwayPoly = null;
        }

        protected override (Guid, Guid)? GetKeyForItem(Halfedge edge)
        {
            return edge.Name;
        }

        // protected override  GetKeyForItem(Halfedge edge)
        // {
        //     return edge.Name.To;
        // }

        #region add methods

        public void Add(Vertex vertex) {
            this.Add(new Halfedge(vertex));
        }

        public void Add(Vertex vertex, Halfedge next, Halfedge prev, Face face) {
            this.Add(new Halfedge(vertex, next, prev, face));
        }

        public void Add(Vertex vertex, Halfedge next, Halfedge prev, Face face, Halfedge pair) {
            this.Add(new Halfedge(vertex, next, prev, face, pair));
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="he">halfedge to change</param>
        /// <param name="newVertex">new vertex</param>
        /// <returns>true on success, false if changing the vertex of this halfedge would cause it to duplicate an existing halfedge</returns>
        public Boolean SetVertex(Halfedge he, Vertex newVertex) {
            try {
                ChangeItemKey(he, (newVertex.Name, he.Prev.Vertex.Name));
                ChangeItemKey(he.Next, (he.Next.Vertex.Name, newVertex.Name));
            }
            catch (ArgumentException) {
                return false;
            }

            he.SetVertex(newVertex);
            // TODO: what if the old vertex was point to this halfedge? (see MeshFaceList.Remove)
            return true;
        }

        /// <summary>
        /// Searches through the mesh and pairs opposing halfedges
        /// </summary>
        public void MatchPairs()
        {
            for (var i = 0; i < this.Count; i++)
            {
                Halfedge edge = this[i];
                var rname = (edge.Prev.Vertex.Name, edge.Vertex.Name);
                edge.Pair = Contains(rname) ? this[rname] : null;
            }
        }

        /// <summary>
        /// Returns a list of unique halfedges.
        /// Halfedge pairs are represented by the halfedge with the lowest index in the mesh.
        /// </summary>
        /// <returns>A list of unique edges</returns>
        public List<Halfedge> GetUnique() {
            MeshHalfedgeList marker = new MeshHalfedgeList();
            List<Halfedge> unique = new List<Halfedge>();

            for (var i = 0; i < this.Count; i++)
            {
                Halfedge halfedge = this[i];
                // do not add halfedge to unique list if its pair has already been added
                if (marker.Contains(halfedge.Name))
                {
                    continue;
                }

                // otherwise, add it to the unique list and 'mark' its pair
                unique.Add(halfedge);

                if (halfedge.Pair != null)
                {
                    try
                    {
                        marker.Add(this[halfedge.Pair.Name]);
                    }
                    catch (KeyNotFoundException)
                    {
                    } // do nothing, halfedge is already unique to 'this' list
                }
            }

            return unique;
        }

        public void Flip() {
            int n = Count;
            var vertices = new Vertex[n];
            var edges = new Halfedge[n];
            for (int i = 0; i < n; i++) {
                vertices[i] = this[i].Prev.Vertex;
                edges[i] = this[i];
            }

            Clear();
            for (int i = 0; i < n; i++) {
                var edge = edges[i];
                var temp = edge.Next;
                edge.Next = edge.Prev;
                edge.Prev = temp;
                edge.SetVertex(vertices[i]);
            }

            for (var i = 0; i < edges.Length; i++)
            {
                Halfedge edge = edges[i];
                Add(edge);
            }
        }

    }
}