using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(ContinuousAnimationController))]
public class ContinuousAnimation : MonoBehaviour
{
    public int ControlledOpIndex = 0;
    public float Rate = 0.1f;
    public int RenderEvery = 2;
    
    private int FrameCount;
    private PolyHydra poly;
    private int CurrentOpIndex;
    private bool amountDecreasing = false;
    private State currentState;
    private Junction nextJunction;
    private int junctionCount;
    private Junction lastJunction;
    private ContinuousAnimationController controller;

    public enum State
    {
        Seed,
        Ambo,
        Dual,
        Ortho,
        Kis,
        Subdivide,
    }

    public struct Junction
    {
        public PolyHydra.Ops Op;
        public float FinalAmount;
        public State FinalState;
        
        public Junction(PolyHydra.Ops o, float a, State s) {Op = o; FinalAmount = a; FinalState = s; }
    }

    public Dictionary<State, List<Junction>> Transitions;

    void Start()
    {
        poly = GetComponent<PolyHydra>();
        controller = GetComponent<ContinuousAnimationController>();
        //poly.ConwayOperators.Clear();
        FrameCount = 0;

        Transitions = new Dictionary<State, List<Junction>>();
        
        Transitions[State.Seed] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Expand, 1, State.Dual),
            new Junction(PolyHydra.Ops.Propeller, 0.5f, State.Subdivide),
            new Junction(PolyHydra.Ops.Truncate, 0.5f, State.Ambo),
            new Junction(PolyHydra.Ops.Loft, 0.5f, State.Kis),
            new Junction(PolyHydra.Ops.Whirl, 0.5f, State.Ortho),
        };
        Transitions[State.Ambo] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Zip, 1, State.Dual),
            new Junction(PolyHydra.Ops.Truncate, 0, State.Seed),
        };
        Transitions[State.Dual] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Zip, 0, State.Ambo),
            new Junction(PolyHydra.Ops.Expand, 0, State.Seed),
        };
        Transitions[State.Ortho] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Stake, 1, State.Kis),
            new Junction(PolyHydra.Ops.Gyro, 0f, State.Kis),
            new Junction(PolyHydra.Ops.Quinto, 0, State.Subdivide),
            new Junction(PolyHydra.Ops.Whirl, 0f, State.Seed),
        };
        Transitions[State.Kis] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Loft, 0, State.Seed),
            new Junction(PolyHydra.Ops.Stake, 0, State.Ortho),
            new Junction(PolyHydra.Ops.Lace, 0, State.Subdivide),
            new Junction(PolyHydra.Ops.Gyro, 0.5f, State.Ortho),
        };
        Transitions[State.Subdivide] = new List<Junction>
        {
            new Junction(PolyHydra.Ops.Propeller, 0, State.Seed),
            new Junction(PolyHydra.Ops.Lace, 1, State.Kis),
            new Junction(PolyHydra.Ops.Quinto, 1, State.Ortho),
        };
        junctionCount = Transitions.Values.Sum(x => x.Count);

        nextJunction = PickRandomJunction();
    }

    private Junction PickRandomJunction()
    {
        int target = Random.Range(0, junctionCount);
        int i = 0;
        foreach (var t in Transitions)
        {
            foreach (var j in t.Value)
            {
                if (i == target) return j;
                i++;
            }
        }
        // Should never happen
        return Transitions.Values.ToArray()[0][0];
    }

    private Junction FindNextJunction(State fromState)
    {
        var t = Transitions[fromState];
        Junction j = t[Random.Range(0, t.Count)];
        while (j.Op == lastJunction.Op)
        {
            j = t[Random.Range(0, t.Count)];
        }
//        Debug.Log($"From: {fromState} To: {j.FinalState} using {j.Op} Decrease: {j.FinalAmount==0}");
        lastJunction = j;
        return j;
    }

    void Update()
    {
        if (Time.renderedFrameCount % RenderEvery != 0) return;
        
        FrameCount++;
        float progress = FrameCount * Rate;
        int opIndex = Mathf.FloorToInt(progress);
        float amount = progress - opIndex;
        //Debug.Log($"FrameCount: {FrameCount} progress: {progress} opIndex: {opIndex} amount: {amount} CurrentOpIndex: {CurrentOpIndex}");
        PolyHydra.ConwayOperator op;
        if (opIndex != CurrentOpIndex || poly.ConwayOperators.Count == 0)
        {
            // New op
            CurrentOpIndex = opIndex;
            //poly.ConwayOperators.Clear();
            nextJunction = FindNextJunction(currentState);
            var opType = nextJunction.Op;
            currentState = nextJunction.FinalState;
            amountDecreasing = nextJunction.FinalAmount == 0;
            op = new PolyHydra.ConwayOperator {opType = opType};
            poly.ConwayOperators[ControlledOpIndex] = op;
        }
        else
        {
            op = poly.ConwayOperators[ControlledOpIndex];
        }

        var config = poly.opconfigs[op.opType];
        if (amountDecreasing)
        {
            op.amount = Mathf.Lerp(config.amountSafeMax, config.amountSafeMin, amount);
        }
        else
        {
            op.amount = Mathf.Lerp(config.amountSafeMin, config.amountSafeMax, amount);
        }
        poly.ConwayOperators[ControlledOpIndex] = op;
        controller.RebuildNeeded = true;
    }
}
