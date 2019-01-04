using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conway;
using Newtonsoft.Json;
using Wythoff;
using UnityEditor;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PolyHydra : MonoBehaviour {
	
	private const bool enableThreading = true;
	private const bool enableCaching = true;   
	const int MAX_CACHE_LENGTH = 5000;
	
	private int _faceCount;
	private int _vertexCount;
	
	public PolyTypes PolyType;
	public GridTypes GridType;
	public string WythoffSymbol;
	public string PresetName;
	public string APresetName;
	public bool BypassOps;
	public bool TwoSided;
	public bool ReScale;
	
	// Parameters for prismatic forms
	public int PrismP = 5;
	public int PrismQ = 2;
	
	public AppearancePreset.ColorMethods ColorMethod;

	public enum GridTypes
	{
		Square,
		Isometric,
		Hex
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
		Quinto,
		Lace,
		JoinedLace,
		Stake,
//		Medial,
//		EdgeMedial,
//		JoinedMedial,
		Propeller,
		Whirl,
		Volute,
		Exalt,
		Yank,
		Offset,
		Extrude,
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
		AddDual,
		Canonicalize,
		CanonicalizeI
	}

	public string GetInfoText()
	{
		string infoText = $"Faces: {_faceCount}\nVertices: {_vertexCount}";
		return infoText;
	}

	public class OpConfig
	{
		public bool usesAmount = true;
		public float amountMin = -20;
		public float amountMax = 20;
		public bool usesFaces = false;
		public bool usesRandomize = false;
		public ConwayPoly.FaceSelections faceSelection = ConwayPoly.FaceSelections.All;	
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
	private Dictionary<string, WythoffPoly> _wythoffCache;
	private ConwayPoly conway;

	private MeshFilter meshFilter;
	private PolyPreset previousState;

	public PolyUI polyUI;

	private bool finishedOpsThread = true;
	private Thread thread;

	private struct ConwayCacheEntry
	{
		public ConwayPoly conway;
		public long timestamp;

		public ConwayCacheEntry(ConwayPoly c, long t)
		{
			conway = c;
			timestamp = t;
		}
	}
	private Dictionary<int, ConwayCacheEntry> _conwayCache;
	
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
		opconfigs = new Dictionary<Ops, OpConfig>()
		{	
			{Ops.Identity, new OpConfig {usesAmount=false}},
			{Ops.Kis, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Dual, new OpConfig{usesAmount=false}},
			{Ops.Ambo, new OpConfig{usesAmount=false}},
			{Ops.Zip, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Expand, new OpConfig{usesAmount=false}},
			{Ops.Bevel, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Join, new OpConfig{usesAmount=false}},
			{Ops.Needle, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Ortho, new OpConfig{usesAmount=false}},
			{Ops.Meta, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Truncate, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Gyro, new OpConfig{amountMin = -.5f, amountMax = 0.5f}},
			{Ops.Snub, new OpConfig{amountMin = -.5f, amountMax = 0.5f}},
			{Ops.Subdivide, new OpConfig {usesAmount=false}},
			{Ops.Loft, new OpConfig {usesFaces=true, amountMin = -4, amountMax = 4}},
			{Ops.Quinto, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Lace, new OpConfig{usesFaces=true, amountMin = -4, amountMax = 4}},
			{Ops.JoinedLace, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Stake, new OpConfig{usesFaces=true, amountMin = -4, amountMax = 4}},
//			{Ops.Medial, new OpConfig{usesAmount=false}},
//			{Ops.EdgeMedial, new OpConfig{amountMin = -4, amountMax = 4}},
//			{Ops.JoinedMedial, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Propeller, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Whirl, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Volute, new OpConfig{amountMin = -4, amountMax = 4}},
			{Ops.Exalt, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.Yank, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.Chamfer new OpConfig{}},
			{Ops.FaceOffset, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.Ribbon, new OpConfig{}},
			{Ops.Extrude, new OpConfig{amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.VertexScale, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			//{Ops.FaceTranslate, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.FaceScale, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6, usesRandomize=true}},
			{Ops.FaceRotate, new OpConfig{usesFaces=true, amountMin = -180, amountMax = 180, usesRandomize=true}},
//			{Ops.FaceRotateX, new OpConfig{usesFaces=true, amountMin = -180, amountMax = 180}},
//			{Ops.FaceRotateY, new OpConfig{usesFaces=true, amountMin = -180, amountMax = 180}},
			//{Ops.Test, new OpConfig{}}
			{Ops.FaceRemove, new OpConfig{usesFaces=true, usesAmount=false}},
			{Ops.FaceKeep, new OpConfig{usesFaces=true, usesAmount=false}},
			{Ops.AddDual, new OpConfig{amountMin = -6, amountMax = 6}},
			{Ops.Canonicalize, new OpConfig{amountMin = 0.0001f, amountMax = 1f}},
			{Ops.CanonicalizeI, new OpConfig{amountMin = 1, amountMax = 400}}
		};
	}

	void Start()
	{
		meshFilter = gameObject.GetComponent<MeshFilter>();
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
	
	public void MakePolyhedron()
	{
		Mesh mesh;
		bool noOps = BypassOps || ConwayOperators == null || ConwayOperators.Count < 1;
		
		if (noOps)
		{
			if (PolyType > 0)  // Uniform Polys
			{
				MakeWythoff();
				mesh = BuildMeshFromWythoffPoly(WythoffPoly);
				AssignFinishedMesh(mesh);
				
			}
			else  // Special cases. Currently just a grid
			{
				conway = MakeGrid(GridType);
				mesh = BuildMeshFromConwayPoly(true);  // Might as well always do two-sided if no ops are applied
				AssignFinishedMesh(mesh);
			}
			
			return;
		}
		
		if (PolyType > 0) // Uniform Polys
		{
			MakeWythoff();
			conway = new ConwayPoly(WythoffPoly);			
		}
		else  // Special cases. Currently just a grid
		{
			conway = MakeGrid(GridType);
		}

		if (!enableThreading)
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
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
		#endif
		
		if (PrismP < 3) PrismP = 3;
		if (PrismP > 16) PrismP = 16;
		if (PrismQ > PrismP - 2) PrismQ = PrismP - 2;
		if (PrismQ < 2) PrismQ = 2;
		
		var currentState = new PolyPreset();
		currentState.CreateFromPoly("temp", this);
		if (previousState != currentState)
		{
			MakePolyhedron();
			previousState = currentState;
		}

	}

	public void MakeWythoff() {
		
		if (!String.IsNullOrEmpty(WythoffSymbol))
		{
			MakeWythoff(WythoffSymbol);
		}
		else
		{
			MakeWythoff((int)PolyType);
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
			if (_wythoffCache==null) _wythoffCache = new Dictionary<string, WythoffPoly>();
			if (_wythoffCache.ContainsKey(symbol))
			{
				WythoffPoly = _wythoffCache[symbol];
			}
			else
			{
				WythoffPoly = new WythoffPoly(symbol);
				_wythoffCache[symbol] = WythoffPoly;
			}
			
		}
		WythoffPoly.BuildFaces(BuildAux: BypassOps);
		
		//_faceCount = WythoffPoly.FaceCount;
		//_vertexCount = WythoffPoly.VertexCount;
	}
	
	public void FinishedApplyOps()
	{
		_faceCount = conway.Faces.Count;
		_vertexCount = conway.Vertices.Count;
		
		var mesh = BuildMeshFromConwayPoly(TwoSided);
		AssignFinishedMesh(mesh);
	}

	public void AssignFinishedMesh(Mesh mesh)
	{
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		
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

	private void ApplyOps()
	{
		
		var cacheKeySource = $"{PolyType} {PrismP} {PrismQ}";
		
		foreach (var op in ConwayOperators.ToList())
		{
			
			if (op.disabled) continue;
			
			cacheKeySource += JsonConvert.SerializeObject(op);
			if (_conwayCache == null) _conwayCache = new Dictionary<int, ConwayCacheEntry>();
			if (enableCaching &&_conwayCache.ContainsKey(cacheKeySource.GetHashCode()))
			{
				conway = _conwayCache[cacheKeySource.GetHashCode()].conway;
			}
			else
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
						conway = conway.Ambo();
						conway = conway.Ambo();
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
						conway = conway.Ambo();
						conway = conway.Ambo();
						conway = conway.Dual();
						break;
					case Ops.Meta:
						conway = conway.Ambo();
						conway = conway.Dual();
						conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
						break;
					case Ops.Truncate:
						conway = conway.Dual();
						conway = conway.Kis(op.amount, op.faceSelections, op.randomize);
						conway = conway.Dual();
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
//					case Ops.Medial:
//						conway = conway.Medial((int)op.amount);
//						break;
//					case Ops.EdgeMedial:
//						conway = conway.EdgeMedial((int)op.amount);
//						break;
//					case Ops.JoinedMedial:
//						conway = conway.JoinedMedial();
//						break;
					case Ops.Propeller:
						conway = conway.Propeller(op.amount);
						break;
					case Ops.Whirl:
						conway = conway.Whirl(op.amount);
						break;
					case Ops.Volute:
						conway = conway.Volute(op.amount);
						break;
					case Ops.Extrude:
						// Split faces
						conway = conway.FaceScale(0, ConwayPoly.FaceSelections.All, false);
						conway = conway.Extrude(op.amount, false, op.randomize);
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
					case Ops.AddDual:
						conway = conway.AddDual(op.amount);
						break;
					case Ops.Canonicalize:
						conway = conway.Canonicalize(op.amount, op.amount);
						break;
					case Ops.CanonicalizeI:
						conway = conway.Canonicalize((int)op.amount, (int)op.amount);
						break;
				}

				if (enableCaching)
				{
					var cached = new ConwayCacheEntry(conway, DateTime.UtcNow.Ticks);
					_conwayCache[cacheKeySource.GetHashCode()] = cached;
					if (_conwayCache.Count > MAX_CACHE_LENGTH)
					{
						// Cull half the cache
						var ordered = _conwayCache.OrderBy(kv => kv.Key);
						var half = _conwayCache.Count/2;
						_conwayCache = ordered.Skip(half).ToDictionary(kv => kv.Key, kv => kv.Value);
					}					
				}

			}
		}

		if (ReScale)
		{
			conway.ScalePolyhedra();			
		}
	}
	
	public Mesh BuildMeshFromWythoffPoly(WythoffPoly source)
	{
		
		var meshVertices = new List<Vector3>();
		var meshTriangles = new List<int>();
		var MeshVertexToVertex = new List<int>(); // Mapping of mesh vertices to poly vertices (one to many as we duplicate verts)
		var meshColors = new List<Color>();
		
		var mesh = new Mesh();
		int meshVertexIndex = 0;

		foreach (Wythoff.Face face in source.faces) {
			face.CalcTriangles();
		}

		for (int faceType = 0; faceType < source.FaceTypeCount; faceType++) {
			foreach (Wythoff.Face face in source.faces) {
				if (face.configuration == source.FaceSidesByType[faceType]) {
					var faceColor = faceColors[(int) ((face.configuration + 2) % faceColors.Length)];
					// Vertices
					for (int i = 0; i < face.triangles.Length; i++) {
						Vector v = source.Vertices[face.triangles[i]];
						meshVertices.Add(v.getVector3());
						meshColors.Add(faceColor);
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
		mesh.RecalculateNormals();
		return mesh;

	}
	
	// Essentially Kis only on non-triangular faces
	// Returns the original number of sides of each face to be used elsewhere
	// TODO Detect convex faces and use fan triangulation to save on a vertex?
	public List<int> KisTriangulate() {
        
		var faceRoles = new List<ConwayPoly.Roles>();
		var vertexRoles = new List<ConwayPoly.Roles>();
		
		var newVerts = conway.Faces.Select(f => f.Centroid);
		var vertexPoints = Enumerable.Concat(conway.Vertices.Select(v => v.Position), newVerts);
		vertexRoles.Concat(Enumerable.Repeat(ConwayPoly.Roles.Existing, vertexPoints.Count()));
		var originalFaceSides = new List<int>();
            
		// vertex lookup
		var vlookup = new Dictionary<string, int>();
		int n = conway.Vertices.Count;
		for (int i = 0; i < n; i++) {
			vlookup.Add(conway.Vertices[i].Name, i);
		}

		var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
		for (int i = 0; i < conway.Faces.Count; i++)
		{
			int faceSides = conway.Faces[i].Sides;
			if (conway.Faces[i].Sides <= 3) {
				faceIndices.Add(conway.ListFacesByVertexIndices()[i]);
				originalFaceSides.Add(faceSides);
				faceRoles.Add(conway.FaceRoles[i]);
			} else {
				foreach (var edge in conway.Faces[i].GetHalfedges()) {
					// create new face from edge start, edge end and centroid
					faceIndices.Add(
						new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n}
					);
					originalFaceSides.Add(faceSides);
					faceRoles.Add(conway.FaceRoles[i]);
				}
			}
		}

		conway = new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
		
		
		
		return originalFaceSides;
	} 

	public Mesh BuildMeshFromConwayPoly(bool forceTwosided)
	{
		//var originalFaceSides = KisTriangulate();
		
		var target = new Mesh();
		var meshTriangles = new List<int>();
		var meshVertices = new List<Vector3>();
		var meshNormals = new List<Vector3>();
		var meshColors = new List<Color32>();
		
		var hasNaked = conway.HasNaked();
		hasNaked = false;  // TODO
		
		
		
		
//		for (int i = 0; i < conway.Faces.Count; i++)
//		{
//			if (conway.Faces[i].Sides > 3)
//			{
//				conway.Faces.Triangulate(i, false);
//			}
//		}
		
		
		
		
		// Strip down to Face-Vertex structure
		var points = conway.ListVerticesByPoints();
		var faceIndices = conway.ListFacesByVertexIndices();

		// Add faces
		int index = 0;
		
		for (var i = 0; i < faceIndices.Length; i++) {
			
			var faceIndex = faceIndices[i];
			var face = conway.Faces[i];
			var faceNormal = face.Normal;
			
			Color32 color;
			switch (ColorMethod)
			{
				case AppearancePreset.ColorMethods.ByRole:
					color = faceColors[(int) conway.FaceRoles[i]];
					break;
				case AppearancePreset.ColorMethods.BySides:
					color = faceColors[face.Sides % faceColors.Length];
					break;
				default:
					color = Color.red;
					break;
			}

			if (face.Sides > 3)
			{
				for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
				{
					
					meshVertices.Add(face.Centroid);
					meshTriangles.Add(index++);
					meshVertices.Add(points[faceIndex[edgeIndex]]);
					meshTriangles.Add(index++);
					meshVertices.Add(points[faceIndex[(edgeIndex + 1) % face.Sides]]);
					meshTriangles.Add(index++);

					meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
					meshColors.AddRange(Enumerable.Repeat(color, 3));
				}
			}
			else
			{
				meshVertices.Add(points[faceIndex[0]]);
				meshTriangles.Add(index++);
				meshVertices.Add(points[faceIndex[1]]);
				meshTriangles.Add(index++);
				meshVertices.Add(points[faceIndex[2]]);
				meshTriangles.Add(index++);
				
				meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
				meshColors.AddRange(Enumerable.Repeat(color, 3));
			}
			
			
			if (hasNaked || forceTwosided)
			{
				if (faceIndex.Count > 3)
				{
					for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
					{					
						meshVertices.Add(face.Centroid);
						meshTriangles.Add(index++);
						meshVertices.Add(points[faceIndex[(edgeIndex + 1) % face.Sides]]);
						meshTriangles.Add(index++);
						meshVertices.Add(points[faceIndex[edgeIndex]]);
						meshTriangles.Add(index++);

						meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
						meshColors.AddRange(Enumerable.Repeat(color, 3));
					}					
				}
				else
				{
					meshVertices.Add(points[faceIndex[0]]);
					meshTriangles.Add(index++);
					meshVertices.Add(points[faceIndex[2]]);
					meshTriangles.Add(index++);
					meshVertices.Add(points[faceIndex[1]]);
					meshTriangles.Add(index++);
					
					meshNormals.AddRange(Enumerable.Repeat(-faceNormal, faceIndex.Count));
					meshColors.AddRange(Enumerable.Repeat(color, faceIndex.Count));
				}
			}		
		}

		target.vertices = meshVertices.ToArray();
		target.normals = meshNormals.ToArray();
		target.triangles = meshTriangles.ToArray();
		target.colors32 = meshColors.ToArray();
		
		if (hasNaked || forceTwosided) {
			target.RecalculateNormals();
		}

		return target;
	}	

	
#if UNITY_EDITOR
	void OnDrawGizmos () {
		
		float GizmoRadius = .03f;
		
		// I had to make too many fields on Kaleido public to do this
		// Need some sensible public methods to give me sensible access
		
		var transform = this.transform;

		if (WythoffPoly == null)
		{
			return;
		}

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
		
		if (conwayVertexGizmos && conway!=null)
		{
			Gizmos.color = Color.white;
			if (conway.Vertices != null)
			{
				for (int i = 0; i < conway.Vertices.Count; i++)
				{
					Vector3 vert = conway.Vertices[i].Position;
					Vector3 pos = transform.TransformPoint(vert);
					Gizmos.DrawWireSphere(pos, GizmoRadius);
					Handles.Label(pos + new Vector3(0, .15f, 0), i.ToString());
				}
			}
		}

		if (faceCenterGizmos)
		{
			if (conway == null)
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
				foreach (var f in conway.Faces)
				{
					Gizmos.DrawWireSphere(transform.TransformPoint(f.Centroid), GizmoRadius);
				}
			}

		}


		if (edgeGizmos)
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

		if (faceGizmos)
		{
			if (conway == null)
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
		var faces = conway.Faces;
		var verts = conway.Vertices;
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
