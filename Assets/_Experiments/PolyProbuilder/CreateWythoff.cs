// This script demonstrates how to create a new action that can be accessed from the ProBuilder toolbar.
// A new menu item is registered under "Geometry" actions called "Gen. Shadows".

using System.Collections.Generic;
using System.Linq;
using Conway;
using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEngine.ProBuilder;
using Wythoff;
using Face = UnityEngine.ProBuilder.Face;


[ProBuilderMenuAction]
sealed class CreateWythoff : MenuAction
{
    public override ToolbarGroup group
    {
        get { return ToolbarGroup.Object; }
    }

    public override Texture2D icon
    {
        get { return null; }
    }

    public override TooltipContent tooltip
    {
        get { return k_Tooltip; }
    }

    // static readonly GUIContent k_VolumeSize = new GUIContent("Volume Size", "How far the shadow volume extends from " +
    //                                                                         "the base mesh.  To visualize, imagine the width of walls.\n\nYou can also select the child ShadowVolume " +
    //                                                                         "object and turn the Shadow Casting Mode to \"One\" or \"Two\" sided to see the resulting mesh.");

    // What to show in the hover tooltip window.  TooltipContent is similar to GUIContent, with the exception
    // that it also includes an optional params[] char list in the constructor to define shortcut keys
    // (ex, CMD_CONTROL, K).
    static readonly TooltipContent k_Tooltip = new TooltipContent(
        "Create Wythoff",
        "ferferferferf"
    );

    // static bool showPreview
    // {
    //     get { return EditorPrefs.GetBool("pb_shadowVolumePreview", true); }
    //     set { EditorPrefs.SetBool("pb_shadowVolumePreview", value); }
    // }

    // Determines if the action should be enabled or shown as disabled in the menu.
    // public override bool enabled
    // {
    //     get { return MeshSelection.selectedObjectCount > 0; }
    // }

    /// <summary>
    /// Determines if the action should be loaded in the menu (ex, face actions shouldn't be shown when in vertex editing mode).
    /// </summary>
    /// <returns></returns>
    public override bool hidden
    {
        get { return false; }
    }

    // protected override void OnSettingsEnable()
    // {
    //     if (showPreview)
    //         DoAction();
    // }

    protected override void OnSettingsGUI()
    {
        GUILayout.Label("Create Wythoff Polyhedra", EditorStyles.boldLabel);

        // EditorGUI.BeginChangeCheck();
        //
        // EditorGUI.BeginChangeCheck();
        //
        // float volumeSize = EditorPrefs.GetFloat("pb_CreateShadowObject_volumeSize", .07f);
        // volumeSize = EditorGUILayout.Slider(k_VolumeSize, volumeSize, 0.001f, 1f);
        // if (EditorGUI.EndChangeCheck()) EditorPrefs.SetFloat("pb_CreateShadowObject_volumeSize", volumeSize);
        //
        // EditorGUI.BeginChangeCheck();
        // var shadowMode = (ShadowCastingMode) EditorPrefs.GetInt("pb_CreateShadowObject_shadowMode", (int) ShadowCastingMode.ShadowsOnly);
        // shadowMode = (ShadowCastingMode) EditorGUILayout.EnumPopup("Shadow Casting Mode", shadowMode);
        // if (EditorGUI.EndChangeCheck()) EditorPrefs.SetInt("pb_CreateShadowObject_shadowMode", (int) shadowMode);
        //
        // EditorGUI.BeginChangeCheck();
        // ExtrudeMethod extrudeMethod = (ExtrudeMethod) EditorPrefs.GetInt("pb_CreateShadowObject_extrudeMethod", (int) ExtrudeMethod.FaceNormal);
        // extrudeMethod = (ExtrudeMethod) EditorGUILayout.EnumPopup("Extrude Method", extrudeMethod);
        // if (EditorGUI.EndChangeCheck()) EditorPrefs.SetInt("pb_CreateShadowObject_extrudeMethod", (int) extrudeMethod);

        if (EditorGUI.EndChangeCheck()) DoAction();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Create Wythoff Polyhedra"))
        {
            DoAction();
            SceneView.RepaintAll();
//				MenuOption.CloseAll();
        }
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


    /// <summary>
    /// Perform the action.
    /// </summary>
    /// <returns>Return a pb_ActionResult indicating the success/failure of action.</returns>
    public override ActionResult DoAction()
    {

        //var prev = GameObject.Find("New Polyhedra");
        //DestroyImmediate(prev);

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
        mr.material = BuiltinMaterials.defaultMaterial;
        pmesh.gameObject.name = "New Polyhedra";




        // ShadowCastingMode shadowMode = (ShadowCastingMode) EditorPrefs.GetInt("pb_CreateShadowObject_shadowMode",   (int) ShadowCastingMode.ShadowsOnly);
        // float extrudeDistance = EditorPrefs.GetFloat("pb_CreateShadowObject_volumeSize", .08f);
        // ExtrudeMethod extrudeMethod =  (ExtrudeMethod) EditorPrefs.GetInt("pb_CreateShadowObject_extrudeMethod", (int) ExtrudeMethod.FaceNormal);

        // foreach (ProBuilderMesh mesh in MeshSelection.top)
        // {
        //     ProBuilderMesh shadow = GetShadowObject(mesh);
        //
        //     if (shadow == null)
        //         continue;
        //
        //     foreach (Face f in shadow.faces)
        //     {
        //         f.SetIndexes(f.indexes.Reverse().ToArray());
        //         f.manualUV = true;
        //     }

            // shadow.Extrude(shadow.faces, extrudeMethod, extrudeDistance);
            // shadow.ToMesh();
            // shadow.Refresh();
            // shadow.Optimize();

            // MeshRenderer mr = shadow.gameObject.GetComponent<MeshRenderer>();
            // mr.shadowCastingMode = shadowMode;
            // if (shadowMode == ShadowCastingMode.ShadowsOnly)
            //     mr.receiveShadows = false;

            // Collider collider = shadow.GetComponent<Collider>();
            //
            // while (collider != null)
            // {
            //     Object.DestroyImmediate(collider);
            //     collider = shadow.GetComponent<Collider>();
            // }
        // }

        // Refresh the Editor wireframe and working caches.
        ProBuilderEditor.Refresh();

        return new ActionResult(ActionResult.Status.Success, "Create Shadow Object");
    }

}