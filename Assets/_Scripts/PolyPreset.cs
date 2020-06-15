using System;
using System.Collections.Generic;
using System.IO;
using Conway;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;


[Serializable]
public class PolyPreset {

	public const string PresetFileNamePrefix = "PolyPreset-";

	public string Name;

	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.ShapeTypes ShapeType;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.PolyTypeCategories PolyTypeCategory;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyTypes PolyType;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.JohnsonPolyTypes JohnsonPolyType;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.OtherPolyTypes OtherPolyType;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.GridTypes GridType;
	[JsonConverter(typeof(MyStringEnumConverter))] public PolyHydra.GridShapes GridShape;
	public bool BypassOps;
	public bool SafeLimits;
	public int PrismP;
	public int PrismQ;
	public string AppearancePresetName;
	
	[Serializable]
	public struct Op {
		[JsonConverter(typeof(StringEnumConverter))] public PolyHydra.Ops OpType;
		[JsonConverter(typeof(StringEnumConverter))] public FaceSelections FaceSelections;
		public float Amount;
		public float Amount2;
		public float AnimatedAmount; // Not needed for presets but needed for cache key generation
		public bool Randomize;
		public bool Disabled;
		public bool Animate;
		public float AnimationRate;
		public float AnimationAmount;
		public float AudioLowAmount;
		public float AudioMidAmount;
		public float AudioHighAmount;
		public string Tags;
	}
	
	public Op[] Ops;
		
	public void CreateFromPoly(string presetName, PolyHydra poly)
	{
		Name = presetName;
		AppearancePresetName = poly.APresetName;
		ShapeType = poly.ShapeType;
		PolyType = poly.UniformPolyType;
		PolyTypeCategory = poly.UniformPolyTypeCategory;
		JohnsonPolyType = poly.JohnsonPolyType;
		OtherPolyType = poly.OtherPolyType;
		GridType = poly.GridType;
		GridShape = poly.GridShape;
		BypassOps = poly.BypassOps;
		PrismP = poly.PrismP;
		PrismQ = poly.PrismQ;
		SafeLimits = poly.SafeLimits;
		Ops = new Op[poly.ConwayOperators.Count];
		
		for (var index = 0; index < poly.ConwayOperators.Count; index++)
		{
			var polyOp = poly.ConwayOperators[index];
			var op = new Op
			{
				OpType = polyOp.opType,
				FaceSelections = polyOp.faceSelections,
				Amount = polyOp.amount,
				Amount2 = polyOp.amount2,
				AnimatedAmount = polyOp.animatedAmount,
				Randomize = polyOp.randomize,
				Disabled = polyOp.disabled,
				Animate = polyOp.animate,
				AnimationRate = polyOp.animationRate,
				AnimationAmount = polyOp.animationAmount,
				AudioLowAmount = polyOp.audioLowAmount,
				AudioMidAmount = polyOp.audioMidAmount,
				AudioHighAmount = polyOp.audioHighAmount,
				Tags = polyOp.Tags
			};
			Ops[index] = op;
		}
	}

	public void ApplyToPoly(PolyHydra poly)
	{
		poly.ShapeType = ShapeType;
		poly.UniformPolyTypeCategory = PolyTypeCategory;
		poly.UniformPolyType = PolyType;
		poly.JohnsonPolyType = JohnsonPolyType;
		poly.OtherPolyType = OtherPolyType;
		poly.BypassOps = BypassOps;
		poly.SafeLimits = SafeLimits;
		poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
		poly.GridType = GridType;
		poly.GridShape = GridShape;
		poly.PrismP = PrismP;
		poly.PrismQ = PrismQ;
		poly.PresetName = Name;

		for (var index = 0; index < Ops.Length; index++)
		{
			var presetOp = Ops[index];
			var op = new PolyHydra.ConwayOperator
			{
				opType = presetOp.OpType,
				faceSelections = presetOp.FaceSelections,
				amount = presetOp.Amount,
				amount2 = presetOp.Amount2,
				animatedAmount = presetOp.AnimatedAmount,
				randomize = presetOp.Randomize,
				disabled = presetOp.Disabled,
				animate = presetOp.Animate,
				animationRate = presetOp.AnimationRate,
				animationAmount = presetOp.AnimationAmount,
				audioLowAmount = presetOp.AudioLowAmount,
				audioMidAmount = presetOp.AudioMidAmount,
				audioHighAmount = presetOp.AudioHighAmount,
				Tags =  presetOp.Tags,
			};
			poly.ConwayOperators.Add(op);
		}
	}

	public void ApplyToPoly(PolyHydra poly, AppearancePresets aPresets, bool loadMatchingAppearance)
	{
		ApplyToPoly(poly);
		if (loadMatchingAppearance)
		{
			aPresets.ApplyPresetToPoly(AppearancePresetName);
		}
	}

	public void Save()
	{
		var fileName = Path.Combine(Application.persistentDataPath, PresetFileNamePrefix + Name + ".json");
        var polyJson = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(fileName, polyJson);
	}
}
