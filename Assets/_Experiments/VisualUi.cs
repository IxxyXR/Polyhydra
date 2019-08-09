using System;
using System.Linq;
using Conway;
using UnityEditor;
using UnityEngine;
using Wythoff;

public class VisualUi : MonoBehaviour
{
    public PolyHydra PolyPrefab;
    public PolyHydra MasterPoly;
    private int CurrentPolyPage = 0;
    private int CurrentOpPage = 0;
    private int NumPolyPages;
    private int NumOpPages;
    public float radius = 2.2f;
    public int ItemsPerPage = 12;

    public int FirstValidPoly;
    public int LastValidPoly;
    public int FirstValidOp;
    public int LastValidOp;

    public enum Menus
    {
        Polys,
        Ops,
    }

    private Menus CurrentMenu;

    private GameObject _pivot;

    void Start()
    {
        NumPolyPages = Mathf.CeilToInt((LastValidPoly - FirstValidPoly) / (float)ItemsPerPage);
        NumOpPages = Mathf.CeilToInt((LastValidOp - FirstValidOp) / (float)ItemsPerPage);
        ShowUniformMenu();
    }

    void RecreatePivot()
    {
        if (_pivot != null)
        {
            Destroy(_pivot);
        }
        _pivot = new GameObject();
        _pivot.transform.parent = MasterPoly.transform;
        _pivot.name = "MenuPivot";
    }

    void ClearMenu()
    {
        RecreatePivot();
    }

    void ShowConwayMenu()
    {
        ClearMenu();

        int firstIndex = CurrentOpPage * ItemsPerPage + FirstValidOp;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidOp);
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            float x = Mathf.Sin(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, y, 0), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            var opType = (PolyHydra.Ops) i;
            var newOp = new PolyHydra.ConwayOperator
            {
                opType = opType,
                faceSelections = ConwayPoly.FaceSelections.All,
                randomize = false,
                amount = copyPoly.opconfigs[opType].amountDefault,
                disabled = false
            };
            copyPoly.ConwayOperators.Add(newOp);
            copyPoly.MakePolyhedron();
        }
    }

    void ShowUniformMenu()
    {
        ClearMenu();
        int firstIndex = (CurrentPolyPage * ItemsPerPage) + FirstValidPoly;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidPoly);
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            float x = Mathf.Sin(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, y, 0), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            copyPoly.UniformPolyType = (PolyTypes) i;
            var uniform = Uniform.Uniforms[i];
            if (uniform.Wythoff == "-") continue;
            var wythoff = new WythoffPoly(uniform.Wythoff.Replace("p", "5").Replace("q", "2"));
            // Which types to create? Example:
            //if (wythoff.SymmetryType != 3) continue;
            copyPoly.MakePolyhedron();
        }
    }

    void Update()
    {
        if (_pivot != null) _pivot.transform.Rotate(0, 0, .1f);
        if (Input.GetKeyDown(KeyCode.P))
        {
            ChangeMenu(-1);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeMenu(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangePage(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangePage(1);
        }
    }

    private void ChangeMenu(int direction)
    {
        CurrentMenu += direction;
        CurrentMenu = (Menus)((int)CurrentMenu % Enum.GetValues(typeof(Menus)).Length);
        switch (CurrentMenu)
        {
            case Menus.Ops:
                ShowConwayMenu();
                break;
            case Menus.Polys:
                ShowUniformMenu();
                break;
        }
    }

    private void ChangePage(int direction)
    {
        switch (CurrentMenu)
        {
            case Menus.Ops:
                CurrentOpPage += direction;
                CurrentOpPage %= NumOpPages;
                ShowConwayMenu();
                break;
            case Menus.Polys:
                CurrentPolyPage += direction;
                CurrentPolyPage %= NumPolyPages;
                ShowUniformMenu();
                break;
        }
    }

    public void MenuItemClicked(PolyHydra clickedPoly)
    {
        switch (CurrentMenu)
        {
            case Menus.Ops:
                MasterPoly.ConwayOperators.Add(clickedPoly.ConwayOperators.Last());
                MasterPoly.MakePolyhedron();
                break;
            case Menus.Polys:
                MasterPoly.UniformPolyType = clickedPoly.UniformPolyType;
                MasterPoly.MakePolyhedron();
                break;
        }
    }

    public void MenuItemMouseEnter()
    {
        Cursor.SetCursor(Texture2D.whiteTexture, Vector2.zero, CursorMode.Auto);
    }

    public void MenuItemMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_pivot == null) return;
        foreach (Transform child in _pivot.transform)
        {
            var poly = child.gameObject.GetComponent<PolyHydra>();
            Handles.Label(child.position + new Vector3(0, .15f, 0), poly.UniformPolyType.ToString());
            if (poly.ConwayOperators.Count==0) continue;
            var lastOp = poly.ConwayOperators.Last();
            Handles.Label(child.position + new Vector3(0, -.15f, 0), lastOp.opType.ToString());
        }
    }
    #endif

}
