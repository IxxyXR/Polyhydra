using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolyMorph : MonoBehaviour
{
    [System.Serializable]
    public struct PolyMorphItem
    {
        public int opIndex;
        public float amount;
        public float offset;
        public float frequency;
    }

    private MeshFilter[] polyList;
    private bool initialized;

    public List<PolyMorphItem> PolyMorphItems;

    private SkinnedMeshRenderer sm;


    void Start()
    {
        Invoke(nameof(Initialise), 0);
    }

    public void Initialise()
    {
        polyList = gameObject.GetComponentsInChildren<MeshFilter>();
        sm = gameObject.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = new Mesh();

        mesh = new Mesh();
        mesh.vertices = polyList[0].mesh.vertices;
        mesh.normals = polyList[0].mesh.normals;
        mesh.uv = polyList[0].mesh.uv;
        mesh.triangles = polyList[0].mesh.triangles;
        mesh.tangents = polyList[0].mesh.tangents;
        polyList[0].gameObject.SetActive(false);

        //Debug.Log($"0: {polyList[0].mesh.vertexCount}");

        for (var i = 1; i < polyList.Length; i++)
        {
            //Debug.Log($"{i}: {polyList[i].mesh.vertexCount}");
            mesh.AddBlendShapeFrame(
                i.ToString(),
                i,
                polyList[i].mesh.vertices.Select((val, index) => val - mesh.vertices[index]).ToArray(),
                polyList[i].mesh.normals.Select((val, index) => val - mesh.normals[index]).ToArray(),
                polyList[i].mesh.tangents.Select((val, index) => (Vector3) val - (Vector3) mesh.tangents[index]).ToArray()
            );

            polyList[i].gameObject.SetActive(false);
        }
        sm.sharedMesh = mesh;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;
        foreach (var item in PolyMorphItems)
        {
            var sine = (Mathf.Sin(Time.time * item.frequency) + 1f) / 2f;
            sm.SetBlendShapeWeight(item.opIndex, sine * item.amount + item.offset);
        }
    }
}
