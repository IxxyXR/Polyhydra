using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
    public class LookatModifier : PointModifier
    {
        public Vector3 rotation;

        public override List<Matrix4x4> Modify (List<Matrix4x4> points)
        {

            for (int i = 0; i < points.Count; i++)
            {
                var samplePosition = transform.worldToLocalMatrix.MultiplyPoint3x4 ((Vector3)points[i].GetColumn (3));
                points[i] *= Matrix4x4.Rotate (Quaternion.LookRotation(Vector3.up, samplePosition));
            }

            return points;
        }
    }
}