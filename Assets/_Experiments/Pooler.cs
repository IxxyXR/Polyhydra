using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Pooler : MonoBehaviour {
    private static Pooler _instance;
    private readonly Dictionary<PooledObjects, GameObject> _lookupDictionary = new Dictionary<PooledObjects, GameObject>();
    private readonly Dictionary<PooledObjects, List<GameObject>> _poolDictionary = new Dictionary<PooledObjects, List<GameObject>>();

    [SerializeField] private bool _debugWarnings;

    public void Awake() {
        _instance = this;
       
        var possibleValues = Enum.GetValues(typeof(PooledObjects)).Cast<PooledObjects>().ToList();
        var prefabs = Resources.LoadAll<GameObject>("Pooled");

        foreach (var prefab in prefabs) {
            Enum.TryParse(prefab.name, out PooledObjects enumValue); // Try get the pooled object enum value
            _lookupDictionary.Add(enumValue, prefab); // Add it to our lookup dictionary for quick instantiating
            _poolDictionary.Add(enumValue, new List<GameObject>()); // Create the pool
            possibleValues.RemoveAll(e => e == enumValue); // Remove the enum from the possible values
        }

        if (_debugWarnings && possibleValues.Any()) {
            if (possibleValues[0] == PooledObjects.Nothing && possibleValues.Count == 1) return;
            var builder = new StringBuilder();
            builder.Append("A prefab resource was not found for: ");
            foreach (var possibleValue in possibleValues) {
                builder.Append($"{possibleValue}{(possibleValue != possibleValues.Last() ? "," : "")}");
            }

            throw new NotImplementedException(builder.ToString());
        }
    }



    public static T Spawn<T>(PooledObjects pooledObject, Transform parent) {
        return Spawn(pooledObject, parent).GetComponent<T>();
    }

    public static GameObject Spawn(PooledObjects pooledObject)
    {
        return _instance.SpawnFromPool(pooledObject, Vector3.zero, Quaternion.identity);
    }

    public static GameObject Spawn(PooledObjects pooledObject, Transform parent) {
        var obj = _instance.SpawnFromPool(pooledObject, Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(parent);
        obj.transform.position = Vector3.zero;
        return obj;
    }

    public static T Spawn<T>(PooledObjects pooledObject, Vector3 position, Quaternion rotation) {
        return _instance.SpawnFromPool(pooledObject, position, rotation).GetComponent<T>();
    }

    public static GameObject Spawn(PooledObjects pooledObject, Vector3 position, Quaternion rotation) {
        return _instance.SpawnFromPool(pooledObject, position, rotation);
    }

    private GameObject SpawnFromPool(PooledObjects pooledObject, Vector3 position, Quaternion rotation) {
        if (!_poolDictionary.ContainsKey(pooledObject)) {
            Debug.Log($"Pool with type {pooledObject} doesn't exist.");
            return default;
        }

        var objToSpawn = _poolDictionary[pooledObject].FirstOrDefault(g => !g.activeSelf);

        // If we cycle to an already active one, expand the pool
        if (objToSpawn == null) {
            // Create a new object to be pooled
            objToSpawn = Instantiate(_lookupDictionary.First(p => p.Key == pooledObject).Value, position, rotation, transform);

            _poolDictionary[pooledObject].Add(objToSpawn);
        }

        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);
        return objToSpawn;
    }
}

/// <summary>
/// A enumeration of the items you want to pool. The enum names must match the prefabs in the Resources folder exactly.
/// </summary>
[Serializable]
public enum PooledObjects {
    Nothing = 0,

    // Units
    Objects = 1,
    You = 2,
    Want = 3,
    To = 4,
    Pool = 5

    // Effects

    // Misc
}