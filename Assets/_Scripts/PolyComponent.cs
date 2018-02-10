using System;
using System.Linq;
using Buckminster.Types;
using Polylib;
using UnityEditor;
using UnityEngine;
using Face = Polylib.Face;

[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PolyComponent : MonoBehaviour {
	
	[Serializable]
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
	public bool BypassConway;
	public bool TwoSided;
	[Serializable]
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
		Truncate
	}
	[Serializable]
	public struct ConwayOperator {  
		public Ops op;
		public float amount;
		public bool disabled;
	}
	public ConwayOperator[] ConwayOperators;
	public double OffsetAmount;
	public float RibbonAmount;
	public double ExtrudeAmount;
		
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

	private SkinnedMeshRenderer meshFilter;
	
	public Color[] gizmoPallette = {
		Color.red,
		Color.yellow,
		Color.green,
		Color.cyan,
		Color.blue,
		Color.magenta
	};

	void Start() {
		meshFilter = gameObject.GetComponent<SkinnedMeshRenderer>();
		MakePolyhedron();
	}

	private void OnValidate() {
		if (meshFilter != null) {
			MakePolyhedron();
		}
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			int num = (int)PolyType;
			num = (num + 1) % Polyhedron.uniform.Length;
			PolyType = (PolyTypes)num;
			gameObject.GetComponent<RotateObject>().Randomize();
			
			MakePolyhedron();

		} else if (Input.GetKeyDown("p")) {
			gameObject.GetComponent<RotateObject>().Pause();
		} else if (Input.GetKeyDown("r")) {
			gameObject.GetComponent<RotateObject>().Randomize();
		} else if (Input.GetKeyDown("1")) {
			meshFilter.sharedMesh = _polyhedron.Explode();
		}
		
	}

	public void MakePolyhedron() {
		MakePolyhedron((int)PolyType);
	}

	public void MakePolyhedron(int currentType) {

		currentType++;  // We're 1-indexed not 0-indexed

		var mesh = new Mesh();
		
		if (!BypassConway) {

			_polyhedron = new Polyhedron(currentType);
			_polyhedron.BuildFaces();
			var conway = new ConwayPoly(_polyhedron);
			
			if (ConwayOperators != null) {
				foreach (var c in ConwayOperators) {
					switch (c.op) {
						case Ops.Identity:
							break;
						case Ops.Kis:
							if (c.disabled) {break;}
							conway = conway.Kis(c.amount);
							break;
						case Ops.Dual:
							if (c.disabled) {break;}
							conway = conway.Dual();
							break;
						case Ops.Ambo:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							break;
						case Ops.Zip:
							if (c.disabled) {break;}
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
						case Ops.Expand:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Ambo();
							break;
						case Ops.Bevel:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
						case Ops.Join:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							break;
						case Ops.Needle:
							if (c.disabled) {break;}
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							break;
						case Ops.Ortho:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Ambo();
							conway = conway.Dual();
							break;
						case Ops.Meta:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							break;
						case Ops.Truncate:
							if (c.disabled) {break;}
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
					}
				}
			}

			// TODO these either break or don't do anything especially useful at the moment
			if (OffsetAmount > 0) {conway = conway.Offset(OffsetAmount);}
			if (RibbonAmount > 0) {conway = conway.Ribbon(RibbonAmount, false, 0.1f);}
			if (ExtrudeAmount > 0) {conway = conway.Extrude(ExtrudeAmount, false);}
			
			conway.ScaleToUnitSphere();
		
			// If we Kis we don't need fan triangulation (which breaks on non-convex faces)
			conway = conway.Kis(0, true);
			mesh = conway.ToUnityMesh(forceTwosided:TwoSided);
			
		} else {
			_polyhedron = new Polyhedron(currentType);
			_polyhedron.BuildFaces(true);  // Build the aux faces
			_polyhedron.BuildMesh();
			mesh = _polyhedron.mesh;
			mesh.RecalculateNormals();
		}

		//_polyhedron.CreateBlendShapes();
		
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		meshFilter.sharedMesh = mesh;
	}

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

}
