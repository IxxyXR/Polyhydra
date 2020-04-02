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
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;
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

	private int _faceCount;
	private int _vertexCount;

	[FormerlySerializedAs("PolyType")]
	public ShapeTypes ShapeType;
	public PolyTypes UniformPolyType;
	public PolyTypeCategories UniformPolyTypeCategory;
	public ColorMethods ColorMethod;
	public JohnsonPolyTypes JohnsonPolyType;
	public OtherPolyTypes OtherPolyType;
	public GridTypes GridType;
	public GridShapes GridShape;
	public string WythoffSymbol;
	public string PresetName;
	public string APresetName;
	public bool BypassOps;
	public bool Rescale;
	public bool SafeLimits = false;
	private PolyCache polyCache;
	private Coroutine geomCoroutine;

	// Parameters for prismatic forms
	public int PrismP = 5;
	public int PrismQ = 2;

	private ConwayPoly stashed;

	// Fields For Unfolding
	private List<PolyEdge> PolyEdges;
	private List<Face> ConnectedFaces;

	public enum PolyTypeCategories
	{
		All,
		Platonic,
		Prismatic,
		Archimedean,
		KeplerPoinsot,
		Convex,
		Star,
	}


	public enum ColorMethods
	{
		BySides,
		ByRole
	}

	public enum ShapeTypes
	{
		Uniform,
		Grid,
		Johnson,
		Other
	}

	public enum GridShapes
	{
		Plane,
		Torus,
		Cylinder,
		Cone,
//		Conic_Frustum,
//		Mobius,
//		Torus_Trefoil,
//		Klein,
//		Klein2,
//		Roman,
//		Roman_Boy,
//		Cross_Cap,
//		Cross_Cap2,
	}

	public enum GridTypes
	{
		Square,
		Isometric,
		Hex,
		Polar,

		U_3_6_3_6,
		U_3_3_3_4_4,
		U_3_3_4_3_4,
//		U_3_3_3_3_6, TODO Fix
		U_3_12_12,
		U_4_8_8,
		U_3_4_6_4,
		U_4_6_12,
	}

	public enum JohnsonPolyTypes
	{
		Prism,
		Antiprism,

		Pyramid,
		ElongatedPyramid,
		GyroelongatedPyramid,

		Dipyramid,
		ElongatedDipyramid,
		GyroelongatedDipyramid,

		Cupola,
		ElongatedCupola,
		GyroelongatedCupola,

		OrthoBicupola,
		GyroBicupola,
		ElongatedOrthoBicupola,
		ElongatedGyroBicupola,
		GyroelongatedBicupola,

		Rotunda,
		ElongatedRotunda,
		GyroelongatedRotunda,
		GyroelongatedBirotunda,
	}

	public enum OtherPolyTypes
	{
		UvSphere,
		UvHemisphere,
		GriddedCube,

		L1,
		L2
	}

	public enum Ops {

		Identity,
		Dual,
		Kis,
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
		OppositeLace,
		JoinKisKis,
		Stake,
		JoinStake,
		Medial,
		EdgeMedial,
//		JoinedMedial,

		Propeller,
		Whirl,
		Volute,
		Exalt,
		Yank,

		Squall,
		JoinSquall,

		Cross,

		Extrude,
		Shell,
		Skeleton,
		VertexScale,
		VertexRotate,
		VertexFlex,
		FaceOffset,
		FaceScale,
		FaceRotate,
//		Ribbon,
//		FaceTranslate,
//		FaceRotateX,
//		FaceRotateY,
		FaceRemove,
		FaceKeep,
		VertexRemove,
		VertexKeep,
		FillHoles,
		FaceMerge,
		Hinge,
		AddDual,
		AddCopyX,
		AddCopyY,
		AddCopyZ,
		AddMirrorX,
		AddMirrorY,
		AddMirrorZ,
		Stash,
		Unstash,
		UnstashToVerts,
		UnstashToFaces,
		Stack,
		Layer,
		Canonicalize,
//		CanonicalizeI,
		Spherize,
		Recenter,
		SitLevel,
		Stretch,
		Slice,
		Weld,
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
		public float amountSafeMin = -10;
		public float amountSafeMax = 0.999f;
		public bool usesAmount2 = false;
		public float amount2Default = 0;
		public float amount2Min = -20;
		public float amount2Max = 20;
		public float amount2SafeMin = -10;
		public float amount2SafeMax = 0.999f;
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
		public float amount2;
		public float animatedAmount;
		public bool disabled;
		public bool animate;
		public float animationRate;
		public float animationAmount;
		public float audioLowAmount;
		public float audioMidAmount;
		public float audioHighAmount;
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

	private Color[] faceColors;

	void InitConfigs()
	{

		// TODO this has become unweidly now we have so many params
		opconfigs = new Dictionary<Ops, OpConfig>
		{
			{Ops.Identity, new OpConfig {usesAmount = false}},
			{
				Ops.Kis,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = -0.5f, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{Ops.Dual, new OpConfig {usesAmount = false}},
			{Ops.Ambo, new OpConfig {usesAmount = false}},
			{
				Ops.Zip,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -2f, amountMax = 2f, amountSafeMin = 0.0001f, amountSafeMax = .999f,
					usesRandomize = true
				}
			},
			{
				Ops.Expand,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.Bevel,
				new OpConfig
				{
					amountDefault = 0.25f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.4999f,
					usesAmount2 = true,
					amount2Default = 0.25f,
					amount2Min = -6, amount2Max = 6, amount2SafeMin = 0.001f, amount2SafeMax = 0.4999f,
					usesRandomize = true
				}
			},
			{
				Ops.Join,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -1f, amountMax = 2f, amountSafeMin = -0.5f, amountSafeMax = 0.999f
				}
			},
			{ // TODO Support random
				Ops.Needle,
				new OpConfig
				{
					amountDefault = 0f,
					amountMin = -6, amountMax = 6, amountSafeMin = -0.5f, amountSafeMax = 0.5f,
					usesRandomize = true
				}
			},
			{
				Ops.Ortho,
				new OpConfig
				{
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = -0.5f, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{
				Ops.Meta,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0f,
					amountMin = -6, amountMax = 6, amountSafeMin = -0.333f, amountSafeMax = 0.666f,
					usesAmount2 = true,
					amount2Default = 0f,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 0.99f,
					usesRandomize = true
				}
			},
			{
				Ops.Truncate,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.499f,
					usesRandomize = true
				}
			},
			{
				Ops.Gyro,
				new OpConfig
				{
					amountDefault = 0.33f,
					amountMin = -.5f, amountMax = 0.5f, amountSafeMin = 0.001f, amountSafeMax = 0.499f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1
				}
			},
			{
				Ops.Snub,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -1f, amountMax = 1f, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{Ops.Subdivide, new OpConfig
			{
				amountDefault = 0,
				amountMin = -3, amountMax = 3, amountSafeMin = -0.5f, amountSafeMax = 1,
			}},
			{
				Ops.Loft,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -1, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.Chamfer,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.Quinto,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.Lace,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.JoinedLace,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.OppositeLace,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.JoinKisKis,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
				}
			},
			{
				Ops.Stake,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f, amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.JoinStake,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.Medial,
				new OpConfig
				{
					amountDefault = 2f,
					amountMin = 2, amountMax = 8, amountSafeMin = 1, amountSafeMax = 6,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
				}
			},
			{
				Ops.EdgeMedial,
				new OpConfig
				{
					amountDefault = 2f,
					amountMin = 2, amountMax = 8, amountSafeMin = 1, amountSafeMax = 6,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
				}
			},
			// {
			// 	Ops.JoinedMedial,
			// 	new OpConfig
			// 	{
			// 		amountDefault=2f,
			// 		amountMin=2, amountMax=8, amountSafeMin=1, amountSafeMax=4,
			// 		usesAmount2 = true,
			// 		amount2Min = -3, amount2Max = 3, amount2SafeMin = -0.5f, amount2SafeMax = 1,
			// 	}
			// },
			{
				Ops.Propeller,
				new OpConfig
				{
					amountDefault = 0.25f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0f, amountSafeMax = 0.5f
				}
			},
			{
				Ops.Whirl,
				new OpConfig
				{
					amountDefault = 0.25f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.5f
				}
			},
			{
				Ops.Volute,
				new OpConfig
				{
					amountDefault = 0.33f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.Exalt,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{
				Ops.Yank,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.33f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{
				Ops.Cross,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -1, amountMax = 1, amountSafeMin = -1, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},

			{
				Ops.Squall,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.JoinSquall,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},

			{
				Ops.FaceOffset,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = -1, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			//{Ops.Ribbon, new OpConfig{}},
			{
				Ops.Extrude,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{
				Ops.Shell,
				new OpConfig
				{
					amountDefault = 0.1f,
					amountMin = -6, amountMax = 6, amountSafeMin = 0.001f, amountSafeMax = 0.999f
				}
			},
			{
				Ops.Skeleton,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -6, amountSafeMin = 0.001f, amountSafeMax = 0.999f, amountMax = 6
				}
			},
			{
				Ops.VertexScale,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f,
					amountMin = -6, amountMax = 6, amountSafeMin = -1, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			{
				Ops.VertexRotate,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f,
					amountMin = -4, amountMax = 4, amountSafeMin = -1, amountSafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.VertexFlex,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f, amountMin = -6, amountMax = 6, amountSafeMin = -1, amountSafeMax = 0.999f,
					usesRandomize = true
				}
			},
			//{Ops.FaceTranslate, new OpConfig{usesFaces=true, amountDefault=0.1f, amountMin=-6, amountMax=6}},
			{
				Ops.FaceScale,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = -0.5f,
					amountMin = -6, amountMax = 6, amountSafeMin = -1, amountSafeMax = 0,
					usesRandomize = true
				}
			},
			{
				Ops.FaceRotate,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 45f,
					amountMin = -180, amountMax = 180, amountSafeMin = -180, amountSafeMax = 180,
					usesRandomize = true
				}
			},
//			{Ops.FaceRotateX, new OpConfig{usesFaces=true, amountDefault=0.1f, amountMin=-180, amountMax=180}},
//			{Ops.FaceRotateY, new OpConfig{usesFaces=true, amountDefault=0.1f, amountMin=-180, amountMax=180}},
			{Ops.FaceRemove, new OpConfig {usesFaces = true, usesAmount = false}},
			{Ops.FillHoles, new OpConfig {usesAmount = false}},
			{Ops.FaceMerge, new OpConfig {usesFaces = true, usesAmount = false}},
			{Ops.FaceKeep, new OpConfig {usesFaces = true, usesAmount = false}},
			{Ops.VertexRemove, new OpConfig {usesFaces = true, usesAmount = false}},
			{Ops.VertexKeep, new OpConfig {usesFaces = true, usesAmount = false}},
			{
				Ops.Layer,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.1f,
					amountMin = -2f, amountMax = 2f, amountSafeMin = -2f, amountSafeMax = 2f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -1, amount2SafeMax = 1,
					usesRandomize = true
				}
			},
			{
				Ops.Hinge,
				new OpConfig
					{
						amountDefault = 15f,
						amountMin = -180, amountMax = 180, amountSafeMin = 0, amountSafeMax = 180
					}
			},
			{
				Ops.AddDual,
				new OpConfig
				{
					amountDefault = 1f,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddCopyX,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddCopyY,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddCopyZ,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddMirrorX,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddMirrorY,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{
				Ops.AddMirrorZ,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2
				}
			},
			{Ops.Stash, new OpConfig
				{
					usesFaces = true,
					usesAmount = false
				}
			},
			{
				Ops.Unstash,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2,
					usesAmount2 = true,
					amount2Min = -6, amount2Max = 6, amount2SafeMin = -2, amount2SafeMax = 2
				}
			},
			{
				Ops.UnstashToFaces,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -1, amount2SafeMax = 1
				}
			},
			{
				Ops.UnstashToVerts,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0,
					amountMin = -6, amountMax = 6, amountSafeMin = -2, amountSafeMax = 2,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -1, amount2SafeMax = 1
				}
			},
			{
				Ops.Stack,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 0.5f,
					amountMin = -2f, amountMax = 2f, amountSafeMin = -2f, amountSafeMax = 2f,
					usesAmount2 = true,
					amount2Default =  0.8f,
					amount2Min = 0.1f, amount2Max = .9f, amount2SafeMin = .01f, amount2SafeMax = .99f
				}
			},
			{
				Ops.Canonicalize,
				new OpConfig
				{
					amountDefault = 0.1f,
					amountMin = 0.0001f, amountMax = 1f, amountSafeMin = .1f, amountSafeMax = .2f
				}
			},
