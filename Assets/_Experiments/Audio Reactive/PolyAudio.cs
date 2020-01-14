using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PolyAudio : MonoBehaviour
{

    public int OpIndex;
    public float Scale = -0.05f;
    public float Offset = 0.5f;
    public int RenderEvery = 3;
    //private int Width = 200;
    
    
    //private float[] _array;
    private PolyHydra poly;
    private float amount;
    
    
    void Start()
    {
        poly = GetComponent<PolyHydra>();
        //_array = new float[Width];
    }

    void Update()
    {
        if (Time.frameCount % RenderEvery != 0) return;
        //Lasp.MasterInput.RetrieveWaveform(Lasp.FilterType.LowPass, _array);
        float peak = Lasp.MasterInput.GetPeakLevelDecibel(Lasp.FilterType.LowPass);
        //float avg = _array.Select(x=>Mathf.Abs(x)).Average();
        amount = Mathf.Abs(peak) * Scale + Offset;
        var op = poly.ConwayOperators[OpIndex];
        op.amount = amount;
        poly.ConwayOperators[OpIndex] = op;
        poly.Rebuild();
    }
}
