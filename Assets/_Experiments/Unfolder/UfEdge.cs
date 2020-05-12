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

public class UfEdge
{

    public UfEdge (Face f1, Face f2, Halfedge he1, Halfedge he2)
    {
        Halfedge1 = he1;
        Halfedge2 = he2;
        Tabbed = false;
        Branched = false;
    }

    public Halfedge Halfedge1;
    public Halfedge Halfedge2;
    
    public bool Tabbed;
    public bool Branched;

    override public String ToString()
    {
        return Halfedge1.Face.Name + " x " + Halfedge2.Face.Name;
    }
    
    public int ContainsFace(Face f) {
        if (f == Halfedge1.Face) {
            return 1;
        } else if (f == Halfedge2.Face) {
            return 2;
        } else {
            return 0;
        }
    }
}