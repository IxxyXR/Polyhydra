using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PolyComponent : MonoBehaviour {

	
	[Range(1,80)]
	public int currentType;
	public bool dual;
	
	[Header("Gizmos")]
	public bool vertexGizmos;
	public bool edgeGizmos;
	public bool dualGizmo;
	
	private int[] meshFaces;
	private Polyhedron _polyhedron;
	private bool ShowDuals = false;

	private SkinnedMeshRenderer meshFilter;

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
		
		Gizmos.color = Color.white;
		var transform = this.transform;

		if (_polyhedron == null) {
			return;
		}

		if (vertexGizmos) {
			if (_polyhedron.Vertices != null) {
				foreach (var vert in _polyhedron.Vertices) {
					Gizmos.DrawWireSphere(transform.TransformPoint(vert.getVector3()), GizmoRadius);
				}
			}
		}
		
//		if (_polyhedron.f != null) {
//			foreach (var faceVert in _polyhedron.f) {
//				Gizmos.DrawWireSphere(transform.TransformPoint(faceVert.getVector3()), GizmoRadius);
//			}
//		}
//		
//		for (int i = 0; i < _polyhedron.E; i++) {
//			var edgeStart = _polyhedron.e[0, i];
//			var edgeEnd = _polyhedron.e[1, i];
//			Gizmos.DrawLine(
//				transform.TransformPoint(_polyhedron.v[edgeStart].getVector3()),
//				transform.TransformPoint(_polyhedron.v[edgeEnd].getVector3())
//			);
//		}
//
//		foreach (var fv in _polyhedron.faceVertices) {
//			foreach (int vnum in fv) {
//				Gizmos.DrawWireSphere(
//					transform.TransformPoint(_polyhedron.v[vnum].getVector3()),
//					GizmoRadius
//				);
//			}
//
//			break;
//		}
//		
//		foreach (var fe in _polyhedron.faceEdges) {
//			foreach (int edgenum in fe) {
//				Gizmos.DrawLine(
//					transform.TransformPoint(_polyhedron.P.v[_polyhedron.P.e[0, edgenum]].getVector3()),
//					transform.TransformPoint(_polyhedron.P.v[_polyhedron.P.e[1, edgenum]].getVector3())
//				);
//			}
//
//			break;
//		}
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
