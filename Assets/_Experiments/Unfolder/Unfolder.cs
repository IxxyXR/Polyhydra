using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;


[ExecuteInEditMode]
public class Unfolder : MonoBehaviour
{
	// Fields For Unfolding
	private List<UfEdge> PolyEdges;
	private List<Face> ConnectedFaces;
	private PolyHydra poly;
	
	
	[Range(0,1)]
	public float completion;
	[Range(0,1)]
	public float completionAngle = 1;

	public bool activate = true;


	void Start()
	{
		poly = GetComponent<PolyHydra>();
		
	}

	
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Unfold();
		}
	}


	private void OnValidate() {
		Unfold();
	}


	public void Unfold() {

		PolyEdges = new List<UfEdge>();
		var CheckedHalfEdges = new List<Halfedge>();
		ConnectedFaces = new List<Face>();
		var BranchedEdges = new List<UfEdge>();
		var TabbedEdges = new List<UfEdge>();
		foreach (Halfedge h in	poly._conwayPoly.Halfedges)
		{
			if (CheckedHalfEdges.Contains(h)){
				continue;
			}
			else
			{
				CheckedHalfEdges.Add(h);
				if (h.Pair == null) {
					continue;
				} else {
					CheckedHalfEdges.Add(h.Pair);
					PolyEdges.Add(new UfEdge(h.Face, h.Pair.Face, h, h.Pair));
				}
			}
		}
		
		Debug.Log("Amount Of Faces: " + poly._conwayPoly.Faces.Count);
		Debug.Log("Amount Of Edges: " + PolyEdges.Count);
		
		UfFace rootFace = new UfFace(poly._conwayPoly.Faces[0], null);
		List<UfFace> children = AddChildren(rootFace);
		List<UfFace> queue = new List<UfFace>();
		List<UfFace> branched = new List<UfFace>();
		foreach (UfFace c in children)
		{
			queue.Add(c);  
			branched.Add(c);
		}
		while (queue.Count != 0)
		{
			children = AddChildren(queue[0]);
			foreach (UfFace c in children)
			{
				if (!queue.Contains(c))
				{
					if (!branched.Contains(c))
					{
						branched.Add(c);
					}
					queue.Add(c);
				}
			}
			queue.RemoveAt(0);
		}
		int tabs = 0;
		int branches = 0;
		foreach (UfEdge e in PolyEdges)
		{
			if (e.Branched)
			{
				branches++;
				Debug.Log("Branched Edge "+ branches + ": " + e.ToString());
				BranchedEdges.Add(e);
			}
			if (e.Tabbed)
			{
				tabs++;
				TabbedEdges.Add(e);
			}
		}
		Debug.Log("Required Tabs: " + tabs.ToString());

		var verticesInBranches = new Dictionary<Vertex, List<Face>>();
		foreach (UfEdge e in BranchedEdges)
		{
			if (verticesInBranches.ContainsKey(e.Halfedge1.Vertex))
			{
				verticesInBranches[e.Halfedge1.Vertex].Add(e.Face1);
				verticesInBranches[e.Halfedge1.Vertex].Add(e.Face2);
			} else {
				verticesInBranches.Add(e.Halfedge1.Vertex, new List<Face>(){e.Face1, e.Face2});
			}
			if (verticesInBranches.ContainsKey(e.Halfedge2.Vertex))
			{
				verticesInBranches[e.Halfedge2.Vertex].Add(e.Face1);
				verticesInBranches[e.Halfedge2.Vertex].Add(e.Face2);
			} else {
				verticesInBranches.Add(e.Halfedge2.Vertex, new List<Face>(){e.Face1, e.Face2});
			}
		}

		
		
		
		
		
		var unfoldedPoly = new ConwayPoly();
		var ConstructedFaces = new List<Face>();
		var AddedVertices = new List<Vertex>();
		
		Debug.Log($"BranchedEdges.Count: {BranchedEdges.Count}");

		for (var i = 0; (i < BranchedEdges.Count) || (i < (BranchedEdges.Count * completion)); i++) {
		//for (var i = 0; i < (float)BranchedEdges.Count * completion; i++) {
			
			UfEdge e = BranchedEdges[i];
			
			// Add the current face to the unfolded poly
			if (!ConstructedFaces.Contains(e.Face1)) {
				unfoldedPoly.Faces.Add(e.Face1.GetVertices());
				unfoldedPoly.FaceRoles.Add(ConwayPoly.Roles.New);
				foreach (Vertex v in e.Face1.GetVertices()) {
					if (!AddedVertices.Contains(v)) {
						unfoldedPoly.Vertices.Add(v);
						AddedVertices.Add(v);
					}
				}

				ConstructedFaces.Add(e.Face1);
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Face1)");
			}

			
			if (!ConstructedFaces.Contains(e.Face2))
			{
				
				float angle = Vector3.Angle(e.Face1.Normal, e.Face2.Normal) * completionAngle;
				Debug.Log($"Face {i} angle to unfold: {angle}");
				Vector3 axis = e.Halfedge2.Vector;
				Quaternion rotation1 = Quaternion.AngleAxis(angle, axis);
				Quaternion rotation2 = Quaternion.AngleAxis(-angle, axis);
				
				// I think this is splitting vertices?
				foreach (Vertex v in e.Face2.GetVertices()) {
					if (!verticesInBranches.ContainsKey(v) && v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex) {
						Halfedge edge = e.Face2.Halfedge;
						do {
							if (edge.Vertex == v) {
								edge.Vertex = new Vertex(v.Position);
								break;
							}

							edge = edge.Next;
						} while (edge != e.Face2.Halfedge);
					}
				}
				List<Face> descendants = GetDescendants(GetNodeById(e.Face2, branched));
				foreach (Face f in descendants) {
					foreach (Vertex v in f.GetVertices()) {
						if (!verticesInBranches.ContainsKey(v)) {
							Halfedge edge = e.Face2.Halfedge;
							do {
								if (edge.Vertex == v) {
									edge.Vertex = new Vertex(v.Position);
									break;
								}

								edge = edge.Next;
							} while (edge != e.Face2.Halfedge);
						}
					}
				}

				// The actual rotation
				bool negative = false;
				var rotatedVertices = new List<Vertex>();
				foreach (Vertex v in e.Face2.GetVertices()) {
					// Only rotate vertices that aren't part of the hinge
					if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex) {
						v.Position -= e.Halfedge2.Vertex.Position;
						v.Position = rotation1 * v.Position;
						v.Position += e.Halfedge2.Vertex.Position;
						rotatedVertices.Add(v);
					}
				}


				if (Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != 1 &&
				    Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != -1) {
					Debug.Log("Negative Angle Required");
					negative = true;
					var FaceVertices = e.Face2.GetVertices();
					foreach (Vertex v in FaceVertices) {
						// Only rotate vertices that aren't part of the hinge
						if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex) {
							v.Position -= e.Halfedge2.Vertex.Position;
							v.Position = rotation2 * v.Position;
							v.Position = rotation2 * v.Position;
							v.Position += e.Halfedge2.Vertex.Position;
						}
					}
				}

				if (negative) {
					foreach (Face f in descendants) {
						foreach (Vertex v in f.GetVertices()) {
							if (!rotatedVertices.Contains(v)) {
								v.Position -= e.Halfedge2.Vertex.Position;
								v.Position = rotation2 * v.Position;
								v.Position += e.Halfedge2.Vertex.Position;
								rotatedVertices.Add(v);
							}
						}
					}
				}
				else {
					foreach (Face f in descendants) {
						foreach (Vertex v in f.GetVertices()) {
							if (!rotatedVertices.Contains(v)) {
								v.Position -= e.Halfedge2.Vertex.Position;
								v.Position = rotation1 * v.Position;
								v.Position += e.Halfedge2.Vertex.Position;
								rotatedVertices.Add(v);
							}
						}
					}
				}

				unfoldedPoly.Faces.Add(e.Face2.GetVertices());
				unfoldedPoly.FaceRoles.Add(ConwayPoly.Roles.New);
				foreach (Vertex v in e.Face2.GetVertices()) {
					if (!AddedVertices.Contains(v)) {
						unfoldedPoly.Vertices.Add(v);
						AddedVertices.Add(v);
					}
				}

				ConstructedFaces.Add(e.Face2);
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Face2)");
			}

		}


		unfoldedPoly.VertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, unfoldedPoly.Vertices.Count).ToList();
		unfoldedPoly.Halfedges.MatchPairs();
		if (activate) {
			var mesh = poly.BuildMeshFromConwayPoly(unfoldedPoly);
			poly.AssignFinishedMesh(mesh);
		}
		else {
			poly.Rebuild();
		}
	}

	
	private List<UfFace> AddChildren(UfFace root)
	{
		List<Face> sharedFaces = SharedFaces(root.GetID());
		foreach (Face sharedFace in sharedFaces)
		{
			root.AddChild(sharedFace);
			foreach (UfEdge edge in PolyEdges)
			{
				if ((edge.Face1 == root.GetID() && edge.Face2 == sharedFace) || (edge.Face2 == root.GetID() && edge.Face1 == sharedFace))
				{
					edge.Branched = true;
				}
			}
		}
		return root.GetChildren();
	}

	
	private List<Face> SharedFaces(Face f)
	{
		List<Face> sharedFaces = new List<Face>();
		foreach(UfEdge e in PolyEdges)
		{
			if (e.Face1 == f)
			{
				if (ConnectedFaces.Contains(e.Face2))
				{
					if (e.Branched)
					{
						continue;
					} else {
						e.Tabbed = true;
					}
				} else {
					sharedFaces.Add(e.Face2);
					ConnectedFaces.Add(e.Face1);
					ConnectedFaces.Add(e.Face2);
					e.EdgeChecked = true;
				}
			}
			else if (e.Face2 == f)
			{
				if (ConnectedFaces.Contains(e.Face1))
				{
					if (e.Branched)
					{
						continue;
					} else {
						e.Tabbed = true;
					}
				} else
				{
					sharedFaces.Add(e.Face1);
					ConnectedFaces.Add(e.Face1);
					ConnectedFaces.Add(e.Face2);
					e.EdgeChecked = true;
				}
			}
		}
		return sharedFaces;
	}

	
	private UfFace GetNodeById(Face f, List<UfFace> p) {
		foreach (UfFace n in p) {
			if (n.GetID() == f) {
				return n;
			}
		}
		return null;
	}

	
	private List<Face> GetDescendants(UfFace n) {
		List<Face> descendants = new List<Face>();
		List<UfFace> queue = new List<UfFace>();
		if (n != null) {
			foreach (UfFace p in n.GetChildren()) {
				queue.Add(p);
			}
			while (queue.Count != 0) {
				descendants.Add(queue[0].GetID());
				foreach (UfFace p in queue[0].GetChildren()) {
					queue.Add(p);
				}
				queue.RemoveAt(0);
			}
		}
		return descendants;
	}
	
	
}