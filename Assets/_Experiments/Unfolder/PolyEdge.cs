using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Conway;
using Newtonsoft.Json;
using Wythoff;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;
using Halfedge = Conway.Halfedge;
using Random = UnityEngine.Random;

public class PolyEdge
{
    Face Face1;
    Face Face2;
    Halfedge Halfedge1;
    Halfedge Halfedge2;
    bool EdgeChecked;
    bool Tabbed;
    bool Branched;

    public PolyEdge (Face f1, Face f2, Halfedge he1, Halfedge he2)
    {
        this.Face1 = f1;
        this.Face2 = f2;
        this.Halfedge1 = he1;
        this.Halfedge2 = he2;
        this.EdgeChecked = false;
        this.Tabbed = false;
        this.Branched = false;
    }

    public Face GetFace1()
    {
        return this.Face1;
    }

    public Face GetFace2()
    {
        return this.Face2;
    }

    private void SetFace1(Face f1)
    {
        this.Face1 = f1;
    }

    private void SetFace2(Face f2)
    {
        this.Face2 = f2;
    }

    public Halfedge GetHalfedge1()
    {
        return this.Halfedge1;
    }

    public Halfedge GetHalfedge2()
    {
        return this.Halfedge2;
    }

    private void SetHalfedge1(Halfedge f1)
    {
        this.Halfedge1 = f1;
    }

    private void SetHalfedge2(Halfedge f2)
    {
        this.Halfedge2 = f2;
    }

    public bool IsEdgeChecked()
    {
        return this.EdgeChecked;
    }

    public void SetEdgeChecked(bool check)
    {
        this.EdgeChecked = check;
    }

    public bool isTabbed()
    {
        return this.Tabbed;
    }

    public void SetTabbed(bool tab)
    {
        this.Tabbed = tab;
    }

    public bool IsBranched()
    {
        return this.Branched;
    }

    public void SetBranched(bool branch)
    {
        this.Branched = branch;
    }
}