﻿using System;
using UnityEngine;


public class Polyhedron {
	
	public int PolyTypeIndex;  // index to the standard list, the array uniform[] 

	public int FaceTypeCount;  // number of faces types (at most 5)
	public int Valency;  // vertex valency (may be big for dihedral polyhedra) 
	public int VertexCount;  // vertex count 
	public int EdgeCount;  // edge count 
	public int FaceCount;  // face count
	
	public int Density;  // density 
	public int Chi;  // Euler characteristic 
	public int SymmetryGroupOrder;  // order of symmetry group
	public int SymmetryType = 2;  // symmetry type: D=2, T=3, O=4, I=5
	
	private int _even = -1;  // removed face in pqr|
	
	public int IsHemi; // flag hemi polyhedron 
	public int IsOneSided; // flag onesided polyhedron 
	
	public int[] FaceCountsByType;  // array of length FaceTypeCount
	public int[] VertexConfig;  // array of size Valency of 0..FaceTypeCount-1
	public int[] SnubConfig;  // array of length Valency of 0..1
	
	private int[] _firstrot;  // temporary for vertex generation (array of length VertexCount)
	private int[] _anti;  // temporary for direction of ideal vertices (array EdgeCount)
	
	public int[] FaceTypes;  // array of length FaceTypeCount

	public int[,] Edges;  // matrix 2 x EdgeCount of 0..VertexCount-1
	public int[,] DualEdges;  // matrix 2 x EdgeCount of 0..FaceCount-1
	public int[,] VertexFaceIncidence;  // matrix of size Valency x VertexCount of 0..FaceCount-1
	public int[,] VertexAdjacency;  // matrix of size Valency x VertexCount of 0..VertexCount-1
	
	public double[] WythoffParams = new double[4];  // p, q and r; |=0 
	public double MinInRadius;  // smallest nonzero inradius 
	public double DihedralBasisType;  // basis type for dihedral polyhedra 
	
	public double[] FaceSidesByType;  // number of side of a face of each type (array of length FaceTypeCount)
	public double[] FaceCountAtVertexType;  // number of faces at a vertex of each type (array of length FaceTypeCount)
	public double[] FundementalAngles;  // in radians (array of length FaceTypeCount)
	
	public string WythoffSymbol;  // printable Wythoff symbol
	public string VertexConfigString;  // printable vertex configuration 
	public string PolyName;  // name, standard or manifuctured
	public string DualName;  // dual name, standard or manifuctured 

	public Vector[] Vertices;  // vertex coordinates (array VertexCount) 
	public Vector[] Faces;  // face coordinates (array FaceCount)
	
	public Polyhedron(int polyType) {
		
		UnpackSym("#" + polyType);
		FindMoebiusTri();  // Find Mebius triangle, its density and Euler characteristic
		DecomposeSchwarzTri();  // Decompose Schwarz triangle
		GuessNames();  // Find the names of the polyhedron and its dual
		SolveFundementalTris();  // Solve Fundamental triangles
		HandlePolyExceptions();  // Deal with exceptional polyhedra
		CalcCounts();  // Count edges and faces, update density and characteristic if needed
		GenerateConfiguration();  // Generate printable vertex configuration
		
		// Compute coordinates
		CalcVertices();
		CalcFaces();
		CalcEdgeList();
		
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

	public static UniformImpl[] uniform = {
			
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
		int _index;
		
		public SymParser(string sym) {
			this.sym = sym;
			_index = 0;
		}

		public double GetNextFraction() {

			char c = sym[_index];
			while (c.ToString() == " ") {
				++_index;
				c = sym[_index];
			}

			if (sym[_index].ToString() == "|") {
				_index++;
				return 0;
			} else {
				c = sym[_index++];
				if (Char.IsDigit(c)) {
					string number = "" + c;
					double a;
					try {
						c = sym[_index];
						while (Char.IsDigit(c)) {
							number += c;
							++_index;
							c = sym[_index];
						}
					} catch (IndexOutOfRangeException) {
						return Double.Parse(number);
					}
					a = Double.Parse(number);
					if (sym[_index] == '/') {
						c = sym[++_index];
						if (Char.IsDigit(c)) {
							number = "";
							try {
								while (Char.IsDigit(c = sym[_index])) {
									number += c;
									++_index;
								}
							} catch (IndexOutOfRangeException) { }
							return a / Double.Parse(number);
						}
						else {throw new ApplicationException("No digit after \"/\": " + c);}
					}
					else {return a;}
				}
				else {throw new ApplicationException("\"" + c + "\" is not a digit");}
			}
		}
	}

	// Unpack input symbol: Wythoff symbol or an index to uniform[]. The symbol is
	// a # followed by a number, or a three fractions and a bar in some order. We
	// allow no bars only if it result from the input symbol #80
	
	private void UnpackSym(string sym) {
		
	    sym = sym.Trim();
		
	    if (!String.IsNullOrEmpty(sym)) {
	    	if (sym.StartsWith("#")) {  // take the number of polyhedron
	    		try {
	    			PolyTypeIndex = Int32.Parse(sym.Substring(1));
	    		} catch (FormatException e) {
	    			throw new ApplicationException("Illegal number: " + e.Message);
	    		}
	    		sym = uniform[PolyTypeIndex].Wythoff;
	    	}
	    }
	    SymParser sp = new SymParser(sym);
	    WythoffParams[0] = sp.GetNextFraction();
	    WythoffParams[1] = sp.GetNextFraction();
	    WythoffParams[2] = sp.GetNextFraction();
	    WythoffParams[3] = sp.GetNextFraction();
	}

	private string SprintFrac(double x) {
	    string s = "";
	    Fraction frax = new Fraction().frac(x);
	    if (frax.d == 0) {s += "infinity";}
	    else if (frax.d == 1) {s+= frax.n;}
	    else {s+= frax.n + "/" + frax.d;}
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
	
	private void FindMoebiusTri() {
		
	    int twos = 0, j;
		
		// Arrange Wythoff symbol in a presentable form. In the same time check the
		// restrictions on the three fractions: They all have to be greater then one,
		// and the numerators 4 or 5 cannot occur together.  We count the ocurrences of
		// 2 in `two', and save the largest numerator in `P->K', since they reflect on
		// the symmetry group
		
	    SymmetryType = 2;
	    if (PolyTypeIndex == 80) {WythoffSymbol = "|";} else {WythoffSymbol = "";}
		
	    for (j = 0; j < 4; j++) {
			if (WythoffParams[j] > 0) {
			    string s = SprintFrac(WythoffParams[j]);
			    if (j > 0 && WythoffParams[j-1] > 0) {
			    	WythoffSymbol += " " + s;
			    } else {WythoffSymbol += s;}
			    if (WythoffParams[j] != 2) {
					int k;
					if ((k = (int)Fraction.numerator(WythoffParams[j])) > SymmetryType) {
					    if (SymmetryType == 4) {break;}
					    SymmetryType = k;
					} else if (k < SymmetryType && k == 4) {break;}
			    } else {twos++;}
			} else  {WythoffSymbol += "|";}
		}

		// Find the symmetry group P->K (where 2, 3, 4, 5 represent the dihedral,
		// tetrahedral, octahedral and icosahedral groups, respectively), and its order
		// P->g
	    if (twos >= 2) {  // dihedral
			SymmetryGroupOrder = 4 * SymmetryType;
			SymmetryType = 2;
	    } else {
			if (SymmetryType > 5) {throw new ApplicationException("numerator too large");}
			SymmetryGroupOrder = 24 * SymmetryType / (6 - SymmetryType);
	    }
	
		// Compute the nominal density P->D and Euler characteristic P->chi.
		// In few exceptional cases, these values will be modified later
	
	    if (PolyTypeIndex != 80) {
			int i;
			Density = Chi = -SymmetryGroupOrder;
			for (j = 0; j < 4; j++) {
				if (WythoffParams[j] > 0) {
				    Chi += i = (int)(SymmetryGroupOrder / Fraction.numerator(WythoffParams[j]));
				    Density += i * (int)Fraction.denominator(WythoffParams[j]);
				}
			}
			Chi /= 2;
			Density /= 4;
		    if (Density <= 0) {throw new ApplicationException("nonpositive density");}
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
	
	private void DecomposeSchwarzTri() {
		
	    int j, J, s, t;
		
	    if (WythoffParams[1] == 0) {  // p|q r
		    
			FaceTypeCount = 2;
			Valency = 2 * (int)Fraction.numerator(WythoffParams[0]);
			VertexCount = SymmetryGroupOrder / Valency;
			FaceSidesByType = new double[FaceTypeCount];
			FaceCountAtVertexType = new double[FaceTypeCount];
			VertexConfig = new int[Valency];
			s = 0;
			for (j = 0; j < 2; j++) {
			    FaceSidesByType[j] = WythoffParams[j+2];
			    FaceCountAtVertexType[j] = WythoffParams[0];
			}
			for (j = Valency / 2; j > 0; j--) {
			    VertexConfig[s++] = 0;
			    VertexConfig[s++] = 1;
			}
	    } else if (WythoffParams[2] == 0) {  // p q|r
			FaceTypeCount = 3;
			Valency = 4;
			VertexCount = SymmetryGroupOrder / 2;
			FaceSidesByType = new double[FaceTypeCount];
			FaceCountAtVertexType = new double[FaceTypeCount];
			VertexConfig = new int[Valency];
			s = 0;  // rot;
			FaceSidesByType[0] = 2 * WythoffParams[3];
			FaceCountAtVertexType[0] = 2;
			for (j = 1; j < 3; j++) {
			    FaceSidesByType[j] = WythoffParams[j-1];
			    FaceCountAtVertexType[j] = 1;
			    VertexConfig[s++] = 0;
			    VertexConfig[s++] = j;
			}
			if (Math.Abs(WythoffParams[0] - Fraction.compl(WythoffParams[1])) < DBL_EPSILON) {  // p = q'
				// P->p[0]==compl(P->p[1]) should work.  However, MSDOS
				// yeilds a 7e-17 difference! Reported by Jim Buddenhagen
				// <jb1556@daditz.sbc.com> */
			    IsHemi = 1;
			    Density = 0;
			    if (WythoffParams[0] != 2 && !(WythoffParams[3] == 3 && (WythoffParams[0] == 3 || WythoffParams[1] == 3))) {
					IsOneSided = 1;
					VertexCount /= 2;
					Chi /= 2;
			    }
			}
	    } else if (WythoffParams[3] == 0) {  // p q r|
			Valency = FaceTypeCount = 3;
			VertexCount = SymmetryGroupOrder;
			FaceSidesByType = new double[FaceTypeCount];
			FaceCountAtVertexType = new double[FaceTypeCount];
			VertexConfig = new int[Valency];
			s = 0;  // rot;
			for (j = 0; j < 3; j++) {
			    if ((Fraction.denominator(WythoffParams[j]) % 2) == 0) {
					// what happens if there is more then one even denominator?
					if (WythoffParams[(j+1)%3] != WythoffParams[(j+2)%3]) {  // needs postprocessing
					    _even = j;/* memorize the removed face */
					    Chi -= SymmetryGroupOrder / (int)Fraction.numerator(WythoffParams[j]) / 2;
					    IsOneSided = 1;
					    Density = 0;
					} else {
						// for p = q we get a double 2 2r|p
						// noted by Roman Maeder <maeder@inf.ethz.ch> for 4 4 3/2|
						// Euler characteristic is still wrong
					    Density /= 2;
					}
					VertexCount /= 2;
			    }
			    FaceSidesByType[j] = 2 * WythoffParams[j];
			    FaceCountAtVertexType[j] = 1;
			    VertexConfig[s++] = j;
			}
	    } else {  // |p q r - snub polyhedron
			FaceTypeCount = 4;
			Valency = 6;
			VertexCount = SymmetryGroupOrder / 2;/* Only "white" triangles carry a vertex */
			FaceSidesByType = new double[FaceTypeCount];
			FaceCountAtVertexType = new double[FaceTypeCount];
			VertexConfig = new int[Valency];
			SnubConfig = new int[Valency];
			s = 0; //rot;
			t = 0; //snub;
			FaceCountAtVertexType[0] = FaceSidesByType[0] = 3;
			for (j = 1; j < 4; j++) {
			    FaceSidesByType[j] = WythoffParams[j];
			    FaceCountAtVertexType[j] = 1;
			    VertexConfig[s++] = 0;
			    VertexConfig[s++] = j;
			    SnubConfig[t++] = 1;
			    SnubConfig[t++] = 0;
			}
	    }
		
		// Sort the fundamental triangles (using bubble sort) according to decreasing
		// n[i], while pushing the trivial triangles (n[i] = 2) to the end
	
	    J = FaceTypeCount - 1;
	    while (J != 0) {
			int last;
			last = J;
			J = 0;
			for (j = 0; j < last; j++) {
			    if ((FaceSidesByType[j] < FaceSidesByType[j+1] || FaceSidesByType[j] == 2) && FaceSidesByType[j+1] != 2) {
					int i;
					double temp;
					temp = FaceSidesByType[j];
					FaceSidesByType[j] = FaceSidesByType[j+1];
					FaceSidesByType[j+1] = temp;
					temp = FaceCountAtVertexType[j];
					FaceCountAtVertexType[j] = FaceCountAtVertexType[j+1];
					FaceCountAtVertexType[j+1] = temp;
					for (i = 0; i < Valency; i++) {
					    if (VertexConfig[i] == j) {VertexConfig[i] = j+1;}
					    else if (VertexConfig[i] == j+1) {VertexConfig[i] = j;}
					}
					if (_even != -1) {
					    if (_even == j) {_even = j+1;}
					    else if (_even == j+1) {_even = j;}
					}
					J = j;
			    }
			}
	    }
		// Get rid of repeated triangles
	    for (J = 0; J < FaceTypeCount && FaceSidesByType[J] != 2; J++) {
			int k, i;
			for (j = J+1; j < FaceTypeCount && FaceSidesByType[j] == FaceSidesByType[J]; j++) {
			    FaceCountAtVertexType[J] += FaceCountAtVertexType[j];
			}
			k = j - J - 1;
			if (k != 0) {
			    for (i = j; i < FaceTypeCount; i++) {
					FaceSidesByType[i - k] = FaceSidesByType[i];
					FaceCountAtVertexType[i - k] = FaceCountAtVertexType[i];
			    }
			    FaceTypeCount -= k;
			    for (i = 0; i < Valency; i++) {
					if (VertexConfig[i] >= j)
					    VertexConfig[i] -= k;
					else if (VertexConfig[i] > J) {
					    VertexConfig[i] = J;
					}
			    }
			    if (_even >= j) {_even -= k;}
			}
	    }
		
		// Get rid of trivial triangles
	    if (J == 0) {J = 1;}  // hosohedron
	    if (J < FaceTypeCount) {
			int i;
			FaceTypeCount = J;
			for (i = 0; i < Valency; i++) {
			    if (VertexConfig[i] >= FaceTypeCount) {
					for (j = i + 1; j < Valency; j++) {
					    VertexConfig[j-1] = VertexConfig[j];
					    if (SnubConfig != null && SnubConfig.Length > j) {
					    	SnubConfig[j-1] = SnubConfig[j];
					    }
					}
					Valency--;
			    }
			}
	    }
		// Truncate arrays
	    Array.Resize(ref FaceSidesByType, FaceTypeCount);
	    Array.Resize(ref FaceCountAtVertexType, FaceTypeCount);
	    Array.Resize(ref VertexConfig, Valency);
	    if (SnubConfig != null) {Array.Resize(ref SnubConfig, Valency);}
		
	}

	private void Dihedral() {
	    string s = SprintFrac(DihedralBasisType < 2 ? Fraction.compl(DihedralBasisType) : DihedralBasisType);
		PolyName = s + "-gonal " + PolyName;
		DualName = s + "-gonal " + DualName;
	}
	
	// Get the polyhedron name, using standard list or guesswork. Ideally, we
	// should try to locate the Wythoff symbol in the standard list (unless, of
	// course, it is dihedral), after doing few normalizations, such as sorting
	// angles and splitting isoceles triangles.
	
	private void GuessNames() {
	    if (PolyTypeIndex != -1) {  // tabulated
		    PolyName = uniform[PolyTypeIndex].name;
			DualName = uniform[PolyTypeIndex].dual;
	    } else if (SymmetryType == 2) {  // dihedral nontabulated
			if (WythoffParams[0] == 0) {
			    if (FaceTypeCount == 1) {
				    PolyName = "octahedron";
					DualName = "cube";
			    } else {
				    DihedralBasisType = FaceSidesByType[0] == 3 ? FaceSidesByType[1] : FaceSidesByType[0];
				    if (DihedralBasisType >= 2) {Dihedral();}
				    else {Dihedral();}
			    }
			} else if (WythoffParams[3] == 0 || WythoffParams[2] == 0 && WythoffParams[3] == 2) {
			    if (FaceTypeCount == 1) {
				    PolyName = "cube";
					DualName = "octahedron";
			    }
			    DihedralBasisType = FaceSidesByType[0] == 4 ? FaceSidesByType[1] : FaceSidesByType[0];
			    Dihedral();
			} else if (WythoffParams[1] == 0 && WythoffParams[0] != 2) {
			    DihedralBasisType = FaceCountAtVertexType[0];
			    Dihedral();
			} else {
			    DihedralBasisType = FaceSidesByType[0];
			    Dihedral();
			}
	    } else {  // other nontabulated
		    
			string[] pre = new string[] {"tetr", "oct", "icos"};
		    PolyName = pre[SymmetryType - 3] + "ahedral ";
		    
			if (IsOneSided != 0) {PolyName += "one-sided ";}
			else if (Density == 1) {PolyName += "convex ";}
			else {PolyName += "nonconvex ";}
		    
			DualName = PolyName;
		    PolyName += "isogonal polyhedron";
			DualName += "isohedral polyhedron";
	    }
	}
	
	// Solve the fundamental right spherical triangles
	
	private void SolveFundementalTris() {
		
		// First, we find initial approximations.
	    int j;
	    double cosa;
	    FundementalAngles = new double[FaceTypeCount];
	    if (FaceTypeCount == 1) {FundementalAngles[0] = M_PI / FaceCountAtVertexType[0];}
	    for (j = 0; j < FaceTypeCount; j++)
		FundementalAngles[j] = M_PI / 2 - M_PI / FaceSidesByType[j];
		
		// Next, iteratively find closer approximations for gamma[0] and compute
		// other gamma[j]'s from Napier's equations
		
	    for (;;) {
			double delta = M_PI, sigma = 0;
			for (j = 0; j < FaceTypeCount; j++) {
			    delta -= FaceCountAtVertexType[j] * FundementalAngles[j];
			}
			if (Math.Abs(delta) < 11 * DBL_EPSILON) {return;}
			// On a RS/6000, fabs(delta)/DBL_EPSILON may occilate between 8 and 10
			// Reported by David W. Sanderson <dws@ssec.wisc.edu>
		    for (j = 0; j < FaceTypeCount; j++) {
			    sigma += FaceCountAtVertexType[j] * Math.Tan(FundementalAngles[j]);
		    }
		    FundementalAngles[0] += delta * Math.Tan(FundementalAngles[0]) / sigma;
			if (FundementalAngles[0] < 0 || FundementalAngles[0] > M_PI) {
			    throw new ApplicationException("gamma out of bounds");
			}
			cosa = Math.Cos(M_PI / FaceSidesByType[0]) / Math.Sin(FundementalAngles[0]);
		    for (j = 1; j < FaceTypeCount; j++) {
			    FundementalAngles[j] = Math.Asin(Math.Cos(M_PI / FaceSidesByType[j]) / cosa);
		    }
	    }
	}

	// Postprocess pqr| where r has an even denominator (cf. Coxeter &al. Sec.9)
	// Remove the {2r} and add a retrograde {2p} and retrograde {2q}
	
	private void HandlePolyExceptions() {
		
	    int j;
	    if (_even != -1) {
			Valency = FaceTypeCount = 4;
			Array.Resize(ref FaceSidesByType, FaceTypeCount);
			Array.Resize(ref FaceCountAtVertexType, FaceTypeCount);
			Array.Resize(ref FundementalAngles, FaceTypeCount);
			Array.Resize(ref VertexConfig, Valency);
			for (j = _even + 1; j < 3; j++) {
			    FaceSidesByType[j-1] = FaceSidesByType[j];
			    FundementalAngles[j-1] = FundementalAngles[j];
			}
			FaceSidesByType[2] = Fraction.compl(FaceSidesByType[1]);
			FundementalAngles[2] = - FundementalAngles[1];
			FaceSidesByType[3] = Fraction.compl(FaceSidesByType[0]);
			FaceCountAtVertexType[3] = 1;
			FundementalAngles[3] = - FundementalAngles[0];
			VertexConfig[0] = 0;
			VertexConfig[1] = 1;
			VertexConfig[2] = 3;
			VertexConfig[3] = 2;
	    }

		// Postprocess the last polyhedron |3/2 5/3 3 5/2 by taking a |5/3 3 5/2,
		// replacing the three snub triangles by four equatorial squares and adding
		// the missing {3/2} (retrograde triangle, cf. Coxeter &al. Sec. 11)
		
	    if (PolyTypeIndex == 80) {
			FaceTypeCount = 5;
			Valency = 8;
			Array.Resize(ref FaceSidesByType, FaceTypeCount);
			Array.Resize(ref FaceCountAtVertexType, FaceTypeCount);
			Array.Resize(ref FundementalAngles, FaceTypeCount);
			Array.Resize(ref VertexConfig, Valency);
			Array.Resize(ref SnubConfig, Valency);
			IsHemi = 1;
			Density = 0;
			for (j = 3; j != 0; j--) {
			    FaceCountAtVertexType[j] = 1;
			    FaceSidesByType[j] = FaceSidesByType[j-1];
			    FundementalAngles[j] = FundementalAngles[j-1];
			}
			FaceCountAtVertexType[0] = FaceSidesByType[0] = 4;
			FundementalAngles[0] = M_PI / 2;
			FaceCountAtVertexType[4] = 1;
			FaceSidesByType[4] = Fraction.compl(FaceSidesByType[1]);
			FundementalAngles[4] = - FundementalAngles[1];
			for (j = 1; j < 6; j += 2) {VertexConfig[j]++;}
			VertexConfig[6] = 0;
			VertexConfig[7] = 4;
			SnubConfig[6] = 1;
			SnubConfig[7] = 0;
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
	
	private void CalcCounts() {
	    int j, temp;
	    FaceCountsByType = new int[FaceTypeCount];
	    for (j = 0; j < FaceTypeCount; j++) {
			EdgeCount += temp = VertexCount * (int)Fraction.numerator(FaceCountAtVertexType[j]);
			FaceCount += FaceCountsByType[j] = (int)(temp / Fraction.numerator(FaceSidesByType[j]));
	    }
	    EdgeCount /= 2;
	    if (Density != 0 && FundementalAngles[0] > M_PI / 2) {
	    	Density = FaceCountsByType[0] - Density;
	    }
	    if (PolyTypeIndex == 80) {
	    	Chi = VertexCount - EdgeCount + FaceCount;
	    }
	}
	
	// Generate a printable vertex configuration symbol
	
	private void GenerateConfiguration() {
	    int j;
	    for (j = 0; j < Valency; j++) {
			if (j == 0) {
			    VertexConfigString = "(";
			} else {
			    VertexConfigString += ".";
			}
			VertexConfigString += SprintFrac(FaceSidesByType[VertexConfig[j]]);
	    }
	    VertexConfigString += ")";
	    if ((j = (int)Fraction.denominator(FaceCountAtVertexType[0])) != 1) {
			VertexConfigString += "/" + j;
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

	private void CalcVertices() {
		
	    int i, newV = 2;
	    double cosa;
	    Vertices = new Vector[VertexCount];
	    VertexAdjacency = new int[Valency, VertexCount];
	    _firstrot = new int[VertexCount];  // temporary , put in Polyhedron structure so that may be freed on error
	    cosa = Math.Cos(M_PI / FaceSidesByType[0]) / Math.Sin(FundementalAngles[0]);
	    Vertices[0] = new Vector(0, 0, 1);
	    _firstrot[0] = 0;
	    VertexAdjacency[0, 0] = 1;
	    Vertices[1] = new Vector(
		    (float)(2 * cosa * Math.Sqrt(1 - cosa * cosa)),
		    0,
		    (float)(2 * cosa * cosa - 1)
		);
	    if (SnubConfig == null) {
			_firstrot[1] = 0;
			VertexAdjacency[0, 1] = -1;  // start the other side
			VertexAdjacency[Valency-1, 1] = 0;
	    } else {
			_firstrot[1] = SnubConfig[Valency-1] != 0 ? 0 : Valency-1 ;
			VertexAdjacency[0, 1] = 0;
	    }
	    for (i = 0; i < newV; i++) {
			int j, k;
			int last, one, start, limit;
			if (VertexAdjacency[0, i] == -1) {
			    one = -1; start = Valency-2; limit = -1;
			} else {
			    one = 1; start = 1; limit = Valency;
			}
			k = _firstrot[i];
			for (j = start; j != limit; j += one) {
				Vector temp;
				int J;
				Vector vertex = Vertices[VertexAdjacency[j - one, i]];
				Vector axis = Vertices[i];
				double angle = one * 2 * FundementalAngles[VertexConfig[k]];
				temp = Vector.rotate(vertex, axis, angle);
				for (J=0; J<newV && !temp.same(Vertices[J], BIG_EPSILON); J++) {
			    	// noop
			    }
			    VertexAdjacency[j, i] = J;
			    last = k;
			    if (++k == Valency) {k = 0;}
			    if (J == newV) {  // new vertex
					if (newV == VertexCount) {throw new ApplicationException("too many vertices");}
					Vertices[newV++] = temp;
					if (SnubConfig == null) {
					    _firstrot[J] = k;
					    if (one > 0) {
							VertexAdjacency[0, J] = -1;
							VertexAdjacency[Valency-1, J] = i;
					    } else {
					    	VertexAdjacency[0, J] = i;
					    }
					} else {
					    _firstrot[J] = SnubConfig[last] == 0 ? last :
						SnubConfig[k] == 0 ? (k+1)%Valency : k ;
					    VertexAdjacency[0, J] = i;
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
	
	private Vector CalcPolarReciprocal(double r, Vector a, Vector b, Vector c)	{
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
	
	private void CalcFaces() {
		
	    int i, newF = 0;
	    Faces = new Vector[FaceCount];
	    FaceTypes = new int[FaceCount];
	    VertexFaceIncidence = new int[Valency, VertexCount];
	    MinInRadius = 1 / Math.Abs (Math.Tan (M_PI / FaceSidesByType[IsHemi]) * Math.Tan (FundementalAngles[IsHemi]));
	    for (i = Valency; --i>=0;) {
		    for (int j = VertexCount; --j >= 0;) {VertexFaceIncidence[i, j] = -1;}
	    }
	    for (i = 0; i < VertexCount; i++) {
			for (int j = 0; j < Valency; j++) {
			    int i0, J;
			    int pap = 0;  // papillon edge type
				if (VertexFaceIncidence[j, i] == -1) {
					VertexFaceIncidence[j, i] = newF;
					if (newF == FaceCount) {throw new ApplicationException("too many faces");}
					Faces[newF] = CalcPolarReciprocal(MinInRadius, Vertices[i], Vertices[VertexAdjacency[j, i]], Vertices[VertexAdjacency[mod(j + 1, Valency), i]]);
					FaceTypes[newF] = VertexConfig[mod(_firstrot[i] + (VertexAdjacency[0, i] < VertexAdjacency[Valency - 1, i] ? j : -j - 2), Valency)];
					if (IsOneSided != 0) {pap = (_firstrot[i] + j) % 2;}
					i0 = i;
					J = j;
					for (;;) {
						int k;
						k = i0;
						if ((i0 = VertexAdjacency[J, k]) == i) {break;}
						for (J = 0; J < Valency && VertexAdjacency[J, i0] != k; J++) {/* noop */}
						if (J == Valency) {throw new ApplicationException("too many faces");}
						if (IsOneSided != 0 && (J + _firstrot[i0]) % 2 == pap) {
							VertexFaceIncidence[J, i0] = newF;
							J++;
							if (J >= Valency) {J = 0;}
						} else {
							J--;
							if (J < 0) {J = Valency - 1;}
							VertexFaceIncidence[J, i0] = newF;
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
	
	private void CalcEdgeList() {
		
	    int i, j, s, t, u;
	    Edges = new int[2, EdgeCount];
	    DualEdges = new int[2, EdgeCount];
	    s = 0;  // e[0];
	    t = 0;  // e[1];
		
	    for (i = 0; i < VertexCount; i++) {
			for (j = 0; j < Valency; j++) {
			    if (i < VertexAdjacency[j, i]) {
					Edges[0, s++] = i;
					Edges[1, t++] = VertexAdjacency[j, i];
			    }
			}
	    }
		
	    s = 0;  // dual_e[0];
	    t = 0;  // dual_e[1];
		
	    if (IsHemi != 0) {_anti = null;} else {_anti = new int[EdgeCount];}
		
	    u = 0;  // anti;
		
	    for (i = 0; i < VertexCount; i++) {
			for (j = 0; j < Valency; j++) {
			    if (i < VertexAdjacency[j, i]) {
					if (_anti == null) {
						DualEdges[0, s++] = VertexFaceIncidence[mod(j-1,Valency), i];
						DualEdges[1, t++] = VertexFaceIncidence[j, i];
					} else {
					    if (FaceTypes[VertexFaceIncidence[j, i]] != 0) {
					    	DualEdges[0, s] = VertexFaceIncidence[j, i];
					    	DualEdges[1, t] = VertexFaceIncidence[mod(j-1,Valency), i];
					    } else {
					    	DualEdges[0, s] = VertexFaceIncidence[mod(j-1,Valency), i];
					    	DualEdges[1, t] = VertexFaceIncidence[j, i];
					    }
					    _anti[u++] = Faces[DualEdges[0, s++]].dot(Faces[DualEdges[1, t++]]) > 0 ? 1 : 0;
					}
			    }
			}
	    }
	}
}