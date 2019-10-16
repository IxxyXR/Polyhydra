﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PolyMorphSimple : MonoBehaviour
{
    [Serializable]
    public struct PolyMorphItem
    {
        public int opIndex;
        public float opAmount;
        public float blendAmount;
        public float blendOffset;
        public float blendFrequency;
    }

    private MeshFilter[] polyList;
    private bool initialized;

    public List<PolyMorphItem> PolyMorphItems;

    private SkinnedMeshRenderer polymorphSkinnedMeshRenderer;
    private MeshFilter polyMeshFilter;
    private Material polyMaterial;
    public PolyHydra poly;

    [Range(0,100)]
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
            var item = PolyMorphItems[i];
            var op = poly.ConwayOperators[item.opIndex];
            float originalAmount = op.amount;
            op.amount = item.opAmount;
            poly.ConwayOperators[item.opIndex] = op;
            poly.Rebuild(true);
            var modifiedMesh = poly.GetComponent<MeshFilter>().mesh;
            baseMesh.AddBlendShapeFrame(
                i.ToString(),
                100f,
                modifiedMesh.vertices.Select((val, index) => val - baseMesh.vertices[index]).ToArray(),
                modifiedMesh.normals.Select((val, index) => val - baseMesh.normals[index]).ToArray(),
                modifiedMesh.tangents.Select((val, index) => (Vector3) (val - baseMesh.tangents[index])).ToArray()
            );
            op.amount = originalAmount;
            poly.ConwayOperators[item.opIndex] = op;
            poly.Rebuild(true);

        }

        baseMesh.AddBlendShapeFrame(
            PolyMorphItems.Count.ToString(),
            PolyMorphItems.Count,
            baseMesh.vertices.Select((val, index) => Vector3.one).ToArray(),
            baseMesh.normals.Select((val, index) => Vector3.zero).ToArray(),
            baseMesh.tangents.Select((val, index) => Vector3.zero).ToArray()
        );

        polymorphSkinnedMeshRenderer.sharedMesh = baseMesh;
        polymorphSkinnedMeshRenderer.material = polyMaterial;
        poly.enabled = false;
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
            var x = Time.time * item.blendFrequency;
            //var val = (Mathf.Sin(x) + 1) / 2f;
            //var val = Mathf.SmoothStep(-0.5f, 0.5f, Mathf.Sin(x)) * 2 - 1;
            var val = Mathf.Pow(Mathf.Abs(Mathf.Cos(x)), 1f / 2f) * Mathf.Sign(Mathf.Cos(x));
            //var val = 1 - Mathf.Pow(25, -1 * Mathf.Sin(Time.time * item.frequency)) / 25f;
            //var val = (Mathf.Sin(Time.time * item.frequency) + 1f) / 2f;
            //var val = Mathf.PerlinNoise(Time.time * item.frequency, i * 10f);
            polymorphSkinnedMeshRenderer.SetBlendShapeWeight(i, (val * item.blendAmount + item.blendOffset) * 100f);
        }
    }
}
