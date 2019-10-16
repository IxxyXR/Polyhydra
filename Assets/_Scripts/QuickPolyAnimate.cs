using System;
using System.Collections.Generic;
using UnityEngine;

public class QuickPolyAnimate : MonoBehaviour
{
    [Serializable]
    public struct PolyAnimatorItem
    {
        public int OpIndex;
        public float rate;
        public float amplitude;
        public float offset;
    }

    public List<PolyAnimatorItem> PolyAnimators;
    private List<float> _originalAmounts;

    private PolyHydra _poly;

    void Start()
    {
        _poly = gameObject.GetComponent<PolyHydra>();
        InvokeRepeating(nameof(AnimateNext), 0f, 0.08f);
        _originalAmounts = new List<float>();
        foreach (var op in _poly.ConwayOperators)
        {
            _originalAmounts.Add(op.amount);
        }
    }

    public void AnimateNext()
    {
        foreach (var item in PolyAnimators)
        {
            var op = _poly.ConwayOperators[item.OpIndex];
            float amount = Mathf.Sin(Time.time * item.rate) * item.amplitude + item.offset;
            amount = Mathf.Round(amount * 1000) / 1000f;
            op.amount = _originalAmounts[item.OpIndex] + amount;
            _poly.ConwayOperators[item.OpIndex] = op;
        }

        _poly.Rebuild();
    }
}
