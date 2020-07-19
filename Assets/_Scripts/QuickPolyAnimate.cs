    using System.Collections.Generic;
using UnityEngine;


public class QuickPolyAnimate : MonoBehaviour
{

    public float updateFrequency = 0.1f;

    private PolyUI polyUi;
    private int frame;
    private PolyHydra _poly;

    void Start()
    {
        _poly = gameObject.GetComponent<PolyHydra>();
        InvokeRepeating(nameof(AnimateNext), 0f, updateFrequency);
    }

    public void AnimateNext()
    {
        float adjustment = Mathf.PI / 60;
        bool isAnimating = false;  // Set to true if any op is animated
        for (var i = 0; i < _poly.ConwayOperators.Count; i++)
        {
            var op = _poly.ConwayOperators[i];
            if (!op.animate) continue;
            if (op.disabled) continue;
            if (!PolyHydraEnums.OpConfigs[op.opType].usesAmount) continue;
            isAnimating = true;
            var amplitude = op.animationAmount;
            var rate = op.animationRate;
            float offset = Mathf.Sin(frame * rate * adjustment) * amplitude;
            offset = Mathf.Round(offset * 100) / 100f;
            op.animatedAmount = op.amount + offset;
            _poly.ConwayOperators[i] = op;
            frame++;
        }

        if (isAnimating) _poly.Rebuild();  // Something animated so let's rebuild
    }
}
