using System.Collections;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SocialPlatforms.GameCenter;

[ExecuteInEditMode]
public class ScreenCaptureTool : MonoBehaviour
{
    public int resWidth = 500;
    public int resHeight = 500;

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

    [ContextMenu("Take All Preset Screenshots")]
    public void TakeAllPresetScreenshots()
    {
        StartCoroutine(nameof(DoTakeAllPresetScreenshots));
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

    public static string PresetScreenShotName(string presetName)
    {
        return string.Format("{0}/preset_{1}.png",
            Application.persistentDataPath,
            presetName
        );
    }

    public static string ScreenShotName(int width, int height) {
        return string.Format("{0}/screenshot_{1}x{2}_{3}.png",
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

    public void TakePresetScreenshotNow(string fname)
    {
        filename = fname;
        TakeShotNow();
    }

    private void TakeShotNow()
    {
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        if (Application.isPlaying)
        {
            Destroy(rt);
        }
        else
        {
            DestroyImmediate(rt);
        }
        byte[] bytes = screenShot.EncodeToPNG();
        System.IO.File.WriteAllBytes(filename, bytes);
    }
}
