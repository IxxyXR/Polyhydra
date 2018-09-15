﻿using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PolyPreset {

	public string Name;

	public int PolyType;
	public bool BypassOps;
	public bool TwoSided;
	
	[Serializable]
	public struct Op {  
		public int optype;
		public float amount;
		public bool disabled;
	}
	
	public Op[] Ops;
		
	public PolyPreset CreateFromPoly(string presetName, PolyComponent _poly)
	{
		var preset = new PolyPreset();
		preset.Name = presetName;
		preset.PolyType = (int)_poly.PolyType;
		preset.BypassOps = _poly.BypassOps;
		preset.TwoSided = _poly.TwoSided;
		if (preset.Ops == null)
		{
			preset.Ops = new Op[_poly.ConwayOperators.Length];
		}
		foreach (var polyop in _poly.ConwayOperators)
		{
			var op = new Op();
			op.optype = (int)polyop.op;
			op.disabled = polyop.disabled;
			op.amount = polyop.amount;
			preset.Ops.Append(op);
		}

		return preset;
	}

	public void ApplyToPoly(ref PolyComponent _poly)
	{
		var preset = this;
		_poly.PolyType = (PolyComponent.PolyTypes)preset.PolyType;
		_poly.BypassOps = preset.BypassOps;
		_poly.TwoSided = preset.TwoSided;
		foreach (var presetop in preset.Ops)
		{
			var op = new PolyComponent.ConwayOperator();
			op.op = (PolyComponent.Ops)presetop.optype;
			op.disabled = presetop.disabled;
			op.amount = presetop.amount;
			_poly.ConwayOperators.Append(op);
		}
	}
    
}
