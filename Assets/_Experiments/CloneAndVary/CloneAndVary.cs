using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


//[ExecuteInEditMode]
public class CloneAndVary : MonoBehaviour
{
    public int rows = 10;
    public int columns = 10;
    public float spacing = 3f;
    public float scale = 1f;
    public float xOffset = 0f;
    public float yOffset = 0f;
    public float xMagnitude = 0.1f;
    public float yMagnitude = 0.1f;
    public float height = 1f;
    public float maxHeight = 15f;
    public float timeHeight = 0.0005f;
    public Transform[,] clones;
    public List<PolyHydraEnums.Ops> ops;
    public int currentOp = 0;
    public AnimationCurve fadeCurve;

    public PolyHydra poly;

    public bool localPosition;

    void Start()
    {
        poly.gameObject.SetActive(false);
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        clones = null;
        Generate();
    }

    private void Update()
    {
        Generate();
    }
    
    // private void OnValidate()
    // {
    //     Generate();
    // }

    private Transform InitClone(int x, int y)
    {
        var clone= Instantiate(poly, transform);
        clone.gameObject.SetActive(true);
        clones[x, y] = clone.transform;
        return clone.transform;

    }

    public void Generate()
    {
        if (clones==null || clones.GetLength(0) != rows || clones.GetLength(1) != columns)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
                // if (Application.isPlaying)
                // {
                //     Destroy(child.gameObject);
                // }
                // else
                // {
                //     StartCoroutine(Destroy(child.gameObject));
                // }
            }
            clones = new Transform[rows, columns];
        }

        int count = 0;
        for (int y = 0; y < columns; y++)
        {
            for (int x = 0; x < rows; x++)
            {
                var clone = clones[x, y];
                
                if (clone == null)
                {
                    clone = InitClone(x, y);
                }
                
                if (localPosition)
                {
                    clone.localPosition = GetClonePosition(x, y);
                }
                else
                {
                    clone.position = GetClonePosition(x, y);
                }

                clone.localScale = Vector3.one * scale;
                var color = clone.GetComponent<MeshRenderer>().material.GetColor("_BaseColor");
                color.a = fadeCurve.Evaluate(clone.position.y / (float)maxHeight);
                clone.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
                var p = clone.GetComponent<PolyHydra>();
                var op = p.ConwayOperators[0];
                //op.opType = ops[Mathf.FloorToInt((count * a + Time.time * b) * c) % ops.Count]; try 0.1 8 0.1
                op.opType = ops[currentOp];
                op.amount = xOffset + x * xMagnitude;
                op.amount2 = yOffset + y * yMagnitude;
                p.ConwayOperators[0] = op;
                p.Rebuild();
                count++;
            }
        }
    }

    private Vector3 GetClonePosition(int x, int y)
    {
        return new Vector3(x * spacing, ((y * rows + x) * height * (Time.frameCount * timeHeight)) % maxHeight , y * spacing);
    }
    
    // IEnumerator Destroy(GameObject go)
    // {
    //     yield return new WaitForEndOfFrame();
    //     DestroyImmediate(go);
    // }
}
