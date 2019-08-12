using System;
using System.Collections;
using System.Linq;
using Conway;
using TMPro;
using UnityEditor;
using UnityEngine;


public class VisualUi : MonoBehaviour
{
    public PolyHydra PolyPrefab;
    public PolyHydra MasterPoly;
    public Transform CenterPivot;
    public float radius = 2.2f;
    public float scale = 0.3f;
    public int ItemsPerPage = 12;
    public int FirstValidPoly;
    public int LastValidPoly;
    public int FirstValidOp;
    public int LastValidOp;
    public Material TransitionMaterial;
    public float transitionDistance = 10f;
    public float transitionSpeed = 0.6f;
    public float menuSpeed = 0.04f;

    private int CurrentPolyPage;
    private int CurrentOpPage;
    private int NumPolyPages;
    private int NumOpPages;
    private int menuIndex;
    private Transform[] currentMenuItems;

    public enum Menus
    {
        Polys,
        Ops,
    }

    private Menus CurrentMenu;

    private GameObject _pivot;

    void Start()
    {
        currentMenuItems = new Transform[ItemsPerPage];
        NumPolyPages = Mathf.CeilToInt((LastValidPoly - FirstValidPoly) / (float)ItemsPerPage);
        NumOpPages = Mathf.CeilToInt((LastValidOp - FirstValidOp) / (float)ItemsPerPage);
        CreatePivot();
        ShowUniformMenu();
    }

    void CreatePivot()
    {
        if (_pivot != null)
        {
            Destroy(_pivot);
        }
        _pivot = new GameObject();
        _pivot.transform.parent = CenterPivot;
        _pivot.name = "MenuPivot";
    }

    void RemoveMenuItems()
    {
        foreach (Transform child in _pivot.transform)
        {
            var mr = child.gameObject.GetComponent<MeshRenderer>();
            IEnumerator coroutine = TransitionOut(mr, transitionDistance, transitionSpeed);
            StartCoroutine(coroutine);
        }

    }

    IEnumerator TransitionIn(MeshRenderer mr, float limit, float step)
    {
        var originalMaterial = mr.material;
        mr.material = TransitionMaterial;
        for (float i = limit; i > 0; i -= step)
        {
            mr.material.SetFloat("_amount", i);
            yield return null;
        }
        mr.material = originalMaterial;
    }

    IEnumerator TransitionOut(MeshRenderer mr, float limit, float step)
    {
        mr.gameObject.GetComponentInChildren<TextMeshPro>().text = "";
        mr.material = TransitionMaterial;
        for (float i = 0; i < limit; i += step)
        {
            mr.material.SetFloat("_amount", i);
            yield return null;
        }
        Destroy(mr.gameObject);
    }

    private void ScrollCurrentMenu(int indexDelta, float speed)
    {
        IEnumerator coroutine;

        coroutine = ScrollMenu(indexDelta, speed);
        StartCoroutine(coroutine);
        currentMenuItems[menuIndex].GetComponent<MeshRenderer>().material.SetColor("_Tint", Color.white);
        menuIndex = PolyUtils.ActualMod(menuIndex + indexDelta, ItemsPerPage);
        currentMenuItems[menuIndex].GetComponent<MeshRenderer>().material.SetColor("_Tint", Color.yellow);
    }

