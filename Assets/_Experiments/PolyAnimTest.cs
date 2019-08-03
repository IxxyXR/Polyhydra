using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;

public class PolyAnimTest : MonoBehaviour
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

        Debug.Log(polyList.Length);

        sm = gameObject.GetComponent<SkinnedMeshRenderer>();

        Mesh mesh;
        meshList = new List<Mesh>();

        mesh = polyList[0].mesh;
        mesh.AddBlendShapeFrame(
            "0",
            0,
            polyList[0].mesh.vertices.Select((val, index) => Vector3.zero).ToArray(),
            polyList[0].mesh.normals.Select((val, index) => Vector3.zero).ToArray(),
            polyList[0].mesh.tangents.Select((val, index) => Vector3.zero).ToArray()
        );
        mesh.AddBlendShapeFrame(
            "1",
            1,
            polyList[1].mesh.vertices.Select((val, index) => val - polyList[0].mesh.vertices[index]).ToArray(),
            polyList[1].mesh.normals.Select((val, index) => val - polyList[0].mesh.normals[index]).ToArray(),
            polyList[1].mesh.tangents.Select((val, index) => (Vector3) val - (Vector3) polyList[0].mesh.tangents[index]).ToArray()
        );
        meshList.Add(mesh);

        mesh = polyList[2].mesh;
        mesh.AddBlendShapeFrame(
            "0",
            0,
            polyList[2].mesh.vertices.Select((val, index) => Vector3.zero).ToArray(),
            polyList[2].mesh.normals.Select((val, index) => Vector3.zero).ToArray(),
            polyList[2].mesh.tangents.Select((val, index) => Vector3.zero).ToArray()
        );
        mesh.AddBlendShapeFrame(
            "1",
            1,
            polyList[3].mesh.vertices.Select((val, index) => val - polyList[2].mesh.vertices[index]).ToArray(),
            polyList[3].mesh.normals.Select((val, index) => val - polyList[2].mesh.normals[index]).ToArray(),
            polyList[3].mesh.tangents.Select((val, index) => (Vector3) val - (Vector3) polyList[2].mesh.tangents[index]).ToArray()
        );
        meshList.Add(mesh);



        sm.sharedMesh = meshList[0];
        sm.SetBlendShapeWeight(0, 1);

    }

    void Update()
    {
        //Debug.Log($"currentMesh: {currentMesh} currentMorphTarget: {currentMorphTarget} currentMorphAmount: {currentMorphAmount}");

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
