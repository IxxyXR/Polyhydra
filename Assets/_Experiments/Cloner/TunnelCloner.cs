using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Cloner
{
    public class TunnelCloner : Cloner
    {
        public int length = 50;
        public int sides = 4;
        public int height = 6;
        public Vector2 padding = Vector2.zero;
        public bool useBoundsInPadding = true;
        public Vector3 initialRotation = new Vector3(0, 90, 0);

        protected override int PointCount { get { return length * sides * height; } }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        protected override void CalculatePoints (ref List<Matrix4x4> points)
        {
            if (length < 0 || sides < 3 || height < 0)
                return;

            float lengthPadding = padding.x + ((useBoundsInPadding) ? mesh.bounds.size.x : 0f);
            float heightPadding = padding.y + ((useBoundsInPadding) ? mesh.bounds.size.y : 0f);
            var initialTheta = initialRotation * Mathf.Deg2Rad;
            for (float sideIndex = 0; sideIndex < sides; sideIndex++)
            {
                float sideAngle = (360f * Mathf.Deg2Rad) / sides;
                float theta = sideAngle * sideIndex;
                float sidelength = (heightPadding * height);
                float radius = (sidelength / (2 * Mathf.Tan(Mathf.PI / sides)));
                for (float lengthIndex = 0; lengthIndex < length; lengthIndex++)
                {
                    for (float heightIndex = 0; heightIndex < height; heightIndex++)
                    {
                        var index = sideIndex + sides * (lengthIndex + length * heightIndex);
                        var p = new Vector3(
                            heightIndex * heightPadding - ((height * heightPadding)/2f) + 0.5f,
                            0,
                            lengthIndex * lengthPadding
                        );
                        p = RotatePointAroundPivot(p + new Vector3(0, radius, 0), Vector3.zero, new Vector3(0, 0, Mathf.Rad2Deg * theta));

                        points[Mathf.FloorToInt(index)] = Matrix4x4.TRS(
                            transform.position + transform.rotation * p,
                            transform.rotation * Quaternion.Euler(initialTheta) * Quaternion.Euler(Mathf.Rad2Deg * ((Mathf.PI / 2f) - theta), 0, 0),
                            transform.localScale
                        );
                    }
                }
            }
        }
    }
}