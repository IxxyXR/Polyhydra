using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PolyComponent : MonoBehaviour {

	
	[Range(1,80)]
	public int currentType;
	public bool dual;
	
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
		dual = false;
		MakePolyhedron(currentType, dual);
		
	}

	private void OnValidate() {
		MakePolyhedron(currentType, dual);
		meshFilter.sharedMesh.RecalculateNormals();
		meshFilter.sharedMesh.RecalculateTangents();
		meshFilter.sharedMesh.RecalculateBounds();
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			
			if (ShowDuals) {dual = !dual;}

			if (!dual || !ShowDuals) {  // TODO Fix duals
				currentType++;
				currentType = currentType % Polyhedron.uniform.Length;
				gameObject.GetComponent<RotateObject>().Randomize();
			}
			
			MakePolyhedron(currentType, dual);
			Debug.Log(currentType + ": " + _polyhedron.PolyName + (dual ? " (dual)" : ""));

		} else if (Input.GetKeyDown("p")) {
			gameObject.GetComponent<RotateObject>().Pause();
		} else if (Input.GetKeyDown("r")) {
			gameObject.GetComponent<RotateObject>().Randomize();
		} else if (Input.GetKeyDown("1")) {
			meshFilter.sharedMesh = _polyhedron.Explode();
		}
		
	}
	
	void MakePolyhedron(int currentType, bool dual) {
		
		_polyhedron = new Polyhedron(currentType);
		_polyhedron.CreateBlendShapes();
		meshFilter.sharedMesh = _polyhedron.mesh;
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
