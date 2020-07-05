/*
   Copyright (c) 2003-2016, Adrian Rossiter
   Antiprism - http://www.antiprism.com
   Permission is hereby granted, free of charge, to any person obtaining a
   copy of this software and associated documentation files (the "Software"),
   to deal in the Software without restriction, including without limitation
   the rights to use, copy, modify, merge, publish, distribute, sublicense,
   and/or sell copies of the Software, and to permit persons to whom the
   Software is furnished to do so, subject to the following conditions:
      The above copyright notice and this permission notice shall be included
      in all copies or substantial portions of the Software.
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
  IN THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Unitile
{

    public Unitile(int pat=1, int cols=5, int rows=5, bool totile=false, Vector3? shr=null)
    {
        pattern = pat;
        x_end = cols;
        y_end = rows;
        to_tile = totile;
        shear = shr ?? Vector3.zero;

        raw_verts = new List<Vector3>();
        raw_faces = new List<List<int>>();
        poly = new List<Vector3>();
    }

    int pattern;
    float x_end;
    float x_inc;
    float y_end;
    float y_inc;
    bool to_tile; // adjust size to make pattern tilable
    Vector3 shear;
    List<Vector3> poly; // current polygon

    public List<Vector3> raw_verts;
    public List<List<int>> raw_faces;

    public enum UT
    {
        ut_open,
        ut_join,
        ut_join2,
        ut_twist,
        ut_twist2,
        ut_twist3
    };


    void set_incs(float xinc, float yinc)
    {
        x_inc = xinc;
        y_inc = yinc;
        if (to_tile)
        {
            // assumes starts are 0
            x_end = Mathf.Ceil(x_end / x_inc) * x_inc;
            y_end = Mathf.Ceil(y_end / y_inc) * y_inc;
        }
    }

    void set_polygon(int num_sides, float rot_ang)
    {
        poly.Clear();
        float ang = 2 * Mathf.PI / num_sides;
        float rad = 0.5f / Mathf.Sin(ang / 2F); // radius for unit edge polygon

        for (int i = num_sides - 1; i >= 0; i--)
            poly.Add(new Vector3(rad * Mathf.Cos(i * ang + rot_ang), 0, rad * Mathf.Sin(i * ang + rot_ang)));
    }

    void add_polygon(float x_start, float y_start)
    {
        int x_steps = Mathf.CeilToInt((x_end - Mathf.Epsilon - x_start) / x_inc);
        int y_steps = Mathf.CeilToInt((y_end - Mathf.Epsilon - y_start) / y_inc);
        for (int i = 0; i < x_steps; i++)
        {
            float x = x_start + i * x_inc;
            if (x < -Mathf.Epsilon) continue;
            for (int j = 0; j < y_steps; j++)
            {
                float y = y_start + j * y_inc;
                if (y < -Mathf.Epsilon) continue;
                var cent = new Vector3(x, 0, y);
                var tile = new List<Vector3>();
                for (var polyIndex = 0; polyIndex < poly.Count; polyIndex++)
                {
                    var k = poly[polyIndex];
                    tile.Add(k + cent);
                }

                var mergedTile = new List<Vector3>();
                var mergedFace = new List<int>();
                for (var index = 0; index < tile.Count; index++)
                {
                    var v = tile[index];
                    int idx = CheckCoincidentVert(v, 1e-5f);
                    if (idx == -1)
                    {
                        mergedTile.Add(v);
                        mergedFace.Add(raw_verts.Count + mergedTile.Count - 1);
                    }
                    else
                    {
                        mergedFace.Add(idx);
                    }

//                    Debug.Log($"Vertex count: {raw_verts.Count + mergedTile.Count} Last index: {mergedFace.Last()}");
                }

                raw_faces.Add(mergedFace.ToList());
                raw_verts.AddRange(mergedTile);
            }
        }
    }

    void ut_4444()
    {
        set_incs(1, 1);
        set_polygon(4, Mathf.PI / 4);
        add_polygon(0.5f, 0.5f);
    }

    void ut_333333()
    {
        set_incs(Mathf.Sqrt(3), 1);
        set_polygon(3, Mathf.PI);
        add_polygon(0, 0);
        add_polygon(Mathf.Sqrt(3) / 2, 0.5f);
        set_polygon(3, 0);
        add_polygon(Mathf.Sqrt(3) / 3, 0);
        add_polygon(Mathf.Sqrt(3) * 5 / 6, 0.5f);
    }

    void ut_666()
    {
        set_incs(Mathf.Sqrt(3), 3);
        set_polygon(6, Mathf.PI / 6);
        add_polygon(0, 0);
        add_polygon(Mathf.Sqrt(3) / 2, 1.5f);
    }

    void ut_3636()
    {
        set_incs(Mathf.Sqrt(3) * 2, 2);
        set_polygon(3, 0);
        add_polygon(Mathf.Sqrt(3) * 2 / 3, 0);
        add_polygon(Mathf.Sqrt(3) * 5 / 3, 1);
        set_polygon(3, Mathf.PI);
        add_polygon(Mathf.Sqrt(3) * 1 / 3, 1);
        add_polygon(Mathf.Sqrt(3) * 4 / 3, 0);
        set_polygon(6, Mathf.PI / 6);
        add_polygon(0, 0);
        add_polygon(Mathf.Sqrt(3), 1);
    }

    void ut_33344()
    {
        set_incs(1, 2 + Mathf.Sqrt(3));
        set_polygon(3, Mathf.PI / 2);
        add_polygon(0, 0.5f + Mathf.Sqrt(3) / 6);
        add_polygon(0.5f, 1.5f + Mathf.Sqrt(3) * 2 / 3);
        set_polygon(3, -Mathf.PI / 2);
        add_polygon(0, 1.5f + Mathf.Sqrt(3) * 5 / 6);
        add_polygon(0.5f, 0.5f + Mathf.Sqrt(3) * 1 / 3);
        set_polygon(4, Mathf.PI / 4);
        add_polygon(0, 0);
        add_polygon(0.5f, 1 + Mathf.Sqrt(3) / 2);
    }

    void ut_33434()
    {
        float l = 1 + Mathf.Sqrt(3);
        set_incs(l, l);
        set_polygon(4, -Mathf.PI / 12);
        add_polygon(l / 4, l / 4);
        add_polygon(3 * l / 4, 3 * l / 4);
        set_polygon(4, Mathf.PI / 12);
        add_polygon(l / 4, 3 * l / 4);
        add_polygon(3 * l / 4, l / 4);
        float d = 1 / Mathf.Sqrt(12);
        set_polygon(3, 0);
        add_polygon(d, l / 2);
        add_polygon(l / 2 + d, 0);
        set_polygon(3, Mathf.PI);
        add_polygon(l - d, l / 2);
        add_polygon(l / 2 - d, 0);
        set_polygon(3, Mathf.PI / 2);
        add_polygon(0, d);
        add_polygon(l / 2, l / 2 + d);
        set_polygon(3, -Mathf.PI / 2);
        add_polygon(0, l - d);
        add_polygon(l / 2, l / 2 - d);
    }

    void ut_33336()
    {
        // TODO Fix...
        float rot = Mathf.Acos(5f / (Mathf.Sqrt(7f) * 2f));
        set_incs(Mathf.Sqrt(7f), Mathf.Sqrt(21f));
        set_polygon(6, rot);
        add_polygon(0, 0);
        add_polygon(Mathf.Sqrt(7f) / 2f, Mathf.Sqrt(21f) / 2f);
        for (int i = 0; i < 6; i++)
        {
            float rot2 = rot + Mathf.PI / 3f + i * Mathf.PI / 3f; //var trans = Matrix4x4.TRS(new Vector3(Mathf.Sqrt(3f) - 1f / Mathf.Sqrt(3f), 0, 0), Quaternion.identity, Vector3.one);
            var trans = Matrix4x4.TRS(
                new Vector3(Mathf.Sqrt(3f) - 1f / Mathf.Sqrt(3f), 0, 0),
                Quaternion.identity,
                Vector3.one
            );
            trans *= Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0, 0, rot2),
                Vector3.one
            );
            set_polygon(3, rot2);
            var cent = trans.MultiplyPoint3x4(Vector3.zero);
            add_polygon(cent.x, cent.z);
            add_polygon(cent.x + Mathf.Sqrt(7f) / 2f, cent.z + Mathf.Sqrt(21f) / 2f);
            if (i < 3)
            {
                trans = Matrix4x4.TRS(
                    new Vector3(0, 0, 1f),
                    Quaternion.identity,
                    Vector3.one) * trans;
                cent = trans.MultiplyPoint3x4(Vector3.zero);
                if (i == 1) add_polygon(cent.x, cent.z);
                add_polygon(cent.x + Mathf.Sqrt(7f) / 2f, cent.z + Mathf.Sqrt(21f) / 2f);
            }
        }
    }

    static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rot)
    {
        Vector3 dir = point - pivot;
        dir = rot * dir;
        point = dir + pivot;
        return point;
    }

    void ut_31212()
    {
        set_incs(2 + Mathf.Sqrt(3), 3 + Mathf.Sqrt(3) * 2);
        set_polygon(12, Mathf.PI / 12);
        add_polygon(0, 0);
        add_polygon(1 + Mathf.Sqrt(3) / 2, 1.5f + Mathf.Sqrt(3));
        set_polygon(3, -Mathf.PI / 6);
        add_polygon(1 + Mathf.Sqrt(3) / 2, 2.5f + Mathf.Sqrt(3) * 5 / 3);
        add_polygon(0, 1 + Mathf.Sqrt(3) * 2 / 3);
        set_polygon(3, Mathf.PI / 6);
        add_polygon(1 + Mathf.Sqrt(3) / 2, 0.5f + Mathf.Sqrt(3) / 3);
        add_polygon(0, 2 + Mathf.Sqrt(3) * 4 / 3);
    }

    void ut_488()
    {
        float l = 1 + Mathf.Sqrt(2);
        set_incs(l, l);
        set_polygon(4, 0);
        add_polygon(l / 2, l / 2);
        set_polygon(8, Mathf.PI / 8);
        add_polygon(0, 0);
    }

    void ut_3464()
    {
        set_incs(1 + Mathf.Sqrt(3), 3 + Mathf.Sqrt(3));
        set_polygon(3, Mathf.PI / 6);
        add_polygon(0, 1 + Mathf.Sqrt(3) / 3);
        add_polygon(0.5f + Mathf.Sqrt(3) / 2, 2.5f + Mathf.Sqrt(3) * 5 / 6);
        set_polygon(3, -Mathf.PI / 6);
        add_polygon(0, 2 + Mathf.Sqrt(3) * 2 / 3);
        add_polygon(0.5f + Mathf.Sqrt(3) / 2, 0.5f + Mathf.Sqrt(3) / 6);
        set_polygon(4, Mathf.PI / 12);
        add_polygon(0.25f + Mathf.Sqrt(3) / 4, 0.75f + Mathf.Sqrt(3) / 4);
        add_polygon(-0.25f - Mathf.Sqrt(3) / 4, 2.25f + Mathf.Sqrt(3) * 3 / 4);
        set_polygon(4, -Mathf.PI / 12);
        add_polygon(-0.25f - Mathf.Sqrt(3) / 4, 0.75f + Mathf.Sqrt(3) / 4);
        add_polygon(0.25f + Mathf.Sqrt(3) / 4, 2.25f + Mathf.Sqrt(3) * 3 / 4);
        set_polygon(4, Mathf.PI / 4);
        add_polygon(0.5f + Mathf.Sqrt(3) / 2, 0);
        add_polygon(0, 1.5f + Mathf.Sqrt(3) / 2);
        set_polygon(6, Mathf.PI / 6);
        add_polygon(0, 0);
        add_polygon(0.5f + Mathf.Sqrt(3) / 2, 1.5f + Mathf.Sqrt(3) / 2);
    }

    void ut_4612()
    {
        set_incs(3 + Mathf.Sqrt(3), 3 + Mathf.Sqrt(3) * 3);
        set_polygon(4, Mathf.PI / 12);
        add_polygon(0.75f + Mathf.Sqrt(3) / 4, 0.75f + Mathf.Sqrt(3) * 3 / 4);
        add_polygon(2.25f + Mathf.Sqrt(3) * 3 / 4, 2.25f + Mathf.Sqrt(3) * 9 / 4);
        set_polygon(4, -Mathf.PI / 12);
        add_polygon(2.25f + Mathf.Sqrt(3) * 3 / 4, 0.75f + Mathf.Sqrt(3) * 3 / 4);
        add_polygon(0.75f + Mathf.Sqrt(3) / 4, 2.25f + Mathf.Sqrt(3) * 9 / 4);
        set_polygon(4, Mathf.PI / 4);
        add_polygon(1.5f + Mathf.Sqrt(3) / 2, 0);
        add_polygon(0, 1.5f + Mathf.Sqrt(3) * 3 / 2);
        set_polygon(6, 0);
        add_polygon(0, 1 + Mathf.Sqrt(3));
        add_polygon(0, 2 + Mathf.Sqrt(3) * 2);
        add_polygon(1.5f + Mathf.Sqrt(3) / 2, 2.5f + Mathf.Sqrt(3) * 5 / 2);
        add_polygon(1.5f + Mathf.Sqrt(3) / 2, 0.5f + Mathf.Sqrt(3) / 2);
        set_polygon(12, Mathf.PI / 12);
        add_polygon(0, 0);
        add_polygon(1.5f + Mathf.Sqrt(3) / 2, 1.5f + Mathf.Sqrt(3) * 3 / 2);
    }

    void GenerateTile()
    {
        switch (pattern)
        {
            case  1:
                ut_4444();
                break;
            case  2:
                ut_333333();
                break;
            case  3:
                ut_666();
                break;
            case  4:
                ut_3636();
                break;
            case  5:
                ut_33344();
                break;
            case  6:
                ut_33434();
                break;
            case  7:
                ut_33336();
                break;
            case  8:
                ut_31212();
                break;
            case  9:
                ut_488();
                break;
            case 10:
                ut_3464();
                break;
            case 11:
                ut_4612();
                break;
        }
    }

    public void plane(UT lr_join=UT.ut_open, UT tb_join=UT.ut_open)
    {
        GenerateTile();

        float x_sh_inc = shear.x * x_inc;
        float y_sh_inc = shear.z * y_inc;
        float x_sh_inc2 = x_sh_inc * (1 + y_sh_inc / y_end);
        float y_sh_inc2 = y_sh_inc * (1 + x_sh_inc / x_end);
        float x_cross = x_end + x_sh_inc - x_sh_inc2;
        float y_cross = y_end + y_sh_inc - y_sh_inc2;

        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];

            float x = vert.x;
            vert.x += (shear.x * x_inc) * (vert.z / y_end);
            vert.x *= x_end / x_cross;
            vert.z += (shear.z * y_inc) * (x / x_end);
            vert.z *= y_end / y_cross;

            raw_verts[i] = vert;
        }

        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            if (lr_join == UT.ut_twist && (vert.x < -Mathf.Epsilon || vert.x > x_end - Mathf.Epsilon))
                vert.z = -vert.z;
            if (lr_join == UT.ut_twist2 && (vert.x < -Mathf.Epsilon || vert.x > x_end - Mathf.Epsilon))
                vert.z = y_end - vert.z;
            if (lr_join == UT.ut_twist3 && (vert.x < -Mathf.Epsilon || vert.x > x_end - Mathf.Epsilon))
                vert.z = 1.5f * y_end - vert.z % y_end;
            if (tb_join == UT.ut_twist && (vert.z < -Mathf.Epsilon || vert.z > y_end - Mathf.Epsilon))
                vert.x = -vert.x;
            if (tb_join == UT.ut_twist2 && (vert.z < -Mathf.Epsilon || vert.z > y_end - Mathf.Epsilon))
                vert.x = x_end - vert.x;
            if (tb_join == UT.ut_twist3 && (vert.z < -Mathf.Epsilon || vert.z > y_end - Mathf.Epsilon))
                vert.x = 1.5f * x_end - vert.x % x_end;
            if (tb_join == UT.ut_join2 && (vert.z < -Mathf.Epsilon || vert.z > y_end - Mathf.Epsilon))
                vert.x = 0.5f * x_end - vert.x % x_end;
            if (lr_join != UT.ut_open)
            {
                vert.x = vert.x + x_end % x_end - Mathf.Epsilon;
            }

            if (tb_join != UT.ut_open)
            {
                vert.z = vert.z + y_end % y_end - Mathf.Epsilon;
            }

            raw_verts[i] = vert;
        }

    }



    public void torus(float sect_rad = 1f, float ring_rad = 2f)
    {
        plane(UT.ut_join, UT.ut_join);
        float a0, a1;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = 2 * Mathf.PI * vert.x / x_end;
            a1 = 2 * Mathf.PI * vert.z / y_end;
            vert = new Vector3(
                Mathf.Sin(a1) * (sect_rad * Mathf.Cos(a0) + ring_rad),
                sect_rad * Mathf.Sin(a0),
                Mathf.Cos(a1) * (sect_rad * Mathf.Cos(a0) + ring_rad)
            );
            raw_verts[i] = vert;
        }
    }


    private int CheckCoincidentVert(Vector3 a, float Epsilon)
    {
        float sqrEpsilon = Epsilon * Epsilon;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            if (Vector3.SqrMagnitude(raw_verts[i] - a) < sqrEpsilon)
            {
                return i;
            }

        }
        return -1;
    }

    public void conic_frust(float top_rad=0.5f, float bot_rad=1f, float ht=2f)
    {
        plane(UT.ut_join, UT.ut_open);
        float a0, h, rad;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = 2 * Mathf.PI * vert.x / x_end;
            h = ht * vert.z / y_end;
            rad = (top_rad - bot_rad) * vert.z / y_end + bot_rad;
            raw_verts[i] = new Vector3(rad * Mathf.Cos(a0), rad * Mathf.Sin(a0), h);
        }
    }

    public void mobius(float sect_rad=0.5f, float ring_rad=1f)
    {
        plane(UT.ut_twist2, UT.ut_open);
        float a0;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = 2 * Mathf.PI * vert.x / x_end;
            vert.z -= y_end / 2;
            raw_verts[i] = new Vector3(Mathf.Sin(a0) * (sect_rad * vert.z / y_end * Mathf.Cos(a0 / 2) + ring_rad),
                sect_rad * vert.z / y_end * Mathf.Sin(a0 / 2),
                Mathf.Cos(a0) * (sect_rad * vert.z / y_end * Mathf.Cos(a0 / 2) + ring_rad));
        }
    }

    public void torus_trefoil(float pqx=2, float pqz=-1, float sect_rad=0.5f, float ring_rad=1f)
    {
        plane(UT.ut_join, UT.ut_join);
        int i = -1;
        for (var index = 0; index < raw_verts.Count; index++)
        {
            var vert = raw_verts[index];
            i++;
            float a0 = 2 * Mathf.PI * vert.x / x_end;
            float a1 = 2 * Mathf.PI * vert.z / y_end;

            int q = 3;
            int p = 2;

            if (Mathf.Floor(pqx) > 0)
                q = (int) pqx;
            p = 1;
            if (Mathf.Floor(pqz) > 0)
                p = (int) pqz;

            float a = ring_rad;
            float b = 1.0f;
            float x = (a + b * Mathf.Cos(q * a1)) * Mathf.Cos(p * a1);
            float y = (a + b * Mathf.Cos(q * a1)) * Mathf.Sin(p * a1);
            float z = b * Mathf.Sin(q * a1);
            var P = new Vector3(x, y, z);

            float a11 = a1 + 0.0001f;
            float x0 = (a + b * Mathf.Cos(q * a11)) * Mathf.Cos(p * a11);
            float y0 = (a + b * Mathf.Cos(q * a11)) * Mathf.Sin(p * a11);
            float z0 = b * Mathf.Sin(q * a11);
            var P0 = new Vector3(x0, y0, z0);
            var Q = new Vector3(a * Mathf.Cos(p * a1), a * Mathf.Sin(p * a1), 0);

            Vector3 dir = (P0 - P).normalized;
            Vector3 norm = P - Q;

            float ang = a0 + (p + 1) * a1; // stops the tube twist
            var v = new Vector3(sect_rad * Mathf.Cos(ang), sect_rad * Mathf.Sin(ang), 0);
            // TODO
//            raw_verts[index] = Trans3d::translate(P) *
//                               Trans3d::align(Vector3::Z, Vector3::Y, dir, norm) * v;
        }
    }

    // http://en.wikipedia.org/wiki/Image:KleinBottle-01.png
    public void klein(float sect_rad = 1f) //, float /*ring_rad*/)
    {
        plane(UT.ut_twist3, UT.ut_join);
        float a0, a1;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            Vector3 vert = raw_verts[i];
            // a0 = 2*Mathf.PI*fmod(1+verts[i].x/x_end, 1);
            // a1 = 2*Mathf.PI*fmod(1+verts[i].z/y_end, 1);
            a0 = 2 * Mathf.PI * vert.x / x_end;
            a1 = 2 * Mathf.PI * vert.z / y_end;
            if (a0 < Mathf.PI)
            {
                vert = new Vector3(
                  6 * Mathf.Cos(a0) * (1 + Mathf.Sin(a0)) + 4 * sect_rad * (1 - 0.5f * Mathf.Cos(a0)) * Mathf.Cos(a0) * Mathf.Cos(a1),
                  16 * Mathf.Sin(a0) + 4 * sect_rad * (1 - 0.5f * Mathf.Cos(a0)) * Mathf.Sin(a0) * Mathf.Cos(a1),
                  4 * sect_rad * (1 - 0.5f * Mathf.Cos(a0)) * Mathf.Sin(a1)
              );
            }
            else
            {
                vert = new Vector3(
                    6 * Mathf.Cos(a0) * (1 + Mathf.Sin(a0)) -  4 * sect_rad * (1 - 0.5f * Mathf.Cos(a0)) * Mathf.Cos(a1),
                    16 * Mathf.Sin(a0), 4 * sect_rad * (1 - 0.5f * Mathf.Cos(a0)) * Mathf.Sin(a1)
                );
            }

            raw_verts[i] = vert;
        }
    }

    public void klein2(float sect_rad=1f, float ring_rad=1f)
{
    plane(UT.ut_twist, UT.ut_join);
    float a0, a1;
    for (var i = 0; i < raw_verts.Count; i++)
    {
        var vert = raw_verts[i];
        a0 = 2 * Mathf.PI * vert.x / x_end;
        a1 = 2 * Mathf.PI * vert.z / y_end;
        vert = new Vector3(
            (ring_rad + (Mathf.Cos(0.5f * a0) * Mathf.Sin(a1) - Mathf.Sin(0.5f * a0) * Mathf.Sin(2 * a1)) * sect_rad) * Mathf.Cos(a0),
            (ring_rad + (Mathf.Cos(0.5f * a0) * Mathf.Sin(a1) - Mathf.Sin(0.5f * a0) * Mathf.Sin(2 * a1)) * sect_rad) * Mathf.Sin(a0),
            (Mathf.Sin(0.5f * a0) * Mathf.Sin(a1) + Mathf.Cos(0.5f * a0) * Mathf.Sin(2 * a1)) * sect_rad
        );
    }
}

    public void roman()
    {
        plane(UT.ut_open, UT.ut_open);
        float a0, a1;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = Mathf.PI * vert.x / x_end;
            a1 = Mathf.PI * (vert.z / y_end - 0.5f);
            vert = new Vector3(0.5f * Mathf.Sin(2 * a0) * Mathf.Sin(a1) * Mathf.Sin(a1),
                0.5f * Mathf.Sin(a0) * Mathf.Cos(2 * a1), 0.5f * Mathf.Cos(a0) * Mathf.Sin(2 * a1));
            // verts[i] = Vector3(
            //   Mathf.Sin(2*a0)*Mathf.Cos(a1)*Mathf.Cos(a1),
            //   Mathf.Sin(a0)*Mathf.Sin(2*a1),
            //   Mathf.Cos(a0)*Mathf.Cos(2*a1) );
            vert = new Vector3(
                0.5f * Mathf.Cos(a0) * Mathf.Sin(2 * a1),
                0.5f * Mathf.Sin(a0) * Mathf.Sin(2 * a1),
                0.5f * Mathf.Sin(2 * a0) * Mathf.Cos(a1) * Mathf.Cos(a1)
            );
            raw_verts[i] = vert;
        }
    }

    public void roman_boy(float t=1f)
    {
        plane(UT.ut_open, UT.ut_open);
        float a0, a1;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = Mathf.PI * (0.5f - vert.x / x_end);
            a1 = Mathf.PI * vert.z / y_end;
            float x =
                (Mathf.Sqrt(2) * Mathf.Cos(2 * a0) * Mathf.Cos(a1) * Mathf.Cos(a1) + Mathf.Cos(a0) * Mathf.Sin(2 * a1)) /
                (2 - t * Mathf.Sqrt(2) * Mathf.Sin(3 * a0) * Mathf.Sin(2 * a1));
            float y =
                (Mathf.Sqrt(2) * Mathf.Sin(2 * a0) * Mathf.Cos(a1) * Mathf.Cos(a1) - Mathf.Sin(a0) * Mathf.Sin(2 * a1)) /
                (2 - t * Mathf.Sqrt(2) * Mathf.Sin(3 * a0) * Mathf.Sin(2 * a1));
            float z =
                (3 * Mathf.Cos(a1) * Mathf.Cos(a1)) / (2 - t * Mathf.Sqrt(2) * Mathf.Sin(3 * a0) * Mathf.Sin(2 * a1));
            raw_verts[i] = new Vector3(x, y, z);
        }
    }

    public void cross_cap()
    {
        plane(UT.ut_twist2, UT.ut_twist2);
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            float x = 1f - 2.0f * vert.x / x_end;
            float y = 1f - 2.0f * vert.z / y_end;
            float a0 = Mathf.Atan2(y, x);
            // x *= 0.9;
            // y *= 0.9;
            float dist_to_edge;
            if (Mathf.Abs(x) < Mathf.Epsilon && Mathf.Abs(y) < Mathf.Epsilon)
                dist_to_edge = 1;
            else if (Mathf.Abs(x) > Mathf.Abs(y))
                dist_to_edge = Mathf.Sqrt(1 + Mathf.Abs(y * y / (x * x)));
            else
                dist_to_edge = Mathf.Sqrt(1 + Mathf.Abs(x * x / (y * y)));

            x /= dist_to_edge;
            y /= dist_to_edge;
            float r = 2.0f / 3f + (1f / (1f + Mathf.Cos(2f * (a0 - Mathf.PI / 4f)) / 2f));
            float a1 = Mathf.PI * Mathf.Sqrt(x * x + y * y);

            x = r * Mathf.Cos(a0) * Mathf.Sin(a1);
            y = r * Mathf.Sin(a0) * Mathf.Sin(a1);
            float z = r * (Mathf.Cos(a1) - 1);
            raw_verts[i] = new Vector3(x, y, z);
        }
    }

    public void cross_cap2()
    {
        plane(UT.ut_open, UT.ut_open);
        float a0, a1;
        for (var i = 0; i < raw_verts.Count; i++)
        {
            var vert = raw_verts[i];
            a0 = 2 * Mathf.PI * vert.x / x_end;
            a1 = 0.5f * Mathf.PI * vert.z / y_end;
            // verts[i] = Vector3(Mathf.Cos(a0)*Mathf.Sin(2*a1), Mathf.Sin(a0)*Mathf.Sin(2*a1),
            //                 Mathf.Cos(a1)*Mathf.Cos(a1)-Mathf.Cos(a0)*Mathf.Cos(a0)*Mathf.Sin(a1)*Mathf.Sin(a1));

            vert = new Vector3(Mathf.Sin(a0) * Mathf.Sin(2 * a1), Mathf.Sin(2 * a0) * Mathf.Sin(a1) * Mathf.Sin(a1),
                Mathf.Cos(2 * a0) * Mathf.Sin(a1) * Mathf.Sin(a1));
            raw_verts[i] = vert;
        }
    }

