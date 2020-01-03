using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitVR : MonoBehaviour
{
    public Transform PolyRoot;
    public Transform AkaiMarker;
    public Transform VRPlayerMarker;
    private AkaiPrefabController _akaiPrefabController;
    
    void Start()
    {
        // Quick Hacks to configure for VR
        PolyRoot.transform.localScale = Vector3.one * 0.75f;

        transform.position = VRPlayerMarker.position;
        transform.rotation = VRPlayerMarker.rotation;

        _akaiPrefabController = FindObjectOfType<AkaiPrefabController>();
        if (_akaiPrefabController != null)
        {
            _akaiPrefabController.transform.position = AkaiMarker.position;
            _akaiPrefabController.transform.rotation = AkaiMarker.rotation;
            _akaiPrefabController.transform.localScale = AkaiMarker.localScale;
        }
    }

}
