using System;
using System.Collections.Generic;
using UnityEngine;

public class PolyDataItem 
{
    public PolyHydra.Ops OpName;
    public int Faces;
    public int Vertices;
}

public class PolyAnalyse : MonoBehaviour
{
    private PolyHydra.Ops currentOpType;
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
        poly.MakePolyhedron();
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
        var defaults = poly.opconfigs[currentOpType];
        poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
        poly.ConwayOperators.Add(new PolyHydra.ConwayOperator()
        {
            opType = currentOpType,
            amount = defaults.amountDefault
        });
        poly.MakePolyhedron();
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
        if (currentOpType == PolyHydra.Ops.Extrude)
        {
            CancelInvoke();
        }
    }
}
