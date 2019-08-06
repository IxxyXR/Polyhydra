using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Conway;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class CallAntiprism : MonoBehaviour
{
    public bool applyOps;
    public string filename = "conway";
    public string arguments = "kC";

    private PolyHydra _poly;

    [ContextMenu("Go")]
    public void Go()
    {
        _poly = FindObjectOfType<PolyHydra>();
        int exitCode = -1;
        string output = "";
        Process process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = filename;
        process.StartInfo.Arguments = arguments;

        try
        {
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Debug.LogError("Run error" + e);
        }
        finally
        {
            exitCode = process.ExitCode;
            process.Dispose();
            process = null;
        }

        string line;

        var faceIndices = new List<int[]>();
        var vertexPoints = new List<Vector3>();

        using (StringReader reader = new StringReader(output)) {
            reader.ReadLine();  // The "OFF" header
            var metrics = reader.ReadLine().Split(' ');
            int NVertices = int.Parse(metrics[0]);
            int NFaces = int.Parse(metrics[1]);
            Debug.Log($"NVertices: {NVertices} NFaces: {NFaces}");

            for (int i = 0; i < NVertices; i++)
            {
                var vert = reader.ReadLine().Split(' ');
                vertexPoints.Add(new Vector3(float.Parse(vert[0]), float.Parse(vert[1]), float.Parse(vert[2])));
            }
            Debug.Log($"vertexPoints: {vertexPoints.Count}");
            for (int i = 0; i < NFaces; i++)
            {
                var faceString = reader.ReadLine().Split(' ');
                int sides = int.Parse(faceString[0]);
                if (sides < 3) continue;
                var face = new int[sides];
                for (int j = 0; j < sides; j++)
                {
                    face[j] = int.Parse(faceString[j + 1]);
                }

                faceIndices.Add(face);
                Debug.Log($"face: {face.Length}");
            }
            Debug.Log($"faceIndices: {faceIndices.Count}");
            var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, faceIndices.Count);
            var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, NVertices);
            var _conwayPoly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
            _poly._conwayPoly = _conwayPoly;
            _poly.DisableInteractiveFlags();
            if (applyOps)
            {
                _poly.ApplyOps();
            }
            _poly.FinishedApplyOps();

        }

    }
}
