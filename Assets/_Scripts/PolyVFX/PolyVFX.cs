using System;
using System.Linq;
using Conway;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using static Conway.ConwayPoly;
using Random = UnityEngine.Random;


public class PolyVFX : MonoBehaviour
{

    private VisualEffect _vfx;    
    private Texture2D texture;

    public bool Randomize;
    public float RandomChangeFrequency = 5f;

    [InspectorButton("UpdateWythoffVFX", "Wythoff")]
    public string Bar;
    [InspectorButton("UpdateConwayVFX", "Conway")]
    public string Foo;
    
    public bool wythoffOnly;
    public PolyHydra polyhydra;
    
    void Start()
    {
        UpdatePolyVFX();
    }
    
    public void UpdatePolyVFX()
    {
        if (wythoffOnly)
        {
            UpdateWythoffVFX();
        }
        else
        {
            UpdateConwayVFX();
        }
    }    
    
    public void UpdateWythoffVFX()
    {
        _vfx = gameObject.GetComponent<VisualEffect>();
        polyhydra.MakePolyhedron();
        
        var edges = polyhydra.WythoffPoly.Edges;
        texture = new Texture2D(edges.Length, 2, TextureFormat.RGBAFloat, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        var pixelData = new Color[edges.Length * 2];

        int numEdges = polyhydra.WythoffPoly.EdgeCount;
        for (var i = 0; i < numEdges; i++)
        {
            var startv = polyhydra.WythoffPoly.Vertices[edges[0, i]];
            var start = new Vector3((float)startv.x, (float)startv.y, (float)startv.z);
            var edgeColor = 1;
            pixelData[i] = new Color(start.x, start.y, start.z, edgeColor);
        }
        
        for (var i = 0; i < numEdges; i++)
        {
            var endv = polyhydra.WythoffPoly.Vertices[edges[1, i]];
            var end = new Vector3((float)endv.x, (float)endv.y, (float)endv.z);
            var edgeColor = 1;
            pixelData[i + edges.Length] = new Color(end.x, end.y, end.z, edgeColor);
        }
        
        texture.SetPixels(pixelData);
        texture.Apply();
        _vfx.SetTexture("Positions", texture);
        _vfx.SetInt("Count", texture.width);
    }

    private static float CalcEdgeColor(Halfedge x)
    {
        float edgeColor = 1.0f;
        try
        {
            edgeColor = (x.Face.Sides + x.Pair.Face.Sides) / 2f;
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Failed to calculate edge color");
        }
        return edgeColor;
    }

    public void UpdateConwayVFX()
    {
        _vfx = gameObject.GetComponent<VisualEffect>();
        polyhydra.DisableInteractiveFlags();
        polyhydra.MakePolyhedron();
        
        //var conwayPoly = new ConwayPoly(polyhydra.WythoffPoly).Stake(.3f, FaceSelections.All).Lace(.3f, FaceSelections.All);
        //edges = conwayPoly.Halfedges.GetUnique().ToArray();
        
        if (polyhydra._conwayPoly == null || polyhydra.ConwayOperators.Count < 1)
        {
            polyhydra.ConwayOperators.Add(new PolyHydra.ConwayOperator(){opType = PolyHydra.Ops.Identity});
            polyhydra.MakePolyhedron();
        }
        var edges = polyhydra._conwayPoly.Halfedges.GetUnique().ToArray();
        
        texture = new Texture2D(edges.Length, 2, TextureFormat.RGBAFloat, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        var pixelData = new Color[edges.Length * 2];

        int numEdges = edges.Length;
        
        for (var i = 0; i < numEdges; i++)
        {
            var start = edges[i].Vertex.Position;
            var edgeColor = CalcEdgeColor(edges[i]);            
            pixelData[i] = new Color(start.x, start.y, start.z, edgeColor);
        }
        
        for (var i = 0; i < numEdges; i++)
        {
            var end = edges[i].Pair.Vertex.Position;
            var edgeColor = CalcEdgeColor(edges[i]);
            pixelData[i + edges.Length] = new Color(end.x, end.y, end.z, edgeColor);
        }
        
        texture.SetPixels(pixelData);
        texture.Apply();
        _vfx.SetTexture("Positions", texture);
        _vfx.SetInt("Count", texture.width);
        //_vfx.SetInt("MinFaces", Max(var => conwayPoly.Faces));
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (Randomize && !IsInvoking(nameof(RandomizePolyhedra)))
        {
            InvokeRepeating(nameof(RandomizePolyhedra), 0, RandomChangeFrequency);
        }
    }

    private float BiasedRandom(float min, float max, int bias)
    {
        float range = max - min;
        float val = Random.Range(min, max) * Random.Range(min, max);
        if (bias < 0)
        {
            val -= range / 2 % range;
            val = Mathf.Abs(val);

        }
        else if (bias > 0)
        {
            val += range / 2 % range;
            val = Mathf.Abs(val);            
        }

        return val;
    }

    public void RandomizePolyhedra()
    {
        _vfx.SetFloat("Thickness", BiasedRandom(0.0015f, 0.02f, -1));
        _vfx.SetFloat("Chaos", BiasedRandom(0f, 0.0025f, -1));
        _vfx.SetFloat("Lifetime", BiasedRandom(0.001f, 5f, -1));
        //var validPolys = Enum.GetValues(typeof(PolyTypes)).Cast<PolyTypes>().Except(polyhydra.OrientablePolyTypes);
        polyhydra.PolyType = (PolyTypes)Random.Range(1, Enum.GetValues(typeof(PolyTypes)).Length);
        Debug.Log($"Random base type: {polyhydra.PolyType} {polyhydra.WythoffPoly.PolyName} {polyhydra.WythoffPoly.PolyTypeIndex}");
        polyhydra.ConwayOperators.Clear();
        wythoffOnly = true;
        if (!polyhydra.NonOrientablePolyTypes.Contains((int) polyhydra.PolyType)) // Don't add Conway ops to non-orientable polys
        {
            if (Random.value > 0.3333)
            {
                wythoffOnly = false;
                polyhydra.AddRandomOp();
                Debug.Log($"Random op: {polyhydra.ConwayOperators.Last().opType}");
                if (Random.value > 0.3333)
                {
                    polyhydra.AddRandomOp();
                    Debug.Log($"Random op 2: {polyhydra.ConwayOperators.Last().opType}");
                    if (Random.value > 0.3333)
                    {
                        polyhydra.AddRandomOp();
                        Debug.Log($"Random op 3: {polyhydra.ConwayOperators.Last().opType}");
                    }
                }
            }
        }
        UpdatePolyVFX();
    }
}
