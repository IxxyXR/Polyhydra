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
        return this.Parent;
    }

    public void SetParent(UfFace p)
    {
        this.Parent = p;
    }

    public List<UfFace> GetChildren()
    {
        return this.Children;
    }

    public List<UfFace> AddChild(Face c)
    {
        this.Children.Add(new UfFace(c, this));
        return this.Children;
    }

    public bool IsRoot()
    {
        return this.Root;
    }

    public void SetRoot(bool b)
    {
        this.Root = b;
    }

    public Face GetID()
    {
        return this.ID;
    }

    public void SetID(Face ID)
    {
        this.ID = ID;
    }

}