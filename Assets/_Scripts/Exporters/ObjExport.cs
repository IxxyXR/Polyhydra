using UnityEngine;
//using UnityEditor;
using System.IO;
using System.Text;
 
public class ObjExport
{
	
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
			sb.Append("usemtl ").Append($"material{material}").Append("\n");
			//sb.Append("usemap ").Append($"material{material}").Append("\n");
 
			int[] triangles = m.GetTriangles(material);
			for (int i=0;i<triangles.Length;i+=3) {
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
					triangles[i]+1+StartIndex, triangles[i+1]+1+StartIndex, triangles[i+2]+1+StartIndex));
			}
		}
 
		StartIndex += numVertices;
		return sb.ToString();
	}
	private static int StartIndex;

	public static void ExportMesh(GameObject gameObject, string directoryPath, string fileName, bool makeSubmeshes=false)
	{
		var filePath = Path.Combine(directoryPath, fileName + ".obj");
		StringBuilder meshString = GenerateObjData(gameObject, fileName, makeSubmeshes);
		WriteToFile(meshString.ToString(), filePath);
		filePath = Path.Combine(directoryPath, fileName + ".mtl");
		StringBuilder mtlString = GenerateMtlData();
		WriteToFile(mtlString.ToString(), filePath);
		StartIndex = 0;
	}

	public static StringBuilder GenerateMtlData()
	{
		var colors = PolyHydra.DefaultFaceColors;
		StringBuilder mtlString = new StringBuilder();
		for (var i = 0; i < colors.Length; i++)
		{
			var color = colors[i];
			mtlString.Append($@"newmtl material{i}
Ka  0.0000  0.0000  0.0000
Kd  {color.r}  {color.g}  {color.b}
Ks  1.0000  1.0000  1.0000
illum 2
Ns 100

");
		}

		return mtlString;
	}

	public static StringBuilder GenerateObjData(GameObject gameObject, string filename="", bool makeSubmeshes=false)
	{
		Transform t = gameObject.transform;
		Vector3 originalPosition = t.position;
		t.position = Vector3.zero;
		string meshName = gameObject.name;
		if (filename == "") filename = meshName;
		StartIndex = 0;
		StringBuilder meshString = new StringBuilder();
		meshString.Append("#" + meshName + ".obj"
		                  + "\n#" + System.DateTime.Now.ToLongDateString()
		                  + "\n#" + System.DateTime.Now.ToLongTimeString()
		                  + "\n#-------"
		                  + "\n\n"
						  + "mtllib " + filename + ".mtl"
		                  + "\n\n");
		if (!makeSubmeshes)
		{
			meshString.Append("g ").Append(t.name).Append("\n");
		}

		meshString.Append(processTransform(t, makeSubmeshes));
		t.position = originalPosition;
		return meshString;
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