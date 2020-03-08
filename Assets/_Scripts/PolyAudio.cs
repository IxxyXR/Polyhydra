using System;
using Lasp;
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

    public SampleType Sample;

    public int RenderEvery = 3;
    [Range(.5f, 1.5f)]
    public float MaxLevelDropoff = .98f;

    [Header("Low Pass")]
    public AnimationCurve CurveLow;
    public float ScaleLow = -0.05f;
    public float OffsetLow = 0.5f;
    public float DecayTimeLow = 1f;
    public float TriggerThesholdLow = 0.5f;

    private float _lastTriggerTimeLow = 99999f;
    private float _maxLevelLow = 0.1f;
    [NonSerialized] public float AmountLow;

    [Header("Band Pass")]
    public AnimationCurve CurveMid;
    public float ScaleMid = -0.05f;
    public float OffsetMid = 0.5f;
    public float DecayTimeMid = 1f;
    public float TriggerThesholdMid = 0.5f;

    private float _lastTriggerTimeMid = 99999f;
    private float _maxLevelMid = 0.1f;
    [NonSerialized] public float AmountMid;

    [Header("High Pass")]
    public AnimationCurve CurveHigh;
    public float ScaleHigh = -0.05f;
    public float OffsetHigh = 0.5f;
    public float DecayTimeHigh = 1f;
    public float TriggerThesholdHigh = 0.5f;

    private float _lastTriggerTimeHigh = 99999f;
    private float _maxLevelHigh = 0.1f;
    [NonSerialized] public float AmountHigh;

    void Update()
    {
        if (Time.frameCount % RenderEvery != 0) return;
        AmountLow = Calc(FilterType.LowPass, ref _maxLevelLow, ref _lastTriggerTimeLow, TriggerThesholdLow, DecayTimeLow, CurveLow, ScaleLow, OffsetLow);
        AmountMid = Calc(FilterType.BandPass, ref _maxLevelMid, ref _lastTriggerTimeMid, TriggerThesholdMid, DecayTimeMid, CurveMid, ScaleMid, OffsetMid);
        AmountHigh = Calc(FilterType.HighPass, ref _maxLevelHigh, ref _lastTriggerTimeHigh, TriggerThesholdHigh, DecayTimeHigh, CurveHigh, ScaleHigh, OffsetHigh);
    }

    public float Calc(FilterType filter, ref float maxLevel, ref float lastTriggerTime, float triggerTheshold,
        float decayTime, AnimationCurve curve, float scale, float offset)
    {
        float rawValue = 0;
        switch (Sample)
        {
            case SampleType.Peak:
                rawValue = MasterInput.GetPeakLevel(filter);
                break;
            case SampleType.PeakDb:
                rawValue = MasterInput.GetPeakLevelDecibel(filter);
                break;
            case SampleType.RMS:
                rawValue = MasterInput.CalculateRMS(filter);
                break;
            case SampleType.RMSDb:
                rawValue = MasterInput.CalculateRMSDecibel(filter);
                break;
        }

        rawValue = Mathf.Abs(rawValue);
        if (rawValue > 254) rawValue = 0;  // Ignore spike on start
        if (rawValue > maxLevel) maxLevel = rawValue;
        maxLevel *= MaxLevelDropoff;
        float peak = rawValue / maxLevel;
        if (peak > triggerTheshold)
        {
            lastTriggerTime = Time.time;
        }
        float timeSinceTrigger = Time.time - lastTriggerTime;
        float result;
        result = Mathf.Lerp(0, 1, (decayTime - timeSinceTrigger) / decayTime);
        result = curve.Evaluate(result);
        result *= scale;
        result += offset;
        return result;
    }
}
