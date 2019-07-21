using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;

public class PolyAnimTest : MonoBehaviour
{

    public MeshFilter poly1;
    public MeshFilter poly2;
    public float amount1;
    public float amount2;
    
    private SkinnedMeshRenderer sm;


    void Start()
    {
        Debug.Log(poly1.sharedMesh.vertexCount);
        Debug.Log(poly2.sharedMesh.vertexCount);
        sm = gameObject.GetComponent<SkinnedMeshRenderer>();
        sm.sharedMesh = poly1.mesh;
        
//        var v = new Vector3[vertexCount];
//        var n = new Vector3[vertexCount];
//        var t = new Vector3[vertexCount];
//        poly1.sharedMesh.GetBlendShapeFrameVertices(0, 0, v, n, t);
//        sm.sharedMesh.AddBlendShapeFrame("b1",1, v, n, t);
//        poly2.sharedMesh.GetBlendShapeFrameVertices(0, 0, v, n, t);
//        sm.sharedMesh.AddBlendShapeFrame("b2",1, v, n, t);
        
        sm.sharedMesh.AddBlendShapeFrame(
            "start",
            0,
            poly1.mesh.vertices.Select((val, index) => Vector3.zero).ToArray(),
            poly1.mesh.normals.Select((val, index) => Vector3.zero).ToArray(),
            poly1.mesh.tangents.Select((val, index) => Vector3.zero).ToArray()
        );

        sm.sharedMesh.AddBlendShapeFrame(
            "end",
            1,
            poly2.mesh.vertices.Select((val, index) => val - sm.sharedMesh.vertices[index]).ToArray(),
            poly2.mesh.normals.Select((val, index) => val - sm.sharedMesh.normals[index]).ToArray(),
            poly2.mesh.tangents.Select((val, index) => (Vector3) val - (Vector3) sm.sharedMesh.tangents[index]).ToArray()
        );
    }
    
    void Update()
    {
        sm.SetBlendShapeWeight(0, amount1);
        sm.SetBlendShapeWeight(1, amount2);
    }
}
