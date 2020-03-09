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

public class Node {

    Face ID;
    Node Parent;
    List<Node> Children;
    bool Root;

    public Node(Face ID, Node p)
    {
        if (p == null)
        {
            this.Parent = null;
            this.Root = true;
        } else
        {
            this.Parent = p;
            this.Root = false;
        }
        this.ID = ID;
        this.Children = new List<Node>();
    }

    public Node GetParent()
    {
        return this.Parent;
    }

    public void SetParent(Node p)
    {
        this.Parent = p;
    }

    public List<Node> GetChildren()
    {
        return this.Children;
    }

    public List<Node> AddChild(Face c)
    {
        this.Children.Add(new Node(c, this));
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