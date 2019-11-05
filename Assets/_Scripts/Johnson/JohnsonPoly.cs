using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wythoff;

namespace Conway
{
    public static class JohnsonPoly
    {
        public static ConwayPoly MakePolygon(int sides, bool flip=false, float angleOffset = 0, float heightOffset = 0, float radius=1)
        {
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 1);
            var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, sides);
            
            faceIndices.Add(new int[sides]);
            
            float theta = Mathf.PI * 2 / sides;
            
            int start, end, inc;
            
            if (flip)
            {
                start = sides - 1;
                end = -1;
                inc = -1;
            }
            else
            {
                start = 0;
                end = sides;
                inc = 1;
            }
            
            for (int i = start; i != end; i += inc)
            {
                float angle = theta * i + (theta * angleOffset);
                vertexPoints.Add(new Vector3(Mathf.Cos(angle) * radius, heightOffset, Mathf.Sin(angle) * radius));
                faceIndices[0][i] = i;
            }
            
            return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
        }



        public static ConwayPoly MakeCupola(int sides, float height, bool bi=false)
        {

            if (sides < 3) sides = 3;

            ConwayPoly poly = MakePolygon(sides * 2);
            Face bottom = poly.Faces[0];
            ConwayPoly top1 = MakePolygon(sides, true, 0.25f, height,0.5f);
            poly.Append(top1);

            int i = 0;
            var squareSideFaces = new List<Face>();
            var edge1 = poly.Halfedges[0];
            var edge2 = poly.Halfedges[sides * 2];
            while (true)
            {
                var side1 = new List<Vertex>
                {
                    edge1.Next.Vertex,
                    edge1.Vertex,
                    edge2.Prev.Vertex
                };
                poly.Faces.Add(side1);
                poly.FaceRoles.Add(ConwayPoly.Roles.New);
                
                var side2 = new List<Vertex>
                {
                    edge1.Vertex,
                    edge1.Prev.Vertex,
                    edge2.Vertex,
                    edge2.Prev.Vertex
                };
                poly.Faces.Add(side2);
                squareSideFaces.Add(poly.Faces.Last());
                poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);

                i++;
                edge1 = edge1.Next.Next;
                edge2 = edge2.Prev;
                if (i == sides) break;
            }

            if (bi)
            {
                ConwayPoly top2 = MakePolygon(sides, false, 0.75f, -height, 0.5f);
                poly.Append(top2);

                i = 0;
                var middleVerts = bottom.GetVertices();
                poly.Faces.Remove(bottom);
                edge2 = poly.Faces.Last().Halfedge.Prev;
                while (true)
                {
                    var side1 = new List<Vertex>
                    {
                        middleVerts[PolyUtils.ActualMod(i * 2 - 1, sides * 2)],
                        middleVerts[PolyUtils.ActualMod(i * 2, sides * 2)],
                        edge2.Vertex
                    };
                    poly.Faces.Add(side1);
                    poly.FaceRoles.Add(ConwayPoly.Roles.New);

                    var side2 = new List<Vertex>
                    {
                        middleVerts[PolyUtils.ActualMod(i * 2, sides * 2)],
                        middleVerts[PolyUtils.ActualMod(i * 2 + 1, sides * 2)],
                        edge2.Next.Vertex,
                        edge2.Vertex,
                    };
                    poly.Faces.Add(side2);
                    poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);

                    i++;
                    edge2 = edge2.Next;

                    if (i == sides) break;

                }
            }

            poly.Halfedges.MatchPairs();
            return poly;
        }

        // Work in progress. Not working at the moment
        public static ConwayPoly MakeRotunda(int sides, float height, bool bi=false)
        {

            if (sides < 3) sides = 3;

            ConwayPoly poly = MakePolygon(sides);
            Face bottom = poly.Faces[0];
            ConwayPoly top1 = MakePolygon(sides, true, 0.25f, height,0.5f);
            poly.Append(top1);

            int i = 0;
//            var upperTriFaces = new List<Face>();
//            var LowerTriFaces = new List<Face>();
//            var SidePentFaces = new List<Face>();

            var edge1 = poly.Halfedges[0];
            var edge2 = poly.Halfedges[sides * 2];

            while (true)
            {
                poly.Vertices.Add(new Vertex(Vector3.Lerp(edge1.Vector, edge2.Vector, 0.5f)));
                var newV1 = poly.Vertices.Last();
                poly.Vertices.Add(new Vertex(Vector3.Lerp(edge1.Prev.Vector, edge2.Next.Vector, 0.5f)));
                var newV2 = poly.Vertices.Last();

                var pentFace = new List<Vertex>
                {
                    edge1.Next.Vertex,
                    edge1.Vertex,
                    newV1,
                };
                poly.Faces.Add(pentFace);
                poly.FaceRoles.Add(ConwayPoly.Roles.New);

//                var upperTriFace = new List<Vertex>
//                {
//                    edge1.Vertex,
//                    edge1.Prev.Vertex,
//                    newV
//                };
//                poly.Faces.Add(upperTriFace);
//                //upperTriFaces.Add(poly.Faces.Last());
//                poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);
//
//                var lowerTriFace = new List<Vertex>
//                {
//                    newV,
//                    edge2.Vertex,
//                    edge2.Prev.Vertex
//                };
//                poly.Faces.Add(lowerTriFace);
//                //lowerTriFaces.Add(poly.Faces.Last());
//                poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);

                i++;
                edge1 = edge1.Next.Next;
                edge2 = edge2.Prev;
                if (i == sides) break;
            }

            poly.Halfedges.MatchPairs();
            return poly;
        }


        public static ConwayPoly MakePrism(int sides)
        {
            float height = SideLength(sides);
            return MakePrism(sides, height);
        }

        public static ConwayPoly MakeAntiprism(int sides)
        {
            float height = SideLength(sides) * Mathf.Sqrt(0.75f);
            return MakePrism(sides, height, true);
        }

        public static ConwayPoly MakePrism(int sides, float height, bool anti=false)
        {
            ConwayPoly poly = MakePolygon(sides);
            ConwayPoly top = MakePolygon(sides, true, anti?0.5f:0, height);
            poly.Append(top);
            
            int i = 0;
            var edge1 = poly.Halfedges[0];
            var edge2 = poly.Halfedges[sides];
            while (true)
            {

                if (anti)
                {
                    var side1 = new List<Vertex>
                    {
                        edge1.Vertex,
                        edge1.Prev.Vertex,
                        edge2.Vertex
                    };
                    poly.Faces.Add(side1);
                    poly.FaceRoles.Add(ConwayPoly.Roles.New);

                    var side2 = new List<Vertex>
                    {
                        edge1.Vertex,
                        edge2.Vertex,
                        edge2.Prev.Vertex
                    };
                    poly.Faces.Add(side2);
                    poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);
                }
                else
                {
                    var side = new List<Vertex>
                    {
                        edge1.Vertex,
                        edge1.Prev.Vertex,
                        edge2.Vertex,
                        edge2.Prev.Vertex
                    };
                    poly.Faces.Add(side);
                    poly.FaceRoles.Add(ConwayPoly.Roles.New);

                }

                i++;
                edge1 = edge1.Next;
                edge2 = edge2.Prev;

                if (i == sides) break;

            }
            
            poly.Halfedges.MatchPairs();
            return poly;
        }

        public static float CalcPyramidHeight(float sides)
        {
            float sideLength = SideLength(sides);
            float height;

            // Try and make equilateral sides if we can
            if (sides >= 3 && sides <= 5)
            {
                height = Mathf.Sqrt(Mathf.Pow(sideLength, 2) - 1f);
            }
            else
            {
                height = 1f;
            }

            return height;
        }
        public static ConwayPoly MakePyramid(int sides)
        {
            var height = CalcPyramidHeight(sides);
            return MakePyramid(sides, height);
        }
        
        public static ConwayPoly MakePyramid(int sides, float height)
        {
            ConwayPoly polygon = MakePolygon(sides, true);
            var poly = polygon.Kis(height, ConwayPoly.FaceSelections.All, false);
            var baseVerts = poly.Vertices.GetRange(0, sides);
            baseVerts.Reverse();
            poly.Faces.Insert(0, baseVerts);
            poly.FaceRoles.Insert(0, ConwayPoly.Roles.Existing);
            poly.Halfedges.MatchPairs();
            return poly;
        }

        // Base forms are pyramids, cupolae and rotundae.
        // For each base we have elongate and bi. For each bi form we can also gyroelongate
        // Bi can come in ortho and gyro flavours in most cases.
        // You can combine bases i.e. cupolarotunda. These also come in ortho and gyro flavours.
        // Gyrobifastigium is just trying to be weird
        // Prisms can be augmented and diminished. Also bi, tri, para and meta
        // Truncation is a thing.and can be combined with augment/diminish.
        // Phew! Then stuff gets weirder.

        public static ConwayPoly MakeElongatedPyramid(int sides)
        {
					float height = SideLength(sides);
					ConwayPoly poly = MakePrism(sides, height);
					
					height = CalcPyramidHeight(sides);
					poly = poly.Kis(height, ConwayPoly.FaceSelections.FacingUp, false);
					
					return poly;
        }
