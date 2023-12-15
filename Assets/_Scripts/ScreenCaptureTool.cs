using System;
using System.Collections;
using Conway;
using Grids;
using UnityEngine;


[ExecuteInEditMode]
public class ScreenCaptureTool : MonoBehaviour
{
    public int resWidth = 500;
    public int resHeight = 500;
    public float ZoomFactor = 2.0f;

    private string filename;
    private Camera camera;
    private bool takeShot;


    void Start()
    {
        camera = FindObjectOfType<Camera>();
    }

    [ContextMenu("Take Screenshot")]
    public void TakeScreenshotFromEditor()
    {
        filename = ScreenShotName(resWidth, resHeight);
        TakeShotNow();
    }

    [ContextMenu("Take All Wythoff Screenshots")]
    public void TakeAllWythoffScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllWythoffScreenshots));
    }

    [ContextMenu("Take All Johnson Screenshots")]
    public void TakeAllJohnsonScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllJohnsonScreenshots));
    }

    [ContextMenu("Take All Grid Screenshots")]
    public void TakeAllGridScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllGridScreenshots));
    }

    [ContextMenu("Take All Preset Screenshots")]
    public void TakeAllPresetScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllPresetScreenshots));
    }

    [ContextMenu("Take All Other Poly Screenshots")]
    public void TakeAllOtherScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllOtherScreenshots));
    }

    IEnumerator DoTakeAllOtherScreenshots()
    {
        Camera.main.clearFlags = CameraClearFlags.Nothing;
        Camera.main.backgroundColor = Color.white;
        var otherNames = Enum.GetNames(typeof(PolyHydraEnums.OtherPolyTypes));
        var poly = FindObjectOfType<PolyHydra>();
        poly.transform.parent.GetComponent<Rigidbody>().isKinematic = true;
        poly.transform.parent.rotation = Quaternion.identity;
        poly.enableThreading = false;
        poly.ConwayOperators.Clear();
        poly.ShapeType = PolyHydraEnums.ShapeTypes.Other;
        for (var index = 0; index < otherNames.Length; index++)
        {
            filename = PolyScreenShotName($"other_{otherNames[index]}");
            poly.OtherPolyType = (PolyHydraEnums.OtherPolyTypes)index;
            switch (poly.OtherPolyType)
            {
                case PolyHydraEnums.OtherPolyTypes.Polygon:
                    poly.PrismP = 5;
                    break;
                case PolyHydraEnums.OtherPolyTypes.GriddedCube:
                    poly.PrismP = 4;
                    poly.PrismQ = 4;
                    break;
                case PolyHydraEnums.OtherPolyTypes.UvHemisphere:
                    poly.PrismP = 12;
                    poly.PrismQ = 12;
                    break;
                case PolyHydraEnums.OtherPolyTypes.UvSphere:
                    poly.PrismP = 12;
                    poly.PrismQ = 12;
                    break;
            }
            poly.Rebuild();
            yield return new WaitForSeconds(0.5f);
            poly._conwayPoly.Recenter();
            Vector3 target = poly._conwayPoly.GetCentroid();
            camera.transform.LookAt(target);
            yield return new WaitForSeconds(0.5f);
            takeShot = true;
            yield return true;
        }
        poly.enableThreading = true;
    }

    IEnumerator DoTakeAllGridScreenshots()
    {
        Camera.main.clearFlags = CameraClearFlags.Nothing;
        Camera.main.backgroundColor = Color.white;
        var gridNames = Enum.GetNames(typeof(GridEnums.GridTypes));
        var poly = FindObjectOfType<PolyHydra>();
        poly.transform.parent.GetComponent<Rigidbody>().isKinematic = true;
        poly.transform.parent.rotation = Quaternion.Euler(30, 0, 0);
        poly.enableThreading = false;
        poly.ConwayOperators.Clear();
        poly.ShapeType = PolyHydraEnums.ShapeTypes.Grid;
        poly.PrismP = 4;
        poly.PrismQ = 4;
        for (var index = 0; index < gridNames.Length; index++)
        {
            filename = PolyScreenShotName($"grid_{gridNames[index]}");
            poly.GridType = (GridEnums.GridTypes)index;
            poly.Rebuild();
            yield return new WaitForSeconds(0.5f);
            poly._conwayPoly.Recenter();
            Vector3 target = poly._conwayPoly.GetCentroid();
            camera.transform.LookAt(target);
            yield return new WaitForSeconds(0.5f);
            takeShot = true;
            yield return true;
        }
        poly.enableThreading = true;
    }

    IEnumerator DoTakeAllJohnsonScreenshots()
    {
        Camera.main.clearFlags = CameraClearFlags.Nothing;
        Camera.main.backgroundColor = Color.white;
        var johnsonNames = Enum.GetNames(typeof(PolyHydraEnums.JohnsonPolyTypes));
        var poly = FindObjectOfType<PolyHydra>();
        poly.transform.parent.GetComponent<Rigidbody>().isKinematic = true;
        poly.transform.parent.rotation = Quaternion.identity;
        poly.enableThreading = false;
        poly.ConwayOperators.Clear();
        poly.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
        poly.PrismP = 5;
        poly.PrismQ = 2;
        for (var index = 0; index < johnsonNames.Length; index++)
        {
            filename = PolyScreenShotName($"johnson_{johnsonNames[index]}");
            poly.JohnsonPolyType = (PolyHydraEnums.JohnsonPolyTypes)index;
            poly.Rebuild();
            yield return new WaitForSeconds(0.5f);
            poly._conwayPoly.Recenter();
            Vector3 target = poly._conwayPoly.GetCentroid();
            camera.transform.LookAt(target);
            yield return new WaitForSeconds(0.5f);
            takeShot = true;
            yield return true;
        }
        poly.enableThreading = true;
    }

    IEnumerator DoTakeAllWythoffScreenshots()
    {
        Camera.main.clearFlags = CameraClearFlags.Nothing;
        Camera.main.backgroundColor = Color.white;
        var uniformNames = Enum.GetNames(typeof(PolyTypes));
        var poly = FindObjectOfType<PolyHydra>();
        poly.transform.parent.GetComponent<Rigidbody>().isKinematic = true;
        poly.transform.parent.rotation = Quaternion.Euler(0, 90, 0);
        poly.enableThreading = false;
        poly.ConwayOperators.Clear();
        poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
        poly.PrismP = 5;
        poly.PrismQ = 2;
        for (var index = 0; index < uniformNames.Length; index++)
        {
            filename = PolyScreenShotName($"uniform_{uniformNames[index]}");
            poly.UniformPolyType = (PolyTypes)index;
            poly.Rebuild();
            yield return new WaitForSeconds(0.5f);
            poly._conwayPoly.Recenter();
            Vector3 target = poly._conwayPoly.GetCentroid();
            camera.transform.LookAt(target);
            yield return new WaitForSeconds(0.5f);
            takeShot = true;
            yield return true;
        }

        poly.enableThreading = true;
    }

    IEnumerator DoTakeAllPresetScreenshots()
    {
        var presets = FindObjectOfType<PolyPresets>();
        var poly = FindObjectOfType<PolyHydra>();
        poly.enableThreading = false;
        foreach(var preset in presets.Items)
        {
            filename = PresetScreenShotName(preset);
            presets.ApplyPresetToPoly(preset, true);
            poly.Rebuild();
            yield return new WaitForSeconds(0.5f);
            poly._conwayPoly.Recenter();
            Vector3 target = poly._conwayPoly.GetCentroid();
            camera.transform.LookAt(target);
            yield return new WaitForSeconds(0.5f);
            takeShot = true;
            yield return true;
        }
        poly.enableThreading = true;
    }

    public static string PresetScreenShotName(PolyPreset preset)
    {
        return PresetScreenShotName(preset.Name);
    }

    public static string PolyScreenShotName(string polyName)
    {
        return string.Format("{0}/poly_{1}.jpg",
            Application.persistentDataPath,
            polyName
        );
    }

    public static string PresetScreenShotName(string presetName)
    {
        return string.Format("{0}/preset_{1}.jpg",
            Application.persistentDataPath,
            presetName
        );
    }

    public static string ScreenShotName(int width, int height) {
        return string.Format("{0}/screenshot_{1}x{2}_{3}.jpg",
            Application.persistentDataPath,
            width, height,
            System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    void LateUpdate() {
        if (takeShot) {
            takeShot = false;
            TakeShotNow();
        }
    }

    public void TakePresetScreenshotNow(string presetName)
    {
        filename = PresetScreenShotName(presetName);
        TakeShotNow();
    }

    private void TakeShotNow()
    {
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        float oldFov = camera.fieldOfView;
        camera.fieldOfView = oldFov / ZoomFactor;
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        camera.fieldOfView = oldFov;
        RenderTexture.active = null;
        if (Application.isPlaying)
        {
            Destroy(rt);
        }
        else
        {
            DestroyImmediate(rt);
        }
        byte[] bytes = screenShot.EncodeToJPG(90);
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log($"Saving shot to {filename}");
    }
}
