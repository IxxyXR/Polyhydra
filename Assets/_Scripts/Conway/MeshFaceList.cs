using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Conway {
    /// <summary>
    /// A collection of mesh faces
    /// </summary>
    public class MeshFaceList : KeyedCollection<string, Face> {
        
        private ConwayPoly _mConwayPoly;

        public MeshFaceList(ConwayPoly conwayPoly) {
            _mConwayPoly = conwayPoly;
        }

        protected override string GetKeyForItem(Face face) {
            return face.Name;
        }

        /// <summary>
        /// Add a new face by its vertices. Will not allow the mesh to become non-manifold (e.g. by duplicating an existing halfedge).
        /// </summary>
        /// <param name="vertices">the vertices which define the face, given in anticlockwise order</param>
        /// <returns>true on success, false on failure</returns>
        public Boolean Add(IEnumerable<Vertex> vertices) {
            Vertex[] array = vertices.ToArray();

            if (array.Length < 3) {
                Debug.LogError("Too few vertices");
                return false;
            }

            int n = array.Length;
            Halfedge[] new_edges = new Halfedge[n]; // temporary container for new halfedges

            // create new halfedges (it is only possible for each to reference their vertex at this point)
            for (int i = 0; i < n; i++) {
                new_edges[i] = new Halfedge(array[i], null, null, null);
            }

            Face new_face = new Face(new_edges[0]); // create new face

            // link halfedges to face, next and prev
            // stop if a similiar halfedge is found in the mesh (avoid duplicates)
            for (int i = 0; i < n; i++) {
                new_edges[i].Face = new_face;
                new_edges[i].Next = new_edges[(i + 1) % n];
                new_edges[i].Prev = new_edges[(i + n - 1) % n];
                if (_mConwayPoly.Halfedges.Contains(new_edges[i].Name)) {
                    Debug.LogError("Duplicate Halfedge");
                    return false;
                }
            }

            // add halfedges to mesh
            for (int j = 0; j < n; j++) {
                array[j].Halfedge = array[j].Halfedge ?? new_edges[j];
                _mConwayPoly.Halfedges.Add(new_edges[j]);
            }

            // add face to mesh
            Add(new_face);

            return true;
        }

        /// <summary>
        /// Remove a face from the mesh, also removing its halfedges. (Replaces IList.Remove(T item).)
        /// </summary>
        /// <param name="item">a reference to the face which is to be removed</param>
        new public void Remove(Face item) {
            List<Halfedge> edges = new List<Halfedge>();
            Halfedge edge = item.Halfedge;
            do {
                edges.Add(edge);
                _mConwayPoly.Halfedges.Remove(edge);
                edge = edge.Next;
            } while (edge != item.Halfedge);

            foreach (Halfedge e in edges) {
                if (e.Pair != null)
                    e.Pair.Pair = null;
                // if halfedge's vertex references halfedge, point it to another
                if (e.Vertex.Halfedge == e) {
                    if (e.Pair != null)
                        e.Vertex.Halfedge = e.Pair.Prev;
                    else if (e.Next.Pair != null)
                        e.Vertex.Halfedge = e.Next.Pair;
                    else {
                        // if all else fails, try searching through all of the halfedges for one which points to this vertex
                        try {
                            e.Vertex.Halfedge = _mConwayPoly.Halfedges.First(i => i.Vertex == e.Vertex);
                        }
                        catch (InvalidOperationException) {
                            // nothing found, remove the vertex
                            _mConwayPoly.Vertices.Remove(e.Vertex);
                        }
                    }
                }
            }

            base.Remove(item);
        }

        /// <summary>
        /// Reduce an n-gon mesh face to triangles.
        /// </summary>
        /// <param name="index">index of the mesh face to be triangulated</param>
        /// <param name="quads">if true, quad-faces will not be touched</param>
        /// <returns>the number of new tri-faces created</returns>
        ///
        public int Triangulate(int index, bool quads) {
            
            // fan method
            Face face = this[index];
            Vertex start = face.Halfedge.Vertex;
            List<Vertex> end = face.GetVertices();
            if (end.Count < 4 && !quads || end.Count <= 4 && quads) {
                return 0;
            }

            int count = 0;
            for (int i = 2; i < end.Count - 1; i++) {
                Face f_new;
                Halfedge he_new, he_new_pair;
                face.Split(start, end[i], out f_new, out he_new, out he_new_pair);
                _mConwayPoly.Faces.Add(f_new);
                _mConwayPoly.Halfedges.Add(he_new);
                _mConwayPoly.Halfedges.Add(he_new_pair);
                count++;
            }

            return count;
        }
    }
}