    IEnumerator ScrollMenu(int indexDelta, float speed)
    {
        float progress = 0;
        float angleToTurn = (360f / ItemsPerPage) * indexDelta;
        Vector3 currentRot = _pivot.transform.eulerAngles;

        while (progress <= 1)
        {
            var newRot = Quaternion.Euler(currentRot.x, currentRot.y + Mathf.LerpAngle(0, angleToTurn, progress), currentRot.z);
            _pivot.transform.rotation = newRot;
            progress += speed;
            yield return null;
        }
        // Correct any over/undershoot
        _pivot.transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y + angleToTurn, currentRot.z);
    }

    void ShowConwayMenu()
    {
        int firstIndex = CurrentOpPage * ItemsPerPage + FirstValidOp;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidOp);

        string polyJson = MasterPoly.PolyToJson();

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            var offset = _pivot.transform.eulerAngles.y * Mathf.Deg2Rad;
            float x = Mathf.Cos((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            float z = Mathf.Sin((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, 0, z), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one * scale;
            var copyPoly = copy.GetComponent<PolyHydra>();
            copyPoly.PolyFromJson(polyJson, false);
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
            copy.GetComponentInChildren<TextMeshPro>().text = newOp.opType.ToString();
            copyPoly.Rebuild();

            var mr = copyPoly.GetComponent<MeshRenderer>();
            IEnumerator coroutine = TransitionIn(mr, transitionDistance, transitionSpeed * 2);
            StartCoroutine(coroutine);
            currentMenuItems[i - firstIndex] = copyPoly.transform;
        }
        currentMenuItems[0].GetComponent<MeshRenderer>().material.SetColor("_Tint", Color.yellow);
    }

    void ShowUniformMenu()
    {
        int firstIndex = (CurrentPolyPage * ItemsPerPage) + FirstValidPoly;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidPoly);

        string polyJson = MasterPoly.PolyToJson();

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            var offset = _pivot.transform.eulerAngles.y * Mathf.Deg2Rad;
            float x = Mathf.Cos((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            float z = Mathf.Sin((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, 0, z), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one * scale;
            var copyPoly = copy.GetComponent<PolyHydra>();
            copyPoly.PolyFromJson(polyJson, false);
            copyPoly.UniformPolyType = (PolyTypes) i;
            copy.GetComponentInChildren<TextMeshPro>().text = copyPoly.UniformPolyType.ToString().Replace("_", " ");
            copyPoly.Rebuild();
            var mr = copyPoly.GetComponent<MeshRenderer>();
            IEnumerator coroutine = TransitionIn(mr, transitionDistance, transitionSpeed * 2);
            StartCoroutine(coroutine);
            currentMenuItems[i - firstIndex] = copyPoly.transform;
        }
        currentMenuItems[0].GetComponent<MeshRenderer>().material.SetColor("_Tint", Color.yellow);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ScrollCurrentMenu(-1, menuSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ScrollCurrentMenu(1, menuSpeed);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangePage(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangePage(1);
        }
        else if (Input.GetKeyDown(KeyCode.O) && CurrentMenu != Menus.Ops)
        {
            ChangeMenu(Menus.Ops);
        }
        else if (Input.GetKeyDown(KeyCode.P) && CurrentMenu != Menus.Polys)
        {
            ChangeMenu(Menus.Polys);
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveLastOp();
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectCurrentMenuItem();
        }
    }

    private void ResetMenuPosition()
    {
        menuIndex = 0;
        _pivot.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    private void SelectCurrentMenuItem()
    {
        switch (CurrentMenu)
        {
            case Menus.Ops:
                MasterPoly.ConwayOperators.Add(currentMenuItems[menuIndex].gameObject.GetComponent<PolyHydra>().ConwayOperators.Last());
                RemoveMenuItems();
                ShowConwayMenu();
                break;
            case Menus.Polys:
                MasterPoly.UniformPolyType = currentMenuItems[menuIndex].gameObject.GetComponent<PolyHydra>().UniformPolyType;
                RemoveMenuItems();
                ShowUniformMenu();
                break;
        }
        MasterPoly.Rebuild();
    }

    private void RemoveLastOp()
    {
        int opCount = MasterPoly.ConwayOperators.Count;
        if (opCount == 0) return;
        MasterPoly.ConwayOperators.RemoveAt(opCount - 1);
        MasterPoly.Rebuild();
    }

    private void ChangeMenu(Menus menu)
    {
        CurrentMenu = menu;
        switch (CurrentMenu)
        {
            case Menus.Ops:
                RemoveMenuItems();
                ResetMenuPosition();
                ShowConwayMenu();
                break;
            case Menus.Polys:
                RemoveMenuItems();
                ResetMenuPosition();
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
                CurrentOpPage = PolyUtils.ActualMod(CurrentOpPage, NumOpPages);
                RemoveMenuItems();
                ResetMenuPosition();
                ShowConwayMenu();
                break;
            case Menus.Polys:
                CurrentPolyPage += direction;
                CurrentPolyPage = PolyUtils.ActualMod(CurrentPolyPage, NumPolyPages);
                RemoveMenuItems();
                ResetMenuPosition();
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
                break;
            case Menus.Polys:
                MasterPoly.UniformPolyType = clickedPoly.UniformPolyType;
                break;
        }
        MasterPoly.Rebuild();
        ChangeMenu(CurrentMenu);
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
        for (var i = 0; i < currentMenuItems.Length; i++)
        {
            Transform child = currentMenuItems[i];
            var poly = child.gameObject.GetComponent<PolyHydra>();
            Handles.Label(child.position + new Vector3(0, .15f, 0), $"{i}: {poly.UniformPolyType.ToString()}");
            if (poly.ConwayOperators.Count == 0) continue;
            var lastOp = poly.ConwayOperators.Last();
            Handles.Label(child.position + new Vector3(0, -.15f, 0), lastOp.opType.ToString());
        }
    }
    #endif

}
