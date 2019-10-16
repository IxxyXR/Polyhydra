﻿using UnityEngine;


public class SyncMeshtoSDF : MonoBehaviour
{

    public MeshFilter _polymr;
    private MeshToSDF _m2sdf;

    void Start()
    {
        _m2sdf = gameObject.GetComponent<MeshToSDF>();
    }

    void Update()
    {
        //_m2sdf.mesh = _polymr.mesh;
    }
}
