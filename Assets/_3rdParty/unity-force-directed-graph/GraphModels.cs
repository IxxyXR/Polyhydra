using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 Position;
    public Vector3 Velocity;
}

public class Edge
{
    public Node Source;
    public Node Target;
}

public abstract class Force
{
    public List<Node> Nodes;
    public List<Edge> Edges;
    public abstract void ApplyForce(float alpha);
}
