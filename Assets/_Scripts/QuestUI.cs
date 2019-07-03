using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class QuestUI : MonoBehaviour {
    
    public PolyHydra poly;
    private Transform polyRoot;
    private Rigidbody polyRigidbody;
    public PolyPresets presets;
    public AppearancePresets aPresets;
    
    public float TweenAmount;
    public Transform StartTween;
    public Transform EndTween;
    public Transform EndTweenResetPosition;

    private int presetIndex;
    private int apresetIndex;

    void Start()
    {
        gameObject.GetComponent<CanvasGroup>().alpha = 0;
        polyRoot = poly.transform.root;
        polyRigidbody = polyRoot.gameObject.GetComponent<Rigidbody>();
        ChangeTween(-1);
    }
    
    void Update ()
    {
        #if OCULUS_VR_ENABLED
        if (OVRInput.GetDown(OVRInput.RawButton.X))
            NextPreset();
        else if (OVRInput.GetDown(OVRInput.RawButton.Y))
            PrevPreset();
        else if (OVRInput.GetDown(OVRInput.RawButton.A))
            ClearOps();
        else if (OVRInput.GetDown(OVRInput.RawButton.B))
            RandomizePolyhedra();
        else if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
            NextAPreset();
        else if (OVRInput.GetDown(OVRInput.RawButton.LHandTrigger))
            PrevAPreset();
        else if (OVRInput.GetDown(OVRInput.RawButton.Start))
        {
            EndTween.position = EndTweenResetPosition.position;
            ChangeTween(1f);
        }
        
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickLeft))
            ChangeTween(.001f);
        else if (OVRInput.Get(OVRInput.RawButton.LThumbstickRight))
            ChangeTween(-.001f);

        if (OVRInput.Get(OVRInput.RawAxis2D.RThumbstick).sqrMagnitude > 0.01f)
        {
            var amount = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            polyRigidbody.angularVelocity = new Vector3(amount.x, amount.y, 0);
        }
        #endif
        
    }
                                    
    public void ChangeTween(float amount)
    {
        TweenAmount += amount;
        if (TweenAmount < 0) TweenAmount = 0f;
        if (TweenAmount > 1) TweenAmount = 1f;
        polyRoot.position = Vector3.Lerp(StartTween.position, EndTween.position, TweenAmount);
        polyRoot.localScale = Vector3.Lerp(StartTween.localScale, EndTween.localScale, TweenAmount);
    }

    public void NextPreset()
    {
        presetIndex += 1;
        presetIndex %= presets.Items.Count;
        presets.Items[presetIndex].ApplyToPoly(ref poly, aPresets);
        poly.MakePolyhedron();
    }
    
    public void PrevPreset()
    {
        presetIndex -= 1;
        if (presetIndex < 0) presetIndex = presets.Items.Count - 1;
        presets.Items[presetIndex].ApplyToPoly(ref poly, aPresets);
        poly.MakePolyhedron();
    }
    
    public void NextAPreset()
    {
        apresetIndex += 1; 
        apresetIndex %= aPresets.Items.Count;
        aPresets.ApplyPresetToPoly(apresetIndex);
    }
    
    public void PrevAPreset()
    {
        apresetIndex -= 1;
        if (apresetIndex < 0) apresetIndex = aPresets.Items.Count - 1;
        aPresets.ApplyPresetToPoly(apresetIndex);
    }

    public void ClearOps()
    {
        poly.ConwayOperators.Clear();
        poly.MakePolyhedron();
    }

    public void RandomizePolyhedra()
    {
        poly.PolyType = (PolyTypes)Random.Range(1, Enum.GetValues(typeof(PolyTypes)).Length);
        poly.ConwayOperators.Clear();
        if (!((IList) poly.NonOrientablePolyTypes).Contains((int) poly.PolyType)) // Don't add Conway ops to non-orientable polys
        {
            if (Random.value > 0.1)
            {
                poly.AddRandomOp();
                if (Random.value > 0.2)
                {
                    poly.AddRandomOp();
                    if (Random.value > 0.3)
                    {
                        poly.AddRandomOp();
                    }
                }
            }
        }
        poly.MakePolyhedron();
        aPresets.ApplyPresetToPoly(Random.Range(0, aPresets.Items.Count));
    }   
}
