using System;
using System.Collections;
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
    public Material material;
    public Transform[,] clones;

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

        for (int y = 0; y < columns; y++)
        {
            for (int x = 0; x < rows; x++)
            {
                var clone = clones[x, y];
                if (clone == null)
                {
                    clone = InitClone(x, y);
                }
                else
                {
                    if (localPosition)
                    {
                        clone.localPosition = GetClonePosition(x, y);
                    }
                    else
                    {
                        clone.position = GetClonePosition(x, y);
                    }

                    clone.localScale = Vector3.one * scale;
                }
                var p = clone.GetComponent<PolyHydra>();
                var op = p.ConwayOperators[0];
                op.amount = xOffset + x * xMagnitude;
                op.amount2 = yOffset + y * yMagnitude;
                p.ConwayOperators[0] = op;
                p.Rebuild();
            }
        }
    }

    private Vector3 GetClonePosition(int x, int y)
    {
        return new Vector3(x * spacing, 1, y * spacing);
    }
    
    // IEnumerator Destroy(GameObject go)
    // {
    //     yield return new WaitForEndOfFrame();
    //     DestroyImmediate(go);
    // }
}
