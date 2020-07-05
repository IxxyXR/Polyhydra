using System;
using System.Collections.Generic;
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

    public static string UniqueID(string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ")
    {
        var t = DateTime.UtcNow;
        char[] charArray = CharList.ToCharArray();
        var result = new Stack<char>();

        var length = charArray.Length;

        long dgit = 1000000000000L +
                    t.Millisecond   * 1000000000L +
                    t.DayOfYear     * 1000000L +
                    t.Hour          * 10000L +
                    t.Minute        * 100L +
                    t.Second;

        while (dgit != 0)
        {
            result.Push(charArray[dgit % length]);
            dgit /= length;
        }
        return new string(result.ToArray());
    }

}
