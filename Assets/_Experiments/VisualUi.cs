using System;
using Conway;
using UnityEngine;
using Wythoff;

public class VisualUi : MonoBehaviour
{
    public PolyHydra PolyPrefab;
    public GameObject Centerpoint;
    private int CurrentPage = 0;
    private int NumPolyPages;
    private int NumOpPages;
    private int LastPolyIndex;
    private int LastOpIndex;
    public float radius = 2.2f;
    public int ItemsPerPage = 12;

    public enum Menus
    {
        Polys,
        Ops,
    }

    private Menus CurrentMenu;

    private GameObject _pivot;

    void Start()
    {
        LastPolyIndex = Uniform.Uniforms.Length;
        LastOpIndex = Enum.GetValues(typeof(PolyHydra.Ops)).Length;
        NumPolyPages = LastPolyIndex / ItemsPerPage;
        NumOpPages = LastOpIndex / ItemsPerPage;
        RecreatePivot();
    }

    void RecreatePivot()
    {
        if (_pivot != null)
        {
            Destroy(_pivot);
        }
        _pivot = new GameObject();
        _pivot.transform.parent = Centerpoint.transform;
        _pivot.name = "MenuPivot";
    }

    void ClearMenu()
    {
        RecreatePivot();
    }

    void ShowConwayMenu()
    {
        ClearMenu();

        int firstIndex = CurrentPage * ItemsPerPage;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastOpIndex);
        Debug.Log($"Page: {CurrentPage} firstIndex: {firstIndex} lastIndex: {lastIndex}");
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            float x = Mathf.Sin(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
            float y = Mathf.Cos(((i - firstIndex) / (float)ItemsPerPage) * Mathf.PI * 2) * radius;
//            Debug.Log($"{i}: {x},{y}");
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, y, 0), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            //copyPoly.ConwayOperators.Clear();
            var opType = (PolyHydra.Ops) i;

            var newOp = new PolyHydra.ConwayOperator()
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
        int firstIndex = CurrentPage * ItemsPerPage;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastPolyIndex);
        Debug.Log($"Page: {CurrentPage} firstIndex: {firstIndex} lastIndex: {lastIndex}");
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
        if (_pivot != null) _pivot.transform.Rotate(0, .1f, 0);
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
        CurrentPage += direction;
        switch (CurrentMenu)
        {
            case Menus.Ops:
                CurrentPage %= NumOpPages;
                ShowConwayMenu();
                break;
            case Menus.Polys:
                CurrentPage %= NumPolyPages;
                ShowUniformMenu();
                break;
        }
    }
}
