using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;

public class PolyAnim : MonoBehaviour
{

    private MeshFilter[] polyList;
    [Range(0.0001f, 0.2f)] public float speed = 0.001f;

    private List<Mesh> meshList;
    private float currentMorphAmount;
    private SkinnedMeshRenderer sm;
    private int currentMorphTarget = 1;
    private int currentMesh;

    void Start()
    {
        polyList = gameObject.GetComponentsInChildren<MeshFilter>();
        sm = gameObject.GetComponent<SkinnedMeshRenderer>();
        meshList = new List<Mesh>();

        int currentBlendFrame = 0;
        Mesh mesh = new Mesh();
        for (var i = 0; i < polyList.Length; i++)
        {
            if (i < 1 || polyList[i].mesh.vertexCount != polyList[i - 1].mesh.vertexCount)
            {
                if (i > 1) meshList.Add(mesh);  // Add the previous finished mesh
                currentBlendFrame = 0;
                mesh = new Mesh();
                mesh.vertices = polyList[i].mesh.vertices;
                mesh.normals = polyList[i].mesh.normals;
                mesh.uv = polyList[i].mesh.uv;
                mesh.triangles = polyList[i].mesh.triangles;
                mesh.tangents = polyList[i].mesh.tangents;
                // Empty first blend
                mesh.AddBlendShapeFrame(
                    currentBlendFrame.ToString(),
                    currentBlendFrame,
                    mesh.vertices.Select((val, index) => Vector3.zero).ToArray(),
                    mesh.normals.Select((val, index) => Vector3.zero).ToArray(),
                    mesh.tangents.Select((val, index) => Vector3.zero).ToArray()
                );
            }
            else
            {
                currentBlendFrame++;
                mesh.AddBlendShapeFrame(
                    currentBlendFrame.ToString(),
                    currentBlendFrame,
                    polyList[i].mesh.vertices.Select((val, index) => val - polyList[i-1].mesh.vertices[index]).ToArray(),
                    polyList[i].mesh.normals.Select((val, index) => val - polyList[i-1].mesh.normals[index]).ToArray(),
                    polyList[i].mesh.tangents
                        .Select((val, index) => (Vector3) val - (Vector3) polyList[i-1].mesh.tangents[index]).ToArray()
                );
            }
        }
        meshList.Add(mesh);  // Add the final finished mesh
        sm.sharedMesh = meshList[0];
        sm.SetBlendShapeWeight(0, 1);
    }

    void Update()
    {
        if (currentMorphAmount >= 1)
        {
            currentMorphAmount = 0;
            currentMorphTarget += 1;
            if (currentMorphTarget >= meshList[currentMesh].blendShapeCount)
            {
                currentMesh += 1;
                currentMorphTarget = 1;

                if (currentMesh >= meshList.Count)
                {
                    currentMesh = 0;
                }
                sm.sharedMesh = meshList[currentMesh];
            }
            sm.SetBlendShapeWeight(currentMorphTarget - 1, 1);
            sm.SetBlendShapeWeight(currentMorphTarget, 0);
        }
        else
        {
            sm.SetBlendShapeWeight(currentMorphTarget - 1, 1 - currentMorphAmount);
            sm.SetBlendShapeWeight(currentMorphTarget, currentMorphAmount);
            currentMorphAmount += speed;
        }
    }
}
