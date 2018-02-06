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

namespace Polylib {
	public class Uniforms {
	
		public string Wythoff, name, dual;
		public int Coxeter, Wenninger;
	
		public Uniforms(string wythoff, string name, string dual, int coxeter, int wenninger) {
			Wythoff = wythoff;
			this.name = name;
			this.dual = dual;
			Coxeter = coxeter;
			Wenninger = wenninger;
		}		
	}
}
