using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class PolyTransitions : MonoBehaviour
{

    public PolyHydra poly1;
    public PolyHydra poly2;
    public float speed = 5f;
    public float amplitude = 0.5f;
    private Vector3 Poly1Scale;
    private Vector3 Poly2Scale;

    // Start is called before the first frame update
    void Start()
    {
        Poly1Scale = poly1.transform.localScale;
        Poly2Scale = poly2.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        float t = Time.time * speed;
        poly1.transform.localScale = amplitude * (Mathf.Sin(t + Mathf.PI) + 1f) * Poly1Scale;
        poly2.transform.localScale = amplitude * (Mathf.Sin(t) + 1f) * Poly2Scale;
    }
}
