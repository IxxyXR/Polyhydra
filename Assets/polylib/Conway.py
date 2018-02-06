using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Conway {
    
    //    functions to implement conway-hart operators on polyhedron
    //    designed for use with Svercook scripted nodes
    //    Is standalone apart from dependency on Blender mathutils module for vector functions
    
    public Vector[] Vertices;
    public int[] Faces;
    public Dictionary<string, bool> flags;
    public Dictionary<string, int> vert_tags;
    
    public Conway(Vector[] v, int[] f) {
        Vertices = v;
        Faces = f;
        flags = new Dictionary<string, bool>();
        vert_tags = new Dictionary<string, int>();
    }

    // ---- Face and edge function

    public Vector normal(Vector[] vectors) {
        var side1 = vectors[1].diff(vectors[0]);
        var side2 = vectors[2].diff(vectors[0]);
        Vector perp = side1.cross(side2);
        // Normalize
        var perpLength = Math.Sqrt(perp.x * perp.x + perp.y * perp.y);
        perp = new Vector(perp.x/perpLength, perp.y/perpLength, perp.z/perpLength);
        return perp;
    }

    public Vector[] verts_for_face(Vector[] verts, int[] face) {
        //In Python: Vector[] verts_xyz = [verts[v_i] for v_i in face];
        var verts_xyz = new Vector[face.Length]; 
        foreach (int v_i in face) {
            verts_xyz[v_i] = verts[face[v_i]];
        }
        return verts_xyz;
    }

    public Vector face_center(Vector[] verts, int[] face, float height=0) {

        // find the center of a face
        // inputs:
        // verts: list of x, y, z coords of verticies
        // face:  list of indcies of verts in face
        // height: height of returned vertex above plane of face.
        // output:
        // center: center x, y, z coords as list

        Vector[] verts_xyz = verts_for_face(verts, face);

        var x_co  = new double[verts_xyz.Length];
        var y_co  = new double[verts_xyz.Length];
        var z_co  = new double[verts_xyz.Length];
        for (var i = 0; i < verts_xyz.Length; i++) {
            Vector coord = verts_xyz[i];
            x_co[i] = coord.x;
            y_co[i] = coord.y;
            z_co[i] = coord.z;
        }
        
        Vector center = new Vector(
            x_co.Sum() / x_co.Length,
            y_co.Sum() / y_co.Length,
            z_co.Sum() / z_co.Length
        );
        if (height > 0) {
            Vector norm = normal(verts_xyz);
            center = center.sum(norm.scale(height));
        }

        return center;
    }

    public Vector face_normal(Vector[] verts, int[] face) {
        
        // normal direction of face as mathutils Vector
        Vector[] verts_xyz = verts_for_face(verts, face);
        var norm = normal(verts_xyz);
        return norm;
    }
    
    public Vector edge_center(Vector[] verts, int[] edge) {
        
        // find the middle point on edge

        Vector v1_xyz = new Vector(verts[edge[0]]);
        Vector v2_xyz = new Vector(verts[edge[1]]);
        Vector va_xyz = v1_xyz.sum(v2_xyz.diff(v1_xyz)).scale(0.5);
        return va_xyz;
    }
    
    public Vector edge_third(Vector[] verts, int[] edge) {
        
        // find a point one third along edge
        Vector v1_xyz = new Vector(verts[edge[0]]);
        Vector v2_xyz = new Vector(verts[edge[1]]);
        Vector va_xyz = v1_xyz.sum(v2_xyz.diff(v1_xyz)).scale(1.0/3);
        return va_xyz;
    }

    public Vector intersect_point_line(Vector point, Vector line0, Vector line1) {
        
        const char *error_prefix = "intersect_point_line";
        PyObject *py_pt, *py_line_a, *py_line_b;
        float pt[3], pt_out[3], line_a[3], line_b[3];
        float lambda;
        PyObject *ret;
        int size = 2;

        if (!PyArg_ParseTuple(
            args, "OOO:intersect_point_line",
            &py_pt, &py_line_a, &py_line_b))
        {
            return NULL;
        }

        /* accept 2d verts */
        if ((((size = mathutils_array_parse(pt, 2, 3 | MU_ARRAY_SPILL | MU_ARRAY_ZERO, py_pt, error_prefix)) != -1) &&
             (mathutils_array_parse(line_a, 2, 3 | MU_ARRAY_SPILL | MU_ARRAY_ZERO, py_line_a, error_prefix) != -1) &&
             (mathutils_array_parse(line_b, 2, 3 | MU_ARRAY_SPILL | MU_ARRAY_ZERO, py_line_b, error_prefix) != -1)) == 0)
        {
            return NULL;
        }
        
        float closest_to_line_v3(Vector r_close, Vector p, Vector l1, Vector l2) {
            Vector h, u;
            float lambda;
            sub_v3_v3v3(u, l2, l1);
            sub_v3_v3v3(h, p, l1);
            lambda = u.dot(h) / u.dot(u);
            r_close[0] = l1[0] + u[0] * lambda;
            r_close[1] = l1[1] + u[1] * lambda;
            r_close[2] = l1[2] + u[2] * lambda;
            return lambda;
        }

        /* do the calculation */
        lambda = closest_to_line_v3(out pt_out, pt, line_a, line_b);

        return new Tuple<Vector, Double>(pt_out, lambda);

    }
    
    public Vector tangent_point(Vector[] verts, int[] edge) {
        
        // find the closest point to the origin on edge

        var v1_xyz = new Vector(verts[edge[0]]);
        var v2_xyz = new Vector(verts[edge[1]]);
        var va_xyz = intersect_point_line(new Vector(0, 0, 0), v1_xyz, v2_xyz);
        return va_xyz;
    
    }
    
    // ---- flag tag function {
    
    public void faces_to_flags(faces, face_tags=None, edge_key=False) {

        //    takes a list of faces, where each face is given as a list of verts in CCW order
        //    and returns flags, verts_tags
    
        //    face_tags is an optional list of tags for the faces
        //    
        //    flags is a dict where:
        //    key = face tag + vert tag
        //    value = (face tag, vert tag, tag of next CCW vert in the face)
        //    
        //    if edge_key == True
        //    the key for flags is
        //    key : v1_t + v2_t
        //    this allows faces opposing across an edge to be found easily
        //    
        //    vert_tags
        //    key   vert tag
        //    value index of vert
        //    
        //    This makes looking up the next vertex quick. There will be two flags for every edge in
        //    the mesh represented by faces.
        
        for (int face_i = 0; face_i < faces.Length; face_i++) {
            face = faces(face_i);
            try {
                face_t = face_tags[face_i];
            } except TypeError, IndexError {
                face_t = 'f{}'.format(face_i);
            }
            for (v1,  v2 in zip(face, face[1:] + face[:1])) {
                v1_t = 'v' + v1;
                v2_t = 'v' + v2;
                if (edge_key) {
                    flags[v1_t + v2_t] = [face_t, v1_t, v2_t];
                } else {
                    // default
                    flags[face_t + v1_t] = [face_t, v1_t, v2_t];
                }
                vert_tags[v1_t] = v1;
            }
        }

        return flags, vert_tags;

    }

    public void flags_to_faces(flags, vert_tags) {

        //    flags is a dict where the
        //    key  face tag + vert tag
        //    value  (face tag, vert tag, tag of next CCW vert in the face)
        //    
        //    vert_tags
        //    key   vert tag
        //    value index of vert
        //    
        //    returns a list of faces, where each face is
        //    given as a list of vert indices in CCW order
        //    and a list of face tags in the same order as faces

        face_count = defaultdict(int);
        vert_one = {};
        faces = [];
        face_tags = []; // list of face_t in the same order as faces

        // find how many verts in each face, and  keep one vert from each face
        for flag in flags.values() {
            face_count[flag[0]] += 1;
            vert_one[flag[0]] = flag[1];
        }
        for (face_t, vert_count in face_count.items()) {
            face_vt =  [vert_one[face_t]];
            while (len(face_vt) < vert_count) {
                v1_t = face_vt[-1];
            // find the flag (face_t, v1_t, v2_t)
                v2_t = flags[face_t + v1_t][2];
                face_vt.append(v2_t);
            face_ccw =  [vert_tags[v_t] for v_t in face_vt];
                faces.append(face_ccw);
                face_tags.append(face_t);
            }
        return faces, face_tags;

    }

    public void face_vt_to_flags(face_t, face_vt) {

        //    returns the flags for a complete new face
        //    input:
        //        face_t: string tag to name face
        //        face_vt: list of vertex tags
        //    output
        //        flags: flags is a dict where the
        //                key  face tag + vert tag
        //                value  (face tag, vert tag, tag of next CCW vert in the face)

        flags = {};
        for (v1_t, v2_t in zip(face_vt, face_vt[1:] +face_vt[:1])) {
            flags[face_t + v1_t] =  [face_t, v1_t, v2_t];
        }

        return flags;

    }
        
    // ---- Conway Operator {
    
    public void kis(verts_in, faces_in, height=0.0) {
        
        //    each n-face is divided into n triangles which extend to the face centroid
        //    existing vertices retained
        //    equivalent to Blender poke operator
        
        flags_kis = {};
        verts_kis = verts_in[:];
        vert_tags = {'v{}'.format(i): i for i, v in enumerate(verts_kis)};
    
        for (face_i, face in enumerate(faces_in)) {
            verts_kis.append(face_center(verts_in, face, height));
            vert_tags['vf{}'.format(face_i)] = len(verts_kis) - 1;
            for (v1, v2 in zip(face, face[1:] +face[:1])) {
                // 3 flags for the face
                f3_t = 'f{}:{}'.format(face_i, v1);
                va_t = 'v{}'.format(v1);
                vb_t = 'v{}'.format(v2);
                vc_t = 'vf{}'.format(face_i);
                flags_new = face_vt_to_flags(f3_t, [va_t, vb_t, vc_t]);
                flags_kis.update(flags_new);
            }
        }
        faces_kis = flags_to_faces(flags_kis, vert_tags)[0];
        return verts_kis, faces_kis;
        }

    public void dual(verts_in, faces_in) {
        
        //    faces become vertices, vertices become faces
        //    The dual of a polyhedron is another mesh wherein:
        //    - every face in the original becomes a vertex in the dual
        //    - every vertex in the original becomes a face in the dual
        //    v = f, e = e, f = v
    
        
        verts_dual = [];
        verts_out_tags = {};
        flags_dual = {};
    
        // make edge indexed flags for old mesh
        flags_in, verts_in_tags = faces_to_flags(faces_in, edge_key = True);
    
        for (face_i, face in enumerate(faces_in)) {
            verts_dual.append(face_center(verts_in, face));
            verts_out_tags['vf{}'.format(face_i)] = len(verts_dual) - 1;

            for (v1, v2 in zip(face, face[1:]+face[:1])) {
                va_t = 'vf{}'.format(face_i);
                // find the tag of the face across edge (v1, v2) from face_i
                edge_key = 'v{}v{}'.format(v2, v1);
                fopp_t = flags_in[edge_key][0];
                vb_t = fopp_t.replace('f', 'vf');

                // make one new flag for each old half-edge
                fdual_t = 'fv{}'.format(v1);
                flags_dual[fdual_t + vb_t] =  [fdual_t, vb_t, va_t];
            }
        }
        faces, face_tags = flags_to_faces(flags_dual, verts_out_tags);
    
        // sort outgoing faces to be in same order as incoming verts
        face_dict = {int(k[2:]): v for k, v in zip(face_tags, faces)};
        faces_sorted = [face_dict[i] for i in sorted(face_dict)];
    
        return verts_dual, faces_sorted;

    }

        public void ambo(verts_in, faces_in) {

            //    New vertices are added mid-edges, while old vertices are removed.
            //    results in a face per original face and a face per original vertex
            //    This is full truncation to the mid-point of the edge
            //    equivalent to the bevel operator, vertex only, percent, amount = 50 2e

            verts_ambo = []; // new verts at the centre of old edges
            vert_tags = {
            }
            ;
            flags_ambo = {
            }
            ;

            for (face_i, face in enumerate(faces_in))
            {
                for (v1, v2, v3 in zip(face, face[1:]
                +face[:1], face[2:] +face[:2])) {
                    va_t = 'v{}:{}'.format(*sorted((v1, v2)));
                    if (va_t not in vert_tags) {
                        verts_ambo.append(edge_center(verts_in, (v1, v2)));
                        vert_tags[va_t] = len(verts_ambo) - 1;
                    }
                    vb_t = 'v{}:{}'.format(*sorted((v2, v3)));
                    // add two flags along the edge (va, vb)
                    fcenter_t = 'f{}'.format(face_i);
                    flags_ambo[fcenter_t + va_t] =  [fcenter_t, va_t, vb_t];
                    fvert_t = 'fv{}'.format(v2);
                    flags_ambo[fvert_t + vb_t] =  [fvert_t, vb_t, va_t];
                }
                faces_ambo = flags_to_faces(flags_ambo, vert_tags)[0];
                return verts_ambo, faces_ambo;
            }
        }

    public void chamfer(verts_in, faces_in, thickness=0.1, height=0.1) {

            //    An edge-truncation.
            //    New hexagonal faces are added in place of edges.
            //     v = v + 2e, e = 4e, f = f + e

            flags_chamf =  {
            }

            verts_chamf = verts_in[:]
            vert_tags =  {
                'v{}'.format(i): i for i,
                v in enumerate(verts_chamf)
            }

            for (face_i, face in enumerate(faces_in))
            {
                for (v1, v2 in zip(face, face[1:]
                +face[:1])) {
                }
                center = Vector(face_center(verts_chamf, face));
                v2_xyz = Vector(verts_chamf[v2]);

                face_norm = mathutils.geometry.normal([verts_chamf[v_i] for v_i in
                face]);
                vb_xyz = v2_xyz + (center - v2_xyz) * thickness + face_norm * height;

                verts_chamf.append(list(vb_xyz));
                vb_t = 'v{}f{}'.format(v2, face_i);
                vert_tags[vb_t] = len(verts_chamf) - 1;

                // add 4 flags
                face_chamf_t = 'f{}:{}'.format(*sorted((v1, v2)));
                face_t = 'f{}'.format(face_i);
                va_t = 'v{}'.format(v2);
                vc_t = 'v{}f{}'.format(v1, face_i);
                vd_t = 'v{}'.format(v1);

                flags_chamf[face_chamf_t + va_t] =  [face_chamf_t, va_t, vb_t];
                flags_chamf[face_chamf_t + vb_t] =  [face_chamf_t, vb_t, vc_t];
                flags_chamf[face_chamf_t + vc_t] =  [face_chamf_t, vc_t, vd_t];

                flags_chamf[face_t + vc_t] =  [face_t, vc_t, vb_t];
            }
        }

        faces_chamf = flags_to_faces(flags_chamf, vert_tags)[0];
        return verts_chamf, faces_chamf;

    }
    public void gyro(verts_in, faces_in) {
        
        //    gyro is like kis but with the new edges connecting the face centers to the 1/3 points
        //    on the edges rather than the vertices.
        //    v = v + 2e +  f,  f = 2e ,  e = 5e
        
        verts_gyro = verts_in[:];
        vert_tags = {'v{}'.format(i): i for i, v in enumerate(verts_gyro)}
        ;
    
        flags_gyro = {}
        ;
    
        for (face_i, face in enumerate(faces_in)) {
            verts_gyro.append(face_center(verts_in, face));
            vert_tags['vf{}'.format(face_i)] = len(verts_gyro) - 1;

            for (v1, v2, v3 in zip(face, face[1:] +face[:1], face[2:] +face[:2])) {
                va_t = 'v{}:{}'.format(v1, v2);
                verts_gyro.append(edge_third(verts_in, (v1, v2)));
                vert_tags[va_t] = len(verts_gyro) - 1;

                // do five flags for the new face that shares two egdes with edge v1, v2
                face_t = 'f{}:{}'.format(face_i, v2);
                vb_t = 'v{}:{}'.format(v2, v1);
                vc_t = 'v{}'.format(v2);
                vd_t = 'v{}:{}'.format(v2, v3);
                ve_t = 'vf{}'.format(face_i);

                flags_new = face_vt_to_flags(face_t, [va_t, vb_t, vc_t, vd_t, ve_t]);
                flags_gyro.update(flags_new);
            }
        }
        faces_gyro = flags_to_faces(flags_gyro, vert_tags)[0];
        return verts_gyro, faces_gyro;

    }

    public void propellor(verts_in, faces_in) {
        
        //    builds a new 'skew face' by making new points along edges, 1/3rd the distance from v1->v2,
        //    then connecting these into a new inset face.  This breaks rotational symmetry about the
        //    faces, whirling them into gyres
        //    v = v +2e, e = 5e, f = f + 2e
        
        verts_prop = verts_in[:];
        vert_tags = {'v' + str(i): i for i, v in enumerate(verts_prop)};
    
        flags_prop = {};
    
        foreach (face_i, face in enumerate(faces_in)) {
            for (v1, v2, v3 in zip(face, face[1:] +face[:1], face[2:] +face[:2])) {
                va_t = 'v{}:{}'.format(v1, v2);
                verts_prop.append(edge_third(verts_in, (v1, v2)));
                vert_tags[va_t] = len(verts_prop) - 1;

                face_t = 'f{}'.format(face_i);
                f4_t = 'f{}:{}'.format(face_i, v2);
                vb_t = 'v{}:{}'.format(v2, v1);
                vc_t = 'v{}'.format(v2);
                vd_t = 'v{}:{}'.format(v2, v3);

                // add flag for centre face
                flags_prop[face_t + va_t] =  [face_t, va_t, vd_t];
                // add 4 sided face which has two verts (vA, vB)  along edge v1, v2
                flags_new = face_vt_to_flags(f4_t, [va_t, vb_t, vc_t, vd_t]);
                flags_prop.update(flags_new);
            }
        }
        faces_prop = flags_to_faces(flags_prop, vert_tags)[0]
        return verts_prop, faces_prop
    
    }
        
    public void whirl(verts_in, faces_in) {
        
        //    gyro followed by truncation of vertices centered at original faces.
        //    This create 2 new hexagons for every original edge,
        //    v = v+4e, e=7e, f=f+2e
        
        verts_whirl = verts_in[:];
        vert_tags = {'v{}'.format(i): i for i, v in enumerate(verts_whirl)}
        ;
    
        flags_whirl = {}
        ;
    
        for (face_i, face in enumerate(faces_in)) {

            for (v1, v2, v3 in zip(face, face[1:] +face[:1], face[2:] +face[:2])) {
                // new vert on face
                va_t = 'vf{}:{}'.format(face_i, v1);
                center = Vector(face_center(verts_whirl, face));
                v1_xyz = Vector(verts_whirl[v1]);
                va_xyz = v1_xyz + (center - v1_xyz) / 2.0;
                verts_whirl.append(list(va_xyz));
                vert_tags[va_t] = len(verts_whirl) - 1;

                // new vert on edge
                vb_t = 'v{}:{}'.format(v1, v2);
                verts_whirl.append(edge_third(verts_in, (v1, v2)));
                vert_tags[vb_t] = len(verts_whirl) - 1;

                // 7 flags
                f6_t = 'f{}:{}'.format(face_i, v2);

                vc_t = 'v{}:{}'.format(v2, v1);
                vd_t = 'v{}'.format(v2);
                ve_t = 'v{}:{}'.format(v2, v3);
                vf_t = 'vf{}:{}'.format(face_i, v2);

                flags_new = face_vt_to_flags(f6_t, [va_t, vb_t, vc_t, vd_t, ve_t, vf_t]);
                flags_whirl.update(flags_new);

                face_t = 'f{}'.format(face_i);
                flags_whirl[face_t + va_t] =  [face_t, va_t, vf_t];
            }
        }
        faces_whirl = flags_to_faces(flags_whirl, vert_tags)[0];
        return verts_whirl, faces_whirl;
    }
