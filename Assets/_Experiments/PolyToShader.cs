using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PolyToShader : MonoBehaviour
{

    private MeshRenderer mr;
    private PolyHydra poly;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        poly = GetComponent<PolyHydra>();
    }

    void Update()
    {
        mr.sharedMaterial.SetFloat("Faces", poly._conwayPoly.Faces.Count);
    }
}
