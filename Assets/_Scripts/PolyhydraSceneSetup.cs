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

    public enum VrSdks
    {
        Oculus,
        OpenVR,
    }

    public VrSdks VrSdk = VrSdks.Oculus;

    // Start is called before the first frame update
    void Start()
    {
        Configure();
    }

    private void OnValidate()
    {
        //Configure();
    }

    void Configure()
    {
        MidiController.gameObject.SetActive(MidiEnabled);
        if (RenderingPipeline==RenderingPipelines.HDRP)
        {
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
        if (VrEnabled)
        {
            MainCamera.SetActive(false);
            VRPlayer.SetActive(true);
            if (VrSdk == VrSdks.Oculus)
            {
                StartCoroutine(LoadDevice("Oculus"));
            }
            else
            {
                StartCoroutine(LoadDevice("OpenVR"));
            }
        }
        else
        {
            VRPlayer.SetActive(false);
            MainCamera.SetActive(true);
            StartCoroutine(LoadDevice("MockHMD"));
        }


    }
    
    IEnumerator LoadDevice(string newDevice)
    {
        if (String.Compare(UnityEngine.XR.XRSettings.loadedDeviceName, newDevice, true) != 0)
        {
            UnityEngine.XR.XRSettings.LoadDeviceByName(newDevice);
            yield return null;
            if (VrEnabled) UnityEngine.XR.XRSettings.enabled = true;
        }
    }
}
