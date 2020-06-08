using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Conway;
using UnityEditor.Rendering;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    public Transform Player;
    public float InnerRange = 10f;
    public float OuterRange = 12f;
    public float AngleVary = 20f;
    public Transform poly;
    public List<Material> PolyMaterials;
    public int InitialPolyCount = 30;
    public float CreationRate = 1;
    private float scale = 5f;

    private List<Transform> Pool;
    private int PoolIndex;

    void Start()
    {
        InvokeRepeating(nameof(Spawn), 0, CreationRate);
        Pool = new List<Transform>();
        for (int i = 0; i < InitialPolyCount; i++)
        {
            DoSpawn(180, 3, InnerRange);
        }
    }

    void Update()
    {
        
    }

    private void Spawn()
    {
        DoSpawn(AngleVary, InnerRange, OuterRange);
    }

    private void DoSpawn(float angleVary, float innerRange, float outerRange)
    {
        float angle = Player.rotation.eulerAngles.y + Random.Range(-angleVary, angleVary) - 90;
        var q = Quaternion.AngleAxis(angle, Vector3.up);
        float distance = Random.Range(innerRange, outerRange);
        Vector3 newPos = Player.position + q * Vector3.right * distance;
        float height = Random.Range(1.2f, 5f);
        newPos.y = (height * scale) - 2f;

        Transform newPoly = Instantiate(poly, newPos, Quaternion.identity);
        newPoly.localScale = Vector3.one * scale;

        newPoly.GetComponent<MeshRenderer>().material = PolyMaterials[Random.Range(0, PolyMaterials.Count)];
        var polyComponent = newPoly.GetComponent<PolyHydra>();

        polyComponent.Rescale = false;
        PolyPreset preset = new PolyPreset();



        preset.ShapeType = PolyHydra.ShapeTypes.Uniform;

        preset.PolyType = (PolyTypes)Random.Range(5,39);
        preset.BypassOps = false;
        preset.PrismP = Random.Range(3,12);
        preset.PrismQ = 2;
        preset.Ops = new PolyPreset.Op[3];

        preset.Ops[0] = new PolyPreset.Op
        {
            OpType = PolyHydra.Ops.SitLevel,
            FaceSelections = FaceSelections.All,
            Amount = 0f,
            Randomize = false,
            Disabled = false
        };
        preset.Ops[1] = new PolyPreset.Op
        {
            OpType = PolyHydra.Ops.Stretch,
            FaceSelections = FaceSelections.ExceptFirst,
            Amount = height,
            Randomize = false,
            Disabled = false
        };
        switch (Mathf.FloorToInt(Random.Range(0, 3)))
        {
            case 0:
                preset.Ops[2] = new PolyPreset.Op
                {
                    OpType = PolyHydra.Ops.Kis,
                    FaceSelections = FaceSelections.FacingUp,
                    Amount = Random.Range(0f, .5f),
                    Randomize = false,
                    Disabled = false
                };
                break;
            case 1:
                preset.Ops[2] = new PolyPreset.Op
                {
                    OpType = PolyHydra.Ops.Extrude,
                    FaceSelections = FaceSelections.FacingUp,
                    Amount = Random.Range(0f, .5f),
                    Randomize = false,
                    Disabled = false
                };
                break;
            case 2:
                break;
        }

        preset.ApplyToPoly(polyComponent);
        //Pool.Add(newPoly);
    }



}
