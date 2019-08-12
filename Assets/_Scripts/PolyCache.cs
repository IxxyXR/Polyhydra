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

    void InitCacheIfNeeded()
    {
        if (WythoffCache==null) WythoffCache = new Dictionary<string, WythoffPoly>();
        if (ConwayCache==null) ConwayCache = new Dictionary<int, ConwayCacheEntry>();
    }

    public WythoffPoly Get(string key)
    {
        InitCacheIfNeeded();

        WythoffPoly value = null;
        if (WythoffCache.ContainsKey(key))
        {
            value = WythoffCache[key];
        }
        return value;
    }

    public void Set(string key, WythoffPoly value)
    {
        WythoffCache[key] = value;
    }

    public ConwayPoly Get(int key)
    {
        InitCacheIfNeeded();

        ConwayPoly value = null;
        if (ConwayCache.ContainsKey(key))
        {
            value = ConwayCache[key].conway;
        }
        return value;
    }

    public void Set(int key, ConwayPoly value)
    {
        CullOlder();
        var cacheEntry = new ConwayCacheEntry(value, DateTime.UtcNow.Ticks);
        ConwayCache[key] = cacheEntry;
    }

    public void CullOlder()
    {
        if (ConwayCache.Count > MAX_CACHE_LENGTH)
        {
            // TODO actually test performance on this
            var ordered = ConwayCache.OrderBy(kv => kv.Value.timestamp);
            var half = ConwayCache.Count/2;
            ConwayCache = ordered.Skip(half).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

    }
}
