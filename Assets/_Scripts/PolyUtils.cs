using UnityEngine;

public class PolyUtils
{
    public static int ActualMod(int x, int m) // Fuck C# deciding that mod isn't actually mod
    {
        return (x % m + m) % m;
    }

    public static void SplitMesh(MeshFilter mf)
    {
        Vector3[] oldVerts = mf.mesh.vertices;
        int[] triangles = mf.mesh.triangles;
        Vector3[] vertices = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++) {
            vertices[i] = oldVerts[triangles[i]];
            triangles[i] = i;
        }
        mf.mesh.vertices = vertices;
        mf.mesh.triangles = triangles;
    }
}
