using System;
using System.Collections.Generic;

namespace Wythoff {
	public class Face {
    
		public List<int> points;  // Indexes into the Vertices array of the polyhedra
		public Fraction frax;
		public double configuration;
		public Vector center;
		public int centerPoint;
		public int[] triangles;  // Indexes into the points array

		public Face(Vector face, Vector vertex, double configuration)  {

			Vector center;
        
			double angle = Vector.angle(face, vertex);
			double r = Math.Sqrt(face.x * face.x + face.y * face.y + face.z * face.z);
			double r0 = Math.Sqrt(vertex.x * vertex.x + vertex.y * vertex.y + vertex.z * vertex.z);
        
			if (Math.Abs(r - r0) < WythoffPoly.DBL_EPSILON) {
				center = new Vector(
					face.x * Math.Cos(angle),
					face.y * Math.Cos(angle),
					face.z * Math.Cos(angle)
				);
			} else {
				center = new Vector(
					face.x * (r0 / r) * Math.Cos(angle),
					face.y * (r0 / r) * Math.Cos(angle),
					face.z * (r0 / r) * Math.Cos(angle)
				);
			}
			this.configuration = configuration;
			this.center = center;
			frax = new Fraction(configuration);
			points = new List<int>();
		}

		public int GetCorners() {
			return (int)frax.n;
		}
    
		public void SetPoint(int p) {
			points.Add(p);
		}
	
		public void CalcTriangles() {  // Only used if we aren't triangulating via the ConwayPoly
		
			var ret = new List<int>();
		
			if (frax.d == 1 || frax.d == frax.n - 1) {
				if (frax.n == 3) {
				
					// Simple Triangle
	            
					ret.Add(points[0]);
					ret.Add(points[1]);
					ret.Add(points[2]);
				
					ret.Add(points[0]);
					ret.Add(points[2]);
					ret.Add(points[1]);
				
				} else {
				
					// Convex Poly
				
					// TODO If we don't have a valid centerPoint then use the first point
					int _centerPoint = centerPoint == -1 ? points[0] : centerPoint;
				
					int previous = -1;
					foreach (int p in points) {
					
						if (previous < 0) {
							previous = p;
						} else {
						
							ret.Add(previous);
							ret.Add(_centerPoint);
							ret.Add(p);
						
							ret.Add(previous);
							ret.Add(p);
							ret.Add(_centerPoint);
						
							previous = p;
						}
					}
				
					ret.Add(previous);
					ret.Add(_centerPoint);
					ret.Add(points[0]);
				
					ret.Add(previous);
					ret.Add(points[0]);
					ret.Add(_centerPoint);
				
				}
			} else {
			
				// Concave Poly
			
				int previous = -1;
			
				foreach (int p in points) {
				
					if (previous < 0) {
					
						ret.Add(centerPoint);
						ret.Add(points[points.Count - 2]);
						ret.Add(p);
			
						ret.Add(centerPoint);
						ret.Add(p);
						ret.Add(points[points.Count - 2]);
					
						previous = p;
					
					} else {
						
						ret.Add(centerPoint);
						ret.Add(previous);
						ret.Add(p);
					
						ret.Add(centerPoint);
						ret.Add(p);
						ret.Add(previous);

						previous = p;
					}
				}
			
			}
			triangles = ret.ToArray();
		}
	
		public bool VertexExists(int vertex) {
			foreach (int i in points) {
				if (i == vertex) {
					return true;
				}
			}
			return false;
		}
	}
}