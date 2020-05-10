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
	private List<UfEdge> UfEdges;
	private List<Face> ConnectedFaces;
	private PolyHydra originalPoly;
	
	
	[Range(0,1)]
	public float completion;
	[Range(0,360)]
	public float completionAngle = 1;

	public bool activate = true;
	public bool dummy;

	void Start()
	{
		originalPoly = GetComponent<PolyHydra>();
		
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
		
		Debug.Log("Unfolding");
	
		UfEdges = new List<UfEdge>(); // creates an empty list to store the edges of the polyhedron
		var CheckedHalfEdges = new List<Halfedge>(); // makes sure Halfedges aren't stored twice
		ConnectedFaces = new List<Face>(); // creates an empty list to store the faces that are connected in the tree
		var BranchedEdges = new List<UfEdge>(); // stores the edges that make up the edges of the tree
		var TabbedEdges = new List<UfEdge>(); // stores the edges that would have a tab on them for craft {optional}
		// this loop goes through all the halfedges in the polyhedron and creates UfEdges to represent them
		foreach (Halfedge h in	originalPoly._conwayPoly.Halfedges)
		{
			if (CheckedHalfEdges.Contains(h)){
				continue; // if the halfedge is part of a UfEdge already, we skip it
			}
			else
			{
				CheckedHalfEdges.Add(h); // adds the halfedge to the the checked list
				if (h.Pair == null) {
					continue; // if the halfedge doesn't have a pair, then it doesn't need to be stored in a UfEdge
				} else {
					CheckedHalfEdges.Add(h.Pair);
					UfEdges.Add(new UfEdge(h.Face, h.Pair.Face, h, h.Pair)); // adds the Halfedge, Face and their pairs to a UfEdge instance
				}
			}
		}
		
		Debug.Log("Amount Of Faces: " + originalPoly._conwayPoly.Faces.Count); // logs the amount of faces in the polyhedron
		Debug.Log("Amount Of Edges: " + UfEdges.Count); // logs the amount of edges in the polyhedron
		
		UfFace rootFace = new UfFace(originalPoly._conwayPoly.Faces[0], null); // stores the first face of the polyhedron as the root of the tree
		List<UfFace> children = AddChildren(rootFace); // finds the children of the root face and stores them
		List<UfFace> queue = new List<UfFace>(); // creates a queue of faces to be stored and processed
		List<UfFace> branched = new List<UfFace>(); // creates a list of faces to represent the faces in the tree
		foreach (UfFace c in children)
		{
			// adds the children of the root face into the queue and branched list
			queue.Add(c);  
			branched.Add(c);
		}
		while (queue.Count != 0)
		{
			// while the queue has items, it will find the children of each item and add them to the queue and the branched list
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
			// once the item's children are added, it gets removed from the queue
			queue.RemoveAt(0);
		}
		int tabs = 0;
		int branches = 0;
		// this loop logs the amount of branched edges to make the tree and the amount of tabbed edges
		foreach (UfEdge e in UfEdges)
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

		var verticesInBranches = new Dictionary<Vertex, List<Face>>(); // new dictionary that holds all the vertices that are in branched edges
		// any vertex stored in this list won't be split later for unfolding
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

		var unfoldedPoly = new ConwayPoly(); // the polyhedra to create
		var ConstructedFaces = new List<Face>(); // a list of faces to check which have been constructed already
		var AddedVertices = new List<Vertex>(); // a list of vertices to make sure vertices aren't being added multiple times
		
		Debug.Log($"BranchedEdges.Count: {BranchedEdges.Count}"); // logs the amount of branched edges

		// loops through all of the branched edges to unfold the polyhedron
		//for (var i = 0; i < (float)BranchedEdges.Count * completion; i++)
		for (var i = 0; (i < (BranchedEdges.Count * completion)); i++)
		{
			UfEdge e = BranchedEdges[i]; //stores the current branch in a local variable
			
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

				ConstructedFaces.Add(e.Face1); // adds the current face to the constructed faces list
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Face1)");
			}

			
			if (!ConstructedFaces.Contains(e.Face2))
			{
				// finds the angle between the normals of both face
				float angle = Vector3.Angle(e.Face1.Normal, e.Face2.Normal);
				angle = completionAngle;
				Debug.Log($"Face {i} angle to unfold: {angle}");
				// sets the axis of the rotation as the vector of the halfedge
				Vector3 axis = e.Halfedge2.Vector;
				// two rotations are available to compensate for different directions of vectors
				Quaternion rotation1 = Quaternion.AngleAxis(angle, axis);
				Quaternion rotation2 = Quaternion.AngleAxis(-angle, axis);
				
				// splits the vertices that aren't in branched edges or in the current halfedge
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
				// splits the vertices of children and further of the face
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
				
				bool negative = false; // if true, then we know which direction to rotate the children
				
				var rotatedVertices = new List<Vertex>(); // makes sure vertices aren't overrotated
				foreach (Vertex v in e.Face2.GetVertices()) {
					// Only rotate vertices that aren't part of the hinge
					if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex) {
						v.Position -= e.Halfedge2.Vertex.Position;
						v.Position = rotation1 * v.Position;
						v.Position += e.Halfedge2.Vertex.Position;
						rotatedVertices.Add(v); // adds the vertex to a list to not be rotated again in this cycle
					}
				}

				// checks if the dot product between the two faces are 1 or -1, AKA if they're on the same plane
				if (Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != 1 &&
				    Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != -1) {
					Debug.Log("Negative Angle Required");
					negative = true; // notes which direction the children have to rotate towards
					var FaceVertices = e.Face2.GetVertices();
					foreach (Vertex v in FaceVertices) {
						// Only rotate vertices that aren't part of the hinge
						if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex) {
							//v.Position -= e.Halfedge2.Vertex.Position;
							// rotates the vertices twice in the opposite direction to negate the effects of the first rotation
							//v.Position = rotation2 * v.Position;
							//v.Position = rotation2 * v.Position;
							//v.Position += e.Halfedge2.Vertex.Position;
						}
					}
				}

				// rotates the children of the current face by the rotation of the current face
				foreach (Face f in descendants) {
					foreach (Vertex v in f.GetVertices()) {
						if (!rotatedVertices.Contains(v)) {
							v.Position -= e.Halfedge2.Vertex.Position;
							if (negative) {
								v.Position = rotation2 * v.Position;
							} else {
								v.Position = rotation1 * v.Position;
							}
							v.Position += e.Halfedge2.Vertex.Position;
							rotatedVertices.Add(v);
						}
					}
				}

				// adds the current face to the unfolded poly
				unfoldedPoly.Faces.Add(e.Face2.GetVertices());
				unfoldedPoly.FaceRoles.Add(ConwayPoly.Roles.New);
				foreach (Vertex v in e.Face2.GetVertices()) {
					if (!AddedVertices.Contains(v)) {
						unfoldedPoly.Vertices.Add(v);
						AddedVertices.Add(v);
					}
				}

				ConstructedFaces.Add(e.Face2); // adds the current face to the list of constructed faces
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Face2)");
			}

		}

		// sets the vertex roles of the unfolded poly
		unfoldedPoly.VertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, unfoldedPoly.Vertices.Count).ToList();
		unfoldedPoly.Halfedges.MatchPairs();
		// on activate, it assigns the unfolded poly mesh
		if (activate) {
			var mesh = originalPoly.BuildMeshFromConwayPoly(unfoldedPoly);
			originalPoly.AssignFinishedMesh(mesh);
		}
		else {
			originalPoly.Rebuild();
		}
	}

	
	private List<UfFace> AddChildren(UfFace root)
	{
		// gets the children of the current face
		List<Face> sharedFaces = SharedFaces(root.GetID());
		// loops through each child
		foreach (Face sharedFace in sharedFaces)
		{
			// adds the shared face as a child of the current face
			root.AddChild(sharedFace);
			// loops through each edge to check which edges need to be marked as a branched edge
			foreach (UfEdge edge in UfEdges)
			{
				if ((edge.Face1 == root.GetID() && edge.Face2 == sharedFace) || (edge.Face2 == root.GetID() && edge.Face1 == sharedFace))
				{
					edge.Branched = true;
				}
			}
		}
		// returns the list of children of the current face
		return root.GetChildren();
	}

	
	private List<Face> SharedFaces(Face f)
	{
		// creates a new list of faces to be returned at the end of the method
		List<Face> sharedFaces = new List<Face>();
		// loops through all UfEdges to check for UfEdges including the given face
		foreach(UfEdge e in UfEdges)
		{
			if (e.Face1 == f)
			{
				// if the face is already connected, then it isn't a child of the face
				if (ConnectedFaces.Contains(e.Face2))
				{
					// if the edge isn't branched, but the face occurs again, then it's a tab edge
					if (e.Branched)
					{
						continue;
					} else {
						e.Tabbed = true;
					}
				} else {
					//adds the face to the returned list
					sharedFaces.Add(e.Face2);
					// adds the face and it's pair to ConnectedFaces
					ConnectedFaces.Add(e.Face1);
					ConnectedFaces.Add(e.Face2);
					// lets the system know that the edge has been checked and doesn't need to be checked again
					e.EdgeChecked = true;
				}
			}
			else if (e.Face2 == f)
			{
				// if the face is already connected, then it isn't a child of the face
				if (ConnectedFaces.Contains(e.Face1))
				{
					// if the edge isn't branched, but the face occurs again, then it's a tab edge
					if (e.Branched)
					{
						continue;
					} else {
						e.Tabbed = true;
					}
				} else
				{
					//adds the face to the returned list
					sharedFaces.Add(e.Face1);
					// adds the face and it's pair to ConnectedFaces
					ConnectedFaces.Add(e.Face1);
					ConnectedFaces.Add(e.Face2);
					// lets the system know that the edge has been checked and doesn't need to be checked again
					e.EdgeChecked = true;
				}
			}
		}
		// returns the list of faces that are the children of the current face
		return sharedFaces;
	}

	
	private UfFace GetNodeById(Face f, List<UfFace> p) {
		// loops through each UfFace in the given list
		foreach (UfFace n in p) {
			// if the UfFace is the same as the given face, then we return the UfFace
			if (n.GetID() == f) {
				return n;
			}
		}
		// if the given face isn't represented in a UfFace, we return null
		return null;
	}

	
	private List<Face> GetDescendants(UfFace n) {
		List<Face> descendants = new List<Face>(); // creates a new list to store the descendants (children of children of etc.) of the current UfFace
		List<UfFace> queue = new List<UfFace>(); // creates a queue for getting descendants
		// makes sure that the UfFace isn't null
		if (n != null) {
			// loops through each child to add to the queue
			foreach (UfFace p in n.GetChildren()) {
				queue.Add(p);
			}
			// loops through the queue (which can change size)
			while (queue.Count != 0) {
				// adds the queue item to the descendants list
				descendants.Add(queue[0].GetID());
				// loops through the children of the descendant and adds it to the queue
				foreach (UfFace p in queue[0].GetChildren()) {
					queue.Add(p);
				}
				// once the item in the queue is processed, we remove it from the queue
				queue.RemoveAt(0);
			}
		}
		// returns the list of the descendants
		return descendants;
	}
}