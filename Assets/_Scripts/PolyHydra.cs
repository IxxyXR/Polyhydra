using System;
using System.Collections.Generic;
using System.Linq;
using Conway;
using Wythoff;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PolyHydra : MonoBehaviour {
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
		//Subdivide,
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
		AddDual
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
		[FormerlySerializedAs("op")]
		public Ops opType;
		public FaceSelections faceSelections;
		public float amount;
		public bool disabled;
	}
	public List<ConwayOperator> ConwayOperators;
		
	[Header("Gizmos")]
	public bool vertexGizmos;
	public bool faceCenterGizmos;
	public bool edgeGizmos;
	public bool faceGizmos;
	public int[] faceGizmosList;
	public bool dualGizmo;
	
	private int[] meshFaces;
	public WythoffPoly WythoffPoly;
	private Dictionary<string, WythoffPoly> _wythoffCache;
	private bool ShowDuals = false;
	private ConwayPoly conway;

	private MeshFilter meshFilter;
	private PolyPreset previousState;

	public PolyUI polyUI;
	
	public Color[] gizmoPallette = {
		Color.red,
		Color.yellow,
		Color.green,
		Color.cyan,
		Color.blue,
		Color.magenta
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
			//{Ops.Subdivide new OpConfig{}},
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
			{Ops.AddDual, new OpConfig{usesAmount=true, amountMin = -6, amountMax = 6}}
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
		var mesh = new Mesh();
		if (BypassOps)
		{
			mesh = BuildMeshFromWythoffPoly(WythoffPoly);
			mesh.RecalculateNormals();
		}
		else
		{
			ApplyOps();
			conway.ScalePolyhedra();
			// If we Kis we don't need fan triangulation (which breaks on non-convex faces)
			conway = conway.Kis(0, true);
			mesh = BuildMeshFromConwayPoly(conway, TwoSided);
		}
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		if (meshFilter != null) meshFilter.mesh = mesh;
	}

	private void ApplyOps()
	{
		if (ConwayOperators == null) return;
		conway = new ConwayPoly(WythoffPoly);
		foreach (var op in ConwayOperators)
		{
			int faceSelection;
			if (op.disabled)
			{
				continue;
			}

			switch (op.opType)
			{
				case Ops.Identity:
					break;
				case Ops.Kis:
					faceSelection = CalculateFaceSelection(op.faceSelections);
					conway = faceSelection == 0 ? conway.Kis(op.amount) : conway.KisN(op.amount, faceSelection);
					break;
				case Ops.Dual:
					conway = conway.Dual();
					break;
				case Ops.Ambo:
					conway = conway.Ambo();
					break;
				case Ops.Zip:
					conway = conway.Kis(op.amount);
					conway = conway.Dual();
					break;
				case Ops.Expand:
					conway = conway.Ambo();
					conway = conway.Ambo();
					break;
				case Ops.Bevel:
					conway = conway.Ambo();
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
					conway = conway.Dual();
					break;
				case Ops.Join:
					conway = conway.Ambo();
					conway = conway.Dual();
					break;
				case Ops.Needle:
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
					break;
				case Ops.Ortho:
					conway = conway.Ambo();
					conway = conway.Ambo();
					conway = conway.Dual();
					break;
				case Ops.Meta:
					conway = conway.Ambo();
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
					break;
				case Ops.Truncate:
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
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
					conway = conway.Kis(op.amount);
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
					break;
				case Ops.Yank:
					conway = conway.Kis(op.amount);
					conway = conway.Dual();
					conway = conway.Kis(op.amount);
					conway = conway.Dual();
					break;

				//						case Ops.Subdivide:
				//							conway = conway.Subdivide();
				//							break;
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
			}
		}
	}
	
	public Mesh BuildMeshFromWythoffPoly(WythoffPoly source)
	{
		
		var meshVertices = new List<Vector3>();
		var meshTriangles = new List<int>();
		var MeshVertexToVertex = new List<int>(); // Mapping of mesh vertices to polyh vertices (one to many as we duplicate verts)
		var meshColors = new List<Color>();
		
		Color[] vertexPallette = {
			Color.red,
			Color.yellow,
			Color.green,
			Color.cyan,
			Color.blue,
			Color.magenta
		};;
		
		
		var mesh = new Mesh();

		int meshVertexIndex = 0;

		foreach (Wythoff.Face face in source.faces) {
			face.CalcTriangles();
		}

		for (int faceType = 0; faceType < source.FaceTypeCount; faceType++) {
			foreach (Wythoff.Face face in source.faces) {
				if (face.configuration == source.FaceSidesByType[faceType]) {
					Color faceColor = vertexPallette[(int) (face.configuration % vertexPallette.Length)];
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
	
	public Mesh BuildMeshFromConwayPoly(ConwayPoly conway, bool forceTwosided=false) {

		var target = new Mesh();
		var meshTriangles = new List<int>();
		var meshVertices = new List<Vector3>();
		var meshNormals = new List<Vector3>();

		ConwayPoly source = conway.Duplicate();
		var hasNaked = source.HasNaked();
		hasNaked = false;

		// Strip down to Face-Vertex structure
		Vector3[] points = source.ListVerticesByPoints();
		List<int>[] faceIndices = source.ListFacesByVertexIndices();

		// Add faces
		int index = 0;

		for (var i = 0; i < faceIndices.Length; i++) {
			List<int> f = faceIndices[i];
			if (f.Count == 3) {
				
				var faceNormal = source.Faces[i].Normal;
				
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
				}
				
			}
			else {
				Debug.Log("Non-triangular face found");
			}
		}

		target.vertices = meshVertices.ToArray();
		target.normals = meshNormals.ToArray();
		target.triangles = meshTriangles.ToArray();
		
		if (hasNaked || forceTwosided) {
			target.RecalculateNormals();
		}
		//target.RecalculateNormals();

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

		if (vertexGizmos)
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

		if (faceCenterGizmos)
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
