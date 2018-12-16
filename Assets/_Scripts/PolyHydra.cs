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
	
	const int MAX_CACHE_LENGTH = 5000;
	
	public PolyTypes PolyType;
	public string WythoffSymbol;
	public bool BypassOps;
	public bool TwoSided;
	
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
		//Chamfer,
		Offset,
		//Ribbon,
		Extrude,
		FaceScale,
		FaceRotate,
		//Test,
		FaceRemove,
		FaceKeep,
		AddDual,
		Canonicalize
	}

	public enum FaceSelections
	{
		All,
		ThreeSided,
		FourSided,
		FiveSided,
		SixSided,
		SevenSided,
		EightSided
	}
	
	public class OpConfig
	{
		public bool usesAmount = true;
		public float amountMin = -20;
		public float amountMax = 20;
		public bool usesFaces = false;
		public FaceSelections faceSelection = FaceSelections.All;	
	}

	public Dictionary<Ops, OpConfig> opconfigs;
	
	[Serializable]
	public struct ConwayOperator {
		public Ops opType;
		public FaceSelections faceSelections;
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
	private bool threadRunning;
	private Thread thread;

	public PolyUI polyUI;
	
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
			{Ops.Kis, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Dual, new OpConfig{usesAmount=false}},
			{Ops.Ambo, new OpConfig{usesAmount=false}},
			{Ops.Zip, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Expand, new OpConfig{usesAmount=false}},
			{Ops.Bevel, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Join, new OpConfig{usesAmount=false}},
			{Ops.Needle, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Ortho, new OpConfig{usesAmount=false}},
			{Ops.Meta, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Truncate, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
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
			{Ops.Exalt, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			{Ops.Yank, new OpConfig{usesFaces=true, amountMin = -6, amountMax = 6}},
			//{Ops.Chamfer new OpConfig{}},
			{Ops.Offset, new OpConfig{amountMin = -6, amountMax = 6}},
			//{Ops.Ribbon, new OpConfig{}},
			{Ops.Extrude, new OpConfig{amountMin = -6, amountMax = 6}},
			{Ops.FaceScale, new OpConfig{amountMin = -6, amountMax = 6, usesFaces=true}},
			{Ops.FaceRotate, new OpConfig{amountMin = -180, amountMax = 180, usesFaces=true}},
			//{Ops.Test, new OpConfig{}}
			{Ops.FaceRemove, new OpConfig{usesAmount=false, usesFaces=true}},
			{Ops.FaceKeep, new OpConfig{usesAmount=false, usesFaces=true}},
			{Ops.AddDual, new OpConfig{usesAmount=true, amountMin = -6, amountMax = 6}},
			{Ops.Canonicalize, new OpConfig{usesAmount=true, amountMin = 0.0001f, amountMax = 1f}}
		};
	}

	void Start() {
		meshFilter = gameObject.GetComponent<MeshFilter>();
	}

	public void MakePolyhedron()
	{
		MakeWythoff();
		MakeMesh();
	}

	private void OnValidate() {
		if (!Application.isPlaying)
		{
			var currentState = new PolyPreset();
			currentState.CreateFromPoly("temp", this);
			if (previousState != currentState)
			{
				MakePolyhedron();
				previousState = currentState;
			}			
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
		polyType++;  // We're 1-indexed not 0-indexed
		MakeWythoff(Uniform.Uniforms[polyType].Wythoff);
	}

	public void MakeWythoff(string symbol)
	{

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
	}
	
	private int CalculateFaceSelection(FaceSelections faceSelections)
	{
		switch (faceSelections)
		{
			case FaceSelections.ThreeSided:
				return 3;
			case FaceSelections.FourSided:
				return 4;
			case FaceSelections.FiveSided:
				return 5;
			case FaceSelections.SixSided:
				return 6;
			case FaceSelections.SevenSided:
				return 7;
			case FaceSelections.EightSided:
				return 8;
		}
		return 0;
	}

	
	public void MakeMesh()
	{
		if (BypassOps)
		{
			// Only need to deal with the Wythoff poly
			var mesh = BuildMeshFromWythoffPoly(WythoffPoly);
			mesh.RecalculateNormals();
			FinishedMeshGeneration(mesh);
		}
		else
		{
			var threaded = false;
			if (threaded)
			{
				if (thread!=null && threadRunning)
				{
					thread.Abort();
				}
				StartCoroutine(RunOffMainThread(ApplyOps, FinishedApplyOps));			
			}
			else
			{
				ApplyOps();
				FinishedApplyOps();
			}
		}
	}

	public void FinishedApplyOps()
	{
		conway.ScalePolyhedra();
		var mesh = BuildMeshFromConwayPoly(TwoSided);
		FinishedMeshGeneration(mesh);
	}

	public void FinishedMeshGeneration(Mesh mesh)
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
	IEnumerator RunOffMainThread(Action toRun, Action callback) {
		threadRunning = false;
		thread = new Thread(() => {
			threadRunning = true;
			toRun();
		});
		thread.Start();
		while (!threadRunning)
		{
			yield return null;			
		}
		callback();
	}

	private void ApplyOps()
	{
		if (ConwayOperators == null) return;		
		conway = new ConwayPoly(WythoffPoly);
		var cacheKeySource = WythoffPoly.WythoffSymbol;
		foreach (var op in ConwayOperators)
		{
			
			if (op.disabled) continue;
			
			cacheKeySource += JsonConvert.SerializeObject(op);
			if (_conwayCache == null) _conwayCache = new Dictionary<int, ConwayCacheEntry>();
			if (_conwayCache.ContainsKey(cacheKeySource.GetHashCode()))
			{
				conway = _conwayCache[cacheKeySource.GetHashCode()].conway;
			}
			else
			{
				int faceSelection;
	
				switch (op.opType)
				{
					case Ops.Identity:
						break;
					case Ops.Kis:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.Kis(op.amount, faceSelection);
						break;
					case Ops.Dual:
						conway = conway.Dual();
						break;
					case Ops.Ambo:
						conway = conway.Ambo();
						break;
					case Ops.Zip:
						conway = conway.Kis(op.amount, 0);
						conway = conway.Dual();
						break;
					case Ops.Expand:
						conway = conway.Ambo();
						conway = conway.Ambo();
						break;
					case Ops.Bevel:
						conway = conway.Ambo();
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
						conway = conway.Dual();
						break;
					case Ops.Join:
						conway = conway.Ambo();
						conway = conway.Dual();
						break;
					case Ops.Needle:
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
						break;
					case Ops.Ortho:
						conway = conway.Ambo();
						conway = conway.Ambo();
						conway = conway.Dual();
						break;
					case Ops.Meta:
						conway = conway.Ambo();
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
						break;
					case Ops.Truncate:
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
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
						conway = conway.Kis(op.amount, 0);
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
						break;
					case Ops.Yank:
						conway = conway.Kis(op.amount, 0);
						conway = conway.Dual();
						conway = conway.Kis(op.amount, 0);
						conway = conway.Dual();
						break;
					case Ops.Subdivide:
						conway = conway.Subdivide();
						break;
					case Ops.Loft:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.Loft(op.amount, faceSelection);
						break;					
					case Ops.Quinto:
						conway = conway.Quinto(op.amount);
						break;
					case Ops.JoinedLace:
						conway = conway.JoinedLace(op.amount);
						break;
					case Ops.Lace:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.Lace(op.amount, faceSelection);
						break;
					case Ops.Stake:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.Stake(op.amount, faceSelection);
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
					//						case Ops.Chamfer:
					//							conway = conway.Chamfer();
					//							break;
	
					case Ops.Offset:
						// Split faces
						conway = conway.FaceScale(0, 0);
						conway = conway.Offset(op.amount);
						break;
					case Ops.Extrude:
						// Split faces
						conway = conway.FaceScale(0, 0);
						conway = conway.Extrude(op.amount, false);
						break;
					//						case Ops.Ribbon:
					//							conway = conway.Ribbon(op.amount, false, 0.1f);
					//							break;
					case Ops.FaceScale:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.FaceScale(op.amount, faceSelection);
						break;
					case Ops.FaceRotate:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.FaceRotate(op.amount, faceSelection);
						break;
					case Ops.FaceRemove:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.FaceRemove(faceSelection, false);
						break;
					case Ops.FaceKeep:
						faceSelection = CalculateFaceSelection(op.faceSelections);
						conway = conway.FaceRemove(faceSelection, true);
						break;
					case Ops.AddDual:
						conway = conway.AddDual(op.amount);
						break;
					case Ops.Canonicalize:
						conway = conway.Canonicalize(op.amount, op.amount);
						break;
				}

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
	
	public Mesh BuildMeshFromWythoffPoly(WythoffPoly source)
	{
		
		var meshVertices = new List<Vector3>();
		var meshTriangles = new List<int>();
		var MeshVertexToVertex = new List<int>(); // Mapping of mesh vertices to polyh vertices (one to many as we duplicate verts)
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

		return mesh;

	}
	
	// Essentially Kis only on non-triangular faces
	// Returns the original number of sides of each face to be used elsewhere
	// TODO Detect convex faces and use fan triangulation to save on a vertex
	public List<int> KisTriangulate() {
            
		var newVerts = conway.Faces.Select(f => f.Centroid);
		var vertexPoints = Enumerable.Concat(conway.Vertices.Select(v => v.Position), newVerts);
		var originalFaceSides = new List<int>();
		var faceRoles = new List<ConwayPoly.Roles>();
            
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
            
		conway = new ConwayPoly(vertexPoints, faceIndices, faceRoles);
		return originalFaceSides;
	}

	public Mesh BuildMeshFromConwayPoly(bool forceTwosided=false, bool colorByRole=false)
	{

		var originalFaceSides = KisTriangulate();

		var target = new Mesh();
		var meshTriangles = new List<int>();
		var meshVertices = new List<Vector3>();
		var meshNormals = new List<Vector3>();
		var meshColors = new List<Color32>();
		
		var hasNaked = conway.HasNaked();
		hasNaked = false;

		// Strip down to Face-Vertex structure
		var points = conway.ListVerticesByPoints();
		var faceIndices = conway.ListFacesByVertexIndices();

		// Add faces
		int index = 0;
		
		for (var i = 0; i < faceIndices.Length; i++) {
			
			var f = faceIndices[i];
			var face = conway.Faces[i];
			var faceNormal = face.Normal;
			
			var color = colorByRole ?
				faceColors[(int)conway.FaceRoles[i]] :
				faceColors[(originalFaceSides[i] - 3) % originalFaceSides.Count];				
			
			meshNormals.Add(faceNormal);
			meshNormals.Add(faceNormal);
			meshNormals.Add(faceNormal);
			
			meshVertices.Add(points[f[0]]);
			meshTriangles.Add(index++);
			meshVertices.Add(points[f[1]]);
			meshTriangles.Add(index++);
			meshVertices.Add(points[f[2]]);
			meshTriangles.Add(index++);

			if (hasNaked || forceTwosided) {
				
				meshNormals.Add(-faceNormal);
				meshNormals.Add(-faceNormal);
				meshNormals.Add(-faceNormal);
			
				meshVertices.Add(points[f[0]]);
				meshTriangles.Add(index++);
				meshVertices.Add(points[f[2]]);
				meshTriangles.Add(index++);
				meshVertices.Add(points[f[1]]);
				meshTriangles.Add(index++);
				
				meshColors.Add(color);
				meshColors.Add(color);
				meshColors.Add(color);

			}
			
			meshColors.Add(color);
			meshColors.Add(color);
			meshColors.Add(color);
			
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
