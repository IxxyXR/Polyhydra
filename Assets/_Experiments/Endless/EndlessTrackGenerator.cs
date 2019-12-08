using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Conway;
using UnityEditor;
using UnityEngine;

public class EndlessTrackGenerator : MonoBehaviour
{
    public float Speed = 0.01f;
    public int StepsPerTile;
    public float gap = 0.03f;
    public int InitialTiles = 10;
    public PolyHydra poly;
    public AnimationCurve ExtrudeProbability;
    public float ExtrudeScale = 0.2f;

    public List<PolyHydra.ConwayOperator> Greebles;

    private int steps;
    private Vector3 pos;

    void Start()
    {
        steps = 0;
        pos = new Vector3();
        poly.enableCaching = false;
        for (int i=0; i<InitialTiles * StepsPerTile; i++)
        {
            IncrementTiles();
        }

    }

    void Update()
    {
        IncrementTiles();
    }

    public void IncrementTiles()
    {
        if (steps % StepsPerTile == 0 && steps > 0)
        {
            var piece = Instantiate(poly.transform, transform);
            piece.transform.position = new Vector3(0, 0, steps * gap);
            var piecePoly = piece.GetComponent<PolyHydra>();
            piecePoly.ConwayOperators.Clear();
            piecePoly.ConwayOperators.Add(Greebles[Random.Range(0, Greebles.Count)]);
            piecePoly.ConwayOperators.Add(Greebles[Random.Range(0, Greebles.Count)]);
            piecePoly.ConwayOperators.Add(new PolyHydra.ConwayOperator{
                opType=PolyHydra.Ops.Extrude,
                faceSelections=ConwayPoly.FaceSelections.All,
                amount=ExtrudeProbability.Evaluate(Random.value) * ExtrudeScale,
                randomize=true,
                disabled=false}
            );
            piecePoly.Rebuild();
            piece.GetComponent<PolyHydra>().Rebuild();
            piece.GetComponent<MeshRenderer>().enabled = true;
        }
        pos.z -= Speed;
        transform.position = pos;
        steps++;
    }
}
