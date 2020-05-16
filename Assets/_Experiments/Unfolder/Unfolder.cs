using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using Face = Conway.Face;


[ExecuteInEditMode]
public class Unfolder : MonoBehaviour
{
	// Fields For Unfolding
	private List<UfEdge> UfEdges;
	private List<Face> ConnectedFaces;
	private PolyHydra originalPoly;
	private List<Vector3> newVertices;
	private List<List<int>> newFaceIndices;


	[Range(0,360)]
	public float completionAngle = 1;

	public bool activate = true;
	public bool dummy;

	public bool constructRootFace;
	public bool constructRotatedFace;
	public bool constructDescendentFace;
	public bool rotationStage1;
	public bool rotationStage2;
	public bool rotationStage3;
	public bool rotationStage4;
	public bool rotationStage5;
	
	void Start() { originalPoly = GetComponent<PolyHydra>(); }
	private void OnValidate() { Unfold(); }






	public void Unfold()
	{
		
		newVertices = new List<Vector3>();
		newFaceIndices = new List<List<int>>();

		UfEdges = new List<UfEdge>(); // Creates an empty list to store the edges of the polyhedron
		ConnectedFaces = new List<Face>(); // Creates an empty list to store the faces that are connected in the tree

		var checkedHalfEdges = new List<Halfedge>(); // Makes sure halfedges aren't stored twice
		var branchedEdges = new List<UfEdge>(); // Stores the edges that make up the edges of the tree



		// This loop goes through all the halfedges in the polyhedron and creates UfEdges to represent them
		foreach (Halfedge h in originalPoly._conwayPoly.Halfedges)
		{
			if (!checkedHalfEdges.Contains(h))
			{
				{
					checkedHalfEdges.Add(h); // adds the halfedge to the the checked list
					if (h.Pair != null)
					{
						// if the halfedge doesn't have a pair, then it doesn't need to be stored in a UfEdge
						checkedHalfEdges.Add(h.Pair);
						UfEdges.Add(new UfEdge(h.Face, h.Pair.Face, h, h.Pair)); // Adds the Halfedge, Face and their pairs to a UfEdge instance
					}
				}
			}
		}



			var rootFace = new UfFace(originalPoly._conwayPoly.Faces[0], null);
			var queue = new List<UfFace>(); // Faces to be stored and processed
			var branched = new List<UfFace>();
			
			List<UfFace> children = AddChildren(rootFace);
			
			foreach (UfFace c in children)
			{
				queue.Add(c);
				branched.Add(c);
			}

			// Find the children of each item and add them to the queue and the branched list
			while (queue.Count != 0)
			{
				children = AddChildren(queue[0]);
				
				foreach (var c in children.Where(c => !queue.Contains(c)))
				{
					if (!branched.Contains(c)) {branched.Add(c);}
					queue.Add(c);
				}
				queue.RemoveAt(0);  // Once the item's children are added, it gets removed from the queue
			}



			// Log branched edges to make the tree
			branchedEdges.AddRange(UfEdges.Where(e => e.Branched));

			// Vertices that are in branched edges
			var verticesInBranches = new Dictionary<Vertex, List<Face>>();

			// Any vertex stored in this list won't be split later for unfolding
			foreach (UfEdge ufEdge in branchedEdges)
			{
				if (verticesInBranches.ContainsKey(ufEdge.Halfedge1.Vertex))
				{
					verticesInBranches[ufEdge.Halfedge1.Vertex].Add(ufEdge.Halfedge1.Face);
					verticesInBranches[ufEdge.Halfedge1.Vertex].Add(ufEdge.Halfedge2.Face);
				}
				else
				{
					verticesInBranches.Add(
						ufEdge.Halfedge1.Vertex,
						new List<Face>() {ufEdge.Halfedge1.Face, ufEdge.Halfedge2.Face}
					);
				}

				if (verticesInBranches.ContainsKey(ufEdge.Halfedge2.Vertex))
				{
					verticesInBranches[ufEdge.Halfedge2.Vertex].Add(ufEdge.Halfedge1.Face);
					verticesInBranches[ufEdge.Halfedge2.Vertex].Add(ufEdge.Halfedge2.Face);
				}
				else
				{
					verticesInBranches.Add(
						ufEdge.Halfedge2.Vertex,
						new List<Face>() {ufEdge.Halfedge1.Face, ufEdge.Halfedge2.Face}
					);
				}
			}

		RotateChildren(branchedEdges, verticesInBranches, branched);
		ConstructMesh(newVertices, newFaceIndices);
	}


















