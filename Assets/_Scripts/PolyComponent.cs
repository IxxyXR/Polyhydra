using System.Linq;
using Buckminster.Types;
using Polylib;
using UnityEditor;
using UnityEngine;
using Face = Polylib.Face;

[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PolyComponent : MonoBehaviour {
	
	public PolyPreset preset;
	
	private int[] meshFaces;
	private Polyhedron _polyhedron;
	private bool ShowDuals = false;
	private ConwayPoly conway;

	private SkinnedMeshRenderer meshFilter;

	void Start() {
		meshFilter = gameObject.GetComponent<SkinnedMeshRenderer>();
		MakePolyhedron();
		preset = new PolyPreset();
	}

	private void OnValidate() {
		if (meshFilter != null) {
			MakePolyhedron();
		}
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			int num = (int)preset.PolyType;
			num = (num + 1) % Polyhedron.uniform.Length;
			preset.PolyType = (PolyPreset.PolyTypes)num;
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
		MakePolyhedron((int)preset.PolyType);
	}

	public void MakePolyhedron(int currentType) {

		currentType++;  // We're 1-indexed not 0-indexed

		var mesh = new Mesh();
		
		if (!preset.BypassConway) {

			// Bypass the Wytoff construction if the type hasn't changed
			if (_polyhedron==null || _polyhedron==null || _polyhedron.PolyTypeIndex!=currentType) {
				_polyhedron = new Polyhedron(currentType);
				_polyhedron.BuildFaces();
			}
			
			if (preset.ConwayOperators != null) {
				conway = new ConwayPoly(_polyhedron);
				foreach (var c in preset.ConwayOperators) {
					switch (c.op) {
						case PolyPreset.Ops.Identity:
							break;
						case PolyPreset.Ops.Foo:
							if (c.disabled) {break;}
							conway = conway.Foo(c.amount);
							break;
						case PolyPreset.Ops.Kis:
							if (c.disabled) {break;}
							conway = conway.Kis(c.amount);
							break;
						case PolyPreset.Ops.Kis3:
							if (c.disabled) {break;}
							conway = conway.KisN(c.amount, 3);
							break;
						case PolyPreset.Ops.Kis4:
							if (c.disabled) {break;}
							conway = conway.KisN(c.amount, 4);
							break;
						case PolyPreset.Ops.Kis5:
							if (c.disabled) {break;}
							conway = conway.KisN(c.amount, 5);
							break;
						case PolyPreset.Ops.Kis6:
							if (c.disabled) {break;}
							conway = conway.KisN(c.amount, 6);
							break;
						case PolyPreset.Ops.Kis8:
							if (c.disabled) {break;}
							conway = conway.KisN(c.amount, 8);
							break;
						case PolyPreset.Ops.Dual:
							if (c.disabled) {break;}
							conway = conway.Dual();
							break;
						case PolyPreset.Ops.Ambo:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							break;
						case PolyPreset.Ops.Zip:
							if (c.disabled) {break;}
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
						case PolyPreset.Ops.Expand:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Ambo();
							break;
						case PolyPreset.Ops.Bevel:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
						case PolyPreset.Ops.Join:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							break;
						case PolyPreset.Ops.Needle:
							if (c.disabled) {break;}
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							break;
						case PolyPreset.Ops.Ortho:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Ambo();
							conway = conway.Dual();
							break;
						case PolyPreset.Ops.Meta:
							if (c.disabled) {break;}
							conway = conway.Ambo();
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							break;
						case PolyPreset.Ops.Truncate:
							if (c.disabled) {break;}
							conway = conway.Dual();
							conway = conway.Kis(c.amount);
							conway = conway.Dual();
							break;
					}
				}
			}

			// TODO these either break or don't do anything especially useful at the moment
			if (preset.OffsetAmount > 0) {conway = conway.Offset(preset.OffsetAmount);}
			if (preset.RibbonAmount > 0) {conway = conway.Ribbon(preset.RibbonAmount, false, 0.1f);}
			if (preset.ExtrudeAmount > 0) {conway = conway.Extrude(preset.ExtrudeAmount, false);}
			
			conway.ScaleToUnitSphere();
		
			// If we Kis we don't need fan triangulation (which breaks on non-convex faces)
			conway = conway.Kis(0, true);
			mesh = conway.ToUnityMesh(forceTwosided:preset.TwoSided);
			
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

		if (preset.vertexGizmos) {
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

		if (preset.faceCenterGizmos) {
			Gizmos.color = Color.blue;
			if (_polyhedron.FaceCenters != null) {
				foreach (var f in _polyhedron.FaceCenters) {
					Gizmos.DrawWireSphere(transform.TransformPoint(f.getVector3()), GizmoRadius);
				}
			}
			
		}


		if (preset.edgeGizmos) {
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

		if (preset.faceGizmos) {
			int gizmoColor = 0;
			for (int f = 0; f < _polyhedron.faces.Count; f++) {
				if (preset.faceGizmosList.Contains(f) || preset.faceGizmosList.Length==0)
				{
					Gizmos.color = preset.gizmoPallette[gizmoColor++ % preset.gizmoPallette.Length];
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
		
		if (preset.dualGizmo) {
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
