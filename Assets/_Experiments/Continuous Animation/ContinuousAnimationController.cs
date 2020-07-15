using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousAnimationController : MonoBehaviour
{

    [NonSerialized] public bool RebuildNeeded = false;
    private PolyHydra poly;

    void Start()
    {
        poly = GetComponent<PolyHydra>();
    }
    
    void Update()
    {
        if (RebuildNeeded)
        {
            poly.ColorMethod = PolyHydraEnums.ColorMethods.BySides;  // Needed to stop color jumps
            poly.Rebuild();
            RebuildNeeded = false;
        }
    }
}
