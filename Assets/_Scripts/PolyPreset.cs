using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


[Serializable]
public class PolyPreset {

	public string Name;
	[JsonConverter(typeof(StringEnumConverter))]
	public PolyTypes PolyType;
	public bool BypassOps;
	public bool TwoSided;
	
	[Serializable]
	public struct Op {
		[JsonConverter(typeof(StringEnumConverter))] public PolyHydra.Ops OpType;
		[JsonConverter(typeof(StringEnumConverter))] public PolyHydra.FaceSelections FaceSelections;
		public float Amount;
		public bool Disabled;
	}
	
	public Op[] Ops;
		
	public void CreateFromPoly(string presetName, PolyHydra poly)
	{
		Name = presetName;
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
				Disabled = polyOp.disabled,
				Amount = polyOp.amount,
				FaceSelections = polyOp.faceSelections
			};
			Ops[index] = op;
		}
	}

	public void ApplyToPoly(ref PolyHydra poly)
	{
		poly.PolyType = PolyType;
		poly.BypassOps = BypassOps;
		poly.TwoSided = TwoSided;
		poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
		for (var index = 0; index < Ops.Length; index++)
		{
			var presetOp = Ops[index];
			var op = new PolyHydra.ConwayOperator();
			op.opType = presetOp.OpType;
			op.faceSelections = presetOp.FaceSelections;
			op.disabled = presetOp.Disabled;
			op.amount = presetOp.Amount;
			poly.ConwayOperators.Add(op);
		}
	}
    
}
