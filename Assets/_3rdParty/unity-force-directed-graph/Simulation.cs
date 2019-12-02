using System;
using System.Collections.Generic;
using Forces;
using UnityEngine;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{
    public List<Node> Nodes = new List<Node>();
    public List<Edge> Edges = new List<Edge>();
    public List<Force> Forces = new List<Force>();

    private static float alpha = 1f;
    private const float AlphaMin = 0.001f;
    private static readonly float AlphaDecay = 1 - (float) Math.Pow(AlphaMin, 1f / 300f);
    private const float AlphaTarget = 0f;
    private const float VelocityDecay = 0.6f;

    public Simulation()
    {
        Forces.Add(new ExpansionForce {Nodes = Nodes, Edges = Edges});
        Forces.Add(new LinkForce {Nodes = Nodes, Edges = Edges});
    }

    // Start is called before the first frame update
    private void Start()
    {
        float RandomDirection() => Random.Range(-1f, 1f);
        int RandomIndex() => Random.Range(0, 20);

        for (var i = 0; i < 20; i++)
        {
            Nodes.Add(new Node
            {
                Position = new Vector3(RandomDirection(), RandomDirection(), RandomDirection()),
                Velocity = Vector3.zero
            });
        }

        for (var i = 0; i < 20; i++)
        {
            Edges.Add(new Edge {Source = Nodes[RandomIndex()], Target = Nodes[RandomIndex()]});
        }
    }

    private void OnDrawGizmos()
    {
        Nodes.ForEach(node => Gizmos.DrawSphere(node.Position, 0.1f));
        Edges.ForEach(edge => Gizmos.DrawLine(edge.Source.Position, edge.Target.Position));
    }

    private void Update()
    {
        if (alpha < AlphaMin) return;

        alpha += (AlphaTarget - alpha) * AlphaDecay;

        Forces.ForEach(force => force.ApplyForce(alpha));
        Nodes.ForEach(node => node.Position += node.Velocity *= VelocityDecay);
    }
}