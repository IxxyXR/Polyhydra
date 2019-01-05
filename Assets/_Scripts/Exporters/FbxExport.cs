using System.IO;
using Autodesk.Fbx;
using UnityEditor;
using UnityEngine;

class FbxExport
{
    
    // true: fbx file is easy-to-debug ascii, false: fbx file is binary.
    static bool saveFbxAsAscii = false;

    // The preferred axis system for the exported fbx file
    static FbxAxisSystem fbxAxisSystem = FbxAxisSystem.Max;
    // The preferred units of the exported fbx file
    static FbxSystemUnit fbxUnit = FbxSystemUnit.m;

    static string fbxFileTitle       = "Polyhydra";
    static string fbxFileSubject     = "";
    static string fbxFileComment     = "";
    static string fbxFileKeywords    = "";
    static string fbxFileAuthor      = "";
    static string fbxFileRevision    = "1.0";
    static string fbxFileApplication = "Unity FBX SDK";
    
    public static void ExportMesh(Mesh mesh, string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        // Make a temporary copy of the mesh to modify it
        Mesh tempMesh = Object.Instantiate(mesh);
        tempMesh.name = mesh.name;

        // If meters, divide by 100 since default is cm. Assume centered at origin.
        if (fbxUnit == FbxSystemUnit.m)
        {
            Vector3[] vertices = tempMesh.vertices;
            for (int i = 0; i < vertices.Length; ++i)
                vertices[i] /= 100.0f;
            tempMesh.vertices = vertices;
        }
        // You could handle other SystemUnits here

        // FBX Manager
        FbxManager manager = FbxManager.Create();
        manager.SetIOSettings(FbxIOSettings.Create(manager, Globals.IOSROOT));

        // FBX Exporter
        FbxExporter fbxExporter = FbxExporter.Create(manager, "Exporter");

        // Binary
        int fileFormat = -1;
        // Ascii
        if (saveFbxAsAscii)
            fileFormat = manager.GetIOPluginRegistry().FindWriterIDByDescription("FBX ascii (*.fbx)");

        fbxExporter.Initialize(filePath, fileFormat, manager.GetIOSettings());
        fbxExporter.SetFileExportVersion("FBX201400");

        // FBX Scene
        FbxScene fbxScene = FbxScene.Create(manager, "Scene");
        FbxDocumentInfo sceneInfo = FbxDocumentInfo.Create(manager, "SceneInfo");

        // Set up scene info
        sceneInfo.mTitle    = fbxFileTitle;
        sceneInfo.mSubject  = fbxFileSubject;
        sceneInfo.mComment  = fbxFileComment;
        sceneInfo.mAuthor   = fbxFileAuthor;
        sceneInfo.mRevision = fbxFileRevision;
        sceneInfo.mKeywords = fbxFileKeywords;
        sceneInfo.Original_ApplicationName.Set(fbxFileApplication);
        sceneInfo.LastSaved_ApplicationName.Set(fbxFileApplication);
        fbxScene.SetSceneInfo(sceneInfo);

        // Set up Global settings
        FbxGlobalSettings globalSettings = fbxScene.GetGlobalSettings();
        globalSettings.SetSystemUnit(fbxUnit);
        globalSettings.SetAxisSystem(fbxAxisSystem);

        FbxNode modelNode = FbxNode.Create(fbxScene, tempMesh.name);
        // Add mesh to a node in the scene
        // TODO Wat??? 
//        using (ModelExporter modelExporter = new ModelExporter())
//        {
//            if (!modelExporter.ExportMesh(tempMesh, modelNode))
//                Debug.LogError("Problem Exporting Mesh");
//        }
        // add the model to the scene
        fbxScene.GetRootNode().AddChild(modelNode);

        // Finally actually save the scene
        bool sceneSuccess = fbxExporter.Export(fbxScene);
        AssetDatabase.Refresh();

        // clean up temporary model
        if (Application.isPlaying)
            Object.Destroy(tempMesh);
        else
            Object.DestroyImmediate(tempMesh);
    }
}