//        public static ConwayPoly MakeElongatedBipyramid(int sides)
//        {
//        }
//        public static ConwayPoly MakeGyroelongatedPyramid(int sides)
//        {
//        }
//        public static ConwayPoly MakeGyroelongatedBipyramid(int sides)
//        {
//        }
//
//        public static ConwayPoly MakeElongatedCupola(int sides)
//        {
//        }
//        public static ConwayPoly MakeElongatedBicupola(int sides)
//        {
//        }
//        public static ConwayPoly MakeGyroelongatedCupola(int sides)
//        {
//        }
//        public static ConwayPoly MakeGyroelongatedBicupola(int sides)
//        {
//        }

        public static float SideLength(float sides)
        {
            return 2 * Mathf.Sin(Mathf.PI / sides);
        }

        public static ConwayPoly MakeDipyramid(int sides)
        {
            float height = CalcPyramidHeight(sides);
            return MakeDipyramid(sides, height);
        }
        
        public static ConwayPoly MakeDipyramid(int sides, float height)
        {
            ConwayPoly poly = MakePyramid(sides, height);
            poly = poly.Kis(height, ConwayPoly.FaceSelections.Existing, false);
            return poly;
        }

        public static ConwayPoly MakeCupola(int sides)
        {
            float height = CalcPyramidHeight(sides) / 2f;
            return MakeCupola(sides, height);
        }

        public static ConwayPoly MakeBicupola(int sides)
        {
            float height = CalcPyramidHeight(sides);
            return MakeBicupola(sides, height);
        }

        public static ConwayPoly MakeBicupola(int sides, float height)
        {
            if (sides < 6) sides = 6;
            ConwayPoly poly = MakePolygon(sides);
            poly = MakeCupola(sides, height/2, true);
            return poly;
        }

        public static ConwayPoly MakeRotunda()
        {
            var wythoffPoly = new WythoffPoly(Uniform.Uniforms[29].Wythoff);
            wythoffPoly.BuildFaces();

            var conwayPoly = new ConwayPoly(wythoffPoly);
            conwayPoly = conwayPoly.FaceRemove(ConwayPoly.FaceSelections.FacingDown, false);
            conwayPoly.FillHoles();
            return conwayPoly;
        }

        public static ConwayPoly MakeL()
        {
            var verts = new List<Vector3>();
            for (var i = -0.25f; i <=0.25f; i+=0.5f)
            {
                verts.Add(new Vector3(0, i, 0));
                verts.Add(new Vector3(0.5f, i, 0));
                verts.Add(new Vector3(0.5f, i, -0.5f));
                verts.Add(new Vector3(-0.5f, i, -0.5f));
                verts.Add(new Vector3(-0.5f, i, 0.5f));
                verts.Add(new Vector3(0, i, 0.5f));
            }

            var faces = new List<List<int>>
            {
                new List<int>{0, 5, 4, 3, 2, 1},
                new List<int>{6, 7, 8, 9, 10, 11},
                new List<int>{3, 9, 8, 2},
                new List<int>{2, 8, 7, 1},
                new List<int>{1, 7, 6, 0},
                new List<int>{0, 6, 11, 5},
                new List<int>{5, 11, 10, 4},
                new List<int>{4, 10, 9, 3}
            };

            var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 8);
            var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 12);

            return new ConwayPoly(verts, faces, faceRoles, vertexRoles);


        }
				
				public static ConwayPoly MakeL1()
				{
					var verts = new List<Vector3>();
					for (var i = -0.25f; i <= 0.25f; i+=0.5f)
					{
						verts.Add(new Vector3(0, i, 0));
						verts.Add(new Vector3(0.5f, i, 0));
						verts.Add(new Vector3(0.5f, i, -0.5f));
						verts.Add(new Vector3(-0.5f, i, -0.5f));
						verts.Add(new Vector3(-0.5f, i, 0.5f));
						verts.Add(new Vector3(0, i, 0.5f));
					}
					
					var faces = new List<List<int>>
					{
						new List<int>{0, 5, 4, 3},
						new List<int>{0, 3, 2, 1},
						new List<int>{6, 7, 8, 9},
						new List<int>{6, 9, 10, 11},
						new List<int>{3, 9, 8, 2},
						new List<int>{2, 8, 7, 1},
						new List<int>{1, 7, 6, 0},
						new List<int>{0, 6, 11, 5},
						new List<int>{5, 11, 10, 4},
						new List<int>{4, 10, 9, 3}
					};
					
					var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 10);
					var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 12);
					
					return new ConwayPoly(verts, faces, faceRoles, vertexRoles);
				}
				
				public static ConwayPoly MakeL2()
				{
					var verts = new List<Vector3>();
					for (var i = -0.25f; i <= 0.25f; i+=0.5f)
					{
						verts.Add(new Vector3(0, i, 0));
						verts.Add(new Vector3(0.5f, i, 0));
						verts.Add(new Vector3(0.5f, i, -0.5f));
						verts.Add(new Vector3(0, i, -0.5f));
						verts.Add(new Vector3(-0.5f, i, -0.5f));
						verts.Add(new Vector3(-0.5f, i, 0));
						verts.Add(new Vector3(-0.5f, i, 0.5f));
						verts.Add(new Vector3(0, i, 0.5f));
					}
					
					var faces = new List<List<int>>
					{
						new List<int>{0, 7, 6, 5},
						new List<int>{0, 5, 4, 3},
						new List<int>{0, 3, 2, 1},
						new List<int>{8, 9, 10, 11},
						new List<int>{8, 11, 12, 13},
						new List<int>{8, 13, 14, 15},
						new List<int>{4, 12, 11, 10, 2, 3},
						new List<int>{2, 10, 9, 1},
						new List<int>{1, 9, 8, 0},
						new List<int>{0, 8, 15, 7},
						new List<int>{7, 15, 14, 6},
						new List<int>{6, 14, 13, 12, 4, 5}
					};
					
					var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 12);
					var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.Existing, 16);
					
					return new ConwayPoly(verts, faces, faceRoles, vertexRoles);
				}
    }
}