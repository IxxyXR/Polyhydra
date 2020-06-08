using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SolidRectangleExample))]
public class BeginAreaTestEditor : Editor
{
    void OnSceneGUI()
    {
        SolidRectangleExample t = target as SolidRectangleExample;
        Vector3 pos = t.transform.position;

        // Vector3[] verts =
        // {
        //     new Vector3(pos.x - t.range, pos.y, pos.z - t.range),
        //     new Vector3(pos.x - t.range, pos.y, pos.z + t.range),
        //     new Vector3(pos.x + t.range, pos.y, pos.z + t.range),
        //     new Vector3(pos.x + t.range, pos.y, pos.z - t.range)
        // };

        var handlePos = new Vector3(pos.x + t.range, pos.y, pos.z);

        // Handles.DrawSolidRectangleWithOutline(verts, new Color(0.5f, 0.5f, 0.5f, 0.1f), new Color(0, 0, 0, 1));

        Handles.DrawWireDisc(pos, Vector3.up, t.range);

        t.range = Handles.ScaleValueHandle(t.range,
            handlePos,
            Quaternion.identity,
            2.0f,
            Handles.CubeHandleCap,
            1.0f
        );

        // foreach (Vector3 posCube in verts)
        // {
        //     t.range = Handles.ScaleValueHandle(t.range,
        //         posCube,
        //         Quaternion.identity,
        //         1.0f,
        //         Handles.CubeHandleCap,
        //         1.0f);
        // }
    }
}