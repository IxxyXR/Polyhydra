using TMPro;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextMeshPro))]
public class VoxelEditor : Editor
{


    // Called when you click any Transform in the scene
    void OnEnable ()
    {
        // remove a possibly existing listener
        SceneView.duringSceneGui -= OnScene;
        // add a listener
        SceneView.duringSceneGui += OnScene;
    }


    // In examples it is recommended to uninstall the delegate listener
    // But we need [CustomEditor(typeof(Transform))] and this means Unity will call our Script whenever a Transform
    // is getting selected. If we want to detect clicks anywhere (so that this listens to ALL OnScene calls) we simply
    // keep the listener installed. This won't overflow because we uninstall listeners before installing new ones.
    // But if your Editor ever crashes or throws errors this miiight be related. But I think it's pretty secure.
    // And if you are fine with clicking on Transforms only then uncomment the code in OnDisable below.
    // If you do, clicks in empty scene-areas are not detected.
    private void OnDisable()
    {
        // SceneView.duringSceneGui -= OnScene;
    }
    private static void OnScene(SceneView sceneview)
    {
        // uncomment if you want to make sure it runs
        // Debug.Log("OnScene !");
        if (Event.current.type == EventType.MouseDown)
        {
            // This kind of works, but results in wrong coordinates, maybe because of the Window offset or something.
            //Ray ray = sceneview.camera.ScreenPointToRay(Event.current.mousePosition);

            // This works fine as it gets you the correct coordinates
            Vector2 mousePosition = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var poly = GameObject.FindObjectOfType<PolyHydra>();
                Debug.Log(hit);
                Debug.Log(hit.point.x.ToString());

                // Draw a pointing upwards where we clicked. Just to make sure we SEE that the coordinates are correct.
                //Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.3f, Color.red, 0.1f);
                // var mesh = hit.transform.GetComponentInChildren<MeshFilter>().sharedMesh;
                // var hitTransform = hit.transform;
                // Vector3[] vertices = mesh.vertices;
                // int[] triangles = mesh.triangles;
                // Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
                // Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
                // Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
                // p0 = hitTransform.TransformPoint(p0);
                // p1 = hitTransform.TransformPoint(p1);
                // p2 = hitTransform.TransformPoint(p2);
                // Debug.DrawLine(p0, p1);
                // Debug.DrawLine(p1, p2);
                // Debug.DrawLine(p2, p0);
            }
        }
    }

}