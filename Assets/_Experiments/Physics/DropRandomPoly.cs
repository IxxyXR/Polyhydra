using System.Linq;
using UnityEngine;
using Wythoff;


public class DropRandomPoly : MonoBehaviour
{
    public Transform PolyPrefab;
    public Material[] Materials;

    private PolyHydra recentPoly;

    void Start()
    {
        InvokeRepeating(nameof(DoDrop), 0f, 1f);
    }

    void DoDrop()
    {
        var poly = Instantiate(PolyPrefab, transform).GetComponent<PolyHydra>();
        float polyRandomValue = Random.value;
        int polyTypeIndex = 0;
        float probabilityOfAddingOp = 0;
        if (polyRandomValue < 0.00000025)
        {
            // Johnson
            poly.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
        }
        else if (true || polyRandomValue >= 0.25 && polyRandomValue < 0.5f)
        {
            // Platonic
            poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            var foo = Uniform.Platonic.ToArray();
            poly.WythoffSymbol = foo.RandomElement().Wythoff;
            probabilityOfAddingOp = 0.1f;
        }
        else if (polyRandomValue >= 0.5 && polyRandomValue < 0.75f)
        {
            // Archimedean
            poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            poly.WythoffSymbol = Uniform.Archimedean.RandomElement().Wythoff;
            probabilityOfAddingOp = 0.1f;
        }
        else
        {
            // Any Uniform except prisms
            poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            polyTypeIndex = (int)(Random.value * 74) + 5;
            poly.UniformPolyType = (PolyTypes) (polyTypeIndex);
            probabilityOfAddingOp = 0.1f;
        }

        poly.enableThreading = false;
        poly.enableCaching = true;
        poly.Rebuild();
        poly.gameObject.GetComponent<MeshRenderer>().material = Materials[Random.Range(0, Materials.Length)];
        recentPoly = poly;
        Invoke(nameof(ActivatePolyPhysics), 0.025f);
    }

    void ActivatePolyPhysics()
    {
        var collider = recentPoly.gameObject.AddComponent<MeshCollider>();
        collider.convex = true;
        var mf = recentPoly.gameObject.GetComponent<MeshFilter>();
        collider.sharedMesh = mf.mesh;
        var rb = recentPoly.gameObject.AddComponent<Rigidbody>();

    }

}
