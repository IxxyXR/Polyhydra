using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using zCode.zMesh;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;

public class OVPair<T1,T2>
{
    public T1 Item1 { get; private set; }
    public T2 Item2 { get; private set; }

    public OVPair(T1 first, T2 second)
    {
        Item1 = first;
        Item2 = second;
    }
}