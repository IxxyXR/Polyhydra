using System;
using UnityEngine;

[Serializable]
public class PolyPreset {

	public string Name;

	[Serializable]
	public enum PolyTypes {
		Pentagonal_Prism,
		Pentagonal_Antiprism,
		Pentagrammic_Prism,
		Pentagrammic_Antiprism,
		Pentagrammic_Crossed_Antiprism,
		Tetrahedron,
		Truncated_Tetrahedron,
		Octahemioctahedron,
		Tetrahemihexahedron,
		Octahedron,
		Cube,
		Cuboctahedron,
		Truncated_Octahedron,
		Truncated_Cube,
		Rhombicuboctahedron,
		Truncated_Cuboctahedron,
		Snub_Cube,
		Small_Cubicuboctahedron,
		Great_Cubicuboctahedron,
		Cubohemioctahedron,
		Cubitruncated_Cuboctahedron,
		Great_Rhombicuboctahedron,
		Small_Rhombihexahedron,
		Stellated_Truncated_Hexahedron,
		Great_Truncated_Cuboctahedron,
		Great_Rhombihexahedron,
		Icosahedron,
		Dodecahedron,
		Icosidodecahedron,
		Truncated_Icosahedron,
		Truncated_Dodecahedron,
		Rhombicosidodecahedron,
		Truncated_Icosidodechedon,
		Snub_Dodecahedron,
		Small_Ditrigonal_Icosidodecahedron,
		Small_Icosicosidodecahedron,
		Small_Snub_Icosicosidodecahedron,
		Small_Dodecicosidodecahedron,
		Small_Stellated_Dodecahedron,
		Great_Dodecahedron,
		Great_Dodecadodecahedron,
		Truncated_Great_Dodecahedron,
		Rhombidodecadodecahedron,
		Small_Rhombidodecahedron,
		Snub_Dodecadodecahedron,
		Ditrigonal_Dodecadodecahedron,
		Great_Ditrigonal_Dodecicosidodecahedron,
		Small_Ditrigonal_Dodecicosidodecahedron,
		Icosidodecadodecahedron,
		Icositruncated_Dodecadodecahedron,
		Snub_Icosidodecadodecahedron,
		Great_Ditrigonal_Icosidodecahedron,
		Great_Icosicosidodecahedron,
		Small_Icosihemidodecahedron,
		Small_Dodecicosahedron,
		Small_Dodecahemidodecahedron,
		Great_Stellated_Dodecahedron,
		Great_Icosahedron,
		Great_Icosidodecahedron,
		Great_Truncated_Icosahedron,
		Rhombicosahedron,
		Great_Snub_Icosidodecahedron,
		Small_Stellated_Truncated_Dodecahedron,
		Truncated_Dodecadodecahedron,
		Inverted_Snub_Dodecadodecahedron,
		Great_Dodecicosidodecahedron,
		Small_Dodecahemicosahedron,
		Great_Dodecicosahedron,
		Great_Snub_Dodecicosidodecahedron,
		Great_Dodecahemicosahedron,
		Great_Stellated_Truncated_Dodecahedron,
		Great_Rhombicosidodecahedron,
		Great_Truncated_Icosidodecahedron,
		Great_Inverted_Snub_Icosidodecahedron,
		Great_Dodecahemidodecahedron,
		Great_Icosihemidodecahedron,
		Small_Retrosnub_Icosicosidodecahedron,
		Great_Rhombidodecahedron,
		Great_Retrosnub_Icosidodecahedron,
		Great_Dirhombicosidodecahedron
	}
	
	public PolyTypes PolyType;
	public bool BypassConway;
	public bool TwoSided;
	
	[Serializable]
	public enum Ops {
		Identity = 0,
		Foo = 12,
		Kis = 1,
		Kis3 = 13,
		Kis4 = 14,
		Kis5 = 15,
		Kis6 = 16,
		Kis8 = 17,
		Dual = 2,
		Ambo = 3,
		Zip = 4,
		Expand = 5,
		Bevel = 6,
		Join = 7,
		Needle = 8,
		Ortho = 9,
		Meta = 10,
		Truncate = 11
	}
	
	[Serializable]
	public struct ConwayOperator {  
		public Ops op;
		public float amount;
		public bool disabled;
	}
	
	public ConwayOperator[] ConwayOperators;
	public double OffsetAmount;
	public float RibbonAmount;
	public double ExtrudeAmount;
		
	[Header("Gizmos")]
	public bool vertexGizmos;
	public bool faceCenterGizmos;
	public bool edgeGizmos;
	public bool faceGizmos;
	public int[] faceGizmosList;
	public bool dualGizmo;
	
	public Color[] gizmoPallette = {
		Color.red,
		Color.yellow,
		Color.green,
		Color.cyan,
		Color.blue,
		Color.magenta
	};
    
}
