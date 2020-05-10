using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[ExecuteInEditMode]
public class CloneAndVary : MonoBehaviour
{
    public int rows = 10;
    public int columns = 10;
    public float spacing = 3f;
    public float xMagnitude = 0.1f;
    public float yMagnitude = 0.1f;
    public Material material;

    private PolyHydra poly;

    public bool dummy;

    void Start()
    {
    }

    void FindPoly()
    {
        poly = FindObjectOfType<PolyHydra>();
    }

    void Update()
    {
    }

    private void OnValidate()
    {
        Generate();
    }

    public void Generate()
    {
        if (poly == null)
        {
            FindPoly();
        }
        
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(child.gameObject);
                }; 
            }
        }
        
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var op = poly.ConwayOperators[0];
                op.amount = x * xMagnitude;
                op.amount2 = y * yMagnitude;
                poly.ConwayOperators[0] = op;
                poly.Rebuild();
                var go = new GameObject();
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = material;
                go.transform.position = new Vector3(x * spacing, 1, y * spacing);
                go.transform.parent = transform;
                if (Application.isPlaying)
                {
                    mf.mesh = poly.GetComponent<MeshFilter>().mesh;
                }
                else
                {
                    mf.sharedMesh = poly.GetComponent<MeshFilter>().sharedMesh;
                }
            }
        }
    }
}