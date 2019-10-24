using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        
    }
}