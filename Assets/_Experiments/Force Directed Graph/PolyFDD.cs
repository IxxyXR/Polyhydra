using System;
using System.Collections.Generic;
using System.Linq;
using Forces;
using UnityEngine;


public class PolyFDD : MonoBehaviour
{

    public PolyHydra poly;

    private List<Node> Nodes;
    private List<Edge> Edges;
    private List<Force> Forces;
    private static float alpha;
    private float AlphaMin;
    private float AlphaDecay;
    private float AlphaTarget;
    private float VelocityDecay;
    private bool Built = false;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        Built = false;
        Nodes = new List<Node>();
        Edges = new List<Edge>();
        Forces = new List<Force>();
        alpha = 1f;
        AlphaMin = 0.01f;
        AlphaDecay = 1 - (float) Math.Pow(AlphaMin, 1f / 300f);
        AlphaTarget = 0f;
        VelocityDecay = 0.6f;

        var nodeMap = new Dictionary<string, Node>();

        foreach (var vertex in poly._conwayPoly.Vertices)
        {
            Nodes.Add(new Node
            {
                Position = vertex.Position,
                Velocity = Vector3.zero
            });
            nodeMap[vertex.Name] = Nodes.Last();
        }

        foreach (var edge in poly._conwayPoly.Halfedges)
        {
            Edges.Add(new Edge {Source=nodeMap[edge.Vertex.Name], Target=nodeMap[edge.Next.Vertex.Name]});
        }

        Forces.Add(new ExpansionForce {Nodes = Nodes, Edges = Edges});
        Forces.Add(new LinkForce {Nodes = Nodes, Edges = Edges});
    }

    private void OnDrawGizmos()
    {
        if (Nodes != null)
        {
            Nodes.ForEach(node => Gizmos.DrawSphere(node.Position, 0.1f));
        }

        if (Edges != null)
        {
            Edges.ForEach(edge => Gizmos.DrawLine(edge.Source.Position, edge.Target.Position));
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            Debug.Log("Initing");
            Init();
        }
        if (alpha < AlphaMin)
        {
            if (!Built)
            {
                Debug.Log("Building");
                var newPoly = poly._conwayPoly.Duplicate();
                for (var i = 0; i < Nodes.Count; i++)
                {
                    var v = Nodes[i];
                    newPoly.Vertices[i].Position = v.Position;
                }
                poly.AssignFinishedMesh(PolyHydra.BuildMeshFromConwayPoly(newPoly, false));
                Built = true;
            }
            return;
        }

        alpha += (AlphaTarget - alpha) * AlphaDecay;

        Forces.ForEach(force => force.ApplyForce(alpha));
        Nodes.ForEach(node => node.Position += node.Velocity *= VelocityDecay);
    }
}