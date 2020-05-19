using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using zCode.zMesh;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;

public class UfFace {

    public Face ID;
    public List<UfFace> Children;

    public UfFace(Face ID)
    {
        this.ID = ID;
        this.Children = new List<UfFace>();
    }

    public List<UfFace> AddChild(Face c)
    {
        Children.Add(new UfFace(c));
        return Children;
    }
}