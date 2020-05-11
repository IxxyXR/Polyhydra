using System;
using System.Collections;
using UnityEngine;


[ExecuteInEditMode]
public class CloneAndVary : MonoBehaviour
{
    public int rows = 10;
    public int columns = 10;
    public float spacing = 3f;
    public float xOffset = 0f;
    public float yOffset = 0f;
    public float xMagnitude = 0.1f;
    public float yMagnitude = 0.1f;
    public Material material;
    public Transform[,] clones;

    public PolyHydra poly;

    public bool dummy;

    void Start()
    {
        Reset();
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
    
    private void OnValidate()
    {
        Generate();
    }

    private Transform InitClone(int x, int y)
    {
        var go = Instantiate(
            poly,
            GetClonePosition(x, y),
            Quaternion.identity,
            transform);
        clones[x, y] = go.transform;
        return go.transform;

    }

    public void Generate()
    {
        if (clones==null || clones.GetLength(0) != rows || clones.GetLength(1) != columns)
        {
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    StartCoroutine(Destroy(child.gameObject));
                }
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
                    clone.position = GetClonePosition(x, y);
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
    
    IEnumerator Destroy(GameObject go)
    {
        yield return new WaitForEndOfFrame();
        DestroyImmediate(go);
    }
}
