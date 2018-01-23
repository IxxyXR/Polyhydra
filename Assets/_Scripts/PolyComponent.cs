using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PolyComponent : MonoBehaviour {

	
	[Range(0,79)]
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

		int vCount = _polyhedron.mesh.vertexCount;
		var deltaVertices = new Vector3[vCount];
		var deltaNormals = new Vector3[vCount];
		int swapFactor = _polyhedron.FaceCount;
		for (int i = 0; i < vCount; i++) {
			deltaVertices[i] = _polyhedron.mesh.vertices[(i+swapFactor) % vCount] - _polyhedron.mesh.vertices[i];
			deltaNormals[i] = _polyhedron.mesh.normals[(i+swapFactor) % vCount] - _polyhedron.mesh.normals[i];
		}
		_polyhedron.mesh.AddBlendShapeFrame("example blendshape", 0, deltaVertices, null, null);
		
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
		
//		if (_polymesh.f != null) {
//			foreach (var faceVert in _polymesh.f) {
//				Gizmos.DrawWireSphere(transform.TransformPoint(faceVert.getVector3()), GizmoRadius);
//			}
//		}
		
//		for (int i = 0; i < _polymesh.E; i++) {
//			var edgeStart = _polymesh.e[0, i];
//			var edgeEnd = _polymesh.e[1, i];
//			Gizmos.DrawLine(
//				transform.TransformPoint(_polymesh.v[edgeStart].getVector3()),
//				transform.TransformPoint(_polymesh.v[edgeEnd].getVector3())
//			);
//		}

//		foreach (var fv in _polymesh.faceVertices) {
//			foreach (int vnum in fv) {
//				Gizmos.DrawWireSphere(
//					transform.TransformPoint(_polymesh.v[vnum].getVector3()),
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
			for (int i = 0; i < _polyhedron.EdgeCount; i++)
			{
				var edgeStart = _polyhedron.DualEdges[0, i];
				var edgeEnd = _polyhedron.DualEdges[1, i];
				Gizmos.DrawLine(
					transform.TransformPoint(_polyhedron.Faces[edgeStart].getVector3()),
					transform.TransformPoint(_polyhedron.Faces[edgeEnd].getVector3())
				);
			}
		}
	}

}
