using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using Wythoff;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Face = Conway.Face;
using Random = UnityEngine.Random;

public class Unfolder : MonoBehaviour
{
	// Fields For Unfolding
	private List<PolyEdge> PolyEdges;
	private List<Face> ConnectedFaces;
	private PolyHydra poly;

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
	
	public void Unfold() {

		PolyEdges = new List<PolyEdge>();
		var CheckedHalfEdges = new List<Halfedge>();
		ConnectedFaces = new List<Face>();
		var BranchedEdges = new List<PolyEdge>();
		var TabbedEdges = new List<PolyEdge>();
		foreach (Halfedge h in	poly._conwayPoly.Halfedges)
		{
			if (CheckedHalfEdges.Contains(h)){
				continue;
			} else
			{
				CheckedHalfEdges.Add(h);
				if (h.Pair == null) {
					continue;
				} else {
					CheckedHalfEdges.Add(h.Pair);
					PolyEdges.Add(new PolyEdge(h.Face, h.Pair.Face, h, h.Pair));
				}
			}
		}
		Debug.Log("Amount Of Faces: " + poly._conwayPoly.Faces.Count);
		Debug.Log("Amount Of Edges: " + PolyEdges.Count);
		PolyNode root = new PolyNode(poly._conwayPoly.Faces[0], null);
		List<PolyNode> children = AddChildren(root);
		List<PolyNode> queue = new List<PolyNode>();
		List<PolyNode> branched = new List<PolyNode>();
		foreach (PolyNode c in children)
		{
			queue.Add(c);  
			branched.Add(c);
		}
		while (queue.Count != 0)
		{
			children = AddChildren(queue[0]);
			foreach (PolyNode c in children)
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
		foreach (PolyEdge e in PolyEdges)
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

		var unfoldedPoly = new ConwayPoly();
		var ConstructedFaces = new List<Face>();
		var AddedVertices = new List<Vertex>();
		foreach (PolyEdge e in BranchedEdges)
		{
			if (!ConstructedFaces.Contains(e.Face1))
			{
				Face face = e.Face1;
				/*
				Vector3 normal = face.Normal;
				var upwards = Vector3.up.normalized;
				float angle = Vector3.Angle(normal, upwards);
				Debug.Log("Angle = " + angle.ToString());
				Vector3 axis = e.Halfedge1.Vector;
				Quaternion rotation = Quaternion.AngleAxis(angle-180, axis);

				foreach (Vertex v in face.GetVertices())
					{
						v.Position -= e.Halfedge1.Vertex.Position;
						v.Position = rotation * v.Position;
						v.Position += e.Halfedge1.Vertex.Position;
					}
				*/
				unfoldedPoly.Faces.Add(face.GetVertices());
				unfoldedPoly.FaceRoles.Add(ConwayPoly.Roles.New);
				foreach(Vertex v in face.GetVertices())
				{
					if (!AddedVertices.Contains(v))
					{
						unfoldedPoly.Vertices.Add(v);
						AddedVertices.Add(v);
					}
				}
				ConstructedFaces.Add(e.Face1);
			}
			if (!ConstructedFaces.Contains(e.Face2))
			{
                float angle = Vector3.Angle(e.Face1.Normal, e.Face2.Normal);
				Debug.Log("Angle = " + angle.ToString());
				Vector3 axis = e.Halfedge2.Vector;
				Quaternion rotation1 = Quaternion.AngleAxis(angle, axis);
				Quaternion rotation2 = Quaternion.AngleAxis(-angle, axis);
				var originalFace = e.Face2;
				foreach (Vertex v in e.Face2.GetVertices())
					{
						// Only rotate vertices that aren't part of the hinge
						if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex)
						{
							v.Position -= e.Halfedge2.Vertex.Position;
							v.Position = rotation1 * v.Position;
							v.Position += e.Halfedge2.Vertex.Position;
						}
					}
				if (Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != 1 && Vector3.Dot(e.Face1.Normal, e.Face2.Normal) != -1) {
					e.Face2 = originalFace;
					Debug.Log("Negative Angle Required");
					foreach (Vertex v in e.Face2.GetVertices())
						{
							// Only rotate vertices that aren't part of the hinge
							if (v != e.Halfedge2.Vertex && v != e.Halfedge1.Vertex)
							{
								v.Position -= e.Halfedge2.Vertex.Position;
								v.Position = rotation2 * v.Position;
								v.Position += e.Halfedge2.Vertex.Position;
							}
						}
				}
				unfoldedPoly.Faces.Add(e.Face2.GetVertices());
				unfoldedPoly.FaceRoles.Add(ConwayPoly.Roles.New);
				foreach(Vertex v in e.Face2.GetVertices())
				{
					if (!AddedVertices.Contains(v))
					{
						unfoldedPoly.Vertices.Add(v);
						AddedVertices.Add(v);
					}
				}
				ConstructedFaces.Add(e.Face2);
			}
		}
		unfoldedPoly.VertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, unfoldedPoly.Vertices.Count).ToList();
		unfoldedPoly.Halfedges.MatchPairs();
		poly._conwayPoly = unfoldedPoly;
		Debug.Log(poly._conwayPoly.Faces.Count.ToString());
		Debug.Log(poly._conwayPoly.Vertices.Count.ToString());
		var mesh = new Mesh();
		mesh = poly.BuildMeshFromConwayPoly();
		poly.AssignFinishedMesh(mesh);
	}

	private List<PolyNode> AddChildren(PolyNode c)
	{
		List<Face> sharedFaces = SharedFaces(c.GetID());
		foreach (Face sf in sharedFaces)
		{
			c.AddChild(sf);
			foreach (PolyEdge e in PolyEdges)
			{
				if ((e.Face1 == c.GetID() && e.Face2 == sf) || (e.Face2 == c.GetID() && e.Face1 == sf))
				{
					e.Branched = true;
				}
			}
		}
		return c.GetChildren();
	}

	private List<Face> SharedFaces(Face f)
	{
		List<Face> sharedFaces = new List<Face>();
		foreach(PolyEdge e in PolyEdges)
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
}