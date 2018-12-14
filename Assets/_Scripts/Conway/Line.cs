using UnityEngine;
using UnityEngine.UI;

namespace Conway {
    
    public class Line {
        
        public Vertex start;
        public Vertex end;
        
        public Line(Vertex start, Vertex end) {
            this.start = start;
            this.end = end;
        }
        
        public Line(Vector3 start, Vector3 end) {
            this.start = new Vertex(start);
            this.end = new Vertex(end);
        }
        
        public Line(Halfedge halfEdge) {
            this.start = halfEdge.Vertex;
            this.end = halfEdge.Next.Vertex;
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
        
        public Vector3 ClosestPoint(Vector3 vPoint)
        {
            var vVector1 = vPoint - start.Position;
            var vVector2 = (end.Position - start.Position).normalized;
 
            var d = Vector3.Distance(start.Position, end.Position);
            var t = Vector3.Dot(vVector2, vVector1);
 
            if (t <= 0) return start.Position;
            if (t >= d) return end.Position;
 
            var vVector3 = vVector2 * t;
            var vClosestPoint = start.Position + vVector3;

            return vClosestPoint;
        }
    }
}