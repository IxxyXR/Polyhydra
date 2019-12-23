using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitVR : MonoBehaviour
{
    public Transform PolyRoot;
    
    void Start()
    {
        // Quick Hacks to configure for VR
        PolyRoot.transform.localScale = Vector3.one * 0.75f;
    }

}
