using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Polyhedron : MonoBehaviour {

	[Range(1,80)]
	public int polyType = 1;
	public bool showDual = false; 
	
	private Mesh mesh;
	private int[] meshFaces;
	
	
	// NOTE: some of the int's can be replaced by short's, char's,
	// or even bit fields, at the expense of readability!!!
	
	int index;  // index to the standard list, the array uniform[] 
	
	int N;  // number of faces types (atmost 5)
	int M;  // vertex valency  (may be big for dihedral polyhedra) 
	int V;  // vertex count 
	int E;  // edge count 
	int F;  // face count 
	int D;  // density 
	int chi;  // Euler characteristic 
	int g;  // order of symmetry group
	
	private int K = 2;  // symmetry type: D=2, T=3, O=4, I=5
	
	private int even = -1;  // removed face in pqr|
	
	int hemi; // flag hemi polyhedron 
	int onesided; // flag onesided polyhedron 
	
	int[] Fi;  // face counts by type (array N)
	int[] rot;  // vertex configuration (array M of 0..N-1)
	int[] snub;  // snub triangle configuration (array M of 0..1) 
	int[] firstrot;  // temporary for vertex generation (array V)
	int[] anti;  // temporary for direction of ideal vertices (array E)
	int[] ftype;  // face types (array F)
	
	int[,] e;  // edges (matrix 2 x E of 0..V-1)
	int[,] dual_e;  // dual edges (matrix 2 x E of 0..F-1)
	int[,] incid;  // vertex-face incidence (matrix M x V of 0..F-1)
	int[,] adj;  // vertex-vertex adjacency (matrix M x V of 0..V-1)
	
	double[] p = new double[4];  // p, q and r; |=0 
	double minr;  // smallest nonzero inradius 
	double gon;  // basis type for dihedral polyhedra 
	double[] n;  // number of side of a face of each type (array N)
	double[] m;  // number of faces at a vertex of each type (array N)
	double[] gamma;  // fundamental angles in radians (array N)
	public string polyform;  // printable Wythoff symbol
	string config;  // printable vertex configuration 
	string polyname;  // name, standard or manifuctured 
	string dual_name;  // dual name, standard or manifuctured 
	
	Vector[] v;  // vertex coordinates (array V) 
	Vector[] f;  // face coordinates (array F)

	Color[] pallette;
	int currentType;
	int[] blacklist;
	
	Vector3 FindCenterPoint(Vector3[] points) {
		if (points.Length == 0) {return Vector3.zero;}
		if (points.Length == 1) {return points[0];}
		var bounds = new Bounds(points[0], Vector3.zero);
		for (var i = 1; i < points.Length; i++)
			bounds.Encapsulate(points[i]); 
		return bounds.center;
	}

	void Start() {

		blacklist = new int[] {13, 14, 17, 20, 23, 27, 28};
		
		pallette = new Color[] {
			Color.red,
			Color.yellow,
			Color.green,
			Color.cyan,
			Color.blue,
			Color.magenta
		};
		
		index = 1;
		currentType = 1;
		
		MakePoly();
	}

	void Update() {
		
		if (Input.GetKeyDown("space")) {
			
			currentType++;
			currentType = currentType % 81;
			index = -1;
			
			K = 2;
			even = -1;
			Fi = null;
			rot = null;
			snub = null; 
			firstrot = null;
			anti = null;
			ftype = null;
			e = null;
			dual_e = null;
			incid = null;
			adj = null;
			n = null;
			m = null;
			gamma = null; 
			v = null; 
			f = null;
			Debug.Log("-------------------------");
			if (!blacklist.Contains(currentType)) {
				Debug.Log(currentType);
				Debug.Log("-------------------------");
				MakePoly();
			}
			else {
				Debug.Log("Skipping " + currentType);
			}
			Debug.Log("-------------------------");
		}
	}
	
	void MakePoly() {
		try {
			kaleido(currentType);
			BuildMesh();
			//Debug.Log(polyname);
		}
		catch (Exception e) {
			Debug.Log("Error on " + currentType + " : " + e.Message);
		}
	}

	void BuildMesh() {
		
		//kaleido(uniform[type].Wythoff);
		
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		
//		Debug.Log("index: " + index);  // index to the standard list, the array uniform[] 
//	
//		Debug.Log("N: " + N);  // number of faces types (atmost 5)
//		Debug.Log("M: " + M);  // vertex valency  (may be big for dihedral polyhedra) 
//		Debug.Log("V: " + V);  // vertex count 
//		Debug.Log("E: " + E);  // edge count 
//		Debug.Log("F: " + F);  // face count 
//		Debug.Log("D: " + D);  // density 
//		Debug.Log("chi: " + chi);  // Euler characteristic 
//		Debug.Log("g: " + g);  // order of symmetry group 
//		Debug.Log("K: " + K);  // symmetry type: D=2, T=3, O=4, I=5 
//		Debug.Log("hemi: " + hemi); // flag hemi polyhedron 
//		Debug.Log("onesided: " + onesided); // flag onesided polyhedron
//		Debug.Log("even: " + even);  // removed face in pqr|
//		Debug.Log("Fi: " + Fi);  // face counts by type (array N)
//		Debug.Log("rot: " + rot);  // vertex configuration (array M of 0..N-1) 
//		Debug.Log("snub: " + snub);  // snub triangle configuration (array M of 0..1) 
//		Debug.Log("firstrot: " + firstrot);  // temporary for vertex generation (array V) 
//		Debug.Log("anti: " + anti);  // temporary for direction of ideal vertices (array E) 
//		Debug.Log("ftype: " + ftype);  // face types (array F)

//		Debug.Log("e: " + e);  // edges (matrix 2 x E of 0..V-1)

//		var meshVertices = new Vector3[V + F];
//		
//		for (int j = 0; j < V; j++) {meshVertices[j] = v[j].getVector3();}
//		for (int j = 0; j < F; j++) {
//			if (f[j] != null) {
//				meshVertices[V + j] = f[j].getVector3();
//			}
//		}
		
		var faceVertices = new List<List<int>>();
		var faceEdges = new List<List<int>>();
		for (int faceNum = 0; faceNum < F; faceNum++) {
			faceVertices.Add(new List<int>());
			faceEdges.Add(new List<int>());
		}
		for (int vertexNum = 0; vertexNum < V; vertexNum++) { 
			for (int i = 0; i < M; i++) {
				int faceNum = incid[i, vertexNum];
				faceVertices[faceNum].Add(vertexNum);
				for (int edgeNum = 0; edgeNum < E; edgeNum++) {
					if (vertexNum==e[0, edgeNum] || vertexNum==e[1, edgeNum]) {
						faceEdges[faceNum].Add(edgeNum);
					}
				}
			}
		}
		
		var meshVertices = new List<Vector3>();
		var meshTriangles = new List<int>();
		var meshColors = new List<Color>();
		int vIndex = 0;
		for (int i = 0; i < faceEdges.Count; i++) {

			Color faceColor = pallette[ftype[i]];
			
			for (int j = 0; j < faceEdges[i].Count; j++) {
				var points = new List<Vector3>();
				foreach (var vertex in faceVertices[i]) {
					points.Add(v[vertex].getVector3());
				}
				var centrePoint = FindCenterPoint(points.ToArray());
				int edgeNum = faceEdges[i][j];

				//meshVertices.Add(f[i].getVector3());

				// Normal direction
				meshVertices.Add(centrePoint);
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				meshVertices.Add(v[e[0, edgeNum]].getVector3());
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				meshVertices.Add(v[e[1, edgeNum]].getVector3());
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				
				// Reverse direction
				meshVertices.Add(centrePoint);
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				meshVertices.Add(v[e[1, edgeNum]].getVector3());
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				meshVertices.Add(v[e[0, edgeNum]].getVector3());
				meshColors.Add(faceColor);
				meshTriangles.Add(vIndex);
				vIndex++;
				
			}
		}
		
		mesh.vertices = meshVertices.ToArray();
		mesh.triangles = meshTriangles.ToArray();
		mesh.colors = meshColors.ToArray();
		mesh.RecalculateNormals();
		mesh.name = polyname;		
		
//		Debug.Log("dual_e: " + dual_e);  // dual edges (matrix 2 x E of 0..F-1)
//		Debug.Log("incid: " + incid);  // vertex-face incidence (matrix M x V of 0..F-1)
//		for (int i = 0; i < V; i++) {
//			string row = "incid" + i + ": ";
//			for (int j = 0; j < M; j++) {
//				row += "" + incid[j, i] + ",";
//			}
//			Debug.Log(row);
//		}
		
//		Debug.Log("adj: " + adj);  // vertex-vertex adjacency (matrix M x V of 0..V-1)
//		for (int i = 0; i < V; i++) {
//			string row = "adj " + i + ": ";
//			for (int j = 0; j < M; j++) {
//				row += "" + adj[j, i] + ",";
//			}
//			Debug.Log(row);
//		}
		
//		Debug.Log("minr: " + minr);  // smallest nonzero inradius 
//		Debug.Log("gon: " + gon);  // basis type for dihedral polyhedra 
//		Debug.Log("n: " + n);  // number of side of a face of each type (array N) 
//		Debug.Log("m: " + m);  // number of faces at a vertex of each type (array N)
//		Debug.Log("gamma: " + gamma);  // fundamental angles in radians (array N)
//		Debug.Log("polyform: " + polyform);  // printable Wythoff symbol
//		Debug.Log("config: " + config);  // printable vertex configuration 
//		Debug.Log("polyname: " + polyname);  // name, standard or manifuctured 
//		Debug.Log("dual_name: " + dual_name);  // dual name, standard or manifuctured
//		Debug.Log("v: " + v);  // vertex coordinates (array V)
//		Debug.Log("f: " + f);  // face coordinates (array F)

	}
	
	private Color GizmoColor = Color.white;
	private float GizmoRadius = .03f;

	void OnDrawGizmos () {
		
		Gizmos.color = GizmoColor;
		var transform = this.transform;

//		if (f != null) {
//			foreach (var face in f) {
//				if (face != null) {
//					Gizmos.DrawWireSphere(transform.TransformPoint(face.getVector3()), GizmoRadius);
//				}
//			}
//		}
		if (v != null) {
			foreach (var vert in v) {
				Gizmos.DrawWireSphere(transform.TransformPoint(vert.getVector3()), GizmoRadius);
			}
		}
		if (e != null) {
			for (int i = 0; i < E; i++) {
				if (showDual) {
					Gizmos.DrawLine(
						transform.TransformPoint(f[dual_e[0, i]].getVector3()),
						transform.TransformPoint(f[dual_e[1, i]].getVector3())
					);
				} else {
					Gizmos.DrawLine(
						transform.TransformPoint(v[e[0, i]].getVector3()),
						transform.TransformPoint(v[e[1, i]].getVector3())
					);
				}
			}
		}
	}
	
	/*
	 *****************************************************************************
	 *	List of Uniform Polyhedra and Their Kaleidoscopic Formulae
	 *	==========================================================
	 *
	 *	Each entry contains the following items:
	 *
	 *	1)	Wythoff symbol.
	 *	2)	Polyhedron name.
	 *	3)	Dual name.
	 *	4)	Coxeter &al. reference figure.
	 *	5)	Wenninger reference figure.
	 *
	 *	Notes:
	 *
	 *	(1)	Cundy&Roulette's trapezohedron has been renamed to
	 *		deltohedron, as its faces are deltoids, not trapezoids.
	 *	(2)	The names of the non-dihedral polyhedra are those
	 *		which appear in Wenninger (1984). Some of them are
	 *		slightly modified versions of those in Wenninger (1971).
	 *
	 *	References:
	 *
	 *	Coxeter, H.S.M., Longuet-Higgins, M.S. & Miller, J.C.P.,
	 *		Uniform polyhedra, Phil. Trans. Royal Soc. London, Ser. A,
	 *		246 (1953), 401-409.
	 *	Cundy, H.M. & Rollett, A.P.,
	 *		"Mathematical Models", 3rd Ed., Tarquin, 1981.
	 *	Har'El, Z.
	 *		Unifom solution for uniform polyhedra, Geometriae Dedicata,
	 *		47 (1993), 57-110.
	 *	Wenninger, M.J.,
	 *		"Polyhedron Models", Cambridge University Press, 1971.
	 *		"Dual Models", Cambridge University Press, 1984.
	 *
	 *****************************************************************************
	 */
	
	public class UniformImpl {
		
		public string Wythoff, name, dual;
		public int Coxeter, Wenninger;
		
		public UniformImpl(string wythoff, string name, string dual, int coxeter, int wenninger) {
			Wythoff = wythoff;
			this.name = name;
			this.dual = dual;
			Coxeter = coxeter;
			Wenninger = wenninger;
		}		
	}

	public UniformImpl[] uniform = {
			
		new UniformImpl("", "", "", 0, 0),
		
		// Dihedral Schwarz Triangles (D5 only)

		// (2 2 5) (D1/5) 
	
		new UniformImpl(  // 1
			"2 5|2",
			"pentagonal prism",
			"pentagonal dipyramid",
			0,0
		),
		new UniformImpl(  // 2
			"|2 2 5",
			"pentagonal antiprism",
			"pentagonal deltohedron",
			0,0
		),
	
		// (2 2 5/2) (D2/5) 
	
		new UniformImpl(  // 3
			"2 5/2|2",
			"pentagrammic prism",
			"pentagrammic dipyramid",
			0,0
		),
		new UniformImpl(  // 4
			"|2 2 5/2",
			"pentagrammic antiprism",
			"pentagrammic deltohedron",
			0,0
		),
	
		// (5/3 2 2) (D3/5) 
	
		new UniformImpl(  // 5
			"|2 2 5/3",
			"pentagrammic crossed antiprism",
			"pentagrammic concave deltohedron",
			0,0
		),
	
		// Tetrahedral Schwarz Triangles
	
		// (2 3 3) (T1) 
	
		new UniformImpl(  // 6
			"3|2 3",
			"tetrahedron",
			"tetrahedron",
			15,1
		),
		new UniformImpl(  // 7
			"2 3|3",
			"truncated tetrahedron",
			"triakistetrahedron",
			16,6
		),
	
		// (3/2 3 3) (T2) 
	
		new UniformImpl(  // 8
			"3/2 3|3",
			"octahemioctahedron",
			"octahemioctacron",
			37,68
		),
	
		// (3/2 2 3) (T3) 
	
		new UniformImpl(  // 9
			"3/2 3|2",
			"tetrahemihexahedron",
			"tetrahemihexacron",
			36,67
		),
	
		// Octahedral Schwarz Triangles
	
		// (2 3 4) (O1) 
	
		new UniformImpl(  // 10
			"4|2 3",
			"octahedron",
			"cube",
			17,2
		),
		new UniformImpl(  // 11
			"3|2 4",
			"cube",
			"octahedron",
			18,3
		),
		new UniformImpl(  // 12
			"2|3 4",
			"cuboctahedron",
			"rhombic dodecahedron",
			19,11
		),
		new UniformImpl(  // 13
			"2 4|3",
			"truncated octahedron",
			"tetrakishexahedron",
			20,7
		),
		new UniformImpl(  // 14
			"2 3|4",
			"truncated cube",
			"triakisoctahedron",
			21,8
		),
		new UniformImpl(  // 15
			"3 4|2",
			"rhombicuboctahedron",
			"deltoidal icositetrahedron",
			22,13
		),
		new UniformImpl(  // 16
			"2 3 4|",
			"truncated cuboctahedron",
			"disdyakisdodecahedron",
			23,15
		),
		new UniformImpl(  // 17
			"|2 3 4",
			"snub cube",
			"pentagonal icositetrahedron",
			24,17
		),
	
		// (3/2 4 4) (O2b) 
	
		new UniformImpl(  // 18
			"3/2 4|4",
			"small cubicuboctahedron",
			"small hexacronic icositetrahedron",
			38,69
		),
	
		// (4/3 3 4) (O4) 
	
		new UniformImpl(  // 19
			"3 4|4/3",
			"great cubicuboctahedron",
			"great hexacronic icositetrahedron",
			50,77
		),
		new UniformImpl(  // 20
			"4/3 4|3",
			"cubohemioctahedron",
			"hexahemioctacron",
			51,78
		),
		new UniformImpl(  // 21
			"4/3 3 4|",
			"cubitruncated cuboctahedron",
			"tetradyakishexahedron",
			52,79
		),
	
		// (3/2 2 4) (O5) 
	
		new UniformImpl(  // 22
			"3/2 4|2",
			"great rhombicuboctahedron",
			"great deltoidal icositetrahedron",
			59,85
		),
		new UniformImpl(  // 23
			"3/2 2 4|",
			"small rhombihexahedron",
			"small rhombihexacron",
			60,86
		),
	
		// (4/3 2 3) (O7) 
	
		new UniformImpl(  // 24
			"2 3|4/3",
			"stellated truncated hexahedron",
			"great triakisoctahedron",
			66,92
		),
		new UniformImpl(  // 25
			"4/3 2 3|",
			"great truncated cuboctahedron",
			"great disdyakisdodecahedron",
			67,93
		),
	
		// (4/3 3/2 2) (O11) 
	
		new UniformImpl(  // 26
			"4/3 3/2 2|",
			"great rhombihexahedron",
			"great rhombihexacron",
			82,103
		),
	
		// Icosahedral Schwarz Triangles
	
		// (2 3 5) (I1) 
	
		new UniformImpl(  // 27
			"5|2 3",
			"icosahedron",
			"dodecahedron",
			25,4
		),
		new UniformImpl(  // 28
			"3|2 5",
			"dodecahedron",
			"icosahedron",
			26,5
		),
		new UniformImpl(  // 29
			"2|3 5",
			"icosidodecahedron",
			"rhombic triacontahedron",
			28,12
		),
		new UniformImpl(  // 30
			"2 5|3",
			"truncated icosahedron",
			"pentakisdodecahedron",
			27,9
		),
		new UniformImpl(  // 31
			"2 3|5",
			"truncated dodecahedron",
			"triakisicosahedron",
			29,10
		),
		new UniformImpl(  // 32
			"3 5|2",
			"rhombicosidodecahedron",
			"deltoidal hexecontahedron",
			30,14
		),
		new UniformImpl(  // 33
			"2 3 5|",
			"truncated icosidodechedon",
			"disdyakistriacontahedron",
			31,16
		),
		new UniformImpl(  // 34
			"|2 3 5",
			"snub dodecahedron",
			"pentagonal hexecontahedron",
			32,18
		),
	
		// (5/2 3 3) (I2a) 
	
		new UniformImpl(  // 35
			"3|5/2 3",
			"small ditrigonal icosidodecahedron",
			"small triambic icosahedron",
			39,70
		),
		new UniformImpl(  // 36
			"5/2 3|3",
			"small icosicosidodecahedron",
			"small icosacronic hexecontahedron",
			40,71
		),
		new UniformImpl(  // 37
			"|5/2 3 3",
			"small snub icosicosidodecahedron",
			"small hexagonal hexecontahedron",
			41,110
		),
	
		// (3/2 5 5) (I2b) 
	
		new UniformImpl(  // 38
			"3/2 5|5",
			"small dodecicosidodecahedron",
			"small dodecacronic hexecontahedron",
			42,72
		),
	
		// (2 5/2 5) (I3) 
	
		new UniformImpl(  // 39
			"5|2 5/2",
			"small stellated dodecahedron",
			"great dodecahedron",
			43,20
		),
		new UniformImpl(  // 40
			"5/2|2 5",
			"great dodecahedron",
			"small stellated dodecahedron",
			44,21
		),
		new UniformImpl(  // 41
			"2|5/2 5",
			"great dodecadodecahedron",
			"medial rhombic triacontahedron",
			45,73
		),
		new UniformImpl(  // 42
			"2 5/2|5",
			"truncated great dodecahedron",
			"small stellapentakisdodecahedron",
			47,75
		),
		new UniformImpl(  // 43
			"5/2 5|2",
			"rhombidodecadodecahedron",
			"medial deltoidal hexecontahedron",
			48,76
		),
		new UniformImpl(  // 44
			"2 5/2 5|",
			"small rhombidodecahedron",
			"small rhombidodecacron",
			46,74
		),
		new UniformImpl(  // 45
			"|2 5/2 5",
			"snub dodecadodecahedron",
			"medial pentagonal hexecontahedron",
			49,111
		),
	
		// (5/3 3 5) (I4) 
	
		new UniformImpl(  // 46
			"3|5/3 5",
			"ditrigonal dodecadodecahedron",
			"medial triambic icosahedron",
			53,80
		),
		new UniformImpl(  // 47
			"3 5|5/3",
			"great ditrigonal dodecicosidodecahedron",
			"great ditrigonal dodecacronic hexecontahedron",
			54,81
		),
		new UniformImpl(  // 48
			"5/3 3|5",
			"small ditrigonal dodecicosidodecahedron",
			"small ditrigonal dodecacronic hexecontahedron",
			55,82
		),
		new UniformImpl(  // 49
			"5/3 5|3",
			"icosidodecadodecahedron",
			"medial icosacronic hexecontahedron",
			56,83
		),
		new UniformImpl(  // 50
			"5/3 3 5|",
			"icositruncated dodecadodecahedron",
			"tridyakisicosahedron",
			57,84
		),
		new UniformImpl(  // 51
			"|5/3 3 5",
			"snub icosidodecadodecahedron",
			"medial hexagonal hexecontahedron",
			58,112
		),
	
		// (3/2 3 5) (I6b) 
	
		new UniformImpl(  // 52
			"3/2|3 5",
			"great ditrigonal icosidodecahedron",
			"great triambic icosahedron",
			61,87
		),
		new UniformImpl(  // 53
			"3/2 5|3",
			"great icosicosidodecahedron",
			"great icosacronic hexecontahedron",
			62,88
		),
		new UniformImpl(  // 54
			"3/2 3|5",
			"small icosihemidodecahedron",
			"small icosihemidodecacron",
			63,89
		),
		new UniformImpl(  // 55
			"3/2 3 5|",
			"small dodecicosahedron",
			"small dodecicosacron",
			64,90
		),
	
		// (5/4 5 5) (I6c) 
	
		new UniformImpl(  // 56
			"5/4 5|5",
			"small dodecahemidodecahedron",
			"small dodecahemidodecacron",
			65,91
		),
	
		// (2 5/2 3) (I7) 
	
		new UniformImpl(  // 57
			"3|2 5/2",
			"great stellated dodecahedron",
			"great icosahedron",
			68,22
		),
		new UniformImpl(  // 58
			"5/2|2 3",
			"great icosahedron",
			"great stellated dodecahedron",
			69,41
		),
		new UniformImpl(  // 59
			"2|5/2 3",
			"great icosidodecahedron",
			"great rhombic triacontahedron",
			70,94
		),
		new UniformImpl(  // 60
			"2 5/2|3",
			"great truncated icosahedron",
			"great stellapentakisdodecahedron",
			71,95
		),
		new UniformImpl(  // 61
			"2 5/2 3|",
			"rhombicosahedron",
			"rhombicosacron",
			72,96
		),
		new UniformImpl(  // 62
			"|2 5/2 3",
			"great snub icosidodecahedron",
			"great pentagonal hexecontahedron",
			73,113
		),
	
		// (5/3 2 5) (I9) 
	
		new UniformImpl(  // 63
			"2 5|5/3",
			"small stellated truncated dodecahedron",
			"great pentakisdodekahedron",
			74,97
		),
		new UniformImpl(  // 64
			"5/3 2 5|",
			"truncated dodecadodecahedron",
			"medial disdyakistriacontahedron",
			75,98
		),
		new UniformImpl(  // 65
			"|5/3 2 5",
			"inverted snub dodecadodecahedron",
			"medial inverted pentagonal hexecontahedron",
			76,114
		),
	
		// (5/3 5/2 3) (I10a) 
	
		new UniformImpl(  // 66
			"5/2 3|5/3",
			"great dodecicosidodecahedron",
			"great dodecacronic hexecontahedron",
			77,99
		),
		new UniformImpl(  // 67
			"5/3 5/2|3",
			"small dodecahemicosahedron",
			"small dodecahemicosacron",
			78,100
		),
		new UniformImpl(  // 68
			"5/3 5/2 3|",
			"great dodecicosahedron",
			"great dodecicosacron",
			79,101
		),
		new UniformImpl(  // 69
			"|5/3 5/2 3",
			"great snub dodecicosidodecahedron",
			"great hexagonal hexecontahedron",
			80,115
		),
	
		// (5/4 3 5) (I10b) 
	
		new UniformImpl(  // 70
			"5/4 5|3",
			"great dodecahemicosahedron",
			"great dodecahemicosacron",
			81,102
		),
	
		// (5/3 2 3) (I13) 
	
		new UniformImpl(  // 71
			"2 3|5/3",
			"great stellated truncated dodecahedron",
			"great triakisicosahedron",
			83,104
		),
		new UniformImpl(  // 72
			"5/3 3|2",
			"great rhombicosidodecahedron",
			"great deltoidal hexecontahedron",
			84,105
		),
		new UniformImpl(  // 73
			"5/3 2 3|",
			"great truncated icosidodecahedron",
			"great disdyakistriacontahedron",
			87,108
		),
		new UniformImpl(  // 74
			"|5/3 2 3",
			"great inverted snub icosidodecahedron",
			"great inverted pentagonal hexecontahedron",
			88,116
		),
	
		// (5/3 5/3 5/2) (I18a) 
	
		new UniformImpl(  // 75
			"5/3 5/2|5/3",
			"great dodecahemidodecahedron",
			"great dodecahemidodecacron",
			86,107
		),
	
		// (3/2 5/3 3) (I18b) 
	
		new UniformImpl(  // 76
			"3/2 3|5/3",
			"great icosihemidodecahedron",
			"great icosihemidodecacron",
			85,106
		),
	
		// (3/2 3/2 5/3) (I22) 
	
		new UniformImpl(  // 77
			"|3/2 3/2 5/2",
			"small retrosnub icosicosidodecahedron",
			"small hexagrammic hexecontahedron",
			91,118
		),
	
		// (3/2 5/3 2) (I23) 
	
		new UniformImpl(  // 78
			"3/2 5/3 2|",
			"great rhombidodecahedron",
			"great rhombidodecacron",
			89,109
		),
		new UniformImpl(  // 79
			"|3/2 5/3 2",
			"great retrosnub icosidodecahedron",
			"great pentagrammic hexecontahedron",
			90,117
		),
	
		// Last But Not Least
		
	
		new UniformImpl(  // 80
			"3/2 5/3 3 5/2",
			"great dirhombicosidodecahedron",
			"great dirhombicosidodecacron"
			,92,119)
		
		};
	
	public static double DBL_EPSILON = 2.2204460492503131e-16;
	public static double BIG_EPSILON = 3e-2;
	public static double M_PI = 3.14159265358979323846;
	
	private class SymParser {
		
		string sym;
		int index;
		
		public SymParser(string sym) {
			this.sym = sym;
			this.index = 0;
		}

		public double getNextFraction() {

			char c = sym[index];
			while (c.ToString() == " ") {
				++index;
				c = sym[index];
			}

			if (sym[index].ToString() == "|") {
				index++;
				return 0;
			} else {
				c = sym[index++];
				if (Char.IsDigit(c)) {
					string number = "" + c;
					double a;
					try {
						c = sym[index];
						while (Char.IsDigit(c)) {
							number += c;
							++index;
							c = sym[index];
						}
					} catch (System.IndexOutOfRangeException e) {
						return Double.Parse(number);
					}
					a = Double.Parse(number);
					if (sym[index] == '/') {
						c = sym[++index];
						if (Char.IsDigit(c)) {
							number = "";
							try {
								while (Char.IsDigit(c = sym[index])) {
									number += c;
									++index;
								}
							} catch (System.IndexOutOfRangeException e) { }
							return a / Double.Parse(number);
						}
						else {throw new System.ApplicationException("No digit after \"/\": " + c);}
					}
					else {return a;}
				}
				else {throw new System.ApplicationException("\"" + c + "\" is not a digit");}
			}
		}
	}

	// Unpack input symbol: Wythoff symbol or an index to uniform[]. The symbol is
	// a # followed by a number, or a three fractions and a bar in some order. We
	// allow no bars only if it result from the input symbol #80
	
	private void unpacksym(string sym) {
		
	    sym = sym.Trim();
		
	    if (!String.IsNullOrEmpty(sym)) {
	    	if (sym.StartsWith("#")) {  // take the number of polyhedron
	    		try {
	    			index = Int32.Parse(sym.Substring(1));
	    		} catch (System.FormatException e) {
	    			throw new System.ApplicationException("Illegal number: " + e.Message);
	    		}
	    		sym = uniform[index].Wythoff;
	    	}
	    }
	    SymParser sp = new SymParser(sym);
	    p[0] = sp.getNextFraction();
	    p[1] = sp.getNextFraction();
	    p[2] = sp.getNextFraction();
	    p[3] = sp.getNextFraction();
	}

	private string sprintfrac(double x) {
	    string s = "";
	    Fraction frax = new Fraction().frac(x);
	    if (frax.getD() == 0) {s += "infinity";}
	    else if (frax.getD() == 1) {s+= frax.getN();}
	    else {s+= frax.getN() + "/" + frax.getD();}
	    return s;
	}
	
	// Using Wythoff symbol (p|qr, pq|r, pqr| or |pqr), find the Moebius triangle
	// (2 3 K) (or (2 2 n)) of the Schwarz triangle (pqr), the order g of its
	// symmetry group, its Euler characteristic chi, and its covering density D.
	// g is the number of copies of (2 3 K) covering the sphere, i.e.,
	//
	//		g * pi * (1/2 + 1/3 + 1/K - 1) = 4 * pi
	//
	// D is the number of times g copies of (pqr) cover the sphere, i.e.
	//
	//		D * 4 * pi = g * pi * (1/p + 1/q + 1/r - 1)
	//
	// chi is V - E + F, where F = g is the number of triangles, E = 3*g/2 is the
	// number of triangle edges, and V = Vp+ Vq+ Vr, with Vp = g/(2*np) being the
	// number of vertices with angle pi/p (np is the numerator of p)
	
	private void moebius() {
		
	    int twos = 0, j;
		
		// Arrange Wythoff symbol in a presentable form. In the same time check the
		// restrictions on the three fractions: They all have to be greater then one,
		// and the numerators 4 or 5 cannot occur together.  We count the ocurrences of
		// 2 in `two', and save the largest numerator in `P->K', since they reflect on
		// the symmetry group
		
	    K = 2;
	    if (index == 80) {polyform = "|";} else {polyform = "";}
		
	    for (j = 0; j < 4; j++) {
			if (p[j] > 0) {
			    string s = sprintfrac(p[j]);
			    if (j > 0 && p[j-1] > 0) {
			    	polyform += " " + s;
			    } else {polyform += s;}
			    if (p[j] != 2) {
					int k;
					if ((k = (int)Fraction.numerator(p[j])) > K) {
					    if (K == 4) {break;}
					    K = k;
					} else if (k < K && k == 4) {break;}
			    } else {twos++;}
			} else  {polyform += "|";}
		}

		// Find the symmetry group P->K (where 2, 3, 4, 5 represent the dihedral,
		// tetrahedral, octahedral and icosahedral groups, respectively), and its order
		// P->g
	    if (twos >= 2) {  // dihedral
			g = 4 * K;
			K = 2;
	    } else {
			if (K > 5) {throw new System.ApplicationException("numerator too large");}
			g = 24 * K / (6 - K);
	    }
	
		// Compute the nominal density P->D and Euler characteristic P->chi.
		// In few exceptional cases, these values will be modified later
	
	    if (index != 80) {
			int i;
			D = chi = -g;
			for (j = 0; j < 4; j++) {
				if (p[j] > 0) {
				    chi += i = (int)(g / Fraction.numerator(p[j]));
				    D += i * (int)Fraction.denominator(p[j]);
				}
			}
			chi /= 2;
			D /= 4;
		    if (D <= 0) {throw new System.ApplicationException("nonpositive density");}
	    }
	}
	
	// Decompose Schwarz triangle into N right triangles and compute the vertex
	// count V and the vertex valency M.  V is computed from the number g of
	// Schwarz triangles in the cover, divided by the number of triangles which
	// share a vertex. It is halved for one-sided polyhedra, because the
	// kaleidoscopic construction really produces a double orientable covering of
	// such polyhedra. All q' q|r are of the "hemi" type, i.e. have equatorial {2r}
	// faces, and therefore are (except 3/2 3|3 and the dihedra 2 2|r) one-sided. A
	// well known example is 3/2 3|4, the "one-sided heptahedron". Also, all p q r|
	// with one even denominator have a crossed parallelogram as a vertex figure,
	// and thus are one-sided as well
	
	private void decompose() {
		
	    int j, J, s, t;
	    if (p[1] == 0) { /* p|q r */
			N = 2;
			M = 2 * (int)Fraction.numerator(p[0]);
			V = g / M;
			n = new double[N];
			m = new double[N];
			rot = new int[M];
			s = 0;
			for (j = 0; j < 2; j++) {
			    n[j] = p[j+2];
			    m[j] = p[0];
			}
			for (j = M / 2; j > 0; j--) {
			    rot[s++] = 0;
			    rot[s++] = 1;
			}
	    } else if (p[2] == 0) {  // p q|r
			N = 3;
			M = 4;
			V = g / 2;
			n = new double[N];
			m = new double[N];
			rot = new int[M];
			s = 0;  // rot;
			n[0] = 2 * p[3];
			m[0] = 2;
			for (j = 1; j < 3; j++) {
			    n[j] = p[j-1];
			    m[j] = 1;
			    rot[s++] = 0;
			    rot[s++] = j;
			}
			if (Math.Abs(p[0] - Fraction.compl(p[1])) < DBL_EPSILON) {  // p = q'
				// P->p[0]==compl(P->p[1]) should work.  However, MSDOS
				// yeilds a 7e-17 difference! Reported by Jim Buddenhagen
				// <jb1556@daditz.sbc.com> */
			    hemi = 1;
			    D = 0;
			    if (p[0] != 2 && !(p[3] == 3 && (p[0] == 3 || p[1] == 3))) {
					onesided = 1;
					V /= 2;
					chi /= 2;
			    }
			}
	    } else if (p[3] == 0) {  // p q r|
			M = N = 3;
			V = g;
			n = new double[N];
			m = new double[N];
			rot = new int[M];
			s = 0;  // rot;
			for (j = 0; j < 3; j++) {
			    if ((Fraction.denominator(p[j]) % 2) == 0) {
					// what happens if there is more then one even denominator?
					if (p[(j+1)%3] != p[(j+2)%3]) {  // needs postprocessing
					    even = j;/* memorize the removed face */
					    chi -= g / (int)Fraction.numerator(p[j]) / 2;
					    onesided = 1;
					    D = 0;
					} else {
						// for p = q we get a double 2 2r|p
						// noted by Roman Maeder <maeder@inf.ethz.ch> for 4 4 3/2|
						// Euler characteristic is still wrong
					    D /= 2;
					}
					V /= 2;
			    }
			    n[j] = 2 * p[j];
			    m[j] = 1;
			    rot[s++] = j;
			}
	    } else {  // |p q r - snub polyhedron
			N = 4;
			M = 6;
			V = g / 2;/* Only "white" triangles carry a vertex */
			n = new double[N];
			m = new double[N];
			rot = new int[M];
			snub = new int[M];
			s = 0; //rot;
			t = 0; //snub;
			m[0] = n[0] = 3;
			for (j = 1; j < 4; j++) {
			    n[j] = p[j];
			    m[j] = 1;
			    rot[s++] = 0;
			    rot[s++] = j;
			    snub[t++] = 1;
			    snub[t++] = 0;
			}
	    }
		
		// Sort the fundamental triangles (using bubble sort) according to decreasing
		// n[i], while pushing the trivial triangles (n[i] = 2) to the end
	
	    J = N - 1;
	    while (J != 0) {
			int last;
			last = J;
			J = 0;
			for (j = 0; j < last; j++) {
			    if ((n[j] < n[j+1] || n[j] == 2) && n[j+1] != 2) {
					int i;
					double temp;
					temp = n[j];
					n[j] = n[j+1];
					n[j+1] = temp;
					temp = m[j];
					m[j] = m[j+1];
					m[j+1] = temp;
					for (i = 0; i < M; i++) {
					    if (rot[i] == j) {rot[i] = j+1;}
					    else if (rot[i] == j+1) {rot[i] = j;}
					}
					if (even != -1) {
					    if (even == j) {even = j+1;}
					    else if (even == j+1) {even = j;}
					}
					J = j;
			    }
			}
	    }
		// Get rid of repeated triangles
	    for (J = 0; J < N && n[J] != 2;J++) {
			int k, i;
			for (j = J+1; j < N && n[j] == n[J]; j++) {
			    m[J] += m[j];
			}
			k = j - J - 1;
			if (k != 0) {
			    for (i = j; i < N; i++) {
					n[i - k] = n[i];
					m[i - k] = m[i];
			    }
			    N -= k;
			    for (i = 0; i < M; i++) {
					if (rot[i] >= j)
					    rot[i] -= k;
					else if (rot[i] > J) {
					    rot[i] = J;
					}
			    }
			    if (even >= j) {even -= k;}
			}
	    }
		
		// Get rid of trivial triangles
	    if (J == 0) {J = 1;}  // hosohedron
	    if (J < N) {
			int i;
			N = J;
			for (i = 0; i < M; i++) {
			    if (rot[i] >= N) {
					for (j = i + 1; j < M; j++) {
					    rot[j-1] = rot[j];
					    if (snub != null && snub.Length > j) {
					    	snub[j-1] = snub[j];
					    }
					}
					M--;
			    }
			}
	    }
		// Truncate arrays
	    Array.Resize(ref n, N);
	    Array.Resize(ref m, N);
	    Array.Resize(ref rot, M);
	    if (snub != null) {Array.Resize(ref snub, M);}
		
	}

	private void dihedral(string polyname, string dual_name) {
	    string s = sprintfrac(gon < 2 ? Fraction.compl(gon) : gon);
		polyname = s + "-gonal " + polyname;
	    dual_name = s + "-gonal " + dual_name;
	}
	
	// Get the polyhedron name, using standard list or guesswork. Ideally, we
	// should try to locate the Wythoff symbol in the standard list (unless, of
	// course, it is dihedral), after doing few normalizations, such as sorting
	// angles and splitting isoceles triangles.
	
	private void guessname() {
	    if (index != -1) {  // tabulated
		    polyname = uniform[index].name;
			dual_name = uniform[index].dual;
	    } else if (K == 2) {  // dihedral nontabulated
			if (p[0] == 0) {
			    if (N == 1) {
				    polyname = "octahedron";
					dual_name = "cube";
			    } else {
				    gon = n[0] == 3 ? n[1] : n[0];
				    if (gon >= 2) {dihedral("antiprism", "deltohedron");}
				    else {dihedral("crossed antiprism", "concave deltohedron");}
			    }
			} else if (p[3] == 0 || p[2] == 0 && p[3] == 2) {
			    if (N == 1) {
				    polyname = "cube";
					dual_name = "octahedron";
			    }
			    gon = n[0] == 4 ? n[1] : n[0];
			    dihedral("prism", "dipyramid");
			} else if (p[1] == 0 && p[0] != 2) {
			    gon = m[0];
			    dihedral("hosohedron", "dihedron");
			} else {
			    gon = n[0];
			    dihedral("dihedron", "hosohedron");
			}
	    } else {  // other nontabulated
		    
			string[] pre = new string[] {"tetr", "oct", "icos"};
		    polyname = pre[K - 3] + "ahedral ";
		    
			if (onesided != 0) {polyname += "one-sided ";}
			else if (D == 1) {polyname += "convex ";}
			else {polyname += "nonconvex ";}
		    
			dual_name = polyname;
		    polyname += "isogonal polyhedron";
			dual_name += "isohedral polyhedron";
	    }
	}
	
	// Solve the fundamental right spherical triangles
	
	private void newton() {
		
		// First, we find initial approximations.
	    int j;
	    double cosa;
	    gamma = new double[N];
	    if (N == 1) {gamma[0] = M_PI / m[0];}
	    for (j = 0; j < N; j++)
		gamma[j] = M_PI / 2 - M_PI / n[j];
		
		// Next, iteratively find closer approximations for gamma[0] and compute
		// other gamma[j]'s from Napier's equations
		
	    for (;;) {
			double delta = M_PI, sigma = 0;
			for (j = 0; j < N; j++) {
			    delta -= m[j] * gamma[j];
			}
			if (Math.Abs(delta) < 11 * DBL_EPSILON) {return;}
			// On a RS/6000, fabs(delta)/DBL_EPSILON may occilate between 8 and 10
			// Reported by David W. Sanderson <dws@ssec.wisc.edu>
		    for (j = 0; j < N; j++) {
			    sigma += m[j] * Math.Tan(gamma[j]);
		    }
		    gamma[0] += delta * Math.Tan(gamma[0]) / sigma;
			if (gamma[0] < 0 || gamma[0] > M_PI) {
			    throw new System.ApplicationException("gamma out of bounds");
			}
			cosa = Math.Cos(M_PI / n[0]) / Math.Sin(gamma[0]);
		    for (j = 1; j < N; j++) {
			    gamma[j] = Math.Asin(Math.Cos(M_PI / n[j]) / cosa);
		    }
	    }
	}

	// Postprocess pqr| where r has an even denominator (cf. Coxeter &al. Sec.9)
	// Remove the {2r} and add a retrograde {2p} and retrograde {2q}
	
	private void exceptions() {
		
	    int j;
	    if (even != -1) {
			M = N = 4;
			Array.Resize(ref n, N);
			Array.Resize(ref m, N);
			Array.Resize(ref gamma, N);
			Array.Resize(ref rot, M);
			for (j = even + 1; j < 3; j++) {
			    n[j-1] = n[j];
			    gamma[j-1] = gamma[j];
			}
			n[2] = Fraction.compl(n[1]);
			gamma[2] = - gamma[1];
			n[3] = Fraction.compl(n[0]);
			m[3] = 1;
			gamma[3] = - gamma[0];
			rot[0] = 0;
			rot[1] = 1;
			rot[2] = 3;
			rot[3] = 2;
	    }

		// Postprocess the last polyhedron |3/2 5/3 3 5/2 by taking a |5/3 3 5/2,
		// replacing the three snub triangles by four equatorial squares and adding
		// the missing {3/2} (retrograde triangle, cf. Coxeter &al. Sec. 11)
		
	    if (index == 80) {
			N = 5;
			M = 8;
			Array.Resize(ref n, N);
			Array.Resize(ref m, N);
			Array.Resize(ref gamma, N);
			Array.Resize(ref rot, M);
			Array.Resize(ref snub, M);
			hemi = 1;
			D = 0;
			for (j = 3; j != 0; j--) {
			    m[j] = 1;
			    n[j] = n[j-1];
			    gamma[j] = gamma[j-1];
			}
			m[0] = n[0] = 4;
			gamma[0] = M_PI / 2;
			m[4] = 1;
			n[4] = Fraction.compl(n[1]);
			gamma[4] = - gamma[1];
			for (j = 1; j < 6; j += 2) {rot[j]++;}
			rot[6] = 0;
			rot[7] = 4;
			snub[6] = 1;
			snub[7] = 0;
	    }
	}
	
	// Compute edge and face counts, and update D and chi.  Update D in the few
	// cases the density of the polyhedron is meaningful but different than the
	// density of the corresponding Schwarz triangle (cf. Coxeter &al., p. 418 and
	// p. 425).
	// In these cases, spherical faces of one type are concave (bigger than a
	// hemisphere), and the actual density is the number of these faces less the
	// computed density.  Note that if j != 0, the assignment gamma[j] = asin(...)
	// implies gamma[j] cannot be obtuse.  Also, compute chi for the only
	// non-Wythoffian polyhedron
	
	private void count() {
	    int j, temp;
	    Fi = new int[N];
	    for (j = 0; j < N; j++) {
			E += temp = V * (int)Fraction.numerator(m[j]);
			F += Fi[j] = (int)(temp / Fraction.numerator(n[j]));
	    }
	    E /= 2;
	    if (D != 0 && gamma[0] > M_PI / 2) {
	    	D = Fi[0] - D;
	    }
	    if (index == 80) {
	    	chi = V - E + F;
	    }
	}
	
	// Generate a printable vertex configuration symbol
	
	private void configuration() {
	    int j;
	    for (j = 0; j < M; j++) {
			if (j == 0) {
			    config = "(";
			} else {
			    config += ".";
			}
			config += sprintfrac(n[rot[j]]);
	    }
	    config += ")";
	    if ((j = (int)Fraction.denominator(m[0])) != 1) {
			config += "/" + j;
	    }
	}
	
	// Compute polyhedron vertices and vertex adjecency lists.
	// The vertices adjacent to v[i] are v[adj[0, i], v[adj[1, i], ...
	// v[adj[M-1, i], ordered counterclockwise.  The algorith is a BFS on the
	// vertices, in such a way that the vetices adjacent to a givem vertex are
	// obtained from its BFS parent by a cyclic sequence of rotations. firstrot[i]
	// points to the first  rotaion in the sequence when applied to v[i]. Note that
	// for non-snub polyhedra, the rotations at a child are opposite in sense when
	// compared to the rotations at the parent. Thus, we fill adj[*, i] from the
	// end to signify clockwise rotations. The firstrot[] array is not needed for
	// display thus it is freed after being used for face computations below.

	private void vertices() {
		
	    int i, newV = 2;
	    double cosa;
	    v = new Vector[V];
	    adj = new int[M, V];
	    firstrot = new int[V];  // temporary , put in Polyhedron structure so that may be freed on error
	    cosa = Math.Cos(M_PI / n[0]) / Math.Sin(gamma[0]);
	    v[0] = new Vector(0, 0, 1);
	    firstrot[0] = 0;
	    adj[0, 0] = 1;
	    v[1] = new Vector(
		    (float)(2 * cosa * Math.Sqrt(1 - cosa * cosa)),
		    0,
		    (float)(2 * cosa * cosa - 1)
		);
	    if (snub == null) {
			firstrot[1] = 0;
			adj[0, 1] = -1;  // start the other side
			adj[M-1, 1] = 0;
	    } else {
			firstrot[1] = snub[M-1] != 0 ? 0 : M-1 ;
			adj[0, 1] = 0;
	    }
	    for (i = 0; i < newV; i++) {
			int j, k;
			int last, one, start, limit;
			if (adj[0, i] == -1) {
			    one = -1; start = M-2; limit = -1;
			} else {
			    one = 1; start = 1; limit = M;
			}
			k = firstrot[i];
			for (j = start; j != limit; j += one) {
				Vector temp;
				int J;
				Vector vertex = v[adj[j - one, i]];
				Vector axis = v[i];
				double angle = one * 2 * gamma[rot[k]];
				temp = Vector.rotate(vertex, axis, angle);
				for (J=0; J<newV && !temp.same(v[J], BIG_EPSILON); J++) {
			    	// noop
			    }
			    adj[j, i] = J;
			    last = k;
			    if (++k == M) {k = 0;}
			    if (J == newV) {  // new vertex
					if (newV == V) {throw new System.ApplicationException("too many vertices");}
					v[newV++] = temp;
					if (snub == null) {
					    firstrot[J] = k;
					    if (one > 0) {
							adj[0, J] = -1;
							adj[M-1, J] = i;
					    } else {
					    	adj[0, J] = i;
					    }
					} else {
					    firstrot[J] = snub[last] == 0 ? last :
						snub[k] == 0 ? (k+1)%M : k ;
					    adj[0, J] = i;
					}
			    }
			}
	    }
	}

	// Compute the polar reciprocal of the plane containing a, b and c:
	//
	// If this plane does not contain the origin, return p such that
	// dot(p,a) = dot(p,b) = dot(p,b) = r
	//
	// Otherwise, return p such that
	// dot(p,a) = dot(p,b) = dot(p,c) = 0
	// and
	// dot(p,p) = 1
	
	private Vector pole(double r, Vector a, Vector b, Vector c)	{
	    Vector p;
	    double k;
	    p = b.diff(a).cross(c.diff(a));
	    k = p.dot(a);
	    if (Mathf.Abs((float)k) < 1e-6) {
	    	return p.scale(1 / Mathf.Sqrt((float)(p.dot(p))));
	    } else {
	    	return p.scale(r/ k);
	    }
	}

	// Compute the mathematical modulus function
	
	private int mod(int i, int j) 	{
	    return (i%=j)>=0?i:j<0?i-j:i+j;
	}

	// Compute polyhedron faces (dual vertices) and incidence matrices.
	// For orientable polyhedra, we can distinguish between the two faces meeting
	// at a given directed edge and identify the face on the left and the face on
	// the right, as seen from the outside.  For one-sided polyhedra, the vertex
	// figure is a papillon (in Coxeter &al.  terminology, a crossed parallelogram)
	// and the two faces meeting at an edge can be identified as the side face
	// (n[1] or n[2]) and the diagonal face (n[0] or n[3])
	
	private void faces() {
		
	    int i, newF = 0;
	    f = new Vector[F];
	    ftype = new int[F];
	    incid = new int[M, V];
	    minr = 1 / Math.Abs (Math.Tan (M_PI / n[hemi]) * Math.Tan (gamma[hemi]));
	    for (i = M; --i>=0;) {
		    for (int j = V; --j >= 0;) {incid[i, j] = -1;}
	    }
	    for (i = 0; i < V; i++) {
			for (int j = 0; j < M; j++) {
			    int i0, J;
			    int pap = 0;  // papillon edge type
				if (incid[j, i] == -1) {
					incid[j, i] = newF;
					if (newF == F) {throw new System.ApplicationException("too many faces");}
					f[newF] = pole(minr, v[i], v[adj[j, i]], v[adj[mod(j + 1, M), i]]);
					ftype[newF] = rot[mod(firstrot[i] + (adj[0, i] < adj[M - 1, i] ? j : -j - 2), M)];
					if (onesided != 0) {pap = (firstrot[i] + j) % 2;}
					i0 = i;
					J = j;
					for (;;) {
						int k;
						k = i0;
						if ((i0 = adj[J, k]) == i) {break;}
						for (J = 0; J < M && adj[J, i0] != k; J++) {/* noop */}
						if (J == M) {throw new System.ApplicationException("too many faces");}
						if (onesided != 0 && (J + firstrot[i0]) % 2 == pap) {
							incid[J, i0] = newF;
							J++;
							if (J >= M) {J = 0;}
						} else {
							J--;
							if (J < 0) {J = M - 1;}
							incid[J, i0] = newF;
						}
					}
					newF++;
				}
			}
	    }
	}

	// Compute edge list and graph polyhedron and dual.
	// If the polyhedron is of the "hemi" type, each edge has one finite vertex and
	// one ideal vertex. We make sure the latter is always the out-vertex, so that
	// the edge becomes a ray (half-line).  Each ideal vertex is represented by a
	// unit Vector, and the direction of the ray is either parallel or
	// anti-parallel this Vector. We flag this in the array P->anti[E]
	
	private void edgelist() {
		
	    int i, j, s, t, u;
	    e = new int[2, E];
	    int[,] dual_e = new int[2, E];
	    s = 0;  // e[0];
	    t = 0;  // e[1];
		
	    for (i = 0; i < V; i++) {
			for (j = 0; j < M; j++) {
			    if (i < adj[j, i]) {
					e[0, s++] = i;
					e[1, t++] = adj[j, i];
			    }
			}
	    }
		
	    s = 0;  // dual_e[0];
	    t = 0;  // dual_e[1];
		
	    if (hemi != 0) {anti = null;} else {anti = new int[E];}
		
	    u = 0;  // anti;
		
	    for (i = 0; i < V; i++) {
			for (j = 0; j < M; j++) {
			    if (i < adj[j, i]) {
					if (anti == null) {
						dual_e[0, s++] = incid[mod(j-1,M), i];
						dual_e[1, t++] = incid[j, i];
					} else {
					    if (ftype[incid[j, i]] != 0) {
					    	dual_e[0, s] = incid[j, i];
					    	dual_e[1, t] = incid[mod(j-1,M), i];
					    } else {
					    	dual_e[0, s] = incid[mod(j-1,M), i];
					    	dual_e[1, t] = incid[j, i];
					    }
					    anti[u++] = f[dual_e[0, s++]].dot(f[dual_e[1, t++]]) > 0 ? 1 : 0;
					}
			    }
			}
	    }
	}

	public void kaleido(int polyType) {
		
		unpacksym("#" + polyType);
		
		Debug.Log("moebius");
		moebius();  // Find Mebius triangle, its density and Euler characteristic
		Debug.Log("decompose");
		decompose();  // Decompose Schwarz triangle
		Debug.Log("guessname");
		guessname();  // Find the names of the polyhedron and its dual
		Debug.Log("newton");
		newton();  // Solve Fundamental triangles, optionally printing approximations
		Debug.Log("exceptions");
		exceptions();  // Deal with exceptional polyhedra
		Debug.Log("count");
		count();  // Count edges and faces, update density and characteristic if needed.
		Debug.Log("configuration");
		configuration();  // Generate printable vertex configuration
		
		// Compute coordinates
		Debug.Log("vertices");
		vertices();		
		Debug.Log("faces");
		faces();
		Debug.Log("edgelist");
		edgelist();  // Compute edgelist
		
	}

}
