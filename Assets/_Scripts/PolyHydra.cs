using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Conway;
using Newtonsoft.Json;
using Wythoff;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PolyHydra : MonoBehaviour {

	private bool enableThreading = true;
	private bool enableCaching = true;

	private int _faceCount;
	private int _vertexCount;

	[FormerlySerializedAs("PolyType")]
	public ShapeTypes ShapeType;
	public PolyTypes UniformPolyType;
	public ColorMethods ColorMethod;
	public JohnsonPolyTypes JohnsonPolyType;
	public GridTypes GridType;
	public string WythoffSymbol;
	public string PresetName;
	public string APresetName;
	public bool BypassOps;
	public bool TwoSided;
	public bool Rescale;
	private PolyCache polyCache;

	// Parameters for prismatic forms
	public int PrismP = 5;
	public int PrismQ = 2;
	
	public enum ColorMethods
	{
		BySides,
		ByRole
	}

	public enum ShapeTypes
	{
		Uniform,
		Grid,
		Johnson
	}

	public enum GridTypes
	{
		Square,
		Isometric,
		Hex
	}

	public enum JohnsonPolyTypes
	{
		Prism,
		Antiprism,

		Pyramid,
		//ElongatedPyramid,
		//GyroelongatedPyramid,

		Dipyramid,
		//ElongatedDipyramid,
		//GyroelongatedDipyramid,

		Cupola,
		//ElongatedCupola,
		//GyroelongatedCupola,

		Bicupola,
		//ElongatedBicupola,
		//GyroelongatedBicupola,

		Rotunda,
		//ElongatedRotunda,
		//GyroelongatedRotunda,

		L,

	}

	public enum Ops {
		Identity,
		Kis,
		Dual,
		Ambo,
		Zip,
		Expand,
		Bevel,
		Join,
		Needle,
		Ortho,
		Meta,
		Truncate,
		Gyro,
		Snub,
		Subdivide,
		Loft,
		Chamfer,
		Quinto,
		Lace,
		JoinedLace,
		Stake,
		Medial,
		EdgeMedial,
//		JoinedMedial,
		Propeller,
		Whirl,
		Volute,
		Exalt,
		Yank,
		Extrude,
		Shell,
		VertexScale,
		FaceOffset,
		FaceScale,
		FaceRotate,
//		Chamfer,
//		Ribbon,
//		FaceTranslate,
//		FaceRotateX,
//		FaceRotateY,
		FaceRemove,
		FaceKeep,
		FillHoles,
		Hinge,
		AddDual,
		Canonicalize,
//		CanonicalizeI,
		Spherize,
		SitLevel,
		Stretch
	}

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

	public class OpConfig
	{
		public bool usesAmount = true;
		public float amountDefault = 0;
		public float amountMin = -20;
		public float amountMax = 20;
		public bool usesFaces = false;
		public bool usesRandomize = false;
		public ConwayPoly.FaceSelections faceSelection = ConwayPoly.FaceSelections.All;	
	}

	public ConwayPoly GetConwayPoly()
	{
		return _conwayPoly;
	}

	public Dictionary<Ops, OpConfig> opconfigs;
	
	[Serializable]
	public struct ConwayOperator {
		public Ops opType;
		public ConwayPoly.FaceSelections faceSelections;
		public bool randomize;
		public float amount;
		public bool disabled;
	}
	public List<ConwayOperator> ConwayOperators;
	
	[Header("Gizmos")]
	public bool wythoffVertexGizmos;
	public bool conwayVertexGizmos;
	public bool faceCenterGizmos;
	public bool edgeGizmos;
	public bool faceGizmos;
	public int[] faceGizmosList;
	public bool dualGizmo;
	
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
	
	private Color32[] faceColors = 
	{
		new Color(1.0f, 0.5f, 0.5f),
		new Color(0.8f, 0.8f, 0.8f),
		new Color(0.5f, 0.6f, 0.6f),
		new Color(1.0f, 0.94f, 0.9f),
		new Color(0.66f, 0.2f, 0.2f),
		new Color(0.6f, 0.0f, 0.0f), 
		new Color(1.0f, 1.0f, 1.0f),
		new Color(0.6f, 0.6f, 0.6f),
		new Color(0.5f, 1.0f, 0.5f),
		new Color(0.5f, 0.5f, 1.0f),
		new Color(0.5f, 1.0f, 1.0f),
		new Color(1.0f, 0.5f, 1.0f),
	};

	void Awake()
	{		
		opconfigs = new Dictionary<Ops, OpConfig>
		{	
			{Ops.Identity, new OpConfig {usesAmount=false}},
			{Ops.Kis, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Dual, new OpConfig{usesAmount=false}},
			{Ops.Ambo, new OpConfig{usesAmount=false}},
			{Ops.Zip, new OpConfig{usesFaces=true, amountDefault = 0f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Expand, new OpConfig{usesAmount=true, amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Bevel, new OpConfig{usesFaces=true, amountDefault = 0f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Join, new OpConfig{usesAmount=false}},
			{Ops.Needle, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Ortho, new OpConfig{usesAmount=false}},
			{Ops.Meta, new OpConfig{usesFaces=true, amountDefault = 0.15f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Truncate, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Gyro, new OpConfig{amountDefault = 0.33f, amountMin = -.5f, amountMax = 0.5f}},
			{Ops.Snub, new OpConfig{amountDefault = 0.5f, amountMin = -.5f, amountMax = 0.5f}},
			{Ops.Subdivide, new OpConfig {usesAmount=false}},
			{Ops.Loft, new OpConfig {usesFaces=true, amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Chamfer, new OpConfig {amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Quinto, new OpConfig{amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Lace, new OpConfig{usesFaces=true, amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.JoinedLace, new OpConfig{amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Stake, new OpConfig{usesFaces=true, amountDefault = 0.5f, amountMin = -4, amountMax = 4}},
			{Ops.Medial, new OpConfig{amountDefault = 2f, amountMin = 2, amountMax = 8}},
			{Ops.EdgeMedial, new OpConfig{amountDefault = 2f, amountMin = 2, amountMax = 8}},
//			{Ops.JoinedMedial, new OpConfig{amountDefault = 2f, amountMin = 2, amountMax = 8}},
			{Ops.Propeller, new OpConfig{amountDefault = 0.75f, amountMin = -4, amountMax = 4}},
			{Ops.Whirl, new OpConfig{amountDefault = 0.25f, amountMin = -4, amountMax = 4}},
			{Ops.Volute, new OpConfig{amountDefault = 0.33f, amountMin = -4, amountMax = 4}},
			{Ops.Exalt, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Yank, new OpConfig{usesFaces=true, amountDefault = 0.33f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.Chamfer new OpConfig{}},
			{Ops.FaceOffset, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.Ribbon, new OpConfig{}},
			{Ops.Extrude, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Shell, new OpConfig{amountDefault = 0.1f, amountMin = -6, amountMax = 6}},
			{Ops.VertexScale, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.FaceTranslate, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -6, amountMax = 6}},
			{Ops.FaceScale, new OpConfig{usesFaces=true, amountDefault = -0.03f, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.FaceRotate, new OpConfig{usesFaces=true, amountDefault = 45f, amountMin = -180, amountMax = 180, usesRandomize=true}},
//			{Ops.FaceRotateX, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -180, amountMax = 180}},
//			{Ops.FaceRotateY, new OpConfig{usesFaces=true, amountDefault = 0.1f, amountMin = -180, amountMax = 180}},
			//{Ops.Test, new OpConfig{}}
			{Ops.FaceRemove, new OpConfig{usesFaces=true, usesAmount=false}},
			{Ops.FillHoles, new OpConfig{usesAmount=false}},
			{Ops.FaceKeep, new OpConfig{usesFaces=true, usesAmount=false}},
			{Ops.Hinge, new OpConfig{amountDefault = 15f, amountMin = -180, amountMax = 180}},
			{Ops.AddDual, new OpConfig{amountDefault = 1f, amountMin = -6, amountMax = 6}},
			{Ops.Canonicalize, new OpConfig{amountDefault = 0.1f, amountMin = 0.0001f, amountMax = 1f}},
//			{Ops.CanonicalizeI, new OpConfig{amountDefault = 200, amountMin = 1, amountMax = 400}},
			{Ops.Spherize, new OpConfig{amountDefault = 1.0f, amountMin = 0, amountMax = 1}},
			{Ops.Stretch, new OpConfig{amountDefault = 1.0f, amountMin = 0, amountMax = 3f}},
			{Ops.SitLevel, new OpConfig{}}

		};
	}

	void Start()
	{
		Debug.unityLogger.logEnabled = false;
		InitCacheIfNeeded();
		meshFilter = gameObject.GetComponent<MeshFilter>();
		MakePolyhedron();
	}

	void InitCacheIfNeeded()
	{
		if (!polyCache) polyCache = FindObjectOfType<PolyCache>();
	}

	public ConwayPoly MakeGrid(GridTypes gridType)
	{
		switch (gridType)
		{
			case GridTypes.Square:
				return ConwayPoly.MakeGrid();
			case GridTypes.Isometric:
				return ConwayPoly.MakeIsoGrid();
			case GridTypes.Hex:
				return ConwayPoly.MakeHexGrid();
		}

		return null;
	}

	public ConwayPoly MakeJohnsonPoly(JohnsonPolyTypes johnsonPolyType)
	{
		switch (johnsonPolyType)
		{
			case JohnsonPolyTypes.Prism:
				return JohnsonPoly.MakePrism(PrismP);
			case JohnsonPolyTypes.Antiprism:
				return JohnsonPoly.MakeAntiprism(PrismP);
			case JohnsonPolyTypes.Pyramid:
				return JohnsonPoly.MakePyramid(PrismP);
			case JohnsonPolyTypes.Dipyramid:
				return JohnsonPoly.MakeDipyramid(PrismP);
			case JohnsonPolyTypes.Cupola:
				return JohnsonPoly.MakeCupola(PrismP);
			case JohnsonPolyTypes.Bicupola:
				return JohnsonPoly.MakeBicupola(PrismP);
			case JohnsonPolyTypes.Rotunda:
				// A fudge for the pentagonal rotunda (which is the only actual Johnson solid Rotunda)
				return JohnsonPoly.MakeRotunda();
				// WIP
				//return JohnsonPoly.MakeRotunda(PrismP, 1, false);
			case JohnsonPolyTypes.L:
				return JohnsonPoly.MakeL();

			default:
				Debug.LogError("Unknown Johnson Poly Type");
				return null;
		}
	}
	
	private void MakePolyhedron(bool disableThreading=false)
	{
		if (ShapeType == ShapeTypes.Uniform && UniformPolyType != PolyTypes.Grid)
		{
			MakeWythoff();
			try
			{
				_conwayPoly = new ConwayPoly(WythoffPoly, abortOnFailure: false);
			}
			catch (InvalidOperationException e)
			{
				Debug.Log($"Failed to build Conway from Wythoff {WythoffPoly.PolyTypeIndex} {WythoffPoly.PolyName}");
				throw;
			}
		}
		else if (ShapeType == ShapeTypes.Grid || UniformPolyType == PolyTypes.Grid)
		{
			_conwayPoly = MakeGrid(GridType);
		}
		
		else if (ShapeType == ShapeTypes.Johnson)
		{
			_conwayPoly = MakeJohnsonPoly(JohnsonPolyType);
		}

		if (!enableThreading || disableThreading)  // TODO fix confusing flags
		{
			ApplyOps();
			FinishedApplyOps();
		}
		else
		{
			StartCoroutine(RunOffMainThread(ApplyOps, FinishedApplyOps));
		}
	}

	private void OnValidate()
	{
		#if UNITY_EDITOR
			// To prevent values getting out of sync
			// ignore the inspector UI if we're showing the runtime UI
			if (polyUI == null || (polyUI != null && EditorApplication.isPlayingOrWillChangePlaymode)) return;
		#endif

		InitCacheIfNeeded();

		if (PrismP < 3) {PrismP = 3;}
		if (PrismP > 16) PrismP = 16;
		if (PrismQ > PrismP - 2) PrismQ = PrismP - 2;
		if (PrismQ < 2) PrismQ = 2;
		
		// Control the amount variables to some degree
		for (var i = 0; i < ConwayOperators.Count; i++)
		{
			if (opconfigs == null) continue;
			var op = ConwayOperators[i];
			if (opconfigs[op.opType].usesAmount)
			{
				op.amount = Mathf.Round(op.amount * 1000) / 1000f;
				float opMin = opconfigs[op.opType].amountMin;
				float opMax = opconfigs[op.opType].amountMax;
				if (op.amount < opMin) op.amount = opMin;
				if (op.amount > opMax) op.amount = opMax;
			}
			else
			{
				op.amount = 0;
			}
		}
		
		if (!gameObject.activeInHierarchy) return;
		Rebuild();

	}

	public void Rebuild(bool disableThreading = false)
	{
//		InitCacheIfNeeded();
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
			MakeWythoff((int)UniformPolyType);
		}

	}

	public void MakeWythoff(int polyType)
	{
		MakeWythoff(Uniform.Uniforms[polyType].Wythoff);
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
		_faceCount = _conwayPoly.Faces.Count;
		_vertexCount = _conwayPoly.Vertices.Count;
		
		if (enableCaching)
		{
			var cacheKeySource = PolyToJson();
			int key = cacheKeySource.GetHashCode();
			var mesh = polyCache.GetMesh(key);
			if (mesh == null)
			{
				mesh = BuildMeshFromConwayPoly(TwoSided);
				polyCache.SetMesh(key, mesh);
			}
			AssignFinishedMesh(mesh);
		}
		else
		{
			var mesh = BuildMeshFromConwayPoly(TwoSided);
			AssignFinishedMesh(mesh);
		}
	}

	public void AssignFinishedMesh(Mesh mesh)
	{

		if (Rescale)
		{
			var size = mesh.bounds.size;
			var maxDimension = Mathf.Max(size.x, size.y, size.z);
			var scale = (1f / maxDimension) * 2f;
			transform.localScale = new Vector3(scale, scale, scale);
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
		if (thread != null)
		{
			thread.Abort();
			thread.Join();			
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

	public static ConwayPoly ApplyOp(ConwayPoly conway, ConwayOperator op)
	{

		switch (op.opType)
		{
			case Ops.Identity:
				break;
			case Ops.Kis:
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.Dual:
				conway = conway.Dual();
				break;
			case Ops.Ambo:
				conway = conway.Ambo();
				break;
			case Ops.Zip:
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				break;
			case Ops.Expand:
				conway = conway.Expand(op.amount);
				break;
			case Ops.Bevel:
				conway = conway.Ambo();
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				break;
			case Ops.Join:
				// conway = conway.Join(op.amount);  // Not currently used as it results in non-coplanar faces
				conway = conway.Ambo();
				conway = conway.Dual();
				break;
			case Ops.Needle:
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.Ortho:
				conway = conway.Ortho();
				break;
			case Ops.Meta:
				conway = conway.Ambo();
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.Truncate:
				conway = conway.Truncate(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.Gyro:
				conway = conway.Gyro(op.amount);
				break;
			case Ops.Snub:
				conway = conway.Gyro(op.amount);
				conway = conway.Dual();
				break;
			case Ops.Exalt:
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.Yank:
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				break;
			case Ops.Subdivide:
				conway = conway.Subdivide();
				break;
			case Ops.Loft:
				conway = conway.Loft(op.amount, op.faceSelections);
				break;					
			case Ops.Chamfer:
				conway = conway.Chamfer(op.amount);
				break;					
			case Ops.Quinto:
				conway = conway.Quinto(op.amount);
				break;
			case Ops.JoinedLace:
				conway = conway.JoinedLace(op.amount);
				break;
			case Ops.Lace:
				conway = conway.Lace(op.amount, op.faceSelections);
				break;
			case Ops.Stake:
				conway = conway.Stake(op.amount, op.faceSelections);
				break;
			case Ops.Medial:
				conway = conway.Medial((int)op.amount);
				break;
			case Ops.EdgeMedial:
				conway = conway.EdgeMedial((int)op.amount);
				break;
//			case Ops.JoinedMedial:
//				conway = conway.JoinedMedial((int)op.amount);
//				break;
			case Ops.Propeller:
				conway = conway.Propeller(op.amount);
				break;
			case Ops.Whirl:
				conway = conway.Whirl(op.amount);
				break;
			case Ops.Volute:
				conway = conway.Volute(op.amount);
				break;
			case Ops.Shell:
				// TODO do this properly with shared edges/vertices
				conway = conway.Extrude(op.amount, false, op.randomize);
				break;
			case Ops.Extrude:
				if (op.faceSelections == ConwayPoly.FaceSelections.All)
				{
					conway = conway.FaceScale(0f, ConwayPoly.FaceSelections.All, false);
					conway = conway.Extrude(op.amount, false, op.randomize);
				}
				else
				{
					// TODO do this properly with shared edges/vertices
					var included = conway.FaceRemove(op.faceSelections, true);
					included = included.FaceScale(0, ConwayPoly.FaceSelections.All, false);
					var excluded = conway.FaceRemove(op.faceSelections, false);
					conway = included.Extrude(op.amount, false, op.randomize);
					conway.Append(excluded);
				}
				break;
			case Ops.VertexScale:
				conway = conway.VertexScale(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceOffset:
				// Split faces
				var origRoles = conway.FaceRoles;
				conway = conway.FaceScale(0, ConwayPoly.FaceSelections.All, false);
				conway.FaceRoles = origRoles;
				conway = conway.Offset(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceScale:
				conway = conway.FaceScale(op.amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceRotate:
				conway = conway.FaceRotate(op.amount, op.faceSelections, 0, op.randomize);
				break;
//					case Ops.Chamfer:
//						conway = conway.Chamfer();
//						break;
//					case Ops.Ribbon:
//						conway = conway.Ribbon(op.amount, false, 0.1f);
//						break;
//					case Ops.FaceTranslate:
//						conway = conway.FaceTranslate(op.amount, op.faceSelections);
//						break;
//					case Ops.FaceRotateX:
//						conway = conway.FaceRotate(op.amount, op.faceSelections, 1);
//						break;
//					case Ops.FaceRotateY:
//						conway = conway.FaceRotate(op.amount, op.faceSelections, 2);
//						break;
			case Ops.FaceRemove:
				conway = conway.FaceRemove(op.faceSelections, false);
				break;
			case Ops.FaceKeep:
				conway = conway.FaceRemove(op.faceSelections, true);
				break;
			case Ops.FillHoles:
				conway.FillHoles();
				break;
			case Ops.Hinge:
				conway = conway.Hinge(op.amount);
				break;
			case Ops.AddDual:
				conway = conway.AddDual(op.amount);
				break;
			case Ops.Canonicalize:
				conway = conway.Canonicalize(op.amount, op.amount);
				break;
//			case Ops.CanonicalizeI:
//				conway = conway.Canonicalize((int)op.amount, (int)op.amount);
//				break;
			case Ops.Spherize:
				conway = conway.Spherize(op.amount);
				break;
			case Ops.SitLevel:
				conway = conway.SitLevel();
				break;
			case Ops.Stretch:
				conway = conway.Stretch(op.amount);
				break;
		}

		return conway;
	}

	public void ApplyOps()
	{

		var cacheKeySource = $"{ShapeType} {JohnsonPolyType} {UniformPolyType} {PrismP} {PrismQ} {GridType} {TwoSided}";
		
		foreach (var op in ConwayOperators.ToList())
		{
			
			if (op.disabled) continue;

			if (enableCaching)
			{
				cacheKeySource += JsonConvert.SerializeObject(op);
				int key = cacheKeySource.GetHashCode();
				var nextOpResult = polyCache.GetConway(key);
				if (nextOpResult == null)
				{
					nextOpResult = ApplyOp(_conwayPoly, op);
					polyCache.SetConway(key, nextOpResult);
				}
				_conwayPoly = nextOpResult;
			}
			else
			{
				_conwayPoly = ApplyOp(_conwayPoly, op);
			}
		}
	}
	
	public Mesh BuildMeshFromWythoffPoly(WythoffPoly source)
	{
		
		var meshVertices = new List<Vector3>();
		var meshTriangles = new List<int>();
		var MeshVertexToVertex = new List<int>(); // Mapping of mesh vertices to poly vertices (one to many as we duplicate verts)
		var meshColors = new List<Color>();
		var meshUVs = new List<Vector2>();
		
		var mesh = new Mesh();
		int meshVertexIndex = 0;

		foreach (Wythoff.Face face in source.faces) {
			face.CalcTriangles();
		}

		for (int faceType = 0; faceType < source.FaceTypeCount; faceType++) {
			foreach (Wythoff.Face face in source.faces) {
				if (face.configuration == source.FaceSidesByType[faceType])
				{
					var v0 = source.Vertices[face.points[0]].getVector3();
					var v1 = source.Vertices[face.points[1]].getVector3();
					var v2 = source.Vertices[face.points[2]].getVector3();
					var normal = Vector3.Cross(v1 - v0, v2 - v0);
					var c = face.center.getVector3();
					var yAxis = c - v0;
					var xAxis = Vector3.Cross(yAxis, normal);
					
					var faceColor = faceColors[(int) ((face.configuration + 2) % faceColors.Length)];
					// Vertices
					for (int i = 0; i < face.triangles.Length; i++) {
						Vector3 vcoords = source.Vertices[face.triangles[i]].getVector3();
						meshVertices.Add(vcoords);
						meshColors.Add(faceColor);

						var u = Vector3.Project(vcoords, xAxis).magnitude;
						var v = Vector3.Project(vcoords, yAxis).magnitude;
						meshUVs.Add(new Vector2(u, v));
						
						meshTriangles.Add(meshVertexIndex);
						MeshVertexToVertex.Add(face.triangles[i]);
						meshVertexIndex++;
					}
				}
			}
		}

		mesh.vertices = meshVertices.ToArray();
		mesh.triangles = meshTriangles.ToArray();
		mesh.colors = meshColors.ToArray();
		mesh.uv = meshUVs.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		return mesh;

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
		var vlookup = new Dictionary<string, int>();
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

	Vector3 Jitter(Vector3 val)
	{
		// Used to reduce Z fighting for coincident faces
		float jitter = 0.0002f;
		return val + new Vector3(Random.value * jitter, Random.value * jitter, Random.value * jitter);
	}

	public Mesh BuildMeshFromConwayPoly(bool forceTwosided)
	{
		
		var target = new Mesh();
		var meshTriangles = new List<int>();
		var meshVertices = new List<Vector3>();
		var meshNormals = new List<Vector3>();
		var meshColors = new List<Color32>();
		var meshUVs = new List<Vector2>();
		var edgeUVs = new List<Vector2>();
		var barycentricUVs = new List<Vector3>();
		var miscUVs = new List<Vector4>();
		
		var hasNaked = _conwayPoly.HasNaked();
		hasNaked = false;  // TODO
		
		// Strip down to Face-Vertex structure
		var points = _conwayPoly.ListVerticesByPoints();
		var faceIndices = _conwayPoly.ListFacesByVertexIndices();

		// Add faces
		int index = 0;
		
		for (var i = 0; i < faceIndices.Length; i++)
		{

			var faceIndex = faceIndices[i];
			var face = _conwayPoly.Faces[i];
			var faceNormal = face.Normal;

			// Axes for UV mapping
			var xAxis = face.Halfedge.Vector;
			var yAxis = Vector3.Cross(xAxis, faceNormal);
			
			Color32 color;
			switch (ColorMethod)
			{
				case ColorMethods.ByRole:
					color = faceColors[(int) _conwayPoly.FaceRoles[i]];
					break;
				case ColorMethods.BySides:
					color = faceColors[face.Sides % faceColors.Length];
					break;
				default:
					color = Color.red;
					break;
			}

			Vector2 calcUV(Vector3 point)
			{
				float u, v;
				u = Vector3.Project(point, xAxis).magnitude;
				u *= Vector3.Dot(point, xAxis) > 0 ? 1 : -1;
				v = Vector3.Project(point, yAxis).magnitude;
				v *= Vector3.Dot(point, yAxis) > 0  ? 1 : -1;
				return new Vector2(u, v);
			}

			var miscUV = new Vector4(face.Centroid.x, face.Centroid.y, face.Centroid.z, ((float)i)/faceIndices.Length);

			if (face.Sides > 3)
			{
				for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
				{
					
					meshVertices.Add(face.Centroid);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					edgeUVs.Add(new Vector2(0, 0));
					barycentricUVs.Add(new Vector3(0, 0, 1));

					meshVertices.Add(points[faceIndex[edgeIndex]]);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					edgeUVs.Add(new Vector2(1, 1));					
					barycentricUVs.Add(new Vector3(0, 1, 0));

					meshVertices.Add(points[faceIndex[(edgeIndex + 1) % face.Sides]]);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					edgeUVs.Add(new Vector2(1, 1));					
					barycentricUVs.Add(new Vector3(1, 0, 0));

					meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
					meshColors.AddRange(Enumerable.Repeat(color, 3));
					miscUVs.AddRange(Enumerable.Repeat(miscUV, 3));
				}
			}
			else
			{
				
				meshVertices.Add(points[faceIndex[0]]);
				meshUVs.Add(calcUV(meshVertices[index]));
				meshTriangles.Add(index++);
				barycentricUVs.Add(new Vector3(0, 0, 1));

				meshVertices.Add(points[faceIndex[1]]);
				meshUVs.Add(calcUV(meshVertices[index]));
				meshTriangles.Add(index++);
				barycentricUVs.Add(new Vector3(0, 1, 0));

				meshVertices.Add(points[faceIndex[2]]);
				meshUVs.Add(calcUV(meshVertices[index]));
				meshTriangles.Add(index++);
				barycentricUVs.Add(new Vector3(1, 0, 0));

				edgeUVs.AddRange(Enumerable.Repeat(new Vector2(1, 1), 3));
				meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
				meshColors.AddRange(Enumerable.Repeat(color, 3));
				miscUVs.AddRange(Enumerable.Repeat(miscUV, 3));
			}


			if (hasNaked || forceTwosided)
			{

				if (faceIndex.Count > 3)
				{
					for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
					{
						meshVertices.Add(face.Centroid);
						meshUVs.Add(calcUV(meshVertices[index]));
						meshTriangles.Add(index++);
						edgeUVs.Add(new Vector2(0, 0));
						barycentricUVs.Add(new Vector3(0, 0, 1));

						meshVertices.Add(points[faceIndex[(edgeIndex + 1) % face.Sides]]);
						meshUVs.Add(calcUV(meshVertices[index]));
						meshTriangles.Add(index++);
						edgeUVs.Add(new Vector2(1, 1));
						barycentricUVs.Add(new Vector3(0, 1, 0));

						meshVertices.Add(points[faceIndex[edgeIndex]]);
						meshUVs.Add(calcUV(meshVertices[index]));
						meshTriangles.Add(index++);
						edgeUVs.Add(new Vector2(1, 1));					
						barycentricUVs.Add(new Vector3(1, 0, 0));

						meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
						meshColors.AddRange(Enumerable.Repeat(color, 3));
						miscUVs.AddRange(Enumerable.Repeat(miscUV, 3));
					}
				}
				else
				{
					meshVertices.Add(points[faceIndex[0]]);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					barycentricUVs.Add(new Vector3(0, 0, 1));

					meshVertices.Add(points[faceIndex[2]]);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					barycentricUVs.Add(new Vector3(0, 1, 0));

					meshVertices.Add(points[faceIndex[1]]);
					meshUVs.Add(calcUV(meshVertices[index]));
					meshTriangles.Add(index++);
					barycentricUVs.Add(new Vector3(1, 0, 0));

					edgeUVs.AddRange(Enumerable.Repeat(new Vector2(1, 1), 3));
					meshNormals.AddRange(Enumerable.Repeat(-faceNormal, 3));
					meshColors.AddRange(Enumerable.Repeat(color, 3));
					miscUVs.AddRange(Enumerable.Repeat(miscUV, 3));
				}
			}		
		}
		
		target.vertices = meshVertices.Select(x => Jitter(x)).ToArray();
		target.normals = meshNormals.ToArray();
		target.triangles = meshTriangles.ToArray();
		target.colors32 = meshColors.ToArray();
		target.SetUVs(0, meshUVs);
		target.SetUVs(1, edgeUVs);
		target.SetUVs(2, barycentricUVs);
		target.SetUVs(3, miscUVs);

		if (hasNaked || forceTwosided) {
			target.RecalculateNormals();
		}

		return target;
	}

	// Returns true if at least one face matches the facesel rule but all of them
	public bool FaceSelectionIsValid(ConwayPoly.FaceSelections facesel)
	{
		if (ConwayOperators.Count == 0 && UniformPolyType > 0) {
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
		OpConfig opConfig;
		try
		{
			opConfig = opconfigs[opType];
		}
		catch (Exception e)
		{
			Debug.Log($"opType: {opType} opconfigs count: {opconfigs.Count}");
			throw;
		}
        
		ConwayPoly.FaceSelections faceSelection = ConwayPoly.FaceSelections.None;
		var maxFaceSel = Enum.GetValues(typeof(ConwayPoly.FaceSelections)).Length - 1; // Exclude "None"

		try
		{
			// Keep picking a random facesel until we get one that will have an effect
			while (!FaceSelectionIsValid(faceSelection))
			{
				faceSelection = (ConwayPoly.FaceSelections) Random.Range(1, maxFaceSel);
			}
		}
		catch (InvalidOperationException r)
		{
			Debug.LogWarning("Failed to pick a random FaceSel as the Wythoff to Conway conversion failed");
			faceSelection = ConwayPoly.FaceSelections.All;
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
		
		var transform = this.transform;

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
					Handles.Label(pos + new Vector3(0, .15f, 0), i.ToString());
				}
			}
		}

		if (faceCenterGizmos)
		{
			if (_conwayPoly == null)
			{
				Gizmos.color = Color.blue;
				if (WythoffPoly.FaceCenters != null)
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
				}
			}

		}


		if (edgeGizmos && WythoffPoly != null)
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
		else if (edgeGizmos && WythoffPoly == null)
		{
			for (int i = 0; i < _conwayPoly.Halfedges.Count; i++)
			{
				Gizmos.color = Color.yellow;
				var edge = _conwayPoly.Halfedges[i];
				Gizmos.DrawLine(
					transform.TransformPoint(edge.Vertex.Position),
					transform.TransformPoint(edge.Next.Vertex.Position)
				);
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
				Handles.Label(face.Centroid + new Vector3(0, .15f, 0), f.ToString());
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
