using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conway;
using Johnson;
using Newtonsoft.Json;
using Wythoff;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PolyHydra : MonoBehaviour
{

	public bool EnableLogging = false;

	public bool enableThreading = true;
	public bool enableCaching = true;
	public float generationTimeout = 1f;
	[NonSerialized] public bool generationAborted = false;

	private int _faceCount;
	private int _vertexCount;

	public PolyHydraEnums.ShapeTypes ShapeType;
	public PolyTypes UniformPolyType;
	public PolyHydraEnums.PolyTypeCategories UniformPolyTypeCategory;
	public PolyHydraEnums.ColorMethods ColorMethod;
	public PolyHydraEnums.JohnsonPolyTypes JohnsonPolyType;
	public PolyHydraEnums.OtherPolyTypes OtherPolyType;
	public PolyHydraEnums.GridTypes GridType;
	public PolyHydraEnums.GridShapes GridShape;
	public string WythoffSymbol;
	public string PresetName;
	public string APresetName;
	public bool BypassOps;
	public bool Rescale;
	public bool SafeLimits = false;
	public bool GenerateSubmeshes = false;
	private PolyCache polyCache;
	private Coroutine geomCoroutine;

	// Parameters for prismatic forms
	public int PrismP = 5;
	public int PrismQ = 2;

	private ConwayPoly stashed;

	public readonly int[] NonOrientablePolyTypes = {
		(int)PolyTypes.Tetrahemihexahedron,
		(int)PolyTypes.Cubohemioctahedron,
		(int)PolyTypes.Small_Rhombihexahedron,
		(int)PolyTypes.Great_Rhombihexahedron,
		(int)PolyTypes.Small_Rhombidodecahedron,
		(int)PolyTypes.Small_Icosihemidodecahedron,
		(int)PolyTypes.Small_Dodecicosahedron,
		(int)PolyTypes.Small_Dodecahemidodecahedron,
		(int)PolyTypes.Rhombicosahedron,
		(int)PolyTypes.Small_Dodecahemicosahedron,
		(int)PolyTypes.Great_Dodecicosahedron,
		(int)PolyTypes.Great_Dodecahemicosahedron,
		(int)PolyTypes.Great_Dodecahemidodecahedron,
		(int)PolyTypes.Great_Icosihemidodecahedron,
		(int)PolyTypes.Great_Rhombidodecahedron
	};

	public string GetInfoText()
	{
		string infoText = $"Faces: {_faceCount}\nVertices: {_vertexCount}";
		return infoText;
	}

	public string PolyToJson()
	{
		var preset = new PolyPreset();
		preset.CreateFromPoly("Temp", this);
		return JsonConvert.SerializeObject(preset, Formatting.Indented);
	}

	[ContextMenu("Copy to clipboard")]
	public void CopyPresetToClipboard()
	{
		GUIUtility.systemCopyBuffer = PolyToJson();
	}

	public void PolyFromJson(string json, bool loadMatchingAppearance)
	{
		var preset = new PolyPreset();
		preset.Name = "Temp";
		preset = JsonConvert.DeserializeObject<PolyPreset>(json);
		preset.ApplyToPoly(this, FindObjectOfType<AppearancePresets>(), loadMatchingAppearance);
	}

	[ContextMenu("Paste from clipboard")]
	public void AddPresetFromClipboard()
	{
		PolyFromJson(GUIUtility.systemCopyBuffer, true);
		Rebuild();
	}

	// Call this if you're *not* using this class via an interactive UI
	public void DisableInteractiveFlags()
	{
		enableCaching = false;
		enableThreading = false;
	}

	public ConwayPoly GetConwayPoly()
	{
		return _conwayPoly;
	}

	[Serializable]
	public struct ConwayOperator {
		public Ops opType;
		public FaceSelections faceSelections;
		public bool randomize;
		public float amount;
		public float amount2;
		public float animatedAmount;
		public bool disabled;
		public bool animate;
		public float animationRate;
		public float animationAmount;
		public float audioLowAmount;
		public float audioMidAmount;
		public float audioHighAmount;
		public string Tags;

		public ConwayOperator ClampAmount(PolyHydraEnums.OpConfig config, bool safe=false)
		{
			float min = safe ? config.amountSafeMin : config.amountMin;
			float max = safe ? config.amountSafeMax : config.amountMax;
			amount = Mathf.Clamp(amount, min, max);
			return this;
		}

		public ConwayOperator ClampAmount2(PolyHydraEnums.OpConfig config, bool safe=false)
		{
			float min = safe ? config.amount2SafeMin : config.amount2Min;
			float max = safe ? config.amount2SafeMax : config.amount2Max;
			amount2 = Mathf.Clamp(amount2, min, max);
			return this;
		}

		public ConwayOperator ChangeAmount(float val)
		{
			amount += val;
			return this;
		}
		public ConwayOperator ChangeAmount2(float val)
		{
			amount2 += val;
			return this;
		}
		public ConwayOperator ChangeOpType(int val)
		{
			opType += val;
			opType = (Ops) Mathf.Clamp(
				(int) opType, 1, Enum.GetNames(typeof(Ops)).Length - 1
			);
			return this;
		}
		public ConwayOperator ChangeFaceSelection(int val)
		{
			faceSelections += val;
			faceSelections = (FaceSelections) Mathf.Clamp(
				(int) faceSelections, 0, Enum.GetNames(typeof(FaceSelections)).Length - 1
			);
			return this;
		}

		public ConwayOperator ChangeTags(int direction)
		{
			throw new NotImplementedException();
		}

		public ConwayOperator SetDefaultValues(PolyHydraEnums.OpConfig config)
		{
			amount = config.amountDefault;
			amount2 = config.amount2Default;
			return this;
		}
	}
	public List<ConwayOperator> ConwayOperators;

	[Header("Gizmos")]
	public bool wythoffVertexGizmos;
	public bool wythoffEdgeGizmos;
	public bool conwayVertexGizmos;
	public bool conwayEdgeGizmos;
	public bool faceCenterGizmos;
	public bool faceGizmos;
	public int[] faceGizmosList;
	public bool dualGizmo;
	public bool symmetryGizmo;

	private int[] meshFaces;
	public WythoffPoly WythoffPoly;
	public ConwayPoly _conwayPoly;

	private MeshFilter meshFilter;
	private PolyPreset previousState;

	// TODO this is only needed to allow the editor script to get a reference to the UI method
	public PolyUI polyUI;

	private bool finishedOpsThread = true;
	private Thread thread;

	public Color[] gizmoPallette =
	{
		Color.red,
		Color.yellow,
		Color.green,
		Color.cyan,
		Color.blue,
		Color.magenta
	};

	void Init()
	{
		Debug.unityLogger.logEnabled = EnableLogging;
		InitCacheIfNeeded();
		meshFilter = gameObject.GetComponent<MeshFilter>();
		MakePolyhedron();
	}

	void Start()
	{
		Init();
	}

	void Update()
	{
		if (generationAborted)
		{
			Rebuild();
		}
	}

	void OnEnable()
	{
		Init();
	}

	void InitCacheIfNeeded()
	{
		if (polyCache==null) polyCache = FindObjectOfType<PolyCache>();
		if (polyCache == null)
		{
			enableCaching = false;
		}
	}

	public ConwayPoly MakeGrid(PolyHydraEnums.GridTypes gridType, PolyHydraEnums.GridShapes gridShape)
	{
		ConwayPoly conway = null;

		switch (gridType)
		{
//			case GridTypes.Square:
//				conway = ConwayPoly.MakeGrid(PrismP, PrismQ);
//				break;
//			case GridTypes.Isometric:
//				conway = ConwayPoly.MakeIsoGrid(PrismP, PrismQ);
//				break;
//			case GridTypes.Hex:
//				conway = ConwayPoly.MakeHexGrid(PrismP, PrismQ);
//				break;

			case PolyHydraEnums.GridTypes.Square:
				conway = Grids.Grids.MakeUnitileGrid(1, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.Isometric:
				conway = Grids.Grids.MakeUnitileGrid(2, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.Hex:
				conway = Grids.Grids.MakeUnitileGrid(3, (int)gridShape, PrismP, PrismQ);
				break;

			case PolyHydraEnums.GridTypes.U_3_6_3_6:
				conway = Grids.Grids.MakeUnitileGrid(4, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.U_3_3_3_4_4:
				conway = Grids.Grids.MakeUnitileGrid(5, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.U_3_3_4_3_4:
				conway = Grids.Grids.MakeUnitileGrid(6, (int)gridShape, PrismP, PrismQ);
				break;
//			case GridTypes.U_3_3_3_3_6:
//				conway = Grids.Grids.MakeUnitileGrid(7, (int)gridShape, PrismP, PrismQ);
//				break;
			case PolyHydraEnums.GridTypes.U_3_12_12:
				conway = Grids.Grids.MakeUnitileGrid(8, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.U_4_8_8:
				conway = Grids.Grids.MakeUnitileGrid(9, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.U_3_4_6_4:
				conway = Grids.Grids.MakeUnitileGrid(10, (int)gridShape, PrismP, PrismQ);
				break;
			case PolyHydraEnums.GridTypes.U_4_6_12:
				conway = Grids.Grids.MakeUnitileGrid(11, (int)gridShape, PrismP, PrismQ);
				break;

			case PolyHydraEnums.GridTypes.Polar:
				conway = Grids.Grids.MakePolarGrid(PrismP, PrismQ);
				break;
		}
		// Welding only seems to work reliably on simpler shapes
		if (gridShape != PolyHydraEnums.GridShapes.Plane) conway = conway.Weld(0.001f);

		return conway;
	}

	public ConwayPoly MakeJohnsonPoly(PolyHydraEnums.JohnsonPolyTypes johnsonPolyType)
	{
		ConwayPoly poly;

		switch (johnsonPolyType)
		{
			case PolyHydraEnums.JohnsonPolyTypes.Prism:
				poly = JohnsonPoly.Prism(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.Antiprism:
				poly = JohnsonPoly.Antiprism(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.Pyramid:
				poly = JohnsonPoly.Pyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid:
				poly = JohnsonPoly.ElongatedPyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid:
				poly = JohnsonPoly.GyroelongatedPyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.Dipyramid:
				poly = JohnsonPoly.Dipyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid:
				poly = JohnsonPoly.ElongatedDipyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid:
				poly = JohnsonPoly.GyroelongatedDipyramid(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.Cupola:
				poly = JohnsonPoly.Cupola(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola:
				poly = JohnsonPoly.ElongatedCupola(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola:
				poly = JohnsonPoly.GyroelongatedCupola(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.OrthoBicupola:
				poly = JohnsonPoly.OrthoBicupola(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroBicupola:
				poly = JohnsonPoly.GyroBicupola(PrismP<3?3:PrismP);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedOrthoBicupola:
				poly = JohnsonPoly.ElongatedBicupola(PrismP<3?3:PrismP, false);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedGyroBicupola:
				poly = JohnsonPoly.ElongatedBicupola(PrismP<3?3:PrismP, true);
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola:
				poly = JohnsonPoly.GyroelongatedBicupola(PrismP<3?3:PrismP, false);
				break;
			// The distinction between these two is simply one of chirality
			// case JohnsonPolyTypes.GyroelongatedOrthoBicupola:
			// 	poly = JohnsonPoly.GyroElongatedBicupola(PrismP<3?3:PrismP, false);
			// 	break;
			// case JohnsonPolyTypes.GyroelongatedGyroBicupola:
			// 	poly = JohnsonPoly.GyroElongatedBicupola(PrismP<3?3:PrismP, true);
			// 	break;
			case PolyHydraEnums.JohnsonPolyTypes.Rotunda:
				poly = JohnsonPoly.Rotunda();
				break;
			case PolyHydraEnums.JohnsonPolyTypes.ElongatedRotunda:
				poly = JohnsonPoly.ElongatedRotunda();
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedRotunda:
				poly = JohnsonPoly.GyroelongatedRotunda();
				break;
			case PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBirotunda:
				poly = JohnsonPoly.GyroelongatedBirotunda();
				break;
			default:
				Debug.LogError("Unknown Johnson Poly Type");
				return null;
		}

		poly.Recenter();
		return poly;
	}

	public ConwayPoly MakeOtherPoly(PolyHydraEnums.OtherPolyTypes otherPolyType)
	{
		switch (otherPolyType)
		{
			case PolyHydraEnums.OtherPolyTypes.UvSphere:
				return JohnsonPoly.UvSphere(PrismP, PrismQ);
			case PolyHydraEnums.OtherPolyTypes.UvHemisphere:
				return JohnsonPoly.UvHemisphere(PrismP, PrismQ);
			case PolyHydraEnums.OtherPolyTypes.L_Shape:
				return JohnsonPoly.L_Shape();
			case PolyHydraEnums.OtherPolyTypes.L_Alt_Shape:
				return JohnsonPoly.L_Alt_Shape();
			case PolyHydraEnums.OtherPolyTypes.C_Shape:
				return JohnsonPoly.C_Shape();
			case PolyHydraEnums.OtherPolyTypes.H_Shape:
				return JohnsonPoly.H_Shape();
			case PolyHydraEnums.OtherPolyTypes.Polygon:
				return JohnsonPoly.Polygon(PrismP);
			case PolyHydraEnums.OtherPolyTypes.GriddedCube:
				return JohnsonPoly.GriddedCube(PrismP);
			default:
				Debug.LogError("Unknown Other Poly Type");
				return null;
		}
	}

	private void MakePolyhedron(bool disableThreading=false)
	{
		if (ShapeType == PolyHydraEnums.ShapeTypes.Uniform)
		{
			MakeWythoff();
			try
			{
				_conwayPoly = new ConwayPoly(WythoffPoly, abortOnFailure: false);
			}
			catch (InvalidOperationException e)
			{
				Debug.LogError($"Failed to build Conway from Wythoff {WythoffPoly.PolyTypeIndex} {WythoffPoly.PolyName}");
				throw;
			}
		}
		else if (ShapeType == PolyHydraEnums.ShapeTypes.Grid)
		{
			_conwayPoly = MakeGrid(GridType, GridShape);
		}

		else if (ShapeType == PolyHydraEnums.ShapeTypes.Johnson)
		{
			_conwayPoly = MakeJohnsonPoly(JohnsonPolyType);
		}

		else if (ShapeType == PolyHydraEnums.ShapeTypes.Other)
		{
			_conwayPoly = MakeOtherPoly(OtherPolyType);
		}

		_conwayPoly.basePolyhedraInfo = new ConwayPoly.BasePolyhedraInfo
		{
			P = PrismP,
			Q = PrismQ
		};

		if (!enableThreading || disableThreading)  // TODO fix confusing flags
		{
			ApplyOps();
			FinishedApplyOps();
		}
		else
		{
			if (geomCoroutine != null)
			{
				Debug.LogWarning("Coroutine already exists. Aborting.");
				return;
			}
			geomCoroutine = StartCoroutine(RunOffMainThread(ApplyOps, FinishedApplyOps));
			geomCoroutine = null;
		}
	}

	private void OnValidate()
	{
		InitCacheIfNeeded();
		#if UNITY_EDITOR
				// To prevent values getting out of sync
				// ignore the inspector UI if we're showing the runtime UI
				if (polyUI != null && EditorApplication.isPlayingOrWillChangePlaymode) return;
		#endif
		Validate();
		if (!gameObject.activeInHierarchy) return;
		Rebuild();
	}

	public void Validate()
	{
		if (ShapeType == PolyHydraEnums.ShapeTypes.Uniform)
		{
			if (PrismP < 3) {PrismP = 3;}
			if (PrismP > 16) PrismP = 16;
			if (PrismQ > PrismP - 2) PrismQ = PrismP - 2;
			if (PrismQ < 2) PrismQ = 2;
		}

		// Control the amount variables to some degree
		for (var i = 0; i < ConwayOperators.Count; i++)
		{
			if (PolyHydraEnums.OpConfigs == null) continue;
			var op = ConwayOperators[i];
			if (PolyHydraEnums.OpConfigs[op.opType].usesAmount)
			{
				op.amount = Mathf.Round(op.amount * 1000) / 1000f;
				op.amount2 = Mathf.Round(op.amount2 * 1000) / 1000f;

				float opMin, opMax;
				if (SafeLimits)
				{
					opMin = PolyHydraEnums.OpConfigs[op.opType].amountSafeMin;
					opMax = PolyHydraEnums.OpConfigs[op.opType].amountSafeMax;
				}
				else
				{
					opMin = PolyHydraEnums.OpConfigs[op.opType].amountMin;
					opMax = PolyHydraEnums.OpConfigs[op.opType].amountMax;
				}
				if (op.amount < opMin) op.amount = opMin;
				if (op.amount > opMax) op.amount = opMax;
			}
			else
			{
				op.amount = 0;
			}

			ConwayOperators[i] = op;
		}


	}

	public void Rebuild(bool disableThreading = false)
	{
		InitCacheIfNeeded();
//		var currentState = new PolyPreset();
//		currentState.CreateFromPoly("temp", this);
//		if (previousState != currentState)
//		{
			MakePolyhedron(disableThreading);
//			previousState = currentState;
//		}
	}

	public void MakeWythoff() {

		if (!String.IsNullOrEmpty(WythoffSymbol))
		{
			MakeWythoff(WythoffSymbol);
		}
		else
		{
			MakeWythoff((int)UniformPolyType + 1);
		}

	}

	public void MakeWythoff(int polyTypeIndex)
	{
		MakeWythoff(Uniform.Uniforms[polyTypeIndex].Wythoff);
	}

	public void MakeWythoff(string symbol)
	{

		symbol = symbol.Replace("p", PrismP.ToString());
		symbol = symbol.Replace("q", PrismQ.ToString());

		if (WythoffPoly == null || WythoffPoly.WythoffSymbol != symbol)
		{
			WythoffPoly = polyCache.GetWythoff(symbol);
			if (WythoffPoly == null)
			{
				WythoffPoly = new WythoffPoly(symbol);
				WythoffPoly.BuildFaces();
				polyCache.SetWythoff(symbol, WythoffPoly);
			}

			if (WythoffPoly == null)
			{
				throw new Exception("Fuck");
			}
		}

		//_faceCount = WythoffPoly.FaceCount;
		//_vertexCount = WythoffPoly.VertexCount;
	}

	public void FinishedApplyOps()
	{
		if (generationAborted) return;

		_faceCount = _conwayPoly.Faces.Count;
		_vertexCount = _conwayPoly.Vertices.Count;

		if (enableCaching)
		{
			var cacheKeySource = PolyToJson();
			int key = cacheKeySource.GetHashCode();
			var mesh = polyCache.GetMesh(key);
			if (mesh == null)
			{
				mesh = BuildMesh();
				polyCache.SetMesh(key, mesh);
			}
			AssignFinishedMesh(mesh);
		}
		else
		{
			var mesh = BuildMesh();
			AssignFinishedMesh(mesh);
		}
	}

	public Mesh BuildMesh()
	{
		return PolyMeshBuilder.BuildMeshFromConwayPoly(_conwayPoly, GenerateSubmeshes, null, ColorMethod);
	}

	public void AssignFinishedMesh(Mesh mesh)
	{

		if (Rescale)
		{
			var size = mesh.bounds.size;
			var maxDimension = Mathf.Max(size.x, size.y, size.z);
			var scale = (1f / maxDimension) * 2f;
			if (scale > 0 && scale != Mathf.Infinity)
			{
				transform.localScale = new Vector3(scale, scale, scale);
			}
			else
			{
				Debug.LogError("Failed to rescale");
			}
		}

		if (meshFilter != null)
		{
			if (Application.isEditor)
			{
				meshFilter.sharedMesh = mesh;
			}
			else
			{
				meshFilter.mesh = mesh;
			}
		}
	}

	// This is a helper coroutine
	IEnumerator RunOffMainThread(Action toRun, Action callback)
	{
		if (thread!=null && thread.IsAlive)
		{
			Debug.LogWarning("Waiting for existing geometry thread");
			yield break;
		}
		finishedOpsThread = false;
		thread = null;

		thread = new Thread(() =>
		{
			toRun();
			finishedOpsThread = true;
		});
		thread.Start();
		while (!finishedOpsThread)
			yield return null;
		callback();
	}

	// TODO Strange things happen with caching if this method isn't static
	// Therefore we have to pass in the stashed poly
	public static ConwayPoly ApplyOp(ConwayPoly conway, ref ConwayPoly stash, ConwayOperator op)
	{

		float amount = op.animate ? op.animatedAmount : op.amount;

		var opParams = new OpParams
		{
			valueA = amount,
			valueB = op.amount2,
			randomize = op.randomize,
			facesel = op.faceSelections,
			tags = op.Tags,
		};

		if (op.opType == Ops.Stash)
		{
			stash = conway.Duplicate();
			stash = stash.FaceKeep(new OpParams{facesel = op.faceSelections});
		}
		else if (op.opType == Ops.Unstash)
		{
			if (stash == null) return conway;
			var dup = conway.Duplicate();
			var offset = Vector3.up * op.amount2;
			dup.Append(stash.FaceKeep(op.faceSelections, op.Tags), offset, Quaternion.identity, amount);
			conway = dup;
		}
		else if (op.opType == Ops.UnstashToFaces)
		{
			if (stash == null) return conway;
			conway = conway.AppendMany(stash, op.faceSelections, op.Tags, amount, 0, op.amount2, true);
		}
		else if (op.opType == Ops.UnstashToVerts)
		{
			if (stash == null) return conway;
			conway = conway.AppendMany(stash, op.faceSelections, op.Tags, amount, 0, op.amount2, false);
		}
		else
		{
			conway = conway.ApplyOp(op.opType, opParams);
		}

		return conway;
	}

	public void ApplyOps()
	{

		var startTime = DateTime.Now.Ticks;
		generationAborted = false;

		var cacheKeySource = $"{ShapeType} {OtherPolyType} {JohnsonPolyType} {UniformPolyType} {PrismP} {PrismQ} {GridType} {GridShape}";

		stashed = null;

		foreach (var op in ConwayOperators.ToList())
		{
			if (op.disabled || op.opType==Ops.Identity) continue;

			if (enableCaching && op.opType!=Ops.Stash) // The cache contain the stash so we can't cache it
			{
				cacheKeySource += JsonConvert.SerializeObject(op);
				int key = cacheKeySource.GetHashCode();
				var nextOpResult = polyCache.GetConway(key);
				if (nextOpResult == null)
				{
					nextOpResult = ApplyOp(_conwayPoly, ref stashed, op);
					polyCache.SetConway(key, nextOpResult);
				}
				_conwayPoly = nextOpResult;
			}
			else
			{
				_conwayPoly = ApplyOp(_conwayPoly, ref stashed, op);
			}

			_conwayPoly.basePolyhedraInfo = new ConwayPoly.BasePolyhedraInfo
			{
				P = PrismP,
				Q = PrismQ
			};
			var elapsedTime = DateTime.Now.Ticks - startTime;
			if (elapsedTime > (generationTimeout * 1e+07f))
			{
				generationAborted = true;
				break;
			}
		}
	}

	// Essentially Kis only on non-triangular faces
	// Returns the original number of sides of each face to be used elsewhere
	// TODO Detect convex faces and use fan triangulation to save on a vertex?
	public List<int> KisTriangulate() {

		var faceRoles = new List<ConwayPoly.Roles>();
		var vertexRoles = new List<ConwayPoly.Roles>();

		var newVerts = _conwayPoly.Faces.Select(f => f.Centroid);
		var vertexPoints = Enumerable.Concat(_conwayPoly.Vertices.Select(v => v.Position), newVerts);
		vertexRoles.Concat(Enumerable.Repeat(ConwayPoly.Roles.Existing, vertexPoints.Count()));
		var originalFaceSides = new List<int>();

		// vertex lookup
		var vlookup = new Dictionary<Guid, int>();
		int n = _conwayPoly.Vertices.Count;
		for (int i = 0; i < n; i++) {
			vlookup.Add(_conwayPoly.Vertices[i].Name, i);
		}

		var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
		for (int i = 0; i < _conwayPoly.Faces.Count; i++)
		{
			int faceSides = _conwayPoly.Faces[i].Sides;
			if (_conwayPoly.Faces[i].Sides <= 3) {
				faceIndices.Add(_conwayPoly.ListFacesByVertexIndices()[i]);
				originalFaceSides.Add(faceSides);
				faceRoles.Add(_conwayPoly.FaceRoles[i]);
			} else {
				foreach (var edge in _conwayPoly.Faces[i].GetHalfedges()) {
					// create new face from edge start, edge end and centroid
					faceIndices.Add(
						new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
					);
					originalFaceSides.Add(faceSides);
					faceRoles.Add(_conwayPoly.FaceRoles[i]);
				}
			}
		}

		_conwayPoly = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);



		return originalFaceSides;
	}

	private static Vector3 Jitter(Vector3 val)
	{
		// Used to reduce Z fighting for coincident faces
		float jitter = 0.0002f;
		return val + new Vector3(Random.value * jitter, Random.value * jitter, Random.value * jitter);
	}

	// Returns true if at least one face matches the facesel rule but all of them
	public bool FaceSelectionIsValid(FaceSelections facesel)
	{
		if (ConwayOperators.Count == 0) {
			_conwayPoly = new ConwayPoly(WythoffPoly);  // We need a conway poly
		}
		int includedFaceCount = Enumerable.Range(0, _conwayPoly.Faces.Count).Count(x => _conwayPoly.IncludeFace(x, facesel));
		return includedFaceCount > 0 && includedFaceCount < _conwayPoly.Faces.Count;

	}

	public ConwayOperator AddRandomOp()
	{
		int maxOpIndex = Enum.GetValues(typeof(Ops)).Length;
		int opTypeIndex = Random.Range(1, maxOpIndex - 2); // No canonicalize as it's pretty rough at the moment
		var opType = (Ops) opTypeIndex;
		PolyHydraEnums.OpConfig opConfig;
		try
		{
			opConfig = PolyHydraEnums.OpConfigs[opType];
		}
		catch (Exception e)
		{
			Debug.LogWarning($"opType: {opType} PolyHydraEnums.OpConfigs count: {PolyHydraEnums.OpConfigs.Count} Exception: {e}");
			throw;
		}

		FaceSelections faceSelection = FaceSelections.None;
		var maxFaceSel = Enum.GetValues(typeof(FaceSelections)).Length - 1; // Exclude "None"

		try
		{
			// Keep picking a random facesel until we get one that will have an effect
			while (!FaceSelectionIsValid(faceSelection))
			{
				faceSelection = (FaceSelections) Random.Range(1, maxFaceSel);
			}
		}
		catch (InvalidOperationException e)
		{
			Debug.LogWarning($"Failed to pick a random FaceSel as the Wythoff to Conway conversion failed ({e})");
			faceSelection = FaceSelections.All;
		}

		// TODO pick another facesel if all faces are chosen
		var newOp = new ConwayOperator
		{
			opType = opType,
			faceSelections = Random.value > 0.25f ? 0: faceSelection,
			randomize = Random.value > 0.8f,
			amount = Random.value > 0.25f ? opConfig.amountDefault : Random.Range(opConfig.amountMin, opConfig.amountMax),
			disabled = false
		};
		ConwayOperators.Add(newOp);
		return newOp;
	}

#if UNITY_EDITOR
	void OnDrawGizmos () {

		float GizmoRadius = .03f;

		// I had to make too many fields on Kaleido public to do this
		// Need some sensible public methods to give me sensible access

		if (WythoffPoly != null)
		{
			if (wythoffVertexGizmos)
			{
				Gizmos.color = Color.white;
				if (WythoffPoly.Vertices != null)
				{
					for (int i = 0; i < WythoffPoly.Vertices.Length; i++)
					{
						Vector3 vert = WythoffPoly.Vertices[i].getVector3();
						Vector3 pos = transform.TransformPoint(vert);
						Gizmos.DrawWireSphere(pos, GizmoRadius);
						Handles.Label(pos + new Vector3(0, .15f, 0), i.ToString());
					}
				}
			}
		}

		if (conwayVertexGizmos && _conwayPoly!=null)
		{
			Gizmos.color = Color.white;
			if (_conwayPoly.Vertices != null)
			{
				for (int i = 0; i < _conwayPoly.Vertices.Count; i++)
				{
					Vector3 vert = _conwayPoly.Vertices[i].Position;
					Vector3 pos = transform.TransformPoint(vert);
					Gizmos.DrawWireSphere(pos, GizmoRadius);
//					Handles.Label(pos + new Vector3(0, .15f, 0), _conwayPoly.VertexRoles[i].ToString());
					Handles.Label(pos + new Vector3(0, .15f, 0), _conwayPoly.Vertices[i].Halfedges.Count.ToString());
				}
			}
		}

		if (faceCenterGizmos)
		{
			if (_conwayPoly == null)
			{
				Gizmos.color = Color.blue;
				if (WythoffPoly!=null && WythoffPoly.FaceCenters != null)
				{
					foreach (var f in WythoffPoly.FaceCenters)
					{
						Gizmos.DrawWireSphere(transform.TransformPoint(f.getVector3()), GizmoRadius);
					}
				}
			}
			else
			{
				Gizmos.color = Color.green;
				foreach (var f in _conwayPoly.Faces)
				{
					Gizmos.DrawWireSphere(transform.TransformPoint(f.Centroid), GizmoRadius);
					Gizmos.DrawRay(transform.TransformPoint(f.Centroid), f.Normal);
				}
			}

		}


		if (wythoffEdgeGizmos && WythoffPoly != null)
		{
			for (int i = 0; i < WythoffPoly.EdgeCount; i++)
			{
				Gizmos.color = Color.yellow;
				var edgeStart = WythoffPoly.Edges[0, i];
				var edgeEnd = WythoffPoly.Edges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(WythoffPoly.Vertices[edgeStart].getVector3()),
					transform.TransformPoint(WythoffPoly.Vertices[edgeEnd].getVector3())
				);
			}
		}
		if (conwayEdgeGizmos)
		{
			for (int i = 0; i < _conwayPoly.Halfedges.Count; i++)
			{
				Gizmos.color = Color.yellow;
				var edge = _conwayPoly.Halfedges[i];
				Gizmos.DrawLine(
					transform.TransformPoint(edge.Vertex.Position),
					transform.TransformPoint(edge.Next.Vertex.Position)
				);
				Gizmos.DrawWireCube(transform.TransformPoint(edge.PointAlongEdge(0.9f)), Vector3.one * 0.02f);
			}
		}

		if (faceGizmos)
		{
			if (_conwayPoly == null)
			{
				NonConwayFaceGizmos();
			}
			else
			{
				ConwayFaceGizmos();
			}
		}

		if (dualGizmo)
		{
			for (int i = 0; i < WythoffPoly.EdgeCount; i++)
			{
				var edgeStart = WythoffPoly.DualEdges[0, i];
				var edgeEnd = WythoffPoly.DualEdges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(WythoffPoly.FaceCenters[edgeStart].getVector3()),
					transform.TransformPoint(WythoffPoly.FaceCenters[edgeEnd].getVector3())
				);
			}
		}

		if (symmetryGizmo)
		{
			string wythoffSymbol;
			if (WythoffPoly.SymmetryType == 2)
			{
				wythoffSymbol = $"2 {PrismP} | 2";
			}
			else
			{
				wythoffSymbol = $"{WythoffPoly.SymmetryType} | 2 3";
			}
			var wythoffGizmoPoly = new WythoffPoly(wythoffSymbol);

			for (int i = 0; i < wythoffGizmoPoly.EdgeCount; i++)
			{
				Gizmos.color = Color.yellow;
				var edgeStart = wythoffGizmoPoly.Edges[0, i] ;
				var edgeEnd = wythoffGizmoPoly.Edges[1, i];
				var v0 = wythoffGizmoPoly.Vertices[edgeStart].getVector3();
				var v1 = wythoffGizmoPoly.Vertices[edgeEnd].getVector3();
//				var q = Quaternion.AngleAxis(Convert.ToSingle(WythoffPoly.FundementalAngles[0] * Mathf.Rad2Deg), Vector3.zero);
//				var q = Quaternion.LookRotation(WythoffPoly.FaceCenters[0].getVector3(), Vector3.up);
//				v0 = q * v0;
//				v1 = q * v1;
				Gizmos.DrawLine(
					transform.TransformPoint(v0),
					transform.TransformPoint(v1)
				);
			}
		}

	}

	private void ConwayFaceGizmos()
	{
		int gizmoColor = 0;
		var faces = _conwayPoly.Faces;
		var verts = _conwayPoly.Vertices;
		for (int f = 0; f < faces.Count; f++)
		{
			if (faceGizmosList.Contains(f) || faceGizmosList.Length==0)
			{
				Gizmos.color = gizmoPallette[gizmoColor++ % gizmoPallette.Length];
				var face = faces[f];
				var faceVerts = face.GetVertices();
				for (int i = 0; i < faceVerts.Count; i++)
				{
					var edgeStart = faceVerts[i];
					var edgeEnd = faceVerts[(i + 1) % faceVerts.Count];
					Gizmos.DrawLine(
						transform.TransformPoint(edgeStart.Position),
						transform.TransformPoint(edgeEnd.Position)
					);
				}

				string label;

				// Tags
				// if (_conwayPoly.FaceTags !=null && _conwayPoly.FaceTags.Count == _conwayPoly.Faces.Count && _conwayPoly.FaceTags[f].Count > 0)
				// {
				// 	label = _conwayPoly.FaceTags[f].First().ToString();
				// }
				// else
				// {
				// 	label = "";
				// }
				label = f.ToString();  // Face index
				//label = faceVerts.Count.ToString();  // Face verts
				// label = ((int)_conwayPoly.FaceRoles[f]).ToString();  // Face Role
				Handles.Label(Vector3.Scale(face.Centroid, transform.lossyScale) + new Vector3(0, .03f, 0), label);
			}
		}
	}

	private void NonConwayFaceGizmos()
	{
		int gizmoColor = 0;
		var faces = WythoffPoly.faces;
		var verts = WythoffPoly.Vertices;
		for (int f = 0; f < faces.Count; f++)
		{
			if (faceGizmosList.Contains(f) || faceGizmosList.Length==0)
			{
				Gizmos.color = gizmoPallette[gizmoColor++ % gizmoPallette.Length];
				var face = faces[f];
				var faceVerts = face.points;
				for (int i = 0; i < faceVerts.Count; i++)
				{
					var edgeStart = faceVerts[i];
					var edgeEnd = faceVerts[(i + 1) % faceVerts.Count];
					Gizmos.DrawLine(
						transform.TransformPoint(verts[edgeStart].getVector3()),
						transform.TransformPoint(verts[edgeEnd].getVector3())
					);
				}
			}
		}
	}
#endif
}
