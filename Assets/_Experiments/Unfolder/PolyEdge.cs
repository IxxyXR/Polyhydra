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

    public Face Face1 { get; set; }
    public Face Face2 { get; set; }

    public Halfedge Halfedge1 { get; set; }
    public Halfedge Halfedge2 { get; set; }

    public bool EdgeChecked { get; set; }
    public bool Tabbed { get; set; }
    public bool Branched { get; set; }

    override
    public String ToString()
    {
        return Face1.Name + " x " + Face2.Name;
    }
}