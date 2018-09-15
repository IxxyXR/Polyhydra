using System;
using UnityEngine;

namespace Wythoff {
    public class Vector {
    
        public double x, y, z;

        public Vector(double x, double y, double z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector(Vector a) {
            x = a.x;
            y = a.y;
            z = a.z;
        }

        public double dot(Vector b) {
            return x * b.x + y * b.y + z * b.z;
        }

        public Vector rotate(Vector axis, double angle) {
            Vector a, b, c;
            a = axis.scale(dot(axis));
            b = diff(a).scale(Math.Cos(angle));
            c = axis.cross(this).scale(Math.Sin(angle));
            return a.sum3(b, c);
        }

        public Vector sum3(Vector b, Vector c) {
            return new Vector(x + b.x + c.x, y + b.y + c.y, z + b.z + c.z);
        }

        public Vector scale(double k) {
            return new Vector(x * k, y * k, z * k);
        }

        public Vector3 getVector3() {
            return new Vector3(
                (float) x,
                (float) y,
                (float) z
            );
        }

        public Vector sum(Vector b) {
            return new Vector(x + b.x, y + b.y, z + b.z);
        }

        public Vector diff(Vector b) {
            return new Vector(x - b.x, y - b.y, z - b.z);
        }

        public Vector cross(Vector b) {
            return new Vector(y * b.z - z * b.y, z * b.x - x * b.z, x * b.y - y * b.x);
        }

        public bool same(Vector b, double epsilon) {
            return Math.Abs(x - b.x) < epsilon && Math.Abs(y - b.y) < epsilon && Math.Abs(z - b.z) < epsilon;
        }

        public static Vector rotate(Vector vertex, Vector axis, double angle) {
            Vector temp = new Vector(vertex);
            return temp.rotate(axis, angle);
        }

        public double angle(Vector b) {
            return Math.Acos(
                (x * b.x + y * b.y + z * b.z) / (
                    Math.Sqrt(x * x + y * y + z * z) *
                    Math.Sqrt(b.x * b.x + b.y * b.y + b.z * b.z)
                )
            );
        }

        public static double angle(Vector a, Vector b) {
            return a.angle(b);
        }
    }
}