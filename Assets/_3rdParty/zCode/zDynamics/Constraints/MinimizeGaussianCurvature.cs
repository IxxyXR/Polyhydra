using System;
using System.Collections.Generic;
using System.Linq;

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
    public class MinimizeGaussianCurvature : Constraint, IConstraint
    {

        #region Static

        protected const int DefaultCapacity = 4;

        #endregion


        private List<H> _neighbors;
        private H _handle = new H();
        private double _epsilon= 0.0001;
       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="capacity"></param>
        public MinimizeGaussianCurvature(double weight = 1.0, int capacity = DefaultCapacity)
        {
            _neighbors = new List<H>(capacity);
            Weight = weight;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="weight"></param>
        public MinimizeGaussianCurvature(int index, IEnumerable<int> neighborIndices, double weight = 1.0, int capacity = DefaultCapacity)
            : this(weight, capacity)
        {
            _handle.Index = index;
            _neighbors.AddRange(neighborIndices.Select(i => new H(i)));
        }


        /// <summary>
        /// 
        /// </summary>
        public H Handle
        {
            get { return _handle; }
        }


        /// <summary>
        /// 
        /// </summary>
        public List<H> Neighbors
        {
            get { return _neighbors; }
        }


        /// <inheritdoc />
        public ConstraintType Type
        {
            get { return ConstraintType.Position; }
        }


        /// <summary>
        /// Need at least 3 neighbors to define projections.
        /// </summary>
        private bool IsValid
        {
            get { return Neighbors.Count > 2; }
        }


        /// <inheritdoc />
        public void Calculate(IReadOnlyList<IBody> bodies)
        {
            if (!IsValid) return;

   
            GetNormalGrad(bodies, _epsilon);
            //GetGaussianGrad(bodies, _epsilon);


        }




        /// <inheritdoc />
        public void Apply(IReadOnlyList<IBody> bodies)
        {
            if (!IsValid) return;

            bodies[_handle].ApplyMove(_handle.Delta, Weight);

            foreach (var h in Neighbors)
                bodies[h].ApplyMove(h.Delta, Weight);
        }




        /// <summary>
        ///  gets an approximation of the gradient with respect of the current particle
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="neighbors"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public void GetGaussianGrad(IReadOnlyList<IBody> bodies, double epsilon)
        {

            double g0 = GetGaussian(bodies[_handle].Position, bodies);

            Vec3d dx = new Vec3d(epsilon, 0.0, 0.0);
            Vec3d sx0 = bodies[_handle].Position - dx;
            double gx0 = GetGaussian(sx0, bodies);
            Vec3d sx1 = bodies[_handle].Position + dx;
            double gx1 = GetGaussian(sx1, bodies);

            Vec3d dy = new Vec3d(0.0, epsilon, 0.0);
            Vec3d sy0 = bodies[_handle].Position - dy;
            double gy0 = GetGaussian(sy0, bodies);
            Vec3d sy1 = bodies[_handle].Position + dy;
            double gy1 = GetGaussian(sy1, bodies);

            Vec3d dz = new Vec3d(0.0, 0.0, epsilon);
            Vec3d sz0 = bodies[_handle].Position - dz;
            double gz0 = GetGaussian(sz0, bodies);
            Vec3d sz1 = bodies[_handle].Position + dz;
            double gz1 = GetGaussian(sz1, bodies);
       

            double edgeLenSum = 0.00;

            for (int i = 0; i < Neighbors.Count; i++)
            {
                double edgeLen = bodies[_handle].Position.DistanceTo(bodies[Neighbors[i]].Position);
                edgeLenSum += edgeLen;
            }

            double avgEdgeLen = edgeLenSum / Neighbors.Count;

            double mag = g0 * avgEdgeLen ;
            Vec3d grad = new Vec3d(gx0 - gx1, gy0 - gy1, gz0 - gz1);
            grad.Unitize();
            Vec3d result = grad * mag;

            _handle.Delta = result;

        }



        public void GetNormalGrad(IReadOnlyList<IBody> bodies, double epsilon)
        {
            double g = GetGaussian(bodies[_handle].Position, bodies);
            Vec3d n = ComputeNormal(bodies);

            Vec3d s0 = bodies[_handle].Position - n * epsilon;
            double g0 = GetGaussian(s0, bodies);

            Vec3d s1 = bodies[_handle].Position + n * epsilon;
            double g1 = GetGaussian(s1, bodies);

            double edgeLenSum = 0.00;

            for (int i = 0; i < Neighbors.Count; i++)
            {
                double edgeLen = bodies[_handle].Position.DistanceTo(bodies[Neighbors[i]].Position);
                edgeLenSum += edgeLen;
            }

            double avgEdgeLen = edgeLenSum / Neighbors.Count;


            double mag = g * avgEdgeLen * 0.1;         
            double grad = Math.Sign(g0 - g1);   
            Vec3d result = n * grad * mag;


            if (Math.Sign(g0) != Math.Sign(g1))
                result = new Vec3d();
       

            _handle.Delta = result;

        }




        /// <summary>
        /// gets an approximation of Gaussian Curvature with respect of the current particle
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="neighbors"></param>
        /// <returns></returns>
        public double GetGaussian(Vec3d pos, IReadOnlyList<IBody> bodies)
        {

            double angleSum = 0.00;
            double areaSum = 0.00;

            for (int i = 0; i < Neighbors.Count - 1; i++)
            {
                Vec3d v0 = bodies[Neighbors[i]].Position - pos;
                Vec3d v1 = bodies[Neighbors[i+1]].Position - pos;
                angleSum += Vec3d.Angle(v0, v1);
                areaSum += Vec3d.Cross(v0 * 0.50, v1 * 0.50).Length;
            }


            Vec3d vlast = bodies[Neighbors[Neighbors.Count - 1]].Position - pos;
            Vec3d vfirst = bodies[Neighbors[0]].Position - pos;
            angleSum += Vec3d.Angle(vlast, vfirst);
            areaSum += Vec3d.Cross(vlast * 0.50, vfirst * 0.50).Length;


            double K = (2.0 * Math.PI - angleSum)/areaSum;

            return K;
        }



        /// <summary>
        /// Calculates the normal as the sum of triangle area gradients
        /// </summary>
        /// <returns></returns>
        private Vec3d ComputeNormal(IReadOnlyList<IBody> bodies)
        {
            var p = bodies[_handle].Position;

            var sum = new Vec3d();
            var n = _neighbors.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                var p0 = bodies[_neighbors[i]].Position;
                var p1 = bodies[_neighbors[j]].Position;
                sum += GeometryUtil.GetTriAreaGradient(p, p0, p1);
            }

            return sum;
        }

 

        #region Explicit interface implementations

        /// <inheritdoc />
        IEnumerable<IHandle> IConstraint.Handles
        {
            get
            {
                yield return _handle;
            }
        }

        #endregion

    }
}
