using UnityEngine;


public class VisualUiMenuItem : MonoBehaviour
{
    private VisualUi visualUi;
    private PolyHydra poly;

    void Start()
    {
        visualUi = FindObjectOfType<VisualUi>();
        poly = gameObject.GetComponent<PolyHydra>();
    }

    public void OnMouseDown()
    {
        visualUi.MenuItemClicked(poly);
    }

    public void OnMouseEnter()
    {
        visualUi.MenuItemMouseEnter();
    }

    public void OnMouseExit()
    {
        visualUi.MenuItemMouseExit();
    }
}