	private void RotateChildren(
			List<UfEdge> branchedEdges,
			Dictionary<Vertex, List<Face>> verticesInBranches,
			List<UfFace> branched)
		{
			
			var constructedFaces = new List<string>(); // Faces which have been constructed already
			var alreadyRotated = new Dictionary<string, int>();
			var alteredEdges = new Dictionary<string, List<Vector3>>();
		
			// Loops through all of the branched edges to unfold the polyhedron
			for (var i = 0; (i < branchedEdges.Count); i++)
			{
				
				UfEdge ufEdge = branchedEdges[i];
			
				// Add the current face to the unfolded poly
				if (!constructedFaces.Contains(ufEdge.Halfedge1.Face.Name))
				{
					var newRootFace = new List<int>();
					foreach (var v in ufEdge.Halfedge1.Face.GetVertices())
					{
						newVertices.Add(v.Position);
						newRootFace.Add(newVertices.Count - 1);
					}
					if (constructRootFace) newFaceIndices.Add(newRootFace);
					constructedFaces.Add(ufEdge.Halfedge1.Face.Name); // adds the current face to the constructed faces list
				}


				if (!constructedFaces.Contains(ufEdge.Halfedge2.Face.Name))
				{
					// Finds the angle between the normals of both face
					float angle = completionAngle;
					//float angle = Vector3.Angle(ufEdge.Halfedge1.Face.Normal, ufEdge.Halfedge2.Face.Normal);
					
					// Sets the axis of the rotation as the vector of the halfedge
					string edgeName = ufEdge.Halfedge2.Name;
					Vector3 axis = alteredEdges.ContainsKey(edgeName) ? alteredEdges[edgeName][1] : ufEdge.Halfedge2.Vector;
					// Two rotations are available to compensate for different directions of vectors
					Quaternion rotationq1 = Quaternion.AngleAxis(angle, axis);
					Quaternion rotationq2 = Quaternion.AngleAxis(-angle, axis);
				
					// Splits the vertices that aren't in branched edges or in the current halfedge
					foreach (Vertex v in ufEdge.Halfedge2.Face.GetVertices())
					{
						if (verticesInBranches.ContainsKey(v) || v == ufEdge.Halfedge2.Vertex || v == ufEdge.Halfedge1.Vertex) continue;
						Halfedge edge = ufEdge.Halfedge2.Face.Halfedge;
							
						do {
							if (edge.Vertex == v) {
								edge.Vertex = new Vertex(v.Position);
								break;
							}
							edge = edge.Next;
						} while (edge != ufEdge.Halfedge2.Face.Halfedge);
					}
					
					// Split the vertices of the face and it's descendents
					List<Face> descendants = GetDescendants(GetNodeById(ufEdge.Halfedge2.Face, branched));
					foreach (Face f in descendants) {
						foreach (Vertex v in f.GetVertices())
						{
							if (verticesInBranches.ContainsKey(v)) continue;
							
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
				
					/////////////////////////
					// The actual rotation //
					/////////////////////////
					
					bool negative = false; // Which direction to rotate the children
					var rotatedVertices = new HashSet<Vector3>(); // Make sure vertices aren't over-rotated

					if (alreadyRotated.ContainsKey(ufEdge.Halfedge2.Face.Name))
					{
						// Rotate an existing new face
						foreach (int vertIndex in newFaceIndices[alreadyRotated[ufEdge.Halfedge2.Face.Name]])
						{
							if (rotationStage1) newVertices[vertIndex] = RotatePoint(newVertices[vertIndex], (negative ? rotationq2 : rotationq1), alteredEdges[ufEdge.Halfedge2.Name][0]);
						}
					}
					else
					{
						// Add a new face to rotate
						var newRotatedFace = new List<int>();
						foreach (Vertex originalVertex in ufEdge.Halfedge2.Face.GetVertices())
						{
							Vector3 newVertex = originalVertex.Position;
							// Only rotate vertices that aren't part of the hinge
							if (newVertex != ufEdge.Halfedge2.Vertex.Position &&
							    newVertex != ufEdge.Halfedge1.Vertex.Position)
							{
								if (rotationStage2) newVertex = RotatePoint(newVertex, (negative ? rotationq2 : rotationq1), ufEdge.Halfedge2.Vertex.Position);
								rotatedVertices.Add(newVertex); // adds the vertex to a list to not be rotated again in this cycle
							}

							newVertices.Add(newVertex);
							newRotatedFace.Add(newVertices.Count - 1);
						}
					
						// The one that does get rotated
						if (constructRotatedFace) newFaceIndices.Add(newRotatedFace);
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
				
					
					// Rotates face's descendents as well
					foreach (Face descendentFace in descendants)
					{ 
						if (alreadyRotated.ContainsKey(descendentFace.Name))
						{
							// Use the existing face but rotate it to follow the rotation of the current descendent
							foreach (int vertIndex in newFaceIndices[alreadyRotated[descendentFace.Name]])
							{
								if (rotationStage3) newVertices[vertIndex] = RotatePoint(newVertices[vertIndex], (negative ? rotationq2 : rotationq1), alteredEdges[ufEdge.Halfedge2.Name][0]);
							}
						}
						else
						{
							// Add the new face that follows the rotation of the current descendent
							var newChildFace = new List<int>();
							foreach (Vertex originalVertex in descendentFace.GetVertices())
							{
								Vector3 newVertex = originalVertex.Position;
						
								if (rotationStage4) newVertex = RotatePoint(newVertex, (negative ? rotationq2 : rotationq1), ufEdge.Halfedge2.Vertex.Position);
						
								newVertices.Add(newVertex);
								newChildFace.Add(newVertices.Count - 1);

								// Check for branched edges 
								foreach(UfEdge branchedEdge in branchedEdges)
								{
									if (branchedEdge.Halfedge2.Vertex.Name != originalVertex.Name) continue;
									Vector3 newPrevVertex = branchedEdge.Halfedge2.Prev.Vertex.Position;
									if (rotationStage5) newPrevVertex = RotatePoint(newPrevVertex, (negative ? rotationq2 : rotationq1), branchedEdge.Halfedge2.Vertex.Position);
									alteredEdges[branchedEdge.Halfedge2.Name] = new List<Vector3>(){newVertex, newVertex - newPrevVertex};
								}
						
							}
							
							// The one that doesn't get rotated
							if (constructDescendentFace)
							{
								newFaceIndices.Add(newChildFace);
								alreadyRotated[descendentFace.Name] = newFaceIndices.Count - 1;
							}
						}
						constructedFaces.Add(descendants.Last().Halfedge.Face.Name);
						
					}

					constructedFaces.Add(ufEdge.Halfedge2.Face.Name); // adds the current face to the list of constructed faces
				}
			}
		}








	
	
		public Vector3 RotatePoint(Vector3 point, Quaternion rotationQ, Vector3 origin)
		{
			point -= origin;
			point = rotationQ * point;
			point += origin;
			return point;
		}






	
	
		private void ConstructMesh(List<Vector3> newVertices, List<List<int>> newFaceIndices)
		{
			// On activate, it assigns the unfolded poly mesh
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
			List<Face> sharedFaces = SharedFaces(root.ID);
			// loops through each child
			foreach (Face sharedFace in sharedFaces)
			{
				// adds the shared face as a child of the current face
				root.AddChild(sharedFace);
				// loops through each edge to check which edges need to be marked as a branched edge
				var edges = UfEdges.Where(edge =>
					edge.Halfedge1.Face == root.ID && edge.Halfedge2.Face == sharedFace ||
					edge.Halfedge2.Face == root.ID && edge.Halfedge1.Face == sharedFace);

				foreach (var edge in edges)
				{
					edge.Branched = true;
				}
			}
			// returns the list of children of the current face
			return root.Children;
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
						e.Tabbed = !e.Branched;
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
						e.Tabbed = !e.Branched;
					}
					else
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

	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
		private UfFace GetNodeById(Face f, List<UfFace> p)
		{
			// loops through each UfFace in the given list
			return p.FirstOrDefault(n => n.ID == f);
			// if the given face isn't represented in a UfFace, we return null
		}

	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
		private List<Face> GetDescendants(UfFace n) {
			
			var descendants = new List<Face>(); // creates a new list to store the descendants (children of children of etc.) of the current UfFace

			if (n == null) return descendants;
			var queue = n.Children.ToList(); // creates a queue for getting descendants

			while (queue.Count != 0) {
				descendants.Add(queue[0].ID);
				foreach (UfFace p in queue[0].Children) {
					queue.Add(p);
				}
				// Remove the processed item
				queue.RemoveAt(0);
			}

			return descendants;
			
		}
		
		
}