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
    private int frame;

    private PolyHydra _poly;
    private HashSet<string> tester;
    [Multiline] public string logger;
    void Start()
    {
        _poly = gameObject.GetComponent<PolyHydra>();
        InvokeRepeating(nameof(AnimateNext), 0f, updateFrequency);
//        _originalAmounts = new List<float>();
//        foreach (var op in _poly.ConwayOperators)
//        {
//            _originalAmounts.Add(op.amount);
//        }
        tester = new HashSet<string>();
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
        bool isAnimating = false;  // Set to true if any op is animated
        var opParent = _poly.polyUI.OpContainer.gameObject.transform;
        foreach (Transform opUiTransform in opParent)
        {
            var opPrefabManager = opUiTransform.gameObject.GetComponent<OpPrefabManager>();
            if (!opPrefabManager.ToggleAnimate.isOn) continue;
            isAnimating = true;
            var opIndex = opPrefabManager.Index;
            var op = _poly.ConwayOperators[opIndex];
            var _originalAmount = float.Parse(opPrefabManager.AmountInput.text);
            var amplitude = float.Parse(opPrefabManager.AnimAmountInput.text);
            var rate = float.Parse(opPrefabManager.AnimRateInput.text);
            float amount = Mathf.Sin(frame * rate * 0.05f) * amplitude;
            amount = Mathf.Round(amount * 100) / 100f;
            op.amount = _originalAmount + amount;
            _poly.ConwayOperators[opIndex] = op;
            frame++;
        }
        if (isAnimating) _poly.Rebuild();  // Something animated so let's rebuild
    }
}
