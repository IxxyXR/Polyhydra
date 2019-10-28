using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorScroller : MonoBehaviour
{
    public float foo = 10f;
    public Transform Floor;
    private Material FloorMat;

    void Start()
    {
        FloorMat = Floor.GetComponent<MeshRenderer>().material;

    }

    void LateUpdate()
    {
        Floor.transform.position = new Vector3(transform.position.x, Floor.transform.position.y, transform.position.z);
        var texScale = FloorMat.GetTextureScale("_BaseColorMap");
        texScale = -(Floor.localScale.x * texScale)/foo;
        FloorMat.SetTextureOffset("_BaseColorMap", new Vector2(transform.position.x / texScale.x, transform.position.z / texScale.y));

    }
}
