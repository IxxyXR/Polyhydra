using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionTest : MonoBehaviour
{

    public Material TransitionMaterial;

    void Start()
    {
        var mr = gameObject.GetComponent<MeshRenderer>();
        var mf = gameObject.GetComponent<MeshFilter>();
        PolyUtils.SplitMesh(mf);
        mr.material = TransitionMaterial;
    }
}
