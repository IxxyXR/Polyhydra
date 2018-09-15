using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Buckminster.Types
{
    public partial class ConwayHart
    {
        private Dictionary<dynamic, Dictionary<dynamic, dynamic>> flags;
        private List<List<dynamic>> faces;
        private Dictionary<dynamic, Vector3> positions;

        public ConwayHart()
        {
            flags = new Dictionary<dynamic, Dictionary<dynamic, dynamic>>();
            faces = new List<List<dynamic>>();
            positions = new Dictionary<dynamic, Vector3>();
        }

        public ConwayHart(List<Vector3> positions, List<List<dynamic>> faces)
        {
            flags = new Dictionary<dynamic, Dictionary<dynamic, dynamic>>();
            
            this.positions = new Dictionary<dynamic, Vector3>();

            for (int i = 0; i < positions.Count; i++)
            {
                this.positions[i] = positions[i];
            }

            this.faces = faces;
        }

        public ConwayHart(Dictionary<dynamic, Vector3> positions, List<List<dynamic>> faces)
        {
            this.positions = positions;
            this.faces = faces;
        }

        public void newFlag(dynamic face, dynamic v1, dynamic v2)
        {
            if (!flags.ContainsKey(face))
            {
                flags[face] = new Dictionary<dynamic, dynamic>();
            }
            
            flags[face][v1] = v2;
        }

        public void newV(dynamic name, Vector3 p)
        {
            positions[name] = p;
        }

        public ConwayHart flags2poly()
        {
            var rpositions = new Dictionary<dynamic, Vector3>();
            var verts = new List<int>();

            for (int i = 0; i < positions.Count; i++)
            {
                verts[i] = rpositions.Count;
                rpositions[i] = positions[i];
            }

            var rfaces = new List<List<dynamic>>();

            for (int i = 0; i < flags.Count; i++)
            {
                var flag = flags[i];
                var f = new List<dynamic>();
                var v0 = flag.Keys.ToList()[0];
                var v = v0;
                do
                {
                    var foo = verts[v];
                    f.Add(foo);
                    v = flag[v];
                } while (v != v0);

                rfaces.Add(f);
            }

            return new ConwayHart(rpositions, rfaces);
        }

        public ConwayHart Dual()
        {
            var dpoly = makeDual(this);
            dpoly.positions = faceCenters();
            return dpoly;
        }

        public ConwayHart canonicalize()
        {
            var poly = this.clone();
            canonicalpositions(poly, 3); /// 3?
            return poly;
        }

        public ConwayHart kis(int n = 0)
        {
            // only kis n-sided faces, but n==0 means kiss all.
            var result = new ConwayHart();
            for (var i = 0; i < positions.Count; i++)
            {
                result.newV("v" + i, positions[i]); // each old vertex is a new vertex
            }

            var centers = faceCenters(); // new vertices in centers of n-sided face
            var foundAny = false; // alert if don't find any
            for (var i = 0; i < faces.Count; i++)
            {
                var v1 = "v" + faces[i][faces[i].Count - 1]; // previous vertex
                for (var j = 0; j < faces[i].Count; j++)
                {
                    var v2 = "v" + faces[i][j]; // this vertex
                    if (faces[i].Count == n || n == 0)
                    {
                        // kiss the n's, or all
                        foundAny = true; // flag that we found some
                        result.newV("f" + i, centers[i]); // new vertex in face center
                        var fname = i + v1;
                        result.newFlag(fname, v1, v2); // three new flags, if n-sided
                        result.newFlag(fname, v2, "f" + i);
                        result.newFlag(fname, "f" + i, v1);
                    }
                    else
                        result.newFlag(i, v1, v2); // same old flag, if non-n

                    v1 = v2; // current becomes previous
                }
            }

            var ans = result.flags2poly();
            adjustpositions(ans, 3); // adjust and
            return ans;
        }

        public ConwayHart ambo()
        {
            // compute ambo of argument
            var result = new ConwayHart();
            for (var i = 0; i < faces.Count; i++)
            {
                var v1 = faces[i][faces[i].Count - 2]; // preprevious vertex
                var v2 = faces[i][faces[i].Count - 1]; // previous vertex
                for (var j = 0; j < faces[i].Count; j++)
                {
                    var v3 = faces[i][j]; // this vertex
                    if (v1 < v2) // new vertices at edge midpoints
                        result.newV(midName(v1, v2), midpoint(positions[v1], positions[v2]));
                    result.newFlag("f" + i, midName(v1, v2), midName(v2, v3)); // two new flags
                    result.newFlag("v" + v2, midName(v2, v3), midName(v1, v2));
                    v1 = v2; // shift over one
                    v2 = v3;
                }
            }

            var ans = result.flags2poly();
            adjustpositions(ans, 2); // canonicalize lightly
            return ans;
        }

        public string midName(int v1, int v2)
        {
            // unique symbolic name, e.g. "1_2"
            if (v1 < v2)
                return (v1 + "_" + v2);
            else
                return (v2 + "_" + v1);
        }

        public ConwayHart gyro()
        {
            // compute gyro of argument
            var result = new ConwayHart();
            for (var i = 0; i < positions.Count; i++)
                result.newV("v" + i, unit(positions[i])); // each old vertex is a new vertex
            var centers = faceCenters(); // new vertices in center of each face
            for (var i = 0; i < faces.Count; i++)
                result.newV("f" + i, unit(centers[i]));
            for (var i = 0; i < faces.Count; i++)
            {
                var v1 = faces[i][faces[i].Count - 2]; // preprevious vertex
                var v2 = faces[i][faces[i].Count - 1]; // previous vertex
                for (var j = 0; j < faces[i].Count; j++)
                {
                    var v3 = faces[i][j]; // this vertex
                    result.newV(v1 + "~" + v2, oneThird(positions[v1], positions[v2])); // new v in face
                    var fname = i + "f" + v1;
                    result.newFlag(fname, "f" + i, v1 + "~" + v2); // five new flags
                    result.newFlag(fname, v1 + "~" + v2, v2 + "~" + v1);
                    result.newFlag(fname, v2 + "~" + v1, "v" + v2);
                    result.newFlag(fname, "v" + v2, v2 + "~" + v3);
                    result.newFlag(fname, v2 + "~" + v3, "f" + i);
                    v1 = v2; // shift over one
                    v2 = v3;
                }
            }

            var ans = result.flags2poly();
            adjustpositions(ans, 3); // canonicalize lightly
            return ans;
        }

        public ConwayHart propellor()
        {
            // compute propellor of argument
            var result = new ConwayHart();
            for (var i = 0; i < positions.Count; i++)
                result.newV("v" + i, unit(positions[i])); // each old vertex is a new vertex
            for (var i = 0; i < faces.Count; i++)
            {
                var v1 = faces[i][faces[i].Count - 2]; // preprevious vertex
                var v2 = faces[i][faces[i].Count - 1]; // previous vertex
                for (var j = 0; j < faces[i].Count; j++)
                {
                    var v3 = faces[i][j]; // this vertex
                    result.newV(v1 + "~" + v2, oneThird(positions[v1], positions[v2])); // new v in face
                    var fname = i + "f" + v2;
                    result.newFlag("v" + i, v1 + "~" + v2, v2 + "~" + v3); // five new flags
                    result.newFlag(fname, v1 + "~" + v2, v2 + "~" + v1);
                    result.newFlag(fname, v2 + "~" + v1, "v" + v2);
                    result.newFlag(fname, v2 + "~" + v3, v1 + "~" + v2);
                    v1 = v2; // shift over one
                    v2 = v3;
                }
            }

            var ans = result.flags2poly();
            adjustpositions(ans, 3); // canonicalize lightly
            return ans;
        }

        public ConwayHart reflect()
        {
            // compute reflection through origin
            var poly = this.clone();
            for (var i = 0; i < poly.positions.Count; i++)
                poly.positions[i] = -1 * poly.positions[i]; // reflect each point
            for (var i = 0; i < poly.faces.Count; i++)
                poly.faces[i] = poly.faces[i]; // repair clockwise-ness
            adjustpositions(poly, 1); // build dual
            return poly;
        }


        public ConwayHart expand()
        {
            ambo();
            ambo();
            return this;
        }

        public ConwayHart bevel()
        {
            ambo();
            truncate();
            return this;
        }

        public ConwayHart ortho()
        {
            join();
            join();
            return this;
        }

        public ConwayHart meta()
        {
            join();
            kis();
            return this;
        }

        public ConwayHart truncate(int n=0)
        {
            Dual();
            kis(n);
            Dual();
            return this;
        }

        public ConwayHart join()
        {
            Dual();
            ambo();
            Dual();
            return this;
        }

        public ConwayHart split()
        {
            Dual();
            gyro();
            Dual();
            return this;
        }
    }
}