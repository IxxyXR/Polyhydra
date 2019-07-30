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
            var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.New, 1);
            var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, sides);
            
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
        
        public static ConwayPoly MakePrism(int sides, float height)
        {
            ConwayPoly polygon = MakePolygon(sides);
            return polygon.Extrude(height, false, false);
        }

        public static ConwayPoly MakeCupola(int sides)
        {
            float height = 0.5f; // TODO
            return MakeCupola(sides, height);
        }

        public static ConwayPoly MakeCupola(int sides, float height)
        {
            if (sides < 6) sides = 6;
            ConwayPoly poly = MakePolygon(sides, false);
            ConwayPoly top = MakePolygon(sides/2, true, 0.25f, height,0.5f);
            poly.Append(top);
            
            int i = 0;
            var edge1 = poly.Halfedges[0];
            var edge2 = poly.Halfedges[sides];
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
                poly.FaceRoles.Add(ConwayPoly.Roles.NewAlt);

                i++;
                edge1 = edge1.Next.Next;
                edge2 = edge2.Prev;
                if (i == sides/2) break;

            }
            poly.Halfedges.MatchPairs();
            return poly;
        }
        
        public static ConwayPoly MakeAntiprism(int sides)
        {
            float height = 1; // TODO
            return MakeAntiprism(sides, height);
        }

        public static ConwayPoly MakeAntiprism(int sides, float height)
        {
            ConwayPoly poly = MakePolygon(sides, false);
            ConwayPoly top = MakePolygon(sides, true, 0.5f, height);
            poly.Append(top);
            
            int i = 0;
            var edge1 = poly.Halfedges[0];
            var edge2 = poly.Halfedges[sides];
            while (true)
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

                i++;
                edge1 = edge1.Next;
                edge2 = edge2.Prev;
                if (i == sides) break;

            }
            
            poly.Halfedges.MatchPairs();
            return poly;
        }

        public static ConwayPoly MakePyramid(int sides)
        {
            float height = 1f; // TODO calculate correct height
            return MakePyramid(sides, height);
        }
        
        public static ConwayPoly MakePyramid(int sides, float height)
        {
            ConwayPoly polygon = MakePolygon(sides, true);
            var poly = polygon.Kis(height, ConwayPoly.FaceSelections.All, false);
            var baseVerts = poly.Vertices.GetRange(0, sides);
            baseVerts.Reverse();
            poly.Faces.Add(baseVerts);
            poly.FaceRoles.Add(ConwayPoly.Roles.Existing);
            poly.Halfedges.MatchPairs();
            return poly;
        }

        public static ConwayPoly MakeDipyramid(int sides)
        {
            float sideLength = 2 * Mathf.Sin(Mathf.PI / sides);
            return MakeDipyramid(sides, sideLength);
        }
        
        public static ConwayPoly MakeDipyramid(int sides, float height)
        {
            ConwayPoly poly = MakePyramid(sides, height);
            poly = poly.Kis(height, ConwayPoly.FaceSelections.Existing, false);
            return poly;
        }
        
    }
}