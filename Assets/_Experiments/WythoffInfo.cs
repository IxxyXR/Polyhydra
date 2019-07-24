using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wythoff;

public class WythoffInfo : MonoBehaviour
{
    
    void Start()
    {
        foreach (var u in Uniform.Uniforms)
        {
            if (u.Wythoff == "-") continue;  // Skip grid placeholder
            string symbol = u.Wythoff.Replace("p", "5").Replace("q", "2");
            var wythoff = new WythoffPoly(symbol);
            Debug.Log($"{symbol}: Sym: {wythoff.SymmetryType}");
        }
        
    }
}
