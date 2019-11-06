using System;
using System.Collections.Generic;
using UnityEngine;

public class QuickPolyAnimate : MonoBehaviour
{

    public float updateFrequency = 0.1f;

    [Serializable]
    public struct PolyAnimatorItem
    {
        public int OpIndex;
        public float rate;
        public float amplitude;
        public float offset;
    }

//    private List<float> _originalAmounts;
    private PolyUI polyUi;

    private PolyHydra _poly;

    void Start()
    {
        _poly = gameObject.GetComponent<PolyHydra>();
        InvokeRepeating(nameof(AnimateNext), 0f, updateFrequency);
//        _originalAmounts = new List<float>();
//        foreach (var op in _poly.ConwayOperators)
//        {
//            _originalAmounts.Add(op.amount);
//        }

    }

    public void AnimateNext()
    {
        // The old way
//        foreach (var item in PolyAnimators)
//        {
//            var op = _poly.ConwayOperators[item.OpIndex];
//            float amount = Mathf.Sin(Time.time * item.rate) * item.amplitude + item.offset;
//            amount = Mathf.Round(amount * 1000) / 1000f;
//            if (item.OpIndex >= _originalAmounts.Count) continue;
//            op.amount = _originalAmounts[item.OpIndex] + amount;
//            _poly.ConwayOperators[item.OpIndex] = op;
//        }

        // Quick and dirty hack to grab anim parameters directly from the UI
        // Evebtually I'll add them to the op params properly.
        var opParent = _poly.polyUI.OpContainer.gameObject.transform;
        foreach (Transform opUiTransform in opParent)
        {
            var opPrefabManager = opUiTransform.gameObject.GetComponent<OpPrefabManager>();
            if (!opPrefabManager.ToggleAnimate.isOn) continue;
            var opIndex = opPrefabManager.Index;
            var op = _poly.ConwayOperators[opIndex];
            var _originalAmount = float.Parse(opPrefabManager.AmountInput.text);
            var amplitude = float.Parse(opPrefabManager.AnimAmountInput.text);
            var rate = float.Parse(opPrefabManager.AnimRateInput.text);
            float amount = Mathf.Sin(Time.time * rate) * amplitude;
            amount = Mathf.Round(amount * 1000) / 1000f;
            op.amount = _originalAmount + amount;
            _poly.ConwayOperators[opIndex] = op;
        }

        _poly.Rebuild();
    }
}
