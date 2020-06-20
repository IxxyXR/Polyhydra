using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEditor.ProBuilder;
using UnityEngine.ProBuilder;
using UnityEngine;
using Wythoff;
using Face = UnityEngine.ProBuilder.Face;


public class PolyProbuilder : MonoBehaviour
{
    public Material mat;

    [ContextMenu("Probuilder Object")]
    public void Foo()
    {

        var prev = GameObject.Find("New Polyhedra");
        DestroyImmediate(prev);

        var wythoff = new WythoffPoly(Uniform.Uniforms[28].Wythoff);
        wythoff.BuildFaces();
        var conway = new ConwayPoly(wythoff);
        //conway = conway.Quinto(0.2f);
        //var conway = JohnsonPoly.Prism(6);

        var verts = new List<Vector3>();
        var faces = new List<Face>();

        ConwayToProbuilderMeshInputs(conway, ref verts, ref faces);

        var pmesh = ProBuilderMesh.Create(verts, faces);
        var mr = pmesh.gameObject.GetComponent<MeshRenderer>();
        mr.material = mat;
        pmesh.gameObject.name = "New Polyhedra";



        // Back to a conway

        var vertexPoints = pmesh.positions;
        var faceIndices = new List<List<int>>();
        foreach (var f in pmesh.faces)
        {
            faceIndices.Add(f.indexes.ToList());
        }

        var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.New, faceIndices.Count).ToList();
        var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, vertexPoints.Count).ToList();

        var conway2 = new ConwayPoly(vertexPoints, faceIndices.ToArray(), faceRoles, vertexRoles);
        //conway2 = conway2.FaceScale(-0.2f, ConwayPoly.FaceSelections.All, false);
        conway2 = conway2.Loft(0.25f);

        var verts2 = new List<Vector3>();
        var faces2 = new List<Face>();

        ConwayToProbuilderMeshInputs(conway2, ref verts2, ref faces2);
        var pmesh2 = ProBuilderMesh.Create(verts2, faces2);
        pmesh2.Refresh();
        EditorMeshUtility.Optimize(pmesh2, true);
        var mr2 = pmesh2.gameObject.GetComponent<MeshRenderer>();
        mr2.material = mat;
        pmesh2.gameObject.name = "New Polyhedra 2";
        DestroyImmediate(pmesh);

    }

    public static void ConwayToProbuilderMeshInputs(ConwayPoly conway, ref List<Vector3> verts, ref List<Face> faces)
    {
        for (var i = 0; i < conway.Faces.Count; i++)
        {
            var face = conway.Faces[i];
            var edges = face.GetHalfedges();

            var faceVerts = new List<int>();

            if (edges.Count == 3)
            {
                verts.Add(edges[0].Vertex.Position);
                faceVerts.Add(verts.Count - 1);
                verts.Add(edges[1].Vertex.Position);
                faceVerts.Add(verts.Count - 1);
                verts.Add(edges[2].Vertex.Position);
                faceVerts.Add(verts.Count - 1);
            }
            else
            {
                verts.Add(face.Centroid);
                int centroidIndex = verts.Count - 1;
                verts.Add(edges[0].Vertex.Position);

                for (var j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j % edges.Count];
                    verts.Add(edge.Next.Vertex.Position);
                    int lastIndex = verts.Count - 1;
                    faceVerts.Add(centroidIndex);
                    faceVerts.Add(lastIndex - 1);
                    faceVerts.Add(lastIndex);
                }
            }

            faces.Add(new Face(faceVerts));
        }

    }
}
