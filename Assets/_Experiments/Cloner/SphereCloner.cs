using UnityEngine;
using System.Collections.Generic;

namespace Cloner
{
    public class SphereCloner : Cloner
    {
        public Vector3Int count = new Vector3Int (3, 3, 3);
        public float radiusStart = 1f;
        public float radiusEnd = 2f;

        protected override int PointCount { get { return count.x * count.y * count.z; } }

        protected override void CalculatePoints (ref List<Matrix4x4> points)
        {
            if (count.x < 0 || count.y < 0 || count.z < 0) return;

            for (float z = 0; z < count.z; z++)
            {
                var w = z / count.z;
                float radiusRange = radiusEnd - radiusStart;
                float radius = w * radiusRange + radiusStart;

                for (float y = 0; y < count.y; y++)
                {
                    var v = y / count.y;

                    for (float x = 0; x < count.x; x++)
                    {
                        var u = x / count.x;

                        int index = (int)(x + count.x * (y + count.y * z));
                        var p = new Vector3
                                (
                                    Mathf.Sin(Mathf.PI * v) * Mathf.Cos(2f * Mathf.PI * u),
                                    Mathf.Sin(Mathf.PI * v) * Mathf.Sin(2f * Mathf.PI * u),
                                    Mathf.Cos(Mathf.PI * v)
                                ) * radius;
                        points[index] = Matrix4x4.TRS (transform.position + transform.rotation * p, transform.rotation, transform.localScale);
                    }
                }
            }
        }
    }
}