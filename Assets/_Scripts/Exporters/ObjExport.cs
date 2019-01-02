using UnityEngine;
//using UnityEditor;
using System.IO;
using System.Text;
 
public class ObjExport
{
	
	private static int StartIndex = 0;
 
	public static string MeshToString(MeshFilter mf, Transform t) 
	{	
		Quaternion r 	= t.localRotation;
 
 
		int numVertices = 0;
		Mesh m = mf.sharedMesh;
		if (!m)
		{
			return "####Error####";
		}
		Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
 
		StringBuilder sb = new StringBuilder();
 
		foreach(Vector3 vv in m.vertices)
		{
			Vector3 v = t.TransformPoint(vv);
			numVertices++;
			sb.Append(string.Format("v {0} {1} {2}\n",v.x,v.y,-v.z));
		}
		sb.Append("\n");
		foreach(Vector3 nn in m.normals) 
		{
			Vector3 v = r * nn;
			sb.Append(string.Format("vn {0} {1} {2}\n",-v.x,-v.y,v.z));
		}
		sb.Append("\n");
		foreach(Vector3 v in m.uv) 
		{
			sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
		}
		for (int material=0; material < m.subMeshCount; material ++) 
		{
			sb.Append("\n");
			sb.Append("usemtl ").Append(mats[material].name).Append("\n");
			sb.Append("usemap ").Append(mats[material].name).Append("\n");
 
			int[] triangles = m.GetTriangles(material);
			for (int i=0;i<triangles.Length;i+=3) {
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
					triangles[i]+1+StartIndex, triangles[i+1]+1+StartIndex, triangles[i+2]+1+StartIndex));
			}
		}
 
		StartIndex += numVertices;
		return sb.ToString();
	}

	public static void ExportMesh(GameObject gameObject, string directoryPath, string fileName, bool makeSubmeshes=false)
	{
		var filePath = Path.Combine(directoryPath, fileName + ".obj");
		
		string meshName = gameObject.name;
 
		StartIndex = 0;
 
		StringBuilder meshString = new StringBuilder();
 
		meshString.Append("#" + meshName + ".obj"
							+ "\n#" + System.DateTime.Now.ToLongDateString() 
							+ "\n#" + System.DateTime.Now.ToLongTimeString()
							+ "\n#-------" 
							+ "\n\n");
 
		Transform t = gameObject.transform;
 
		Vector3 originalPosition = t.position;
		t.position = Vector3.zero;
 
		if (!makeSubmeshes)
		{
			meshString.Append("g ").Append(t.name).Append("\n");
		}
		meshString.Append(processTransform(t, makeSubmeshes));
 
		WriteToFile(meshString.ToString(), filePath);
 
		t.position = originalPosition;
 
		StartIndex = 0;
		Debug.Log("Exported Mesh: " + fileName);
	}
 
	static string processTransform(Transform t, bool makeSubmeshes)
	{
		StringBuilder meshString = new StringBuilder();
 
		meshString.Append("#" + t.name
						+ "\n#-------" 
						+ "\n");
 
		if (makeSubmeshes)
		{
			meshString.Append("g ").Append(t.name).Append("\n");
		}
 
		MeshFilter mf = t.GetComponent<MeshFilter>();
		if (mf)
		{
			meshString.Append(MeshToString(mf, t));
		}
 
		for(int i = 0; i < t.childCount; i++)
		{
			meshString.Append(processTransform(t.GetChild(i), makeSubmeshes));
		}
 
		return meshString.ToString();
	}
 
	static void WriteToFile(string s, string filename)
	{
		using (StreamWriter sw = new StreamWriter(filename)) 
		{
			sw.Write(s);
		}
	}
}