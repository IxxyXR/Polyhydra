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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Calibrate();
        }
    }
    
    public void Calibrate()
    {
        var VRCamera = Camera.main.transform;
        var playerStart = VRPlayerMarker.position;
        var position = gameObject.transform.position;
        position.x += playerStart.x - VRCamera.position.x;
        position.y += playerStart.y - VRCamera.position.y;
        position.z += playerStart.z - VRCamera.position.z;
        gameObject.transform.position = position;
    }
    
}
