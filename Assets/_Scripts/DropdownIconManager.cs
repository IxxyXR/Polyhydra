using System.Collections;
using System.Collections.Generic;
using Conway;
using UnityEngine;
using UnityEngine.UI;

public class DropdownIconManager : MonoBehaviour
{
    private Image img;
    
    void Start()
    {
        // This returns null for the main image so only calls SetIcon for dropdown icons
        var label = transform.parent.GetComponentInChildren<Text>();
        if (label != null)
        {
            SetIcon(label.text.Replace(" ", ""));
        }
    }

    public void SetIcon(Ops opType)
    {
        SetIcon(opType.ToString());
    }

    public void SetIcon(string iconName)
    {
        if (img == null)
        {
            img = GetComponent<Image>();
        }
        img.sprite = Resources.Load<Sprite>("Icons/" + iconName);
    }
}
