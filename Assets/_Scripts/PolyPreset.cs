using System;
using System.Collections.Generic;
using Conway;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


[Serializable]
public class PolyPreset {

	public string Name;
	[JsonConverter(typeof(StringEnumConverter))]
	public PolyTypes PolyType;
	public bool BypassOps;
	public bool TwoSided;
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
		BypassOps = poly.BypassOps;
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
		poly.PresetName = Name;
		aPresets.ApplyPresetToPoly(AppearancePresetName);
		
		for (var index = 0; index < Ops.Length; index++)
		{
			var presetOp = Ops[index];
			var op = new PolyHydra.ConwayOperator();
			op.opType = presetOp.OpType;
			op.faceSelections = presetOp.FaceSelections;
			op.amount = presetOp.Amount;
			op.randomize = presetOp.Randomize;
			op.disabled = presetOp.Disabled;
			poly.ConwayOperators.Add(op);
		}
	}
    
}
