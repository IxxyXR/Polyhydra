using System;
using Conway;
using UnityEditor;
using UnityEngine;


public class CheckStuff : MonoBehaviour
{
    [MenuItem("Tools/Check Stuff")]
    static void DoCheckStuff()
    {
        foreach (Ops op in Enum.GetValues(typeof(Ops)))
        {
            if (!PolyHydraEnums.OpConfigs.ContainsKey(op))
            {
                Debug.Log($"Missing OpConfig for: {op}");
            }
            // Debug.Log($"{PolyHydraEnums.OpConfigs[op].amountSafeMin}");
        }
    }
}
