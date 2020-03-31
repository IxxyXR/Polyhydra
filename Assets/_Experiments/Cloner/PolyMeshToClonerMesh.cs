using System;
using Cloner;
using UnityEngine;


public class PolyMeshToClonerMesh : MonoBehaviour
{
    private PolyHydra poly;
    private Cloner.Cloner cloner;
    private MeshFilter polymf;

    void Start()
    {
        poly = FindObjectOfType<PolyHydra>();
        polymf = poly.gameObject.GetComponent<MeshFilter>();
        cloner = FindObjectOfType<Cloner.Cloner>();
    }

    void Update()
    {
        cloner.mesh = polymf.mesh;
    }
}
