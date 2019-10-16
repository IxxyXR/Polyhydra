using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PolyMorphSimpler : MonoBehaviour
{
    [Serializable]
    public struct PolyMorphItem
    {
        public MeshFilter polyMf;
        public float frequency;
        public float blendMin;
        public float blendMax;
    }

    private MeshFilter[] polyList;
    private bool initialized;

    public List<PolyMorphItem> PolyMorphItems;

    private SkinnedMeshRenderer polymorphSkinnedMeshRenderer;
    private MeshFilter polyMeshFilter;
    private Material polyMaterial;
    public PolyHydra poly;

    [Range(0f, 100f)]
    public float blend;
    public int blendIndex;

    void Start()
    {
        polymorphSkinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        polyMeshFilter = poly.gameObject.GetComponent<MeshFilter>();
        polyMaterial = poly.gameObject.GetComponent<MeshRenderer>().material;
        Invoke(nameof(Initialise), 0.25f);
    }

    public void Initialise()
    {
        var baseMesh = new Mesh();
        baseMesh.vertices = polyMeshFilter.mesh.vertices;
        baseMesh.normals = polyMeshFilter.mesh.normals;
        baseMesh.uv = polyMeshFilter.mesh.uv;
        baseMesh.triangles = polyMeshFilter.mesh.triangles;
        baseMesh.tangents = polyMeshFilter.mesh.tangents;

        for (var i = 0; i < PolyMorphItems.Count; i++)
        {
            var modifiedMesh = PolyMorphItems[i].polyMf.mesh;
            baseMesh.AddBlendShapeFrame(
                i.ToString(),
                100f,
                modifiedMesh.vertices.Select((val, index) => val - baseMesh.vertices[index]).ToArray(),
                modifiedMesh.normals.Select((val, index) => val - baseMesh.normals[index]).ToArray(),
                modifiedMesh.tangents.Select((val, index) => (Vector3) (val - baseMesh.tangents[index])).ToArray()
            );
            PolyMorphItems[i].polyMf.gameObject.SetActive(false);
        }

        polymorphSkinnedMeshRenderer.sharedMesh = baseMesh;
        polymorphSkinnedMeshRenderer.material = polyMaterial;
        initialized = true;
    }

//    private void OnValidate()
//    {
//        if (polymorphSkinnedMeshRenderer == null) return;
//        polymorphSkinnedMeshRenderer.SetBlendShapeWeight(blendIndex, blend);
//    }

    private void Update()
    {
        if (!initialized) return;
        for (var i = 0; i < PolyMorphItems.Count; i++)
        {
            var item = PolyMorphItems[i];
            var x = Time.time * item.frequency;
            //var val = Mathf.SmoothStep(-0.5f, 0.5f, Mathf.Sin(x)) * 2 - 1;
            //var val = Mathf.Pow(Mathf.Abs(Mathf.Cos(x)), 1f / 2f) * Mathf.Sign(Mathf.Cos(x));
            //var val = 1 - Mathf.Pow(25, -1 * Mathf.Sin(Time.time * item.frequency)) / 25f;
            //var val = (Mathf.Sin(Time.time * item.frequency) + 1f) / 2f;
            var val = Mathf.PerlinNoise(x, i * 10f);
            Debug.Log(val);
            polymorphSkinnedMeshRenderer.SetBlendShapeWeight(i, Mathf.Lerp(item.blendMin, item.blendMax, val));
        }
    }
}
