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

public class UfFace {

    Face ID;
    UfFace Parent;
    List<UfFace> Children;
    bool Root;

    public UfFace(Face ID, UfFace p)
    {
        if (p == null)
        {
            this.Parent = null;
            this.Root = true;
        }
        else
        {
            this.Parent = p;
            this.Root = false;
        }
        this.ID = ID;
        this.Children = new List<UfFace>();
    }

    public UfFace GetParent()
    {
        return Parent;
    }

    public void SetParent(UfFace p)
    {
        Parent = p;
    }

    public List<UfFace> GetChildren()
    {
        return Children;
    }

    public List<UfFace> AddChild(Face c)
    {
        Children.Add(new UfFace(c, this));
        return Children;
    }

    public bool IsRoot()
    {
        return Root;
    }

    public void SetRoot(bool b)
    {
        Root = b;
    }

    public Face GetID()
    {
        return ID;
    }

    public void SetID(Face ID)
    {
        ID = ID;
    }

}