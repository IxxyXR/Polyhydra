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

	
	[Range(1,80)]
	[Tooltip("Tetrahedron: 6, Oct: 10, Cube: 11, Dodec: 28, Icos: 27")]
	public int currentType;
	public bool BypassConway;
	public bool TwoSided;
	[Tooltip("i/k/d/a/z/e/b/j/n/o/p/t")]
	public string ConwayOperators;

	public double OffsetAmount;
	public float RibbonAmount;
	public double ExtrudeAmount;
	public float KisOffset = 1.0f;
	public float ZipOffset = 1.0f;
	public float NeedleOffset = 1.0f;
	public float PropellerOffset = 1.0f;
	public float BevelOffset = 1.0f;
	public float TruncateOffset = 1.0f;
		
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
		currentType = 1;
		MakePolyhedron(currentType);
		
	}

	private void OnValidate() {
		MakePolyhedron(currentType);
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {

			currentType++;
			currentType = currentType % Polyhedron.uniform.Length;
			gameObject.GetComponent<RotateObject>().Randomize();
			
			MakePolyhedron(currentType);
			Debug.Log(currentType + ": " + _polyhedron.PolyName);

		} else if (Input.GetKeyDown("p")) {
			gameObject.GetComponent<RotateObject>().Pause();
		} else if (Input.GetKeyDown("r")) {
			gameObject.GetComponent<RotateObject>().Randomize();
		} else if (Input.GetKeyDown("1")) {
			meshFilter.sharedMesh = _polyhedron.Explode();
		}
		
	}
	
	void MakePolyhedron(int currentType) {

		var mesh = new Mesh();
		
		if (!BypassConway) {

			_polyhedron = new Polyhedron(currentType);
			_polyhedron.BuildFaces();
			var conway = new ConwayPoly(_polyhedron);

			foreach (char c in ConwayOperators.ToLower().Reverse()) {
				switch (c) {
					case 'i':
						// Identity
						break;
					case 'k':
						conway = conway.Kis(KisOffset);
						break;
					case 'd':
						conway = conway.Dual();
						break;
					case 'a':
						conway = conway.Ambo();
						break;
					case 'z':
						conway = conway.Kis(ZipOffset);
						conway = conway.Dual();
						break;
					case 'e':
						conway = conway.Ambo();
						conway = conway.Ambo();
						break;
					case 'b':
						conway = conway.Ambo();
						conway = conway.Dual();
						conway = conway.Kis(BevelOffset);
						conway = conway.Dual();
						break;
					case 'j':
						conway = conway.Ambo();
						conway = conway.Dual();
						break;
					case 'n':
						conway = conway.Dual();
						conway = conway.Kis(NeedleOffset);
						break;
					case 'o':
						conway = conway.Ambo();
						conway = conway.Ambo();
						conway = conway.Dual();
						break;
					case 'p':
						conway = conway.Ambo();
						conway = conway.Dual();
						conway = conway.Kis(PropellerOffset);
						break;
					case 't':
						conway = conway.Dual();
						conway = conway.Kis(TruncateOffset);
						conway = conway.Dual();
						break;
					default:
						Debug.Log("Unknown Conway operator: " + c);
						break;
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
			// Fallback if the geometry is invalid
			Debug.Log("Failed. Falling back to simple meshing");
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
