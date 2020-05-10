using System.Linq;
using Conway;
using UnityEditor;
using UnityEngine;
using zCode.zCore;
using zCode.zDynamics;
using zCode.zMesh;

[ExecuteInEditMode]
public class heMeshToUnityMesh : MonoBehaviour {
    
    private HeMesh3d heMesh;
    public PolyHydra poly;
    public bool dummy;

    void Start() {
        //CreateFromPoly(poly._conwayPoly);
    }
    
    private void OnValidate() {
        CreateFromPoly(poly._conwayPoly);
        //Planarize();
        //heMesh.AppendDual(heMesh);
        //var foo = HeSelection.GetEdgeLoop<HeMesh3d.Vertex, HeMesh3d.Halfedge, HeMesh3d.Face>(heMesh.Halfedges[0]);
        //var foo = HeSelection.GetQuadStrips<HeMesh3d.Vertex, HeMesh3d.Halfedge, HeMesh3d.Face>(heMesh, false);
        //heMesh.DivideEdge(heMesh.Edges[0], 3);
        //heMesh.SplitFace(heMesh.Edges[0], heMesh.Edges[2]);
        //heMesh.MergeVertices(heMesh.Vertices[0], heMesh.Vertices[1]);
    }

    public void Planarize() {

        // create particles
        var bodies = heMesh.Vertices.Select(v => new Body(v.Position)).ToArray();

        // create constraints
        var constraints = heMesh.Faces.Where(f => !f.IsDegree3)
            .Select(f => new Coplanar(f.Vertices.Select(v => v.Index)))
            .ToArray();

        // create solver
        var solver = new ConstraintSolver();
        solver.Settings.Damping = 0.1;

        // step the solver until converged
        while (!solver.IsConverged)
        {
            solver.Step(bodies, constraints, true);
        }

        // update mesh vertices
        heMesh.Vertices.Action(v => v.Position = bodies[v].Position); // mesh elements (vertices, halfedges, faces) are implicitly converted to their index

        // compute vertex normals
        heMesh.Vertices.Action(v => v.Normal = v.GetNormal(), true);
    }
    
    private void CreateFromPoly(ConwayPoly conway)
    {
        
        var points = conway.ListVerticesByPoints();
        var faceIndices = conway.ListFacesByVertexIndices();
        Debug.Log(heMesh);
        foreach (var p in points) {
            var v = heMesh.AddVertex();
            v.Position = new Vec3d(p.x, p.y, p.z);
        }
        
        foreach (var i in faceIndices) {
            heMesh.AddFace(i);
        }
        
    }

    // public void Import() {
    //     var factory = new HeMesh3dFactory();
    //     heMesh = factory.CreateFromOBJ("Assets/_Exported Meshes/ditrogonal.obj");
    // }

    public static Vector3 VertToVector3(Vec3d vec) {
        return new Vector3((float) vec.X, (float) vec.Y, (float) vec.Z);
    }

#if UNITY_EDITOR

    void OnDrawGizmos() {
        for (var i = 0; i < heMesh.Edges.Count; i++) {
            var edge = heMesh.Edges[i];
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                transform.TransformPoint(VertToVector3(edge.Start.Position)),
                transform.TransformPoint(VertToVector3(edge.End.Position))
            );
            var p = edge.Start.Position + -0.5f * (edge.Start.Position - edge.End.Position);
            Handles.Label(VertToVector3(p) + new Vector3(0, .15f, 0), i.ToString());
            //Gizmos.DrawWireCube(transform.TransformPoint(edge.PointAlongEdge(0.9f)), Vector3.one * 0.02f);
        }
    }
#endif
}