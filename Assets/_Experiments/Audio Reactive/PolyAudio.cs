using UnityEngine;


public class PolyAudio : MonoBehaviour
{

    public enum SampleType
    {
        Peak,
        PeakDb,
        RMS,
        RMSDb,
    }

    public int OpIndex;
    public float Scale = -0.05f;
    public float Offset = 0.5f;
    public float DecayTime = 1f;
    public float TriggerTheshold = 0.5f;
    public SampleType Sample;
    public Lasp.FilterType Filter = Lasp.FilterType.LowPass;
    public AnimationCurve Curve;
    public int RenderEvery = 3;

    private float _lastTriggerTime = 99999f;
    private float _maxLevel = 0.1f;
    private PolyHydra _poly;
    private float _amount;
    
    
    void Start()
    {
        _poly = GetComponent<PolyHydra>();
    }

    void Update()
    {
        if (Time.frameCount % RenderEvery != 0) return;
        if (OpIndex >= _poly.ConwayOperators.Count) return;
        var op = _poly.ConwayOperators[OpIndex];
        if (op.disabled) return;
        if (!_poly.opconfigs[op.opType].usesAmount) return;
        float rawValue = 0;
        switch (Sample)
        {
            case SampleType.Peak:
                rawValue = Lasp.MasterInput.GetPeakLevel(Filter);
                break;
            case SampleType.PeakDb:
                rawValue = Lasp.MasterInput.GetPeakLevelDecibel(Filter);
                break;
            case SampleType.RMS:
                rawValue = Lasp.MasterInput.CalculateRMS(Filter);
                break;
            case SampleType.RMSDb:
                rawValue = Lasp.MasterInput.CalculateRMSDecibel(Filter);
                break;
        }

        rawValue = Mathf.Abs(rawValue);
        if (rawValue > 254) rawValue = 0;  // Ignore spike on start
        if (rawValue > _maxLevel) _maxLevel = rawValue;
        _maxLevel *= .95f;
//        _maxLevel = _maxLevel < 0.1f ? 0.1f : _maxLevel;
        float peak = rawValue / _maxLevel;
        if (peak > TriggerTheshold)
        {
            _lastTriggerTime = Time.time;
        }
        float timeSinceTrigger = Time.time - _lastTriggerTime;
        _amount = Mathf.Lerp(0, 1, (DecayTime - timeSinceTrigger) / DecayTime);
        Debug.Log($"MaxLevel: {_maxLevel} peak: {peak} amount: {_amount}");
        _amount = Curve.Evaluate(_amount);
        _amount *= Scale;
        _amount += Offset;
        _amount = Mathf.Round(_amount * 100) / 100f;
        op.amount = _amount;
        _poly.ConwayOperators[OpIndex] = op;
        _poly.Rebuild();
    }
}
