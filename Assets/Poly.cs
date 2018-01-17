using System;
using System.Linq;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Poly : MonoBehaviour {

//	[Range(1,80)]
//	public int polyType = 1;
	
	private bool showDual = false; 
	private int[] meshFaces;
	int currentType;
	int[] blacklist;
	private Kaleido _kaleido;

	void Start() {

		blacklist = new int[] {/* 13, 14, 17, 20, 23, 27, 28 */};
		currentType = 1;
		MakePoly();
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			
			currentType++;
			currentType = currentType % 81;

			Debug.Log("=========================");
			if (!blacklist.Contains(currentType)) {
				Debug.Log(currentType);
				Debug.Log("-------------------------");
				MakePoly();
			}
			else {
				Debug.Log("Skipping " + currentType);
			}
			Debug.Log("-------------------------");
		}
	}
	
	void MakePoly() {
		try {
			_kaleido = new Kaleido(currentType);
			gameObject.GetComponent<MeshFilter>().mesh = _kaleido.BuildMesh();
			//Debug.Log(polyname);
		}
		catch (Exception e) {
			Debug.Log("Error on " + currentType + " : " + e.Message);
		}
	}
	
	private Color GizmoColor = Color.white;
	private float GizmoRadius = .03f;

	void OnDrawGizmos () {
		
		// I had to make too many fields on Kaleido public to do this
		// Need some sensible public methods to give me sensible access
		
		Gizmos.color = GizmoColor;
		var transform = this.transform;

		if (_kaleido == null) {
			return;
		}
		
		if (_kaleido.v != null) {
			foreach (var vert in _kaleido.v) {
				Gizmos.DrawWireSphere(transform.TransformPoint(vert.getVector3()), GizmoRadius);
			}
		}
		if (_kaleido.e != null) {
			for (int i = 0; i < _kaleido.E; i++) {
				if (showDual) {
					Gizmos.DrawLine(
						transform.TransformPoint(_kaleido.f[_kaleido.dual_e[0, i]].getVector3()),
						transform.TransformPoint(_kaleido.f[_kaleido.dual_e[1, i]].getVector3())
					);
				} else {
					Gizmos.DrawLine(
						transform.TransformPoint(_kaleido.v[_kaleido.e[0, i]].getVector3()),
						transform.TransformPoint(_kaleido.v[_kaleido.e[1, i]].getVector3())
					);
				}
			}
		}
	}

}
