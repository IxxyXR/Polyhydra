using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Buckminster.Types
{
  public partial class ConwayHart
  {

    //-------------------Canonicalization Algorithm--------------------------
    // True canonicalization rather slow.  Using center of gravity of vertices for each
    // face gives a quick "adjustment" which planarizes faces at least.



    public Dictionary<dynamic, Vector3> reciprocalN(ConwayHart poly)
    {
      // make array of vertices reciprocal to given planes
      var ans = new Dictionary<dynamic, Vector3>();
      for (var i = 0; i < poly.faces.Count; ++i)
      {
        // for each face:
        var centroid = Vector3.zero; // running sum of vertex coords
        var normal = Vector3.zero; // running sum of normal vectors
        var avgEdgeDist = 0.0f; // running sum for avg edge distance
        var v1 = poly.faces[i][poly.faces[i].Count - 2]; // preprevious vertex
        var v2 = poly.faces[i][poly.faces[i].Count - 1]; // previous vertex
        for (var j = 0; j < poly.faces[i].Count; j++)
        {
          var v3 = poly.faces[i][j]; // this vertex
          centroid = centroid + poly.positions[v3];
          normal = normal + orthogonal(poly.positions[v1], poly.positions[v2], poly.positions[v3]);
          avgEdgeDist = avgEdgeDist + edgeDist(poly.positions[v1], poly.positions[v2]);
          v1 = v2; // shift over one
          v2 = v3;
        }

        centroid = 1.0f / poly.faces[i].Count * centroid;
        normal = unit(normal);
        avgEdgeDist = avgEdgeDist / poly.faces[i].Count;
        ans[i] = reciprocal(Vector3.Dot(centroid, normal) * normal); // based on face
        ans[i] = (1 + avgEdgeDist) / 2 * ans[i]; // edge correction
      }

      return ans;
    }

    public Dictionary<dynamic, Vector3> reciprocalC(ConwayHart poly)
    {
      // return array of reciprocals of face centers
      var center = faceCenters();
      for (var i = 0; i < poly.faces.Count; i++)
      {
        var m2 = center[i][0] * center[i][0] + center[i][1] * center[i][1] + center[i][2] * center[i][2];
        center[i] = new Vector3( // divide each coord by magnitude squared
          center[i].x / m2,
          center[i].y / m2,
          center[i].z / m2);
      }

      return center;
    }

    public Dictionary<dynamic, Vector3> faceCenters()
    {
      // return array of "face centers"
      var ans = new Dictionary<dynamic, Vector3>();
      for (var i = 0; i < faces.Count; i++)
      {
        ans[i] = Vector3.zero; // running sum
        for (var j = 0; j < faces[i].Count; j++) // just average vertex coords:
        {
          ans[i] = ans[i] + positions[faces[i][j]]; // sum and...
        }

        ans[i] = 1.0f / faces[i].Count * ans[i]; // ...divide by n
      }

      return ans;
    }

    public void canonicalpositions(ConwayHart poly, int nIterations)
    {
      // compute new vertex coords.
      var dpoly = makeDual(poly); // v's of dual are in order or arg's f's
      for (var count = 0; count < nIterations; count++)
      {
        // iteration:
        dpoly.positions = reciprocalN(poly);
        poly.positions = reciprocalN(dpoly);
      }
    }

    public void adjustpositions(ConwayHart poly, int nIterations)
    {
      var dpoly = makeDual(poly); // v's of dual are in order or arg's f's
      for (var count = 0; count < 1; count++)
      {
        // iteration:
        dpoly.positions = reciprocalC(poly); // reciprocate face centers
        poly.positions = reciprocalC(dpoly); // reciprocate face centers
      }
    }

  }
}
