using System;
using System.Collections.Generic;
using zCode.zCore;

/*
 * Notes 
 */

namespace zCode.zDynamics
{
    using H = ParticleHandle;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DihedralAngle : Constraint, IConstraint
    {
        #region Static Members

        /// <summary>
        /// Assumes both angles are between 0 and 2PI
        /// </summary>
        /// <param name="a0"></param>
        /// <param name="a1"></param>
        /// <returns></returns>
        private static double GetMinAngleDifference(double a0, double a1)
        {
            var d0 = (a0 < a1) ? a0 - a1 + 2.0 * Math.PI : a0 - a1;
            return d0 > Math.PI ? d0 - 2.0 * Math.PI : d0;
        }

        #endregion

        private H _h0 = new H();
        private H _h1 = new H();
        private H _h2 = new H();
        private H _h3 = new H();
        private double _target;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="target"></param>
        /// <param name="weight"></param>
        public DihedralAngle(int start, int end, int left, int right, double target, double weight = 1.0)
        {
            _h0.Index = start;
            _h1.Index = end;
            _h2.Index = left;
            _h3.Index = right;

            _target = target;
            Weight = weight;
        }


        /// <summary>
        /// 
        /// </summary>
        public H Start
        {
            get { return _h0; }
        }


        /// <summary>
        /// 
        /// </summary>
        public H End
        {
            get { return _h1; }
        }


        /// <summary>
        /// 
        /// </summary>
        public H Left
        {
            get { return _h2; }
        }


        /// <summary>
        /// 
        /// </summary>
        public H Right
        {
            get { return _h3; }
        }


        /// <summary>
        /// 
        /// </summary>
        public double Target
        {
            get { return _target; }
        }


        /// <inheritdoc />
        public ConstraintType Type
        {
            get { return ConstraintType.Position; }
        }


        /// <inheritdoc />
        public void Calculate(IReadOnlyList<IBody> bodies)
        {
            
            //Method 01
            Vec3d p0 = bodies[_h0.Index].Position;
            Vec3d p1 = bodies[_h1.Index].Position;
            Vec3d p2 = bodies[_h2.Index].Position;
            Vec3d p3 = bodies[_h3.Index].Position;

            var rotation = AxisAngle3d.Identity;
            rotation.Axis = p1 - p0;

            var d2 = p2 - p0;
            var d3 = p3 - p0;

            var angle = GeometryUtil.GetDihedralAngle(rotation.Axis, Vec3d.Cross(rotation.Axis, d2), Vec3d.Cross(rotation.Axis, -d3)) + Math.PI;
            rotation.Angle = GetMinAngleDifference(_target, angle) * 0.5;

            

            // calculate deltas as diff bw current and rotated
            _h2.Delta = (rotation.Inverse.Apply(d2) - d2) * 0.5;
            _h3.Delta = (rotation.Apply(d3) - d3) * 0.5;

            // distribute reverse projection among hinge bodies
            _h0.Delta = _h1.Delta = (_h2.Delta + _h3.Delta) * -0.5;
            

        }


        /// <inheritdoc />
        public void Apply(IReadOnlyList<IBody> bodies)
        {
            bodies[_h0.Index].ApplyMove(_h0.Delta, Weight);
            bodies[_h1.Index].ApplyMove(_h1.Delta, Weight);
            bodies[_h2.Index].ApplyMove(_h2.Delta, Weight);
            bodies[_h3.Index].ApplyMove(_h3.Delta, Weight);
        }


        #region Explicit interface implementations

        /// <inheritdoc />
        IEnumerable<IHandle> IConstraint.Handles
        {
            get
            {
                yield return _h0;
                yield return _h1;
                yield return _h2;
                yield return _h3;
            }
        }

        #endregion
    }
}
