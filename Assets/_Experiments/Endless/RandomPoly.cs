using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPoly : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var poly = gameObject.GetComponent<PolyHydra>();
        poly.UniformPolyType = (PolyTypes) (Random.value * 10 + 5);
        poly.Rebuild();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
