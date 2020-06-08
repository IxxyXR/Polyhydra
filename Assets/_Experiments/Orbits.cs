using System.Collections;
using System.Collections.Generic;
using Conway;
using UnityEngine;
using Wythoff;

public class Orbits : MonoBehaviour
{
    public PolyHydra MainPoly;
    public int FirstOp = 1;
    public int LastOp = 12;
    public int FirstPoly = 1;
    public int LastPoly = 8;
    public float radius = 2.2f;

    private GameObject _pivot;

    void Start()
    {
        CreatePivot();
    }

    void CreatePivot()
    {
        if (_pivot == null)
        {
            _pivot = new GameObject();
            _pivot.transform.parent = MainPoly.transform;
        }
    }

    void RemoveExistingOptions()
    {
        foreach (GameObject child in _pivot.transform)
        {
            Destroy(child);
        }
    }

    void ShowOps()
    {
        float numItems = LastOp - FirstOp + 1;
        for (int i = FirstOp; i <= LastOp; i++)
        {
            float x = Mathf.Sin((i / numItems) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos((i / numItems) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(MainPoly.gameObject, new Vector3(x, y, 0), Quaternion.identity);
            copy.transform.parent = _pivot.transform;
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            var opType = (PolyHydra.Ops) i;

            var newOp = new PolyHydra.ConwayOperator()
            {
                opType = opType,
                faceSelections = FaceSelections.All,
                randomize = false,
                amount = copyPoly.opconfigs[opType].amountDefault,
                disabled = false
            };
            copyPoly.ConwayOperators.Add(newOp);
            copyPoly.Rebuild();
        }
    }

    void ShowBasePolys()
    {
        float numItems = LastPoly - FirstPoly + 1;
        for (int i = FirstPoly; i <= LastPoly; i++)
        {
            float x = Mathf.Sin((i / numItems) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos((i / numItems) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(MainPoly.gameObject, new Vector3(x, y, 0), Quaternion.identity);
            copy.transform.parent = _pivot.transform;
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            copyPoly.UniformPolyType = (PolyTypes) i;
            var uniform = Uniform.Uniforms[i];
            if (uniform.Wythoff == "-") continue;
            var wythoff = new WythoffPoly(uniform.Wythoff.Replace("p", "5").Replace("q", "2"));
            // Which types to create? Example:
            //if (wythoff.SymmetryType != 3) continue;
            copyPoly.Rebuild();
        }
    }

    void Update()
    {
        _pivot.transform.Rotate(0, .1f, 0);
        switch (Input.inputString.ToLower())
        {
            case "p":
                RemoveExistingOptions();
                ShowBasePolys();
                break;
            case "o":
                RemoveExistingOptions();
                ShowOps();
                break;
        }
    }
}
