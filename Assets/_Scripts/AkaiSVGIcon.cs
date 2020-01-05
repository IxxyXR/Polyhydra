using UnityEngine;


[ExecuteInEditMode]
public class AkaiSVGIcon : MonoBehaviour
{

    public PolyHydra.Ops opType;

    public RectTransform icon;

    public float originX = 46.9f;
    public float originY = -21.5f;
    public float shiftX = 8.3f;
    public float shiftY = 8.3f;

    void Start()
    {
        UpdateIcon();
        //InvokeRepeating(nameof(UpdateIcon), .5f, .2f);  // For debugging
    }

    private void OnValidate()
    {
        UpdateIcon();
    }

    public void UpdateIcon()
    {
        int idx = (int) opType;
        var pos = icon.anchoredPosition;
        pos.x = originX - idx / 6 * shiftX;
        pos.y = originY + idx % 6 * shiftY;
        icon.anchoredPosition = pos;
    }
}
