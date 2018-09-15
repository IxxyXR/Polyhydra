using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Buckminster.Types
{
  public partial class ConwayHart
  {

    public ConwayHart clone()
    {
      var npositions = new Dictionary<dynamic, Vector3>();
      for(var i=0; i < positions.Count; ++i) {
        npositions[i] = positions[i.ToString()];
      }
      var nfaces = new List<List<dynamic>>();
      for(var i=0; i < faces.Count; ++i) {
        nfaces[i] = faces[i];
      }

      return new ConwayHart(npositions, nfaces);
    }

    public dynamic intersect(dynamic set1, dynamic set2, dynamic set3)
    {
      // find element common to 3 sets
      for (var i = 0; i < set1.length; i++)
      {
        // by brute force search
        for (var j = 0; j < set2.length; j++)
        {
          if (set1[i] == set2[j])
          {
            for (var k = 0; k < set3.length; k++)
            {
              if (set1[i] == set3[k])
              {
                return set1[i];
              }
            }
          }
        }
      }

      throw new Exception("program bug in intersect()");
    }

    public int[] sequence(int start, int stop)
    {
      // make list of integers, inclusive
      var ans = new List<int>();
      if (start <= stop)
      {
        for (var i = start; i <= stop; i++)
        {
          ans.Add(i);
        }
      }
      else
      {
        for (var i = start; i >= stop; i--)
        {
          ans.Add(i);
        }
      }

      return ans.ToArray();
    }

    public Vector3 orthogonal(Vector3 v3, Vector3 v2, Vector3 v1)
    {
      // find unit vector orthog to plane of 3 pts
      var d1 = v2 - v1; // adjacent edge vectors
      var d2 = v3 - v2;
      return new Vector3(
        d1[1] * d2[2] - d1[2] * d2[1], // cross product
        d1[2] * d2[0] - d1[0] * d2[2],
        d1[0] * d2[1] - d1[1] * d2[0]
      );
    }

    public float mag2(Vector3 vec)
    {
      // magnitude squared of 3-vector
      return vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2];
    }

    public Vector3 tangentPoint(Vector3 v1, Vector3 v2)
    {
      // point where line v1...v2 tangent to an origin sphere
      var d = v2 - v1; // difference vector
      return v1 - Vector3.Dot(d, v1) / mag2(d) * d;
    }

    public float edgeDist(Vector3 v1, Vector3 v2)
    {
      // distance of line v1...v2 to origin
      return Mathf.Sqrt(mag2(tangentPoint(v1, v2)));
    }



    public Vector3 midpoint(Vector3 vec1, Vector3 vec2)
    {
      // mean of two 3-vectors
      return new Vector3(
        0.5f * (vec1[0] + vec2[0]),
        0.5f * (vec1[1] + vec2[1]),
        0.5f * (vec1[2] + vec2[2]));
    }

    public Vector3 oneThird(Vector3 vec1, Vector3 vec2)
    {
      // approx. (2/3)v1 + (1/3)v2   (assumes 3-vector)
      return new Vector3(
        0.7f * vec1[0] + 0.3f * vec2[0],
        0.7f * vec1[1] + 0.3f * vec2[1],
        0.7f * vec1[2] + 0.3f * vec2[2]);
    }

    public Vector3 reciprocal(Vector3 vec)
    {
      // reflect 3-vector in unit sphere
      var factor = 1.0f / mag2(vec);
      return new Vector3(
        factor * vec[0],
        factor * vec[1],
        factor * vec[2]);
    }

    public Vector3 unit(Vector3 vec)
    {
      // normalize 3-vector to unit magnitude
      var size = mag2(vec);
      if (size <= 1e-8)
      {
        // remove this test someday...
        return vec;
      }

      var c = 1.0f / Mathf.Sqrt(size);
      return c * vec;
    }

  }
}
