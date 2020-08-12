using System;
using System.Collections.Generic;
using Conway;
using UnityEngine;

public class PolyDataItem 
{
    public Ops OpName;
    public int Faces;
    public int Vertices;
}

public class ConwayOpInfo
{

    public ConwayOpInfo()
    {
        var matrices = new Dictionary<string, Matrix4x4>();
//            { };
//            {"Indentity", new Matrix4x4();
//			matrix.SetRow(0, new Vector4(1f, 0f, 0f, position.x));
//			matrix.SetRow(1, new Vector4(0f, 1f, 0f, position.y));
//			matrix.SetRow(2, new Vector4(0f, 0f, 1f, position.z));
//			matrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
//        };

    }
}

public class PolyAnalyse : MonoBehaviour
{
    private Ops currentOpType;
    private PolyTypes currentPolyType;
    private PolyHydra poly;
    [Multiline]
    public string output;

    public List<PolyDataItem> polyData;
    
    void Start()
    {
        poly = gameObject.GetComponent<PolyHydra>();
        poly.DisableInteractiveFlags();
        InvokeRepeating(nameof(NewPoly), 0, 1);
        //InvokeRepeating(nameof(NewOp), 0, 1);
        polyData = new List<PolyDataItem>();
        var output = "";
    }

    public void NewPoly()
    {
        poly.UniformPolyType = currentPolyType;
        poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
        poly.Rebuild();
        Debug.Log($"{currentPolyType}: {poly.WythoffPoly.faces.Count} faces");
        currentPolyType++;
        if ((int) currentPolyType >= Enum.GetValues(typeof(PolyTypes)).Length - 1)
        {
            CancelInvoke();
        }
    }
    
    public void NewOp()
    {
        //poly.PolyType = currentPolyType;
        var defaults = PolyHydraEnums.OpConfigs[currentOpType];
        poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
        poly.ConwayOperators.Add(new PolyHydra.ConwayOperator()
        {
            opType = currentOpType,
            amount = defaults.amountDefault
        });
        poly.Rebuild();
        var item = new PolyDataItem();
        item.OpName = currentOpType;
        item.Faces = poly._conwayPoly.Faces.Count;
        item.Vertices = poly._conwayPoly.Vertices.Count;
        polyData.Add(item);
        output += $"{item.OpName}\t{item.Faces}\t{item.Vertices}\n";
        Debug.Log($"{item.OpName}: {item.Faces} faces, {item.Vertices} vertices");

        var done = new HashSet<string>();
        foreach (var edge in poly._conwayPoly.Halfedges)
        {
            var f1 = edge.Face;
            var f2 = edge.Pair.Face;
            if (done.Contains(f1.Name) || (done.Contains(f1.Name))) continue;
            done.Add(f1.Name);
            done.Add(f2.Name);
            Debug.Log(Vector3.Angle(f1.Normal, f2.Normal));
        }
        
        currentOpType++;
        if (currentOpType == Ops.Extrude)
        {
            CancelInvoke();
        }
    }
}