//			{Ops.CanonicalizeI, new OpConfig{amountDefault=200, amountMin=1, amountMax=400}},
			{
				Ops.Spherize,
				new OpConfig
				{
					usesFaces = true,
					amountDefault = 1.0f, amountMin = -2, amountMax = 2, amountSafeMin = -2,
					amountSafeMax = 2f
				}
			},
			{
				Ops.Stretch,
				new OpConfig
					{
						amountDefault = 1.0f,
						amountMin = -3f, amountMax = 3f, amountSafeMin = -1.5f, amountSafeMax = 1.5f
					}
			},
			{
				Ops.Slice,
				new OpConfig
				{
					amountDefault = 0.5f,
					amountMin = 0f, amountMax = 1f, amountSafeMin = 0f, amountSafeMax = 1f,
					usesAmount2 = true,
					amount2Min = -3, amount2Max = 3, amount2SafeMin = -1, amount2SafeMax = 1
				}
			},
			{Ops.Recenter, new OpConfig {usesAmount = false}},
			{Ops.SitLevel, new OpConfig
			{
				amountDefault = 0,
				amountMin = 0f, amountMax = 1f, amountSafeMin = 0f, amountSafeMax = 1f,
			}},
			{
				Ops.Weld,
				new OpConfig
				{
					amountDefault = 0.001f,
					amountMin = 0, amountMax = .25f, amountSafeMin = 0.001f, amountSafeMax = 0.1f
				}
			}
		};
	}

	void Init()
	{
		InitConfigs();

	faceColors = new[]
	{
		new Color(1.0f, 0.5f, 0.5f),
		new Color(0.8f, 0.85f, 0.9f),
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

		// faceColors = new[] {
		// 	new Color(1.0f, 0.75f, 0.75f),
		// 	new Color(1.0f, 0.5f, 0.5f),
		// 	new Color(0.8f, 0.4f, 0.4f),
		// 	new Color(0.8f, 0.8f, 0.8f),
		// 	new Color(0.5f, 0.6f, 0.6f),
		// 	new Color(0.6f, 0.0f, 0.0f),
		// 	new Color(1.0f, 1.0f, 1.0f),
		// 	new Color(0.6f, 0.6f, 0.6f),
		// 	new Color(0.5f, 1.0f, 0.5f),
		// 	new Color(0.5f, 0.5f, 1.0f),
		// 	new Color(0.5f, 1.0f, 1.0f),
		// 	new Color(1.0f, 0.5f, 1.0f),
		// };

		Debug.unityLogger.logEnabled = EnableLogging;
		InitCacheIfNeeded();
		meshFilter = gameObject.GetComponent<MeshFilter>();
		MakePolyhedron();
	}

	void Start()
	{
		Init();
	}

	void OnEnable()
	{
		Init();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Unfold();
		}
	}

	void InitCacheIfNeeded()
	{
		if (polyCache==null) polyCache = FindObjectOfType<PolyCache>();
		if (polyCache == null)
		{
			enableCaching = false;
		}
	}

	public ConwayPoly MakeGrid(GridTypes gridType, GridShapes gridShape)
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

			case GridTypes.Square:
				conway = ConwayPoly.MakeUnitileGrid(1, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.Isometric:
				conway = ConwayPoly.MakeUnitileGrid(2, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.Hex:
				conway = ConwayPoly.MakeUnitileGrid(3, (int)gridShape, PrismP, PrismQ);
				break;

			case GridTypes.U_3_6_3_6:
				conway = ConwayPoly.MakeUnitileGrid(4, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.U_3_3_3_4_4:
				conway = ConwayPoly.MakeUnitileGrid(5, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.U_3_3_4_3_4:
				conway = ConwayPoly.MakeUnitileGrid(6, (int)gridShape, PrismP, PrismQ);
				break;
//			case GridTypes.U_3_3_3_3_6:
//				conway = ConwayPoly.MakeUnitileGrid(7, (int)gridShape, PrismP, PrismQ);
//				break;
			case GridTypes.U_3_12_12:
				conway = ConwayPoly.MakeUnitileGrid(8, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.U_4_8_8:
				conway = ConwayPoly.MakeUnitileGrid(9, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.U_3_4_6_4:
				conway = ConwayPoly.MakeUnitileGrid(10, (int)gridShape, PrismP, PrismQ);
				break;
			case GridTypes.U_4_6_12:
				conway = ConwayPoly.MakeUnitileGrid(11, (int)gridShape, PrismP, PrismQ);
				break;

			case GridTypes.Polar:
				conway = ConwayPoly.MakePolarGrid(PrismP, PrismQ);
				break;
		}
		// Welding only seems to work reliably on simpler shapres
		if (gridShape != GridShapes.Plane) conway = conway.Weld(0.0001f);

		return conway;
	}

	public ConwayPoly MakeJohnsonPoly(JohnsonPolyTypes johnsonPolyType)
	{
		ConwayPoly poly;

		switch (johnsonPolyType)
		{
			case JohnsonPolyTypes.Prism:
				poly = JohnsonPoly.Prism(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.Antiprism:
				poly = JohnsonPoly.Antiprism(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.Pyramid:
				poly = JohnsonPoly.Pyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.ElongatedPyramid:
				poly = JohnsonPoly.ElongatedPyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.GyroelongatedPyramid:
				poly = JohnsonPoly.GyroelongatedPyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.Dipyramid:
				poly = JohnsonPoly.Dipyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.ElongatedDipyramid:
				poly = JohnsonPoly.ElongatedBipyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.GyroelongatedDipyramid:
				poly = JohnsonPoly.GyroelongatedBipyramid(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.Cupola:
				poly = JohnsonPoly.Cupola(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.ElongatedCupola:
				poly = JohnsonPoly.ElongatedCupola(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.GyroelongatedCupola:
				poly = JohnsonPoly.GyroelongatedCupola(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.OrthoBicupola:
				poly = JohnsonPoly.OrthoBicupola(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.GyroBicupola:
				poly = JohnsonPoly.GyroBicupola(PrismP<3?3:PrismP);
				break;
			case JohnsonPolyTypes.ElongatedOrthoBicupola:
				poly = JohnsonPoly.ElongatedBicupola(PrismP<3?3:PrismP, false);
				break;
			case JohnsonPolyTypes.ElongatedGyroBicupola:
				poly = JohnsonPoly.ElongatedBicupola(PrismP<3?3:PrismP, true);
				break;
			case JohnsonPolyTypes.GyroelongatedBicupola:
				poly = JohnsonPoly.GyroelongatedBicupola(PrismP<3?3:PrismP, false);
				break;
			// The distinction between these two is simply one of chirality
			// case JohnsonPolyTypes.GyroelongatedOrthoBicupola:
			// 	poly = JohnsonPoly.GyroElongatedBicupola(PrismP<3?3:PrismP, false);
			// 	break;
			// case JohnsonPolyTypes.GyroelongatedGyroBicupola:
			// 	poly = JohnsonPoly.GyroElongatedBicupola(PrismP<3?3:PrismP, true);
			// 	break;
			case JohnsonPolyTypes.Rotunda:
				poly = JohnsonPoly.Rotunda();
				break;
			case JohnsonPolyTypes.ElongatedRotunda:
				poly = JohnsonPoly.ElongatedRotunda();
				break;
			case JohnsonPolyTypes.GyroelongatedRotunda:
				poly = JohnsonPoly.GyroelongatedRotunda();
				break;
			case JohnsonPolyTypes.GyroelongatedBirotunda:
				poly = JohnsonPoly.GyroelongatedBirotunda();
				break;
			default:
				Debug.LogError("Unknown Johnson Poly Type");
				return null;
		}

		poly.Recenter();
		return poly;
	}

	public ConwayPoly MakeOtherPoly(OtherPolyTypes otherPolyType)
	{
		switch (otherPolyType)
		{
			case OtherPolyTypes.UvSphere:
				return JohnsonPoly.UvSphere(PrismP, PrismQ);
			case OtherPolyTypes.UvHemisphere:
				return JohnsonPoly.UvHemisphere();
			case OtherPolyTypes.L1:
				return JohnsonPoly.L1();
			case OtherPolyTypes.L2:
				return JohnsonPoly.L2();
			case OtherPolyTypes.GriddedCube:
				var conway = ConwayPoly.MakeUnitileGrid(1, 0, PrismP, PrismP);
				conway = conway.AddMirrored(Vector3.up, PrismP);
				conway.Recenter();
				ConwayPoly xPair = conway.Rotate(Vector3.forward, 90);
				ConwayPoly zPair = conway.Rotate(Vector3.left, 90);
				conway.Append(xPair);
				conway.Append(zPair);
				return conway.Weld(0.0001f);
				return conway;
			default:
				Debug.LogError("Unknown Other Poly Type");
				return null;
		}
	}

	private void MakePolyhedron(bool disableThreading=false)
	{
		if (ShapeType == ShapeTypes.Uniform)
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
		else if (ShapeType == ShapeTypes.Grid)
		{
			_conwayPoly = MakeGrid(GridType, GridShape);
		}

		else if (ShapeType == ShapeTypes.Johnson)
		{
			_conwayPoly = MakeJohnsonPoly(JohnsonPolyType);
		}

		else if (ShapeType == ShapeTypes.Other)
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
		#if UNITY_EDITOR
			// To prevent values getting out of sync
			// ignore the inspector UI if we're showing the runtime UI
			if (polyUI == null || (polyUI != null && EditorApplication.isPlayingOrWillChangePlaymode)) return;
		#endif

		InitCacheIfNeeded();

		if (ShapeType == ShapeTypes.Uniform)
		{
			if (PrismP < 3) {PrismP = 3;}
			if (PrismP > 16) PrismP = 16;
			if (PrismQ > PrismP - 2) PrismQ = PrismP - 2;
			if (PrismQ < 2) PrismQ = 2;
		}

		// Control the amount variables to some degree
		for (var i = 0; i < ConwayOperators.Count; i++)
		{
			if (opconfigs == null) continue;
			var op = ConwayOperators[i];
			if (opconfigs[op.opType].usesAmount)
			{
				op.amount = Mathf.Round(op.amount * 1000) / 1000f;
				op.amount2 = Mathf.Round(op.amount2 * 1000) / 1000f;

				float opMin, opMax;
				if (SafeLimits)
				{
					opMin = opconfigs[op.opType].amountSafeMin;
					opMax = opconfigs[op.opType].amountSafeMax;
				}
				else
				{
					opMin = opconfigs[op.opType].amountMin;
					opMax = opconfigs[op.opType].amountMax;
				}
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
		_faceCount = _conwayPoly.Faces.Count;
		_vertexCount = _conwayPoly.Vertices.Count;

		if (enableCaching)
		{
			var cacheKeySource = PolyToJson();
			int key = cacheKeySource.GetHashCode();
			var mesh = polyCache.GetMesh(key);
			if (mesh == null)
			{
				mesh = BuildMeshFromConwayPoly();
				polyCache.SetMesh(key, mesh);
			}
			AssignFinishedMesh(mesh);
		}
		else
		{
			var mesh = BuildMeshFromConwayPoly();
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

		switch (op.opType)
		{
			case Ops.Identity:
				break;
			case Ops.Kis:
				conway = conway.Kis(amount, op.faceSelections, op.randomize);
				break;
			case Ops.Dual:
				conway = conway.Dual();
				break;
			case Ops.Ambo:
				conway = conway.Ambo();
				break;
			case Ops.Zip:
				conway = conway.Zip(amount);
				break;
			case Ops.Expand:
				conway = conway.Expand(amount);
				break;
			case Ops.Bevel:
				conway = conway.Bevel(amount, op.amount2);
				break;
			case Ops.Join:
				conway = conway.Join(amount);
				break;
			case Ops.Needle:
				conway = conway.Needle(amount, op.randomize);
				break;
			case Ops.Ortho:
				conway = conway.Ortho(amount, op.randomize);
				break;
			case Ops.Meta:
				conway = conway.Meta(amount, op.amount2, op.faceSelections, op.randomize);
				break;
			case Ops.Truncate:
				conway = conway.Truncate(amount, op.faceSelections, op.randomize);
				break;
			case Ops.Gyro:
				conway = conway.Gyro(amount, op.amount2);
				break;
			case Ops.Snub:
				conway = conway.Gyro(amount);
				conway = conway.Dual();
				break;
			case Ops.Exalt:
				// TODO return a correct VertexRole array
				// I suspect the last vertices map to the original shape verts
				conway = conway.Dual();
				conway = conway.Kis(amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				conway = conway.Kis(amount, op.faceSelections, op.randomize);
				break;
			case Ops.Yank:
				conway = conway.Kis(amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				conway = conway.Kis(amount, op.faceSelections, op.randomize);
				conway = conway.Dual();
				break;
			case Ops.Subdivide:
				conway = conway.Subdivide(amount);
				break;
			case Ops.Loft:
				conway = conway.Loft(amount, op.amount2, op.faceSelections, op.randomize);
				break;
			case Ops.Chamfer:
				conway = conway.Chamfer(amount);
				break;
			case Ops.Quinto:
				conway = conway.Quinto(amount, op.amount2, op.randomize);
				break;
			case Ops.JoinedLace:
				conway = conway.JoinedLace(amount, op.amount2, op.randomize);
				break;
			case Ops.OppositeLace:
				conway = conway.OppositeLace(amount, op.amount2, op.randomize);
				break;
			case Ops.Lace:
				conway = conway.Lace(amount, op.faceSelections, op.amount2, op.randomize);
				break;
			case Ops.JoinKisKis:
				conway = conway.JoinKisKis(amount, op.amount2);
				break;
			case Ops.Stake:
				conway = conway.Stake(amount, op.faceSelections);
				break;
			case Ops.JoinStake:
				conway = conway.Stake(amount, op.faceSelections, true);
				break;
			case Ops.Medial:
				conway = conway.Medial((int)amount, op.amount2);
				break;
			case Ops.EdgeMedial:
				conway = conway.EdgeMedial((int)amount, op.amount2);
				break;
			// case Ops.JoinedMedial:
			// 	conway = conway.JoinedMedial((int)amount, op.amount2);
			// 	break;
			case Ops.Propeller:
				conway = conway.Propeller(amount);
				break;
			case Ops.Whirl:
				conway = conway.Whirl(amount);
				break;
			case Ops.Volute:
				conway = conway.Volute(amount);
				break;
			case Ops.Cross:
				conway = conway.Cross(amount);
				break;
			case Ops.Squall:
				conway = conway.Squall(amount, false);
				break;
			case Ops.JoinSquall:
				conway = conway.Squall(amount, true);
				break;
			case Ops.Shell:
				// TODO do this properly with shared edges/vertices
				conway = conway.Extrude(amount, false, op.randomize);
				break;
			case Ops.Skeleton:
				conway = conway.FaceRemove(op.faceSelections);
				if ((op.faceSelections==ConwayPoly.FaceSelections.New || op.faceSelections==ConwayPoly.FaceSelections.NewAlt) && op.opType == Ops.Skeleton)
				{
					// Nasty hack until I fix extrude
					// Produces better results specific for PolyMidi
					conway = conway.FaceScale(0f, ConwayPoly.FaceSelections.All, false);
				}
				conway = conway.Extrude(amount, false, op.randomize);
				break;
			case Ops.Extrude:
				conway = conway.Loft(0, amount, op.faceSelections, op.randomize);

				//conway = conway.Extrude(amount, op.faceSelections, op.randomize);
				// if (op.faceSelections == ConwayPoly.FaceSelections.All)
				// {
				// 	conway = conway.FaceScale(0f, ConwayPoly.FaceSelections.All, false);
				// 	conway = conway.Extrude(amount, false, op.randomize);
				// }
				// else
				// {
				// 	// TODO do this properly with shared edges/vertices
				// 	var included = conway.FaceRemove(op.faceSelections, true);
				// 	included = included.FaceScale(0, ConwayPoly.FaceSelections.All, false);
				// 	var excluded = conway.FaceRemove(op.faceSelections, false);
				// 	conway = included.Extrude(amount, false, op.randomize);
				// 	conway.Append(excluded);
				// }
				break;
			case Ops.VertexScale:
				conway = conway.VertexScale(amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceMerge:
				conway = conway.FaceMerge(op.faceSelections);
				break;
			case Ops.VertexRotate:
				conway = conway.VertexRotate(amount, op.faceSelections, op.randomize);
				break;
			case Ops.VertexFlex:
				conway = conway.VertexFlex(amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceOffset:
				// TODO Faceroles ignored. Vertex Roles
				// Split faces
				var origRoles = conway.FaceRoles;
				conway = conway.FaceScale(0, ConwayPoly.FaceSelections.All, false);
				conway.FaceRoles = origRoles;
				conway = conway.Offset(amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceScale:
				conway = conway.FaceScale(amount, op.faceSelections, op.randomize);
				break;
			case Ops.FaceRotate:
				conway = conway.FaceRotate(amount, op.faceSelections, 0, op.randomize);
				break;
//					case Ops.Ribbon:
//						conway = conway.Ribbon(amount, false, 0.1f);
//						break;
//					case Ops.FaceTranslate:
//						conway = conway.FaceTranslate(amount, op.faceSelections);
//						break;
//					case Ops.FaceRotateX:
//						conway = conway.FaceRotate(amount, op.faceSelections, 1);
//						break;
//					case Ops.FaceRotateY:
//						conway = conway.FaceRotate(amount, op.faceSelections, 2);
//						break;
			case Ops.FaceRemove:
				conway = conway.FaceRemove(op.faceSelections);
				break;
			case Ops.FaceKeep:
				conway = conway.FaceKeep(op.faceSelections);
				break;
			case Ops.VertexRemove:
				conway = conway.VertexRemove(op.faceSelections, false);
				break;
			case Ops.VertexKeep:
				conway = conway.VertexRemove(op.faceSelections, true);
				break;
			case Ops.FillHoles:
				conway.FillHoles();
				break;
			case Ops.Hinge:
				conway = conway.Hinge(amount);
				break;
			case Ops.AddDual:
				conway = conway.AddDual(amount);
				break;
			case Ops.AddCopyX:
				conway = conway.AddCopy(Vector3.right, amount, op.faceSelections);
				break;
			case Ops.AddCopyY:
				conway = conway.AddCopy(Vector3.up, amount, op.faceSelections);
				break;
			case Ops.AddCopyZ:
				conway = conway.AddCopy(Vector3.forward, amount, op.faceSelections);
				break;
			case Ops.AddMirrorX:
				conway = conway.AddMirrored(Vector3.right, amount, op.faceSelections);
				break;
			case Ops.AddMirrorY:
				conway = conway.AddMirrored(Vector3.up, amount, op.faceSelections);
				break;
			case Ops.AddMirrorZ:
				conway = conway.AddMirrored(Vector3.forward, amount, op.faceSelections);
				break;
			case Ops.Stash:
				stash = conway.Duplicate();
				stash = stash.FaceKeep(op.faceSelections);
				break;
			case Ops.Unstash:
				if (stash == null) return conway;
				var dup = conway.Duplicate();
				var offset = Vector3.up * op.amount2;
				dup.Append(stash.FaceKeep(op.faceSelections), offset, Quaternion.identity, amount);
				conway = dup;
				break;
			case Ops.UnstashToFaces:
				if (stash == null) return conway;
				conway = conway.AppendMany(stash, op.faceSelections, amount, 0, op.amount2, true);
				break;
			case Ops.UnstashToVerts:
				if (stash == null) return conway;
				conway = conway.AppendMany(stash, op.faceSelections, amount, 0, op.amount2, false);
				break;
			case Ops.Layer:
				conway = conway.Layer(4, 1f - amount, amount / 10f, op.faceSelections);
				break;
			case Ops.Canonicalize:
				conway = conway.Canonicalize(amount, amount);
				break;
			case Ops.Spherize:
				conway = conway.Spherize(op.faceSelections, amount);
				break;
			case Ops.Recenter:
				conway.Recenter();
				break;
			case Ops.SitLevel:
				conway = conway.SitLevel(amount);
				break;
			case Ops.Stretch:
				conway = conway.Stretch(amount);
				break;
			case Ops.Slice:
				conway = conway.Slice(amount, op.amount2);
				break;
			case Ops.Stack:
				conway = conway.Stack(Vector3.up, amount, op.amount2, 0.1f, op.faceSelections);
				conway.Recenter();
				break;
			case Ops.Weld:
				conway = conway.Weld(amount);
				break;
		}

		return conway;
	}

	public void ApplyOps()
	{

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

	public Mesh BuildMeshFromConwayPoly()
	{
		return BuildMeshFromConwayPoly(this._conwayPoly);
	}


	public Mesh BuildMeshFromConwayPoly(ConwayPoly conway)
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

		// TODO
		// var hasNaked = conway.HasNaked();

		// Strip down to Face-Vertex structure
		var points = conway.ListVerticesByPoints();
		var faceIndices = conway.ListFacesByVertexIndices();

		// Add faces
		int index = 0;

		for (var i = 0; i < faceIndices.Length; i++)
		{

			var faceIndex = faceIndices[i];
			var face = conway.Faces[i];
			var faceNormal = face.Normal;
			var faceCentroid = face.Centroid;

			// Axes for UV mapping
			var xAxis = face.Halfedge.Vector;
			var yAxis = Vector3.Cross(xAxis, faceNormal);

			Color32 color;

			// TODO why do we need to do this check?
			if (conway.FaceRoles != null && faceColors != null)
			{
				switch (ColorMethod)
				{
					case ColorMethods.ByRole:
						color = faceColors[(int) conway.FaceRoles[i]];
						break;
					case ColorMethods.BySides:
						color = faceColors[face.Sides % faceColors.Length];
						break;
					default:
						color = Color.red;
						break;
				}
			}
			else
			{
				color = Color.white;
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

			float faceScale = 0;
			foreach (var v in face.GetVertices())
			{
				faceScale += Vector3.Distance(v.Position, faceCentroid);
			}
			faceScale /= face.Sides;

			var miscUV = new Vector4(faceScale, face.Sides, faceCentroid.magnitude, ((float)i)/faceIndices.Length);

			if (face.Sides > 3)
			{
				for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
				{

					meshVertices.Add(faceCentroid);
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
		}

		target.vertices = meshVertices.Select(x => Jitter(x)).ToArray();
		target.normals = meshNormals.ToArray();
		target.triangles = meshTriangles.ToArray();
		target.colors32 = meshColors.ToArray();
		target.SetUVs(0, meshUVs);
		target.SetUVs(1, edgeUVs);
		target.SetUVs(2, barycentricUVs);
		target.SetUVs(3, miscUVs);

		target.RecalculateTangents();
		return target;
	}

	// Returns true if at least one face matches the facesel rule but all of them
	public bool FaceSelectionIsValid(ConwayPoly.FaceSelections facesel)
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
		OpConfig opConfig;
		try
		{
			opConfig = opconfigs[opType];
		}
		catch (Exception e)
		{
			Debug.LogWarning($"opType: {opType} opconfigs count: {opconfigs.Count}");
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

	public void Unfold() {
		ConwayPoly toBeUnfolded = GetConwayPoly();
		MeshFaceList unfoldFaces = toBeUnfolded.Faces;
		MeshHalfedgeList unfoldHalfedges = toBeUnfolded.Halfedges;
		PolyEdges = new List<PolyEdge>();
		List<Halfedge> CheckedHalfEdges = new List<Halfedge>();
		ConnectedFaces = new List<Face>();
		foreach (Halfedge h in unfoldHalfedges)
		{
			if (CheckedHalfEdges.Contains(h)){
				continue;
			} else
			{
				CheckedHalfEdges.Add(h);
				CheckedHalfEdges.Add(h.Pair);
				PolyEdges.Add(new PolyEdge(h.Face, h.Pair.Face, h, h.Pair));
			}
		}
		Debug.Log("Amount Of Faces: " + unfoldFaces.Count);
		Debug.Log("Amount Of Edges: " + PolyEdges.Count);
		PolyNode root = new PolyNode(unfoldFaces[0], null);
		List<PolyNode> children = AddChildren(root);
		List<PolyNode> queue = new List<PolyNode>();
		List<PolyNode> branched = new List<PolyNode>();
		foreach (PolyNode c in children)
		{
			queue.Add(c);
			branched.Add(c);
		}
		while (queue.Count != 0)
		{
			children = AddChildren(queue[0]);
			foreach (PolyNode c in children)
			{
				if (!queue.Contains(c))
				{
					if (!branched.Contains(c))
					{
						branched.Add(c);
					}
					queue.Add(c);
				}
			}
			queue.RemoveAt(0);
		}
		int tabs = 0;
		foreach (PolyEdge e in PolyEdges)
		{
			if (e.IsBranched())
			{
				Debug.Log(e.ToString());
			}
			if (e.IsTabbed())
			{
				tabs++;
			}
		}
		Debug.Log("Required Tabs: " + tabs.ToString());
	}

	private List<PolyNode> AddChildren(PolyNode c)
	{
		List<Face> sharedFaces = GetSharedFaces(c.GetID());
		foreach (Face sf in sharedFaces)
		{
			c.AddChild(sf);
			foreach (PolyEdge e in PolyEdges)
			{
				if ((e.GetFace1() == c.GetID() && e.GetFace2() == sf) || (e.GetFace2() == c.GetID() && e.GetFace1() == sf))
				{
					e.SetBranched(true);
				}
			}
		}
		return c.GetChildren();
	}

	private List<Face> GetSharedFaces(Face f)
	{
		List<Face> sharedFaces = new List<Face>();
		foreach(PolyEdge e in PolyEdges)
		{
			if (e.GetFace1() == f)
			{
				if (ConnectedFaces.Contains(e.GetFace2()))
				{
					if (e.IsBranched())
					{
						continue;
					} else {
						e.SetTabbed(true);
					}
				} else {
					sharedFaces.Add(e.GetFace2());
					ConnectedFaces.Add(e.GetFace1());
					ConnectedFaces.Add(e.GetFace2());
					e.SetEdgeChecked(true);
				}
			}
			else if (e.GetFace2() == f)
			{
				if (ConnectedFaces.Contains(e.GetFace1()))
				{
					if (e.IsBranched())
					{
						continue;
					} else {
						e.SetTabbed(true);
					}
				} else
				{
					sharedFaces.Add(e.GetFace1());
					ConnectedFaces.Add(e.GetFace1());
					ConnectedFaces.Add(e.GetFace2());
					e.SetEdgeChecked(true);
				}
			}
		}
		return sharedFaces;
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
				Handles.Label(Vector3.Scale(face.Centroid, transform.lossyScale) + new Vector3(0, .03f, 0), face.Sides.ToString());
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
