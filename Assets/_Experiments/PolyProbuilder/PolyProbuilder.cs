using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
//using UnityEditor.ProBuilder;
using UnityEngine.ProBuilder;
//using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine;

public class PolyProbuilder : MonoBehaviour
{
    [ContextMenu("Probuilder Object")]
    public void Foo()
    {
        var poly = gameObject.GetComponent<PolyHydra>();
        var pmesh = new ProBuilderMesh();

    }

}
