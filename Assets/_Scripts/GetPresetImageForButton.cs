using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GetPresetImageForButton : MonoBehaviour
{
    public int idx;
    public int width = 150;


    public void UpdateImage()
    {
        var img = GetComponent<Image>();
        idx = transform.parent.GetSiblingIndex();
        var pname = FindObjectOfType<PolyPresets>().Items[idx].Name;
        var filePath = ScreenCaptureTool.PresetScreenShotName(pname);
        byte[] fileData;
        if (File.Exists(filePath))     {
            fileData = File.ReadAllBytes(filePath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            float pixelsPerUnit =  (float)tex.width / width * 100f;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0,0), pixelsPerUnit);
            img.sprite = sprite;
            img.SetNativeSize();
        }
        else
        {
            Debug.LogError($"UpdateImage failed. Index: {idx} Pname: {pname} Not found: {filePath}");
        }
    }
}
