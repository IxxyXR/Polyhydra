using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;


public class PolyAnimator : MonoBehaviour
{
    private List<Mesh> _skinnedMeshes;
    public bool IsAnimating;
    private SkinnedMeshRenderer _smr;
    public int morphDuration;
    private int _framesThisBlend;
    private int _currentMeshIndex;
    private int _currentBlendShapeIndex;

    void Start()
    {
        InitMeshes();
        InitAnim();
    }

    void InitMeshes()
    {
        _skinnedMeshes = new List<Mesh>();
        var poly = gameObject.GetComponent<PolyHydra>();
        var conway = new ConwayPoly(poly.WythoffPoly);
        
        var previousMesh = new Mesh();
        var currentMesh = new Mesh();

        for (var i = 0; i < poly.ConwayOperators.Count; i++)
        {
            var op = poly.ConwayOperators[i];
            conway = PolyHydra.ApplyOp(conway, op);
            poly.BuildMeshFromConwayPoly(poly.TwoSided);
            currentMesh = poly.GetComponent<MeshFilter>().mesh;
            if (previousMesh.vertices.Length == 0)
            {
                // First time round
                previousMesh = currentMesh;
                continue;
            }

            if (currentMesh.vertices.Length == previousMesh.vertices.Length)
            {
                var deltaVertices = currentMesh.vertices.Zip(previousMesh.vertices, (a, b) => a - b).ToArray();
                var deltaNormals = currentMesh.normals.Zip(previousMesh.normals, (a, b) => a - b).ToArray();
                var deltaTangents = currentMesh.tangents.Zip(previousMesh.tangents, (a, b) => a - b)
                    .Select(el => new Vector3(el.x, el.y, el.z)).ToArray(); // Dunno why we need to convert Vector4 to 3
                                                                            // but we're not using tangents in any case
                currentMesh.AddBlendShapeFrame("blendshape" + i, 1, deltaVertices, deltaNormals, deltaTangents);
            }
            else
            {
                _skinnedMeshes.Add(currentMesh);
                previousMesh = currentMesh;
            }
        }
    }
    
    void InitAnim()
    {
        IsAnimating = true;
        gameObject.GetComponent<MeshRenderer>().enabled = false;
        _smr = gameObject.GetComponent<SkinnedMeshRenderer>();

        if (_smr == null)
        {
            _smr = gameObject.AddComponent<SkinnedMeshRenderer>();            
        }
        
        _framesThisBlend = 0;
        _currentMeshIndex = 0;
        _currentBlendShapeIndex = 0;
    }

    void Update()
    {
        if (!IsAnimating) return;

        _framesThisBlend++;
        
        if (_smr.sharedMesh == null || _framesThisBlend >= morphDuration) // Start animating a new Mesh
        {
            _smr.sharedMesh = _skinnedMeshes[_currentMeshIndex];
            _framesThisBlend = 0;
            _currentMeshIndex++;
            _currentMeshIndex %= _skinnedMeshes.Count;
        }
        
        var blendAmount =_framesThisBlend / morphDuration;
        if (_currentBlendShapeIndex > 0)
        {
            _smr.SetBlendShapeWeight(_currentBlendShapeIndex - 1, 1f - blendAmount);            
        }
        _smr.SetBlendShapeWeight(_currentBlendShapeIndex, blendAmount);
        _framesThisBlend %= morphDuration;
    }
}
