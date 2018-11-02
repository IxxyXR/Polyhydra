using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;


[Serializable]
public class PolyPreset {

	public string Name;
	[JsonConverter(typeof(StringEnumConverter))]
	public PolyComponent.PolyTypes PolyType;
	public bool BypassOps;
	public bool TwoSided;
	
	[Serializable]
	public struct Op {
		[JsonConverter(typeof(StringEnumConverter))]
		public PolyComponent.Ops OpType;
		public float Amount;
		public bool Disabled;
	}
	
	public Op[] Ops;
		
	public void CreateFromPoly(string presetName, PolyComponent poly)
	{
		Name = presetName;
		PolyType = poly.PolyType;
		BypassOps = poly.BypassOps;
		TwoSided = poly.TwoSided;
		Ops = new Op[poly.ConwayOperators.Length];
		for (var index = 0; index < poly.ConwayOperators.Length; index++)
		{
			var polyOp = poly.ConwayOperators[index];
			var op = new Op
			{
				OpType = polyOp.opType,
				Disabled = polyOp.disabled,
				Amount = polyOp.amount
			};
			Ops[index] = op;
		}
	}

	public void ApplyToPoly(ref PolyComponent poly)
	{
		poly.PolyType = PolyType;
		poly.BypassOps = BypassOps;
		poly.TwoSided = TwoSided;
		poly.ConwayOperators = new PolyComponent.ConwayOperator[Ops.Length];
		for (var index = 0; index < Ops.Length; index++)
		{
			var presetOp = Ops[index];
			var op = new PolyComponent.ConwayOperator();
			op.opType = presetOp.OpType;
			op.disabled = presetOp.Disabled;
			op.amount = presetOp.Amount;
			poly.ConwayOperators[index] = op;
		}
	}
    
}
