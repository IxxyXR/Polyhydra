using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownSVGIcon : MonoBehaviour
{

    public bool IsMainItem = false;

    private float startX = 136f;
    private float startY = -131f;
    private float offsetX = 39.25f;
    private float offsetY = 53f;

    void Start()
    {
        UpdateIcon();
        //InvokeRepeating(nameof(UpdateIcon), .5f, .2f);  // For debugging
    }

    public void UpdateIcon()
    {
        int idx;
        if (IsMainItem)
        {
            idx = transform.parent.parent.GetComponent<Dropdown>().value;
        }
        else
        {
            idx = transform.parent.parent.GetSiblingIndex() - 1;

        }

        var pos = gameObject.GetComponent<RectTransform>().anchoredPosition;
        pos.x = startX - ((idx / 6) * offsetX);
        pos.y = startY + ((idx % 6) * offsetY);
        gameObject.GetComponent<RectTransform>().anchoredPosition = pos;
    }
}
