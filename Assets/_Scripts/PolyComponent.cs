using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyComponent : MonoBehaviour {

	
	[Range(0,79)]
	public int currentType;
	public bool dual;
	
	[Header("Gizmos")]
	public bool vertexGizmos;
	public bool edgeGizmos;
	public bool dualGizmo;
	
	private int[] meshFaces;
	private PolyMesh _polymesh;
	private bool ShowDuals = false;

	private MeshFilter meshFilter;

	void Start() {
		
		meshFilter = gameObject.GetComponent<MeshFilter>();
		currentType = 1;
		dual = false;
		MakePolyMesh(currentType, dual);
		
	}

	private void OnValidate() {
		MakePolyMesh(currentType, dual);
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			
			if (ShowDuals) {dual = !dual;}

			if (!dual || !ShowDuals) {  // TODO Fix duals
				currentType++;
				currentType = currentType % Polyhedron.uniform.Length;
				gameObject.GetComponent<RotateObject>().Randomize();
			}
			
			MakePolyMesh(currentType, dual);
			Debug.Log(currentType + ": " + _polymesh.polyhedron.PolyName + (dual ? " (dual)" : ""));

		} else if (Input.GetKeyDown("p")) {
			gameObject.GetComponent<RotateObject>().Pause();
		} else if (Input.GetKeyDown("r")) {
			gameObject.GetComponent<RotateObject>().Randomize();
		} else if (Input.GetKeyDown("1")) {
			meshFilter.sharedMesh = _polymesh.Explode();
		}
		
	}
	
	void MakePolyMesh(int currentType, bool dual) {
		
		_polymesh = new PolyMesh(currentType, dual);
		meshFilter.sharedMesh = _polymesh.mesh;
	}

	void OnDrawGizmos () {
		
		float GizmoRadius = .03f;
		
		// I had to make too many fields on Kaleido public to do this
		// Need some sensible public methods to give me sensible access
		
		Gizmos.color = Color.white;
		var transform = this.transform;

		if (_polymesh == null) {
			return;
		}

		if (vertexGizmos) {
			if (_polymesh.polyhedron.Vertices != null) {
				foreach (var vert in _polymesh.polyhedron.Vertices) {
					Gizmos.DrawWireSphere(transform.TransformPoint(vert.getVector3()), GizmoRadius);
				}
			}
		}
		
//		if (_polymesh.polyhedron.f != null) {
//			foreach (var faceVert in _polymesh.polyhedron.f) {
//				Gizmos.DrawWireSphere(transform.TransformPoint(faceVert.getVector3()), GizmoRadius);
//			}
//		}
		
//		for (int i = 0; i < _polymesh.polyhedron.E; i++) {
//			var edgeStart = _polymesh.polyhedron.e[0, i];
//			var edgeEnd = _polymesh.polyhedron.e[1, i];
//			Gizmos.DrawLine(
//				transform.TransformPoint(_polymesh.polyhedron.v[edgeStart].getVector3()),
//				transform.TransformPoint(_polymesh.polyhedron.v[edgeEnd].getVector3())
//			);
//		}

//		foreach (var fv in _polymesh.polyhedron.faceVertices) {
//			foreach (int vnum in fv) {
//				Gizmos.DrawWireSphere(
//					transform.TransformPoint(_polymesh.polyhedron.v[vnum].getVector3()),
//					GizmoRadius
//				);
//			}
//
//			break;
//		}
		
//		foreach (var fe in _polymesh.faceEdges) {
//			foreach (int edgenum in fe) {
//				Gizmos.DrawLine(
//					transform.TransformPoint(_polymesh.P.v[_polymesh.P.e[0, edgenum]].getVector3()),
//					transform.TransformPoint(_polymesh.P.v[_polymesh.P.e[1, edgenum]].getVector3())
//				);
//			}
//
//			break;
//		}
		if (dualGizmo) {
			for (int i = 0; i < _polymesh.polyhedron.EdgeCount; i++)
			{
				var edgeStart = _polymesh.polyhedron.DualEdges[0, i];
				var edgeEnd = _polymesh.polyhedron.DualEdges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(_polymesh.polyhedron.Faces[edgeStart].getVector3()),
					transform.TransformPoint(_polymesh.polyhedron.Faces[edgeEnd].getVector3())
				);
			}
		}
	}

}
