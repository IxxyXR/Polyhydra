using System;
using System.Collections.Generic;
using Conway;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Animations;
using Vector3 = System.Numerics.Vector3;


[Serializable]
public class PolyPreset {

	public string Name;
	[JsonConverter(typeof(StringEnumConverter))]
	public PolyTypes PolyType;
	public bool BypassOps;
	public bool TwoSided;
	[JsonConverter(typeof(StringEnumConverter))]
	public PolyHydra.GridTypes GridType;
	public int PrismP;
	public int PrismQ;
	public string AppearancePresetName;
	
	[Serializable]
	public struct Op {
		[JsonConverter(typeof(StringEnumConverter))] public PolyHydra.Ops OpType;
		[JsonConverter(typeof(StringEnumConverter))] public ConwayPoly.FaceSelections FaceSelections;
		public float Amount;
		public bool Randomize;
		public bool Disabled;
	}
	
	public Op[] Ops;
		
	public void CreateFromPoly(string presetName, PolyHydra poly)
	{
		Name = presetName;
		AppearancePresetName = poly.APresetName;
		PolyType = poly.PolyType;
		GridType = poly.GridType;
		BypassOps = poly.BypassOps;
		PrismP = poly.PrismP;
		PrismQ = poly.PrismP;
		TwoSided = poly.TwoSided;
		Ops = new Op[poly.ConwayOperators.Count];
		
		for (var index = 0; index < poly.ConwayOperators.Count; index++)
		{
			var polyOp = poly.ConwayOperators[index];
			var op = new Op
			{
				OpType = polyOp.opType,
				FaceSelections = polyOp.faceSelections,
				Amount = polyOp.amount,
				Randomize = polyOp.randomize,
				Disabled = polyOp.disabled
			};
			Ops[index] = op;
		}
	}

	public void ApplyToPoly(ref PolyHydra poly, AppearancePresets aPresets)
	{
		poly.PolyType = PolyType;
		poly.BypassOps = BypassOps;
		poly.TwoSided = TwoSided;
		poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
		poly.GridType = GridType;
		poly.PrismP = PrismP;
		poly.PrismQ = PrismQ;
		poly.PresetName = Name;
		aPresets.ApplyPresetToPoly(AppearancePresetName);
		
		for (var index = 0; index < Ops.Length; index++)
		{
			var presetOp = Ops[index];
			var op = new PolyHydra.ConwayOperator
			{
				opType = presetOp.OpType,
				faceSelections = presetOp.FaceSelections,
				amount = presetOp.Amount,
				randomize = presetOp.Randomize,
				disabled = presetOp.Disabled
			};
			poly.ConwayOperators.Add(op);
		}
	}
    
}
