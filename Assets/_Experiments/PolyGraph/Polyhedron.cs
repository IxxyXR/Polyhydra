using UnityEngine;
using Wythoff;

namespace Graphmesh {

    public class Polyhedron //: GraphmeshNode
    {

//        public PolyTypes polyType;
//        [Input] public Material material;
//        [Output] public ModelGroup output;
//
//        public override object GetValue(NodePort port) {
//            object o = base.GetValue(port);
//            if (o != null) return o;
//            string symbol = Uniform.Uniforms[32].Wythoff;
//            var w = new WythoffPoly(symbol);
//            var p = new PolyHydra();
//            Mesh mesh = p.BuildMeshFromWythoffPoly(w);
//            Material material = GetInputValue("material", this.material);
//
//            if (mesh == null) return new ModelGroup();
//            //Fixme: Support for more than one material
//            Model model = new Model(mesh.Copy(), new Material[] { material });
//            return new ModelGroup() { model };
//        }
    }
}