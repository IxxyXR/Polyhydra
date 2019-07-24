using System.Collections;
using System.Collections.Generic;
using Conway;
using UnityEngine;
using Wythoff;

public class VisualUi : MonoBehaviour
{
    public PolyHydra MainPoly;
    public int FirstOp = 1;
    public int LastOp = 12;
    public int FirstPoly = 1;
    public int LastPoly = 8;
    public float radius = 2.2f;
    
    void Start()
    {
        ShowBasePolys();
    }

    void ShowOps()
    {
        float numOps = LastOp - FirstOp + 1;
        for (int i = FirstOp; i <= LastOp; i++)
        {
            float x = Mathf.Sin((i / numOps) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos((i / numOps) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(MainPoly.gameObject, new Vector3(x, y, 0), Quaternion.identity);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            var opType = (PolyHydra.Ops) i;

            var newOp = new PolyHydra.ConwayOperator()
            {
                opType = opType,
                faceSelections = ConwayPoly.FaceSelections.All,
                randomize = false,
                amount = copyPoly.opconfigs[opType].amountDefault,
                disabled = false
            };
            copyPoly.ConwayOperators.Add(newOp);
            copyPoly.MakePolyhedron();
        }
    }

    void ShowBasePolys()
    {
        float numOps = LastPoly - FirstPoly + 1;
        for (int i = FirstPoly; i <= LastPoly; i++)
        {
            float x = Mathf.Sin((i / numOps) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos((i / numOps) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(MainPoly.gameObject, new Vector3(x, y, 0), Quaternion.identity);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            copyPoly.PolyType = (PolyTypes) i;
            var uniform = Uniform.Uniforms[i];
            if (uniform.Wythoff == "-") continue;
            var wythoff = new WythoffPoly(uniform.Wythoff);
            // Which types to create? Example:
            if (wythoff.SymmetryType != 3) continue;
            copyPoly.MakePolyhedron();
        }
    }

    void Update()
    {
        
    }
}
