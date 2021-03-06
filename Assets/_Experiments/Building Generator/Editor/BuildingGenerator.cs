﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Conway;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BuildingGenerator : MonoBehaviour
{

    public Material material;
    public float aspect = 0.6f;
    public float height = 1f;
    public float wallThickness = 0.1f;
    public float skylightSize = 0.5f;
    public int numSkylights = 2;
    public int numSkylightRows = 1;
    public bool generateLightmapUVs = false;


    void Start()
    {
        Generate();
    }

    private void OnValidate()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        var building = Grids.Grids.MakeGrid(1, 1, aspect, 1f);
        building = building.Loft(new OpParams{valueA = wallThickness});
        building = building.Loft(new OpParams{valueA = 0, valueB = height, facesel = FaceSelections.AllNew});

        float roofScale = 1f - wallThickness;
        var roof = Grids.Grids.MakeGrid(numSkylights, numSkylightRows, (aspect/numSkylights) * roofScale, (aspect/numSkylightRows) * (1f / aspect) * roofScale);
        roof = roof.Loft(new OpParams{valueA = skylightSize});
        roof = roof.FaceRemove(new OpParams{facesel = FaceSelections.Existing});

        building.Append(roof, new Vector3(0, height, 0), Quaternion.Euler(0, 0, 0), 1f);

        var mesh = PolyMeshBuilder.BuildMeshFromConwayPoly(building, false);
        if (generateLightmapUVs)
        {
            var unwrapSettings = new UnwrapParam();
            UnwrapParam.SetDefaults(out unwrapSettings);
            unwrapSettings.packMargin = 0.01f;
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
        }
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

    }

    private ConwayPoly RibbedExtrude(ConwayPoly poly, int numRibs)
    {
        float translateForwardsPerRib = Random.Range(0.02f, 0.2f);
        float ribDepth = Random.Range(0.02f, 0.2f);
        for (int i=0; i<numRibs; i++)
        {
            poly = poly.Loft(new OpParams{valueA = ribDepth, valueB = translateForwardsPerRib * 0.25f, facesel = FaceSelections.FacingStraightForward});
            poly = poly.Loft(new OpParams{valueA = 0, valueB = translateForwardsPerRib * 0.5f, facesel = FaceSelections.FacingStraightForward});
            poly = poly.Loft(new OpParams{valueA = -ribDepth, valueB = translateForwardsPerRib * 0.25f, facesel = FaceSelections.FacingStraightForward});
            poly = poly.Loft(new OpParams{valueA = 0, valueB = translateForwardsPerRib * 0.25f, facesel = FaceSelections.FacingStraightForward});
        }

        return poly;
    }

}
