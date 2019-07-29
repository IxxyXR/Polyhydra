using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Conway
{
    public static class JohnsonPoly
    {
        public static ConwayPoly MakePolygon(int sides)
        {
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var faceRoles = Enumerable.Repeat(ConwayPoly.Roles.New, 1);
            var vertexRoles = Enumerable.Repeat(ConwayPoly.Roles.New, sides);
            
            faceIndices.Add(new int[sides]);
            
            for (int i = 0; i < sides; i++)
            {
                float theta = Mathf.PI * 2 / sides;
                vertexPoints.Add(new Vector3(Mathf.Cos(theta * i), 0, Mathf.Sin(theta * i)));
                faceIndices[0][i] = i;
            }
            
            return new ConwayPoly(vertexPoints, faceIndices, faceRoles, vertexRoles);
        }
        
        public static ConwayPoly MakePrism(int sides, float height)
        {
            ConwayPoly polygon = MakePolygon(sides);
            return polygon.Extrude(height, false, false);
        }
        
        public static ConwayPoly MakePyramid(int sides, float height)
        {
            return new ConwayPoly();
        }

        public static ConwayPoly MakeDipyramid(int sides)
        {
            float sideLength = 2 * Mathf.Sin(Mathf.PI / sides);
            float capHeight = 0.4f;  // TODO find height to create equilateral triangles
            return MakeDipyramid(sides, sideLength, capHeight);
        }
        
        public static ConwayPoly MakeDipyramid(int sides, float height, float capHeight)
        {
            ConwayPoly polygon = MakePolygon(sides);
            polygon = polygon.Extrude(height, false, false);
            return polygon.Kis(capHeight, ConwayPoly.FaceSelections.All, false);
        }
        
    }
}