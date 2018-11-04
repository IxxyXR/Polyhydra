using System;
using System.Collections.Generic;
using System.Linq;
using Conway;
using Wythoff;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Face = Wythoff.Face;


// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PolyComponent : MonoBehaviour {
	
	public enum PolyTypes {
		Pentagonal_Prism,
		Pentagonal_Antiprism,
		Pentagrammic_Prism,
		Pentagrammic_Antiprism,
		Pentagrammic_Crossed_Antiprism,
		Tetrahedron,
		Truncated_Tetrahedron,
		Octahemioctahedron,
		Tetrahemihexahedron,
		Octahedron,
		Cube,
		Cuboctahedron,
		Truncated_Octahedron,
		Truncated_Cube,
		Rhombicuboctahedron,
		Truncated_Cuboctahedron,
		Snub_Cube,
		Small_Cubicuboctahedron,
		Great_Cubicuboctahedron,
		Cubohemioctahedron,
		Cubitruncated_Cuboctahedron,
		Great_Rhombicuboctahedron,
		Small_Rhombihexahedron,
		Stellated_Truncated_Hexahedron,
		Great_Truncated_Cuboctahedron,
		Great_Rhombihexahedron,
		Icosahedron,
		Dodecahedron,
		Icosidodecahedron,
		Truncated_Icosahedron,
		Truncated_Dodecahedron,
		Rhombicosidodecahedron,
		Truncated_Icosidodechedon,
		Snub_Dodecahedron,
		Small_Ditrigonal_Icosidodecahedron,
		Small_Icosicosidodecahedron,
		Small_Snub_Icosicosidodecahedron,
		Small_Dodecicosidodecahedron,
		Small_Stellated_Dodecahedron,
		Great_Dodecahedron,
		Great_Dodecadodecahedron,
		Truncated_Great_Dodecahedron,
		Rhombidodecadodecahedron,
		Small_Rhombidodecahedron,
		Snub_Dodecadodecahedron,
		Ditrigonal_Dodecadodecahedron,
		Great_Ditrigonal_Dodecicosidodecahedron,
		Small_Ditrigonal_Dodecicosidodecahedron,
		Icosidodecadodecahedron,
		Icositruncated_Dodecadodecahedron,
		Snub_Icosidodecadodecahedron,
		Great_Ditrigonal_Icosidodecahedron,
		Great_Icosicosidodecahedron,
		Small_Icosihemidodecahedron,
		Small_Dodecicosahedron,
		Small_Dodecahemidodecahedron,
		Great_Stellated_Dodecahedron,
		Great_Icosahedron,
		Great_Icosidodecahedron,
		Great_Truncated_Icosahedron,
		Rhombicosahedron,
		Great_Snub_Icosidodecahedron,
		Small_Stellated_Truncated_Dodecahedron,
		Truncated_Dodecadodecahedron,
		Inverted_Snub_Dodecadodecahedron,
		Great_Dodecicosidodecahedron,
		Small_Dodecahemicosahedron,
		Great_Dodecicosahedron,
		Great_Snub_Dodecicosidodecahedron,
		Great_Dodecahemicosahedron,
		Great_Stellated_Truncated_Dodecahedron,
		Great_Rhombicosidodecahedron,
		Great_Truncated_Icosidodecahedron,
		Great_Inverted_Snub_Icosidodecahedron,
		Great_Dodecahemidodecahedron,
		Great_Icosihemidodecahedron,
		Small_Retrosnub_Icosicosidodecahedron,
		Great_Rhombidodecahedron,
		Great_Retrosnub_Icosidodecahedron,
		Great_Dirhombicosidodecahedron
	}
	
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
		Ribbon,
		Extrude,
		Scale,
		Test
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
	private Polyhedron _polyhedron;
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
			{Ops.Kis, new OpConfig{usesFaces=true}},
			{Ops.Dual, new OpConfig{usesAmount=false}},
			{Ops.Ambo, new OpConfig{usesAmount=false}},
			{Ops.Zip, new OpConfig{}},
			{Ops.Expand, new OpConfig{usesAmount=false}},
			{Ops.Bevel, new OpConfig{}},
			{Ops.Join, new OpConfig{usesAmount=false}},
			{Ops.Needle, new OpConfig{}},
			{Ops.Ortho, new OpConfig{usesAmount=false}},
			{Ops.Meta, new OpConfig{}},
			{Ops.Truncate, new OpConfig{}},
			{Ops.Gyro, new OpConfig{}},
			{Ops.Snub, new OpConfig{}},
			//{Ops.Subdivide new OpConfig{}},
			{Ops.Exalt, new OpConfig{}},
			{Ops.Yank, new OpConfig{}},
			//{Ops.Chamfer new OpConfig{}},
			{Ops.Offset, new OpConfig{}},
			{Ops.Ribbon, new OpConfig{}},
			{Ops.Extrude, new OpConfig{}},
			{Ops.Scale, new OpConfig{}},
			{Ops.Test, new OpConfig{}}
		};
	}

	void Start() {
		meshFilter = gameObject.GetComponent<MeshFilter>();
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

	public void MakePolyhedron() {
		
		if (!String.IsNullOrEmpty(WythoffSymbol))
		{
			MakePolyhedron(WythoffSymbol);
		}
		else
		{
			MakePolyhedron((int)PolyType);						
		}
	}

	public void MakePolyhedron(int polyType)
	{
		polyType++;  // We're 1-indexed not 0-indexed
		MakePolyhedron(Uniform.Uniforms[polyType].Wythoff);
	}

	public void MakePolyhedron(string symbol)
	{

		if (_polyhedron == null || _polyhedron.WythoffSymbol != symbol)
		{
			_polyhedron = new Polyhedron(symbol);
		}
		_polyhedron.BuildFaces(BuildAux: BypassOps);
		MakeMesh();
	}
	
	public void MakePolyhedron(Vector4 wythoffParams)
	{
		_polyhedron = new Polyhedron(wythoffParams[0], wythoffParams[1], wythoffParams[2], wythoffParams[3]);
		_polyhedron.BuildFaces(BuildAux: BypassOps);
		MakeMesh();	
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

	public void MakeMesh() {
		
		var mesh = new Mesh();
		
		if (BypassOps)
		{
			_polyhedron.BuildMesh();
			mesh = _polyhedron.mesh;
			mesh.RecalculateNormals();
		}
		else
		{
			if (ConwayOperators != null) {
				conway = new ConwayPoly(_polyhedron);
				foreach (var op in ConwayOperators) {
					if (op.disabled) {continue;}
					switch (op.opType) {
						case Ops.Identity:
							break;
						case Ops.Scale:
							conway = conway.Foo(op.amount);
							break;
						case Ops.Kis:
							var faceSelection = CalculateFaceSelection(op.faceSelections);
							conway = faceSelection==0 ? conway.Kis(op.amount) : conway.KisN(op.amount, faceSelection);								
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
							conway = conway.Offset(op.amount);
							break;
						case Ops.Extrude:
							conway = conway.Extrude(op.amount, false);
							break;
						case Ops.Ribbon:
							conway = conway.Ribbon(op.amount, false, 0.1f);
							break;
					}
				}
			}
			
			conway.ScaleToUnitSphere();
		
			// If we Kis we don't need fan triangulation (which breaks on non-convex faces)
			conway = conway.Kis(0, true);
			mesh = conway.ToUnityMesh(forceTwosided:TwoSided);
			
		}

		//_polyhedron.CreateBlendShapes();
		
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		if (meshFilter != null)
		{
			meshFilter.mesh = mesh;
		}

	}
#if UNITY_EDITOR
	void OnDrawGizmos () {
		
		float GizmoRadius = .03f;
		
		// I had to make too many fields on Kaleido public to do this
		// Need some sensible public methods to give me sensible access
		
		var transform = this.transform;

		if (_polyhedron == null) {
			return;
		}

		if (vertexGizmos) {
			Gizmos.color = Color.white;
			if (_polyhedron.Vertices != null) {
				for (int i = 0; i < _polyhedron.Vertices.Length; i++) {
					Vector3 vert = _polyhedron.Vertices[i].getVector3();
					Vector3 pos = transform.TransformPoint(vert);
					Gizmos.DrawWireSphere(pos, GizmoRadius);
					Handles.Label(pos + new Vector3(0, .15f, 0), i.ToString());
				}
			}
		}

		if (faceCenterGizmos) {
			Gizmos.color = Color.blue;
			if (_polyhedron.FaceCenters != null) {
				foreach (var f in _polyhedron.FaceCenters) {
					Gizmos.DrawWireSphere(transform.TransformPoint(f.getVector3()), GizmoRadius);
				}
			}
			
		}


		if (edgeGizmos) {
			for (int i = 0; i < _polyhedron.EdgeCount; i++) {
				Gizmos.color = Color.yellow;
				var edgeStart = _polyhedron.Edges[0, i];
				var edgeEnd = _polyhedron.Edges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(_polyhedron.Vertices[edgeStart].getVector3()),
					transform.TransformPoint(_polyhedron.Vertices[edgeEnd].getVector3())
				);
			}
		}

		if (faceGizmos) {
			int gizmoColor = 0;
			for (int f = 0; f < _polyhedron.faces.Count; f++) {
				if (faceGizmosList.Contains(f) || faceGizmosList.Length==0)
				{
					Gizmos.color = gizmoPallette[gizmoColor++ % gizmoPallette.Length];
					Face face = _polyhedron.faces[f];
					for (int i = 0; i < face.points.Count; i++)
					{
						var edgeStart = face.points[i];
						var edgeEnd = face.points[(i + 1) % face.points.Count];
						Gizmos.DrawLine(
							transform.TransformPoint(_polyhedron.Vertices[edgeStart].getVector3()),
							transform.TransformPoint(_polyhedron.Vertices[edgeEnd].getVector3())
						);
					}
				}
			}
		}
		
		if (dualGizmo) {
			for (int i = 0; i < _polyhedron.EdgeCount; i++)
			{
				var edgeStart = _polyhedron.DualEdges[0, i];
				var edgeEnd = _polyhedron.DualEdges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(_polyhedron.FaceCenters[edgeStart].getVector3()),
					transform.TransformPoint(_polyhedron.FaceCenters[edgeEnd].getVector3())
				);
			}
		}
	}
#endif
}
