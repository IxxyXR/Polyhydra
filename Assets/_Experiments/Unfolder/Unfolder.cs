using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using zCode.zMesh;
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
		
		var newVertices = new List<Vector3>();
		var newFaceIndices = new List<IEnumerable<int>>();


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
		foreach (UfEdge ufEdge in BranchedEdges)
		{
			if (verticesInBranches.ContainsKey(ufEdge.Halfedge1.Vertex))
			{
				verticesInBranches[ufEdge.Halfedge1.Vertex].Add(ufEdge.Halfedge1.Face);
				verticesInBranches[ufEdge.Halfedge1.Vertex].Add(ufEdge.Halfedge2.Face);
			} else {
				verticesInBranches.Add(ufEdge.Halfedge1.Vertex, new List<Face>(){ufEdge.Halfedge1.Face, ufEdge.Halfedge2.Face});
			}
			if (verticesInBranches.ContainsKey(ufEdge.Halfedge2.Vertex))
			{
				verticesInBranches[ufEdge.Halfedge2.Vertex].Add(ufEdge.Halfedge1.Face);
				verticesInBranches[ufEdge.Halfedge2.Vertex].Add(ufEdge.Halfedge2.Face);
			} else {
				verticesInBranches.Add(ufEdge.Halfedge2.Vertex, new List<Face>(){ufEdge.Halfedge1.Face, ufEdge.Halfedge2.Face});
			}
		}

		var ConstructedFaces = new List<string>(); // a list of faces to check which have been constructed already
		var alreadyRotated = new Dictionary<string, int>();
		
		Debug.Log($"BranchedEdges.Count: {BranchedEdges.Count}"); // logs the amount of branched edges

		
		// loops through all of the branched edges to unfold the polyhedron
		//for (var i = 0; i < (float)BranchedEdges.Count * completion; i++)
		for (var i = 0; (i < (BranchedEdges.Count * completion)); i++)
		{
			UfEdge ufEdge = BranchedEdges[i]; //stores the current branch in a local variable
			
			// Add the current face to the unfolded poly
			if (!ConstructedFaces.Contains(ufEdge.Halfedge1.Face.Name))
			{
				var newFace = new List<int>();
				foreach (var v in ufEdge.Halfedge1.Face.GetVertices())
				{
					newVertices.Add(v.Position);
					newFace.Add(newVertices.Count - 1);
				}
				newFaceIndices.Add(newFace);
				ConstructedFaces.Add(ufEdge.Halfedge1.Face.Name); // adds the current face to the constructed faces list
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Halfedge1.Face)");
			}

			
			if (!ConstructedFaces.Contains(ufEdge.Halfedge2.Face.Name))
			{
				// finds the angle between the normals of both face
				float angle = Vector3.Angle(ufEdge.Halfedge1.Face.Normal, ufEdge.Halfedge2.Face.Normal);
				angle = completionAngle;
				// sets the axis of the rotation as the vector of the halfedge
				Vector3 axis = ufEdge.Halfedge2.Vector;
				// two rotations are available to compensate for different directions of vectors
				Quaternion rotation1 = Quaternion.AngleAxis(angle, axis);
				Quaternion rotation2 = Quaternion.AngleAxis(-angle, axis);
				
				// splits the vertices that aren't in branched edges or in the current halfedge
				foreach (Vertex v in ufEdge.Halfedge2.Face.GetVertices()) {
					if (!verticesInBranches.ContainsKey(v) && v != ufEdge.Halfedge2.Vertex && v != ufEdge.Halfedge1.Vertex) {
						Halfedge edge = ufEdge.Halfedge2.Face.Halfedge;
						do {
							if (edge.Vertex == v) {
								edge.Vertex = new Vertex(v.Position);
								break;
							}

							edge = edge.Next;
						} while (edge != ufEdge.Halfedge2.Face.Halfedge);
					}
				}
				// splits the vertices of children and further of the face
				List<Face> descendants = GetDescendants(GetNodeById(ufEdge.Halfedge2.Face, branched));
				foreach (Face f in descendants) {
					foreach (Vertex v in f.GetVertices()) {
						if (!verticesInBranches.ContainsKey(v)) {
							Halfedge edge = ufEdge.Halfedge2.Face.Halfedge;
							do {
								if (edge.Vertex == v) {
									edge.Vertex = new Vertex(v.Position);
									break;
								}

								edge = edge.Next;
							} while (edge != ufEdge.Halfedge2.Face.Halfedge);
						}
					}
				}
				
				// The actual rotation
				
				bool negative = false; // if true, then we know which direction to rotate the children
				
				var rotatedVertices = new HashSet<Vector3>(); // makes sure vertices aren't overrotated


				if (alreadyRotated.ContainsKey(ufEdge.Halfedge2.Face.Name))
				{
					foreach (int vertIndex in newFaceIndices[alreadyRotated[ufEdge.Halfedge2.Face.Name]])
					{
						newVertices[vertIndex] -= ufEdge.Halfedge2.Vertex.Position;

						if (negative)
						{
							newVertices[vertIndex] = rotation2 * newVertices[vertIndex];
						}
						else
						{
							newVertices[vertIndex] = rotation1 * newVertices[vertIndex];
						}
						newVertices[vertIndex] += ufEdge.Halfedge2.Vertex.Position;
					}

				}
				else
				{
					var newFace1 = new List<int>();
					foreach (Vertex originalVertex in ufEdge.Halfedge2.Face.GetVertices())
					{
						Vector3 newVertex = originalVertex.Position;
						// Only rotate vertices that aren't part of the hinge
						if (newVertex != ufEdge.Halfedge2.Vertex.Position &&
						    newVertex != ufEdge.Halfedge1.Vertex.Position)
						{
							newVertex -= ufEdge.Halfedge2.Vertex.Position;
							newVertex = rotation1 * newVertex;
							newVertex += ufEdge.Halfedge2.Vertex.Position;
							rotatedVertices
								.Add(newVertex); // adds the vertex to a list to not be rotated again in this cycle
						}

						newVertices.Add(newVertex);
						newFace1.Add(newVertices.Count - 1);
					}

					newFaceIndices.Add(newFace1);
					
				}


				// checks if the dot product between the two faces are 1 or -1, AKA if they're on the same plane
				// if (Vector3.Dot(ufEdge.Halfedge1.Face.Normal, ufEdge.Halfedge2.Face.Normal) != 1 &&
				//     Vector3.Dot(ufEdge.Halfedge1.Face.Normal, ufEdge.Halfedge2.Face.Normal) != -1) {
				// 	Debug.Log("Negative Angle Required");
				// 	negative = true; // notes which direction the children have to rotate towards
				// 	var newFace2 = new List<int>();
				// 	foreach (Vertex originalVertex in ufEdge.Halfedge2.Face.GetVertices()) {
				// 		Vector3 newVertex = originalVertex.Position;
				// 		// Only rotate vertices that aren't part of the hinge
				// 		if (newVertex != ufEdge.Halfedge2.Vertex.Position && newVertex != ufEdge.Halfedge1.Vertex.Position) {
				// 			newVertex -= ufEdge.Halfedge2.Vertex.Position;
				// 			// rotates the vertices twice in the opposite direction to negate the effects of the first rotation
				// 			newVertex = rotation2 * newVertex;
				// 			newVertex = rotation2 * newVertex;
				// 			newVertex += ufEdge.Halfedge2.Vertex.Position;
				// 		}
				// 		newVertices.Add(newVertex);
				// 		newFace2.Add(newVertices.Count - 1);
				// 	}
				// 	newFaceIndices.Add(newFace2);
				// }
				
				// rotates the children of the current face by the rotation of the current face
				foreach (Face f in descendants)
				{
					if (alreadyRotated.ContainsKey(f.Name))
					{
						foreach (int vertIndex in newFaceIndices[alreadyRotated[f.Name]])
						{
							newVertices[vertIndex] -= ufEdge.Halfedge2.Vertex.Position;

							if (negative)
							{
								newVertices[vertIndex] = rotation2 * newVertices[vertIndex];
							}
							else
							{
								newVertices[vertIndex] = rotation1 * newVertices[vertIndex];
							}
							newVertices[vertIndex] += ufEdge.Halfedge2.Vertex.Position;
						}
					}
					else
					{
						var newChildFace = new List<int>();
						foreach (Vertex originalVertex in f.GetVertices())
						{
							Vector3 newVertex = originalVertex.Position;
						
							newVertex -= ufEdge.Halfedge2.Vertex.Position;
						
							if (negative) {
								newVertex = rotation2 * newVertex;
							} else {
								newVertex = rotation1 * newVertex;
							}
						
							newVertex += ufEdge.Halfedge2.Vertex.Position;
						
							newVertices.Add(newVertex);
							newChildFace.Add(newVertices.Count - 1);
						
						}
						newFaceIndices.Add(newChildFace);
						alreadyRotated[f.Name] = newFaceIndices.Count - 1;
					}

				}

				ConstructedFaces.Add(ufEdge.Halfedge2.Face.Name); // adds the current face to the list of constructed faces
			}
			else
			{
				Debug.LogWarning($"Face {i} ConstructedFaces.Contains(e.Halfedge2.Face)");
			}

		}

		// on activate, it assigns the unfolded poly mesh
		if (activate) {
			var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.New, newFaceIndices.Count);
			var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, newVertices.Count);
			var unfoldedPoly = new ConwayPoly(newVertices, newFaceIndices, faceRoles, vertexRoles);
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
				if ((edge.Halfedge1.Face == root.GetID() && edge.Halfedge2.Face == sharedFace) || (edge.Halfedge2.Face == root.GetID() && edge.Halfedge1.Face == sharedFace))
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
			if (e.Halfedge1.Face == f)
			{
				// if the face is already connected, then it isn't a child of the face
				if (ConnectedFaces.Contains(e.Halfedge2.Face))
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
					sharedFaces.Add(e.Halfedge2.Face);
					// adds the face and it's pair to ConnectedFaces
					ConnectedFaces.Add(e.Halfedge1.Face);
					ConnectedFaces.Add(e.Halfedge2.Face);

				}
			}
			else if (e.Halfedge2.Face == f)
			{
				// if the face is already connected, then it isn't a child of the face
				if (ConnectedFaces.Contains(e.Halfedge1.Face))
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
					sharedFaces.Add(e.Halfedge1.Face);
					// adds the face and it's pair to ConnectedFaces
					ConnectedFaces.Add(e.Halfedge1.Face);
					ConnectedFaces.Add(e.Halfedge2.Face);
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