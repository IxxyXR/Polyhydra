using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;


public class PolyhydraSceneSetup : MonoBehaviour
{
    public enum RenderingPipelines
    {
        URP,
        HDRP,
    }
    
    public bool VrEnabled;
    public bool MidiEnabled;
    public RenderingPipelines RenderingPipeline = RenderingPipelines.HDRP;

    public GameObject VRPlayer;
    public GameObject MainCamera;
    public GameObject MidiController;

    // Start is called before the first frame update
    void Start()
    {
        Configure();
    }

    private void OnValidate()
    {
        Configure();
    }

    void Configure()
    {
        MidiController.gameObject.SetActive(MidiEnabled);
        VRPlayer.SetActive(VrEnabled);
        MainCamera.SetActive(!VrEnabled);

        if (VrEnabled)
        {
            UnityEngine.XR.XRSettings.LoadDeviceByName("Oculus");
        }
        else
        {
            UnityEngine.XR.XRSettings.LoadDeviceByName("MockHMD");
        }
        
        if (RenderingPipeline==RenderingPipelines.URP)
        {
            Debug.Log(Resources.FindObjectsOfTypeAll<HDAdditionalCameraData>());
            Resources.FindObjectsOfTypeAll<HDAdditionalCameraData>().Select(x => x.enabled = true);
            Resources.FindObjectsOfTypeAll<HDAdditionalLightData>().Select(x => x.enabled = true);
            Resources.FindObjectsOfTypeAll<UniversalAdditionalCameraData>().Select(x => x.enabled = false);
            Resources.FindObjectsOfTypeAll<UniversalAdditionalLightData>().Select(x => x.enabled = false);
        }
        else if (RenderingPipeline == RenderingPipelines.URP)
        {
            Resources.FindObjectsOfTypeAll<HDAdditionalCameraData>().Select(x => x.enabled = false);
            Resources.FindObjectsOfTypeAll<HDAdditionalLightData>().Select(x => x.enabled = false);
            Resources.FindObjectsOfTypeAll<UniversalAdditionalCameraData>().Select(x => x.enabled = true);
            Resources.FindObjectsOfTypeAll<UniversalAdditionalLightData>().Select(x => x.enabled = true);
        }
    }
}
