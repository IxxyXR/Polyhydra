using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownSVGIcon : MonoBehaviour
{

    public bool IsMainItem;

    private float startX = 281f;
    private float startY = -128f;
    private float offsetX = 51.5f;
    private float offsetY = 51.5f;

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