//void torus4d(Trans4d rot4d_m)
//{
//  plane(ut_join, ut_join);
//  float a0, a1;
//  List<Vector3> verts = raw_verts;
//  foreach (var vert in verts) {
//    a0 = 2 * Mathf.PI * vert.x / x_end;
//    a1 = 2 * Mathf.PI * vert.z / y_end;
//    Vec4d v4d = rot4d_m * Vec4d(Mathf.Cos(a0), Mathf.Sin(a0), Mathf.Cos(a1), Mathf.Sin(a1));
//    // Vec4d v4d = rot * Vec4d(Mathf.Cos(a0+a1), Mathf.Sin(a0+a1), Mathf.Cos(a0-a1), Mathf.Sin(a0-a1));
//    vert = Vector3(v4d.x, v4d.z, v4d.z);
//  }
//}
//
//void klein4d(Trans4d rot4d_m)
//{
//  plane(ut_join, ut_join2);
//  float a0, a1;
//  List<Vector3> verts = raw_verts;
//  foreach (var vert in verts) {
//    a0 = 2 * Mathf.PI * vert.x / x_end;
//    a1 = 2 * Mathf.PI * vert.z / y_end;
//
//    float x = Mathf.Cos(a0) / (1 + pow(Mathf.Sin(a0), 2));
//    float y = Mathf.Sin(a0) * x;
//    float a2 = a1 / 2;
//    float x2 = x * Mathf.Cos(a2) - y * Mathf.Sin(a2);
//    float y2 = x * Mathf.Sin(a2) + y * Mathf.Cos(a2);
//    Vec4d v4d = rot4d_m * Vec4d(x2, y2, Mathf.Cos(a1), Mathf.Sin(a1));
//    // Vec4d v4d = rot * Vec4d(Mathf.Cos(a0+a1), Mathf.Sin(a0+a1), Mathf.Cos(a0-a1), Mathf.Sin(a0-a1));
//    vert = Vector3(v4d.x, v4d.z, v4d.z);
//  }
//}
//
//void proj4d(Trans4d rot4d_m)
//{
//  plane(ut_twist, ut_twist);
//  float a0, a1;
//  List<Vector3> verts = raw_verts;
//  foreach (var vert in verts) {
//    a0 = -Mathf.PI * (2 * vert.x / x_end - 0.5f);
//    a1 = Mathf.PI * (2 * vert.z / y_end - 0.5f);
//
//    float x = Mathf.Cos(a0) / (1 + pow(Mathf.Sin(a0), 2));
//    float y = Mathf.Sin(a0) * x;
//    float a2 = a1 / 2;
//    float x2 = x * Mathf.Cos(a2) - y * Mathf.Sin(a2);
//    float y2 = x * Mathf.Sin(a2) + y * Mathf.Cos(a2);
//    float z = Mathf.Cos(a1) / (1 + pow(Mathf.Sin(a1), 2));
//    float w = Mathf.Sin(a1) * z;
//    float a3 = a0 / 2;
//    float z2 = z * Mathf.Cos(a3) - w * Mathf.Sin(a3);
//    float w2 = z * Mathf.Sin(a3) + w * Mathf.Cos(a3);
//    Vec4d v4d = rot4d_m * Vec4d(x2, y2, z2, w2);
//    // Vec4d v4d = rot * Vec4d(Mathf.Cos(a0+a1), Mathf.Sin(a0+a1), Mathf.Cos(a0-a1), Mathf.Sin(a0-a1));
//    vert = Vector3(v4d.x, v4d.z, v4d.z);
//  }
//}
}