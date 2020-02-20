using UnityEngine;


public class PolyAnimate : MonoBehaviour
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
            if (!_poly.opconfigs[op.opType].usesAmount) continue;
            isAnimating = true;

            float sinAnimOffset = 0, audioOffsetLow = 0, audioOffsetMid = 0, audioOffsetHigh = 0;

            if (op.audioLowAmount != 0 || op.audioMidAmount != 0 || op.audioHighAmount !=0)
            {
                audioOffsetLow = GetComponent<PolyAudio>().AmountLow;
                audioOffsetLow -= op.audioLowAmount / 2f;
                audioOffsetLow *= op.audioLowAmount;

                audioOffsetMid = GetComponent<PolyAudio>().AmountMid;
                audioOffsetMid -= op.audioMidAmount / 2f;
                audioOffsetMid *= op.audioMidAmount;

                audioOffsetHigh = GetComponent<PolyAudio>().AmountHigh;
                audioOffsetHigh -= op.audioHighAmount / 2f;
                audioOffsetHigh *= op.audioHighAmount;
            }

            if (op.animationAmount != 0)
            {
                var rate = op.animationRate;
                sinAnimOffset = Mathf.Sin(frame * rate * adjustment);
                sinAnimOffset *= op.animationAmount;
            }

            float totalOffset = audioOffsetLow + audioOffsetMid + audioOffsetHigh + sinAnimOffset;
            totalOffset = Mathf.Round(totalOffset * 100) / 100f;
            op.animatedAmount = op.amount + totalOffset;
            _poly.ConwayOperators[i] = op;
            frame++;
        }

        if (isAnimating) _poly.Rebuild();  // Something animated so let's rebuild
    }
}
