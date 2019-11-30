using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class RandomElementExtensions
{
    public static T RandomElement<T>(this IEnumerable<T> enumerable)
    {
        int index = Random.Range(0, enumerable.Count());
        Debug.Log($"Index: {index} Count: {enumerable.Count()}");
        return enumerable.ElementAt(index);
    }

}
