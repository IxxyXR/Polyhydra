using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitVR : MonoBehaviour
{
    public Transform PolyRoot;
    public Transform AkaiMarker;
    private AkaiPrefabController _akaiPrefabController;
    
    void Start()
    {
        // Quick Hacks to configure for VR
        PolyRoot.transform.localScale = Vector3.one * 0.75f;
        _akaiPrefabController = FindObjectOfType<AkaiPrefabController>();
        _akaiPrefabController.transform.position = AkaiMarker.position;
        _akaiPrefabController.transform.rotation = AkaiMarker.rotation;
    }

}
