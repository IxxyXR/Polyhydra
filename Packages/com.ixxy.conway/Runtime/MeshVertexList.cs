using System.Collections.Generic;
using System.Linq;

namespace Conway {
    /// <summary>
    /// 
    /// </summary>
    public class MeshVertexList : List<Vertex> {
        
        private ConwayPoly _mConwayPoly;

        /// <summary>
        /// Creates a vertex list that is aware of its parent mesh
        /// </summary>
        /// <param name="conwayPoly"></param>
        public MeshVertexList(ConwayPoly conwayPoly) : base() {
            _mConwayPoly = conwayPoly;
        }

        /// <summary>
        /// Convenience constructor, for use outside of the mesh class
        /// </summary>
        public MeshVertexList() : base() {
            _mConwayPoly = null;
        }

        /// <summary>
        /// Removes all vertices that are currently not used by the Halfedge list.
        /// </summary>
        /// <returns>The number of unused vertices that were removed.</returns>
        public int CullUnused() {
            var orig = new List<Vertex>(this);
            Clear();
            // re-add vertices which reference a halfedge
            AddRange(orig.Where(v => v.Halfedge != null));
            return orig.Count - Count;
        }
    }
}