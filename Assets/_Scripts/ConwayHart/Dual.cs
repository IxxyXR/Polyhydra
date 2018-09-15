using System;
using System.Collections.Generic;
using UnityEngine;


namespace Buckminster.Types
{
  public partial class ConwayHart
  {

    //--------------------------------Dual------------------------------------------
    // the makeDual function computes the dual's topology, needed for canonicalization,
    // where positions's are determined.  It is then saved in a global variable globSavedDual.
    // when the d operator is executed, d just returns the saved value.


    public ConwayHart makeDual(ConwayHart poly)
    {
      // compute dual of argument, matching V and F indices
      var result = new ConwayHart();
      var faces = new Dictionary<dynamic, Dictionary<dynamic, dynamic>>(); // make table of face as fn of edge
      for (var i = 0; i < poly.faces.Count; i++)
      {
        faces[i] = new  Dictionary<dynamic, dynamic>(); // create empty associative table
      }

      for (var i = 0; i < poly.faces.Count; i++)
      {
        var v1 = poly.faces[i][poly.faces[i].Count - 1]; // previous vertex
        for (int j = 0; j < poly.faces[i].Count; j++)
        {
          var v2 = poly.faces[i][j]; // this vertex
          faces[v1]["v" + v2] = new List<dynamic>();
          faces[v1]["v" + v2] = i; // fill it.  2nd index is associative
          v1 = v2; // current becomes previous
        }
      }

      for (var i = 0; i < poly.faces.Count; i++)
      {
        // create d's v's per p's f's
        result.newV(i, Vector3.zero); // only topology needed for canonicalize
      }

      for (var i = 0; i < poly.faces.Count; i++)
      {
        // one new flag for each old one
        var v1 = poly.faces[i][poly.faces[i].Count - 1]; // previous vertex
        for (int j = 0; j < poly.faces[i].Count; j++)
        {
          var v2 = poly.faces[i][j]; // this vertex
          result.newFlag(v1, faces[v2]["v" + v1], i); // look up face across edge
          v1 = v2; // current becomes previous
        }
      }

      var ans = result.flags2poly(); // this gives one indexing of answer
      var sortF = new List<List<dynamic>>(ans.faces.Count); // but f's of dual are randomly ordered, so sort
      for (var i = 0; i < ans.faces.Count; i++)
      {
        var j = intersect(poly.faces[ans.faces[i][0]], poly.faces[ans.faces[i][1]], poly.faces[ans.faces[i][2]]);
        sortF[j] = ans.faces[i]; // p's v for d's f is common to three of p's f's
      }

      ans.faces = sortF; // replace with the sorted list of faces
      return ans;
    }
  }
}
