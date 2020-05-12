using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using zCode.zMesh;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;

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

    override 
    public string ToString()
    {
        return Halfedge1.Face.Name + " x " + Halfedge2.Face.Name;
    }
}