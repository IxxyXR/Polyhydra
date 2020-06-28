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

using System.Linq;

namespace Wythoff {

	public class Uniform
    {
        public readonly int Index;
		public readonly string Wythoff;
	    public readonly string Name;
	    public readonly string Dual;
	    public int Coxeter, Wenninger;
	
		public Uniform(int index, string wythoff, string name, string dual, int coxeter, int wenninger)
        {
            Index = index;
			Wythoff = wythoff;
			Name = name;
			Dual = dual;
			Coxeter = coxeter;
			Wenninger = wenninger;
		}
		
		
        public static Uniform[] Uniforms = {

            // Dummy entry as these are 1-indexed
            new Uniform(
                0,
                "",
                "",
                "",
                0, 0
                ),
            
            // Dihedral Schwarz Triangles (D5 only)

            // (2 2 5) (D1/5) 

            new Uniform(
                1,
                "2 p|2",
                "polygonal prism",
                "polygonal dipyramid",
                0, 0
            ),
            new Uniform(
                2,
                "|2 2 p",
                "polygonal antiprism",
                "polygonal deltohedron",
                0, 0
            ),

            // (2 2 5/2) (D2/5) 

            new Uniform(
                3,
                "2 p/q|2",
                "polygrammic prism",
                "polygrammic dipyramid",
                0, 0
            ),
            new Uniform(
                4,
                "|2 2 p/q",
                "polygrammic antiprism",
                "polygrammic deltohedron",
                0, 0
            ),

            // (5/3 2 2) (D3/5) 

            new Uniform(
                5,
                "|2 2 p/q",
                "polygrammic crossed antiprism",
                "polygrammic concave deltohedron",
                0, 0
            ),

            // Tetrahedral Schwarz Triangles

            // (2 3 3) (T1) 

            new Uniform(
                6,
                "3|2 3",
                "tetrahedron",
                "tetrahedron",
                15, 1
            ),
            new Uniform(
                7,
                "2 3|3",
                "truncated tetrahedron",
                "triakistetrahedron",
                16, 6
            ),

            // (3/2 3 3) (T2) 

            new Uniform(
                8,
                "3/2 3|3",
                "octahemioctahedron",
                "octahemioctacron",
                37, 68
            ),

            // (3/2 2 3) (T3) 

            new Uniform(
                9,
                "3/2 3|2",
                "tetrahemihexahedron",
                "tetrahemihexacron",
                36, 67
            ),

            // Octahedral Schwarz Triangles

            // (2 3 4) (O1) 

            new Uniform(
                10,
                "4|2 3",
                "octahedron",
                "cube",
                17, 2
            ),
            new Uniform(
                11,
                "3|2 4",
                "cube",
                "octahedron",
                18, 3
            ),
            new Uniform(
                12,
                "2|3 4",
                "cuboctahedron",
                "rhombic dodecahedron",
                19, 11
            ),
            new Uniform(
                13,
                "2 4|3",
                "truncated octahedron",
                "tetrakishexahedron",
                20, 7
            ),
            new Uniform(
                14,
                "2 3|4",
                "truncated cube",
                "triakisoctahedron",
                21, 8
            ),
            new Uniform(
                15,
                "3 4|2",
                "rhombicuboctahedron",
                "deltoidal icositetrahedron",
                22, 13
            ),
            new Uniform(
                16,
                "2 3 4|",
                "truncated cuboctahedron",
                "disdyakisdodecahedron",
                23, 15
            ),
            new Uniform(
                17,
                "|2 3 4",
                "snub cube",
                "pentagonal icositetrahedron",
                24, 17
            ),

            // (3/2 4 4) (O2b) 

            new Uniform(
                18,
                "3/2 4|4",
                "small cubicuboctahedron",
                "small hexacronic icositetrahedron",
                38, 69
            ),

            // (4/3 3 4) (O4) 

            new Uniform(
                19,
                "3 4|4/3",
                "great cubicuboctahedron",
                "great hexacronic icositetrahedron",
                50, 77
            ),

            // TODO Fix
            new Uniform(
                20,
                "4/3 4|3",
                "cubohemioctahedron",
                "hexahemioctacron",
                51, 78
            ),

            new Uniform(
                21,
                "4/3 3 4|",
                "cubitruncated cuboctahedron",
                "tetradyakishexahedron",
                52, 79
            ),

            // (3/2 2 4) (O5) 

            new Uniform(
                22,
                "3/2 4|2",
                "great rhombicuboctahedron",
                "great deltoidal icositetrahedron",
                59, 85
            ),
            // TODO Fix
            new Uniform(
                23,
                "3/2 2 4|",
                "small rhombihexahedron",
                "small rhombihexacron",
                60, 86
            ),

            // (4/3 2 3) (O7) 

            new Uniform(
                24,
                "2 3|4/3",
                "stellated truncated hexahedron",
                "great triakisoctahedron",
                66, 92
            ),

            // TODO Fix small holes
            new Uniform(
                25,
                "4/3 2 3|",
                "great truncated cuboctahedron",
                "great disdyakisdodecahedron",
                67, 93
            ),

            // (4/3 3/2 2) (O11) 

            // TODO Fix
            new Uniform(
                26,
                "4/3 3/2 2|",
                "great rhombihexahedron",
                "great rhombihexacron",
                82, 103
            ),

            // Icosahedral Schwarz Triangles

            // (2 3 5) (I1) 

            new Uniform(
                27,
                "5|2 3",
                "icosahedron",
                "dodecahedron",
                25, 4
            ),
            new Uniform(
                28,
                "3|2 5",
                "dodecahedron",
                "icosahedron",
                26, 5
            ),
            new Uniform(
                29,
                "2|3 5",
                "icosidodecahedron",
                "rhombic triacontahedron",
                28, 12
            ),
            new Uniform(
                30,
                "2 5|3",
                "truncated icosahedron",
                "pentakisdodecahedron",
                27, 9
            ),
            new Uniform(
                31,
                "2 3|5",
                "truncated dodecahedron",
                "triakisicosahedron",
                29, 10
            ),
            new Uniform(
                32,
                "3 5|2",
                "rhombicosidodecahedron",
                "deltoidal hexecontahedron",
                30, 14
            ),
            new Uniform(
                33,
                "2 3 5|",
                "truncated icosidodechedon",
                "disdyakistriacontahedron",
                31, 16
            ),
            new Uniform(
                34,
                "|2 3 5",
                "snub dodecahedron",
                "pentagonal hexecontahedron",
                32, 18
            ),

            // (5/2 3 3) (I2a) 

            new Uniform(
                35,
                "3|5/2 3",
                "small ditrigonal icosidodecahedron",
                "small triambic icosahedron",
                39, 70
            ),
            new Uniform(
                36,
                "5/2 3|3",
                "small icosicosidodecahedron",
                "small icosacronic hexecontahedron",
                40, 71
            ),
            new Uniform(
                37,
                "|5/2 3 3",
                "small snub icosicosidodecahedron",
                "small hexagonal hexecontahedron",
                41, 110
            ),

            // (3/2 5 5) (I2b) 

            new Uniform(
                38,
                "3/2 5|5",
                "small dodecicosidodecahedron",
                "small dodecacronic hexecontahedron",
                42, 72
            ),

            // (2 5/2 5) (I3) 

            new Uniform(
                39,
                "5|2 5/2",
                "small stellated dodecahedron",
                "great dodecahedron",
                43, 20
            ),
            new Uniform(
                40,
                "5/2|2 5",
                "great dodecahedron",
                "small stellated dodecahedron",
                44, 21
            ),
            new Uniform(
                41,
                "2|5/2 5",
                "great dodecadodecahedron",
                "medial rhombic triacontahedron",
                45, 73
            ),
            new Uniform(
                42,
                "2 5/2|5",
                "truncated great dodecahedron",
                "small stellapentakisdodecahedron",
                47, 75
            ),
            new Uniform(
                43,
                "5/2 5|2",
                "rhombidodecadodecahedron",
                "medial deltoidal hexecontahedron",
                48, 76
            ),

            // TODO Fix
            new Uniform(
                44,
                "2 5/2 5|",
                "small rhombidodecahedron",
                "small rhombidodecacron",
                46, 74
            ),
            new Uniform(
                45,
                "|2 5/2 5",
                "snub dodecadodecahedron",
                "medial pentagonal hexecontahedron",
                49, 111
            ),

            // (5/3 3 5) (I4) 

            new Uniform(
                46,
                "3|5/3 5",
                "ditrigonal dodecadodecahedron",
                "medial triambic icosahedron",
                53, 80
            ),
            new Uniform(
                47,
                "3 5|5/3",
                "great ditrigonal dodecicosidodecahedron",
                "great ditrigonal dodecacronic hexecontahedron",
                54, 81
            ),
            new Uniform(
                48,
                "5/3 3|5",
                "small ditrigonal dodecicosidodecahedron",
                "small ditrigonal dodecacronic hexecontahedron",
                55, 82
            ),
            new Uniform(
                49,
                "5/3 5|3",
                "icosidodecadodecahedron",
                "medial icosacronic hexecontahedron",
                56, 83
            ),
            new Uniform(
                50,
                "5/3 3 5|",
                "icositruncated dodecadodecahedron",
                "tridyakisicosahedron",
                57, 84
            ),
            new Uniform(
                51,
                "|5/3 3 5",
                "snub icosidodecadodecahedron",
                "medial hexagonal hexecontahedron",
                58, 112
            ),

            // (3/2 3 5) (I6b) 

            new Uniform(
                52,
                "3/2|3 5",
                "great ditrigonal icosidodecahedron",
                "great triambic icosahedron",
                61, 87
            ),
            new Uniform(
                53,
                "3/2 5|3",
                "great icosicosidodecahedron",
                "great icosacronic hexecontahedron",
                62, 88
            ),

            // TODO Fix
            new Uniform(
                54,
                "3/2 3|5",
                "small icosihemidodecahedron",
                "small icosihemidodecacron",
                63, 89
            ),

            // TODO Fix
            new Uniform(
                55,
                "3/2 3 5|",
                "small dodecicosahedron",
                "small dodecicosacron",
                64, 90
            ),

            // (5/4 5 5) (I6c) 

            // TODO Fix
            new Uniform(
                56,
                "5/4 5|5",
                "small dodecahemidodecahedron",
                "small dodecahemidodecacron",
                65, 91
            ),

            // (2 5/2 3) (I7) 

            new Uniform(
                57,
                "3|2 5/2",
                "great stellated dodecahedron",
                "great icosahedron",
                68, 22
            ),
            new Uniform(
                58,
                "5/2|2 3",
                "great icosahedron",
                "great stellated dodecahedron",
                69, 41
            ),
            new Uniform(
                59,
                "2|5/2 3",
                "great icosidodecahedron",
                "great rhombic triacontahedron",
                70, 94
            ),
            new Uniform(
                60,
                "2 5/2|3",
                "great truncated icosahedron",
                "great stellapentakisdodecahedron",
                71, 95
            ),

            // TODO Fix
            new Uniform(
                61,
                "2 5/2 3|",
                "rhombicosahedron",
                "rhombicosacron",
                72, 96
            ),
            new Uniform(
                62,
                "|2 5/2 3",
                "great snub icosidodecahedron",
                "great pentagonal hexecontahedron",
                73, 113
            ),

            // (5/3 2 5) (I9) 

            new Uniform(
                63,
                "2 5|5/3",
                "small stellated truncated dodecahedron",
                "great pentakisdodekahedron",
                74, 97
            ),
            new Uniform(
                64,
                "5/3 2 5|",
                "truncated dodecadodecahedron",
                "medial disdyakistriacontahedron",
                75, 98
            ),
            new Uniform(
                65,
                "|5/3 2 5",
                "inverted snub dodecadodecahedron",
                "medial inverted pentagonal hexecontahedron",
                76, 114
            ),

            // (5/3 5/2 3) (I10a) 

            new Uniform(
                66,
                "5/2 3|5/3",
                "great dodecicosidodecahedron",
                "great dodecacronic hexecontahedron",
                77, 99
            ),

            // TODO Fix
            new Uniform(
                67,
                "5/3 5/2|3",
                "small dodecahemicosahedron",
                "small dodecahemicosacron",
                78, 100
            ),

            // TODO Fix
            new Uniform(
                68,
                "5/3 5/2 3|",
                "great dodecicosahedron",
                "great dodecicosacron",
                79, 101
            ),
            new Uniform(
                69,
                "|5/3 5/2 3",
                "great snub dodecicosidodecahedron",
                "great hexagonal hexecontahedron",
                80, 115
            ),

            // (5/4 3 5) (I10b) 

            // TODO Fix
            new Uniform(
                70,
                "5/4 5|3",
                "great dodecahemicosahedron",
                "great dodecahemicosacron",
                81, 102
            ),

            // (5/3 2 3) (I13) 

            new Uniform(
                71,
                "2 3|5/3",
                "great stellated truncated dodecahedron",
                "great triakisicosahedron",
                83, 104
            ),
            new Uniform(
                72,
                "5/3 3|2",
                "great rhombicosidodecahedron",
                "great deltoidal hexecontahedron",
                84, 105
            ),
            new Uniform(
                73,
                "5/3 2 3|",
                "great truncated icosidodecahedron",
                "great disdyakistriacontahedron",
                87, 108
            ),
            new Uniform(
                74,
                "|5/3 2 3",
                "great inverted snub icosidodecahedron",
                "great inverted pentagonal hexecontahedron",
                88, 116
            ),

            // (5/3 5/3 5/2) (I18a) 

            new Uniform(
                75,
                "5/3 5/2|5/3",
                "great dodecahemidodecahedron",
                "great dodecahemidodecacron",
                86, 107
            ),

            // (3/2 5/3 3) (I18b) 

            // TODO Fix
            new Uniform(
                76,
                "3/2 3|5/3",
                "great icosihemidodecahedron",
                "great icosihemidodecacron",
                85, 106
            ),

            // (3/2 3/2 5/3) (I22) 

            new Uniform(
                77,
                "|3/2 3/2 5/2",
                "small retrosnub icosicosidodecahedron",
                "small hexagrammic hexecontahedron",
                91, 118
            ),

            // (3/2 5/3 2) (I23) 

            // TODO Fix
            new Uniform(
                78,
                "3/2 5/3 2|",
                "great rhombidodecahedron",
                "great rhombidodecacron",
                89, 109
            ),
            new Uniform(
                79,
                "|3/2 5/3 2",
                "great retrosnub icosidodecahedron",
                "great pentagrammic hexecontahedron",
                90, 117
            ),

            // Last But Not Least


            new Uniform(
                80,
                "3/2 5/3 3 5/2",
                "great dirhombicosidodecahedron",
                "great dirhombicosidodecacron"
                , 92, 119)
        };

        public static Uniform[] Platonic =
        {
            Uniforms[6],
            Uniforms[11],
            Uniforms[10],
            Uniforms[28],
            Uniforms[27],
        };

        public static Uniform[] Archimedean =
        {
            Uniforms[7],
            Uniforms[12],
            Uniforms[14],
            Uniforms[13],
            Uniforms[15],
            Uniforms[16],
            Uniforms[17],
            Uniforms[29],
            Uniforms[31],
            Uniforms[30],
            Uniforms[32],
            Uniforms[33],
            Uniforms[34],
        };

        public static Uniform[] KeplerPoinsot =
        {
            Uniforms[40],
            Uniforms[39],
            Uniforms[58],
            Uniforms[57],
        };

        public static Uniform[] Prismatic =
        {
            Uniforms[1],
            Uniforms[2],
            Uniforms[3],
            Uniforms[4],
            Uniforms[5],
        };

        public static Uniform[] Convex = Uniforms.Where(x => !x.Wythoff.Contains("/")).Skip(1).ToArray();
        public static Uniform[] Star = Uniforms.Where(x => x.Wythoff.Contains("/")).ToArray();

    }
}
