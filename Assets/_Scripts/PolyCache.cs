using System;
using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using Wythoff;

public class PolyCache : MonoBehaviour
{
    public int MAX_CACHE_LENGTH = 5000;

    private struct ConwayCacheEntry
    {
        public ConwayPoly conway;
        public long timestamp;

        public ConwayCacheEntry(ConwayPoly c, long t)
        {
            conway = c;
            timestamp = t;
        }
    }

    private Dictionary<string, WythoffPoly> WythoffCache;
    private Dictionary<int, ConwayCacheEntry> ConwayCache;
    private Dictionary<int, Mesh> MeshCache;


    void InitCacheIfNeeded()
    {
        if (WythoffCache==null) WythoffCache = new Dictionary<string, WythoffPoly>();
        if (ConwayCache==null) ConwayCache = new Dictionary<int, ConwayCacheEntry>();
        if (MeshCache==null) MeshCache = new Dictionary<int, Mesh>();
    }

    public WythoffPoly GetWythoff(string key)
    {
        InitCacheIfNeeded();

        WythoffPoly value = null;
        if (WythoffCache.ContainsKey(key))
        {
            value = WythoffCache[key];
        }
        return value;
    }

    public void SetWythoff(string key, WythoffPoly value)
    {
        CullWythoff();
        WythoffCache[key] = value;
    }

    public ConwayPoly GetConway(int key)
    {
        InitCacheIfNeeded();
        ConwayPoly value = null;
        if (ConwayCache.ContainsKey(key))
        {
            value = ConwayCache[key].conway;
        }
        return value;
    }

    public void SetConway(int key, ConwayPoly value)
    {
        CullOlderConway();
        var cacheEntry = new ConwayCacheEntry(value, DateTime.UtcNow.Ticks);
        ConwayCache[key] = cacheEntry;
    }

    public Mesh GetMesh(int key)
    {
        InitCacheIfNeeded();
        Mesh value = null;
        if (MeshCache.ContainsKey(key))
        {
            value = MeshCache[key];
        }
        return value;
    }

    public void SetMesh(int key, Mesh value)
    {
        CullMesh();
        var cacheEntry = value;
        MeshCache[key] = cacheEntry;
    }

    public void CullWythoff()
    {
        // Todo Use a proper evicion pollicy
        if (WythoffCache.Count > MAX_CACHE_LENGTH)
        {
            Debug.LogWarning("Wythoff cache cull");
            WythoffCache = WythoffCache.Skip(MAX_CACHE_LENGTH/2).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

    public void CullOlderConway()
    {
        if (ConwayCache.Count > MAX_CACHE_LENGTH)
        {
            // TODO actually test performance on this
            var ordered = ConwayCache.OrderBy(kv => kv.Value.timestamp);
            var half = ConwayCache.Count/2;
            ConwayCache = ordered.Skip(half).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

    public void CullMesh()
    {
        // Todo Use a proper eviction pollicy
        if (MeshCache.Count > MAX_CACHE_LENGTH)
        {
            Debug.LogWarning("Conway cache cull");
            MeshCache = MeshCache.Skip(MAX_CACHE_LENGTH/2).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

}
