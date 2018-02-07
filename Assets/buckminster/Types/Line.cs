using UnityEngine;
using UnityEngine.UI;

namespace Buckminster.Types {
    
    public class Line {
        
        public Vertex start;
        public Vertex end;
        
        public Line(Vertex start, Vertex end) {
            this.start = start;
            this.end = end;
        }

        public Vertex PointAt(double p) {
            // TODO
            return start;
        }

        public double Intersect(Line l) {
            // TODO
            return 1.0;
        }

        public Vector3 Direction() {
            return (end.Position - start.Position).normalized;
        }

        public double Distance() {
            return Direction().magnitude;
        }

        public bool Intersect(out Vector3 intersection, Line line2){
            
            //Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2
 
            Vector3 lineVec3 = line2.start.Position - start.Position;
            Vector3 crossVec1and2 = Vector3.Cross(Direction(), line2.Direction());
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, line2.Direction());
 
            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);
 
            //is coplanar, and not parallel
            if(Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                intersection = start.Position + Direction() * s;
                return true;
            } else {
                intersection = Vector3.zero;
                return false;
            }
        }
        
    }
}