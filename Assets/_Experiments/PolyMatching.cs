using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEditor;
using UnityEngine;

public class PolyMatching : MonoBehaviour
{
    public PolyHydra PolyPrefab;
    public PolyHydra MasterPoly;
    public Transform CenterPivot;
    public float radius = 2.2f;
    public float scale = 0.5f;
    public int ItemsPerPage = 12;
    public int FirstValidPoly;
    public int LastValidPoly;
    public int FirstValidOp;
    public int LastValidOp;
    public Material TransitionMaterial;
    public float transitionSpeed = 0.2f;

    public Dictionary<string, List<PolyPreset>> tally;

    private int CurrentPolyPage;
    private int CurrentOpPage;
    private int NumPolyPages;
    private int NumOpPages;

    public enum Menus
    {
        Polys,
        Ops,
    }

    private Menus CurrentMenu;

    private GameObject _pivot;

    void Start()
    {
        tally = new Dictionary<string, List<PolyPreset>>();
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

    void ClearMenu()
    {
        if (_pivot == null) return;
        foreach (Transform child in _pivot.transform)
        {
            var mr = child.gameObject.GetComponent<MeshRenderer>();
            mr.material = TransitionMaterial;
            IEnumerator coroutine = TransitionOut(mr, 10, transitionSpeed);
            StartCoroutine(coroutine);
        }
//        RecreatePivot();
    }

    IEnumerator TransitionIn(MeshRenderer mr, float limit, float step, Material originalMaterial)
    {
        for (float i = limit; i > 0; i -= step)
        {
            mr.material.SetFloat("_amount", i);
            yield return null;
        }
        mr.gameObject.SetActive(false);
        mr.material = originalMaterial;
    }

    IEnumerator TransitionOut(MeshRenderer mr, float limit, float step)
    {
        for (float i = 0; i < limit; i += step)
        {
            mr.material.SetFloat("_amount", i);
            yield return null;
        }
        mr.gameObject.SetActive(false);
        Destroy(mr.gameObject);
    }

    void ShowConwayMenu()
    {
        ClearMenu();

        int firstIndex = CurrentOpPage * ItemsPerPage + FirstValidOp;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidOp);

        string polyJson = MasterPoly.PolyToJson();

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            var offset = _pivot.transform.eulerAngles.y * Mathf.Deg2Rad;
            float x = Mathf.Sin((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            float z = Mathf.Cos((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, 0, z), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one * scale;
            var copyPoly = copy.GetComponent<PolyHydra>();
            copyPoly.PolyFromJson(polyJson, false);
            var opType = (PolyHydraEnums.Ops) i;
            var newOp = new PolyHydra.ConwayOperator
            {
                opType = opType,
                faceSelections = FaceSelections.All,
                randomize = false,
                amount = PolyHydraEnums.OpConfigs[opType].amountDefault,
                disabled = false
            };
            copyPoly.ConwayOperators.Add(newOp);
            copyPoly.Rebuild();
            var mr = copyPoly.GetComponent<MeshRenderer>();
            var originalMaterial = mr.material;
            mr.material = TransitionMaterial;
            TransitionIn(mr, 8, transitionSpeed, originalMaterial);
            UpdateTally(copyPoly);
        }
    }

    void ShowUniformMenu()
    {
        ClearMenu();
        int firstIndex = (CurrentPolyPage * ItemsPerPage) + FirstValidPoly;
        int lastIndex = firstIndex + ItemsPerPage - 1;
        lastIndex = Math.Min(lastIndex, LastValidPoly);

        string polyJson = MasterPoly.PolyToJson();

        for (int i = firstIndex; i <= lastIndex; i++)
        {
            var offset = _pivot.transform.eulerAngles.y * Mathf.Deg2Rad;
            float x = Mathf.Sin((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            float z = Mathf.Cos((i - firstIndex + offset) / ItemsPerPage * Mathf.PI * 2) * radius;
            GameObject copy = Instantiate(PolyPrefab.gameObject, new Vector3(x, 0, z), Quaternion.identity, _pivot.transform);
            copy.transform.localScale = Vector3.one / 2f;
            var copyPoly = copy.GetComponent<PolyHydra>();
            copyPoly.PolyFromJson(polyJson, false);
            copyPoly.UniformPolyType = (PolyTypes) i;
//            var uniform = Uniform.Uniforms[i];
//            var wythoff = new WythoffPoly(uniform.Wythoff.Replace("p", "5").Replace("q", "2"));
            copyPoly.Rebuild();
            var mr = copyPoly.GetComponent<MeshRenderer>();
            var originalMaterial = mr.material;
            mr.material = TransitionMaterial;
            TransitionIn(mr, 8, transitionSpeed, originalMaterial);
            UpdateTally(copyPoly);
        }
    }

    void UpdateTally(PolyHydra poly)
    {
        var preset = new PolyPreset();
        preset.CreateFromPoly("", poly);
        var faceCounts = new int[16];
        foreach (var f in poly._conwayPoly.Faces)
        {
            faceCounts[f.Sides] += 1;
        }
        string key = $"{string.Join(",", faceCounts)}";
        if (tally.ContainsKey(key))
        {
            string msg = $"{tally[key][0].PolyType} : {preset.PolyType}  ";
            if (tally[key][0].Ops.Length > 0) msg += $"{tally[key][0].Ops.Last().OpType} : ";
            if (preset.Ops.Length > 0) msg += $"{preset.Ops.Last().OpType}";
            Debug.Log(msg);
            Debug.Log("--------------");
        }

        if (tally[key] == null)
        {
            tally[key] = new List<PolyPreset>();
        }
        tally[key].Add(preset);
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
        CurrentMenu = (Menus)PolyUtils.ActualMod((int)CurrentMenu, Enum.GetValues(typeof(Menus)).Length);
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
                CurrentOpPage = PolyUtils.ActualMod(CurrentOpPage, NumOpPages);
                ShowConwayMenu();
                break;
            case Menus.Polys:
                CurrentPolyPage += direction;
                CurrentPolyPage = PolyUtils.ActualMod(CurrentPolyPage, NumPolyPages);
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
        ChangeMenu(0);
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
