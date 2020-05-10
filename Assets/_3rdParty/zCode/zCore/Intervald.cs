using System;
using System.Collections.Generic;
using System.Linq;

/*
 * Notes
 */

namespace zCode.zCore
{
    /// <summary>
    /// Represents a double precision interval.
    /// https://en.wikipedia.org/wiki/Interval_(mathematics)
    /// </summary>
    [Serializable]
    public partial struct Intervald
    {
        #region Static
        
        /// <summary></summary>
        public static readonly Intervald Zero = new Intervald();
        /// <summary></summary>
        public static readonly Intervald Unit = new Intervald(0.0, 1.0);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        public static implicit operator string(Intervald interval)
        {
            return interval.ToString();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Intervald operator +(Intervald d, double t)
        {
            d.Translate(t);
            return d;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Intervald operator -(Intervald d, double t)
        {
            d.Translate(-t);
            return d;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Intervald operator *(Intervald d, double t)
        {
            d.Scale(t);
            return d;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Intervald operator *(double t, Intervald d)
        {
            d.Scale(t);
            return d;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Intervald operator /(Intervald d, double t)
        {
            d.Scale(1.0 / t);
            return d;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static double Remap(double t, Intervald from, Intervald to)
        {
            if (!from.IsValid) 
                throw new InvalidOperationException("Can't remap from an invalid interval");

            return zMath.Remap(t, from.A, from.B, to.A, to.B);
        }


        /// <summary>
        /// Returns the union of a and b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Intervald Union(Intervald a, Intervald b)
        {
            a.Include(b.A);
            a.Include(b.B);
            return a;
        }


        /// <summary>
        /// Returns the region of a that is also in b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Intervald Intersect(Intervald a, Intervald b)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Returns the region of a that is not in b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Intervald Difference(Intervald a, Intervald b)
        {
            throw new NotImplementedException();
        }

        #endregion


        /// <summary>The start of the interval</summary>
        public double A;

        /// <summary>The end of the interval</summary>
        public double B;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ab"></param>
        public Intervald(double ab)
        {
            A = B = ab;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Intervald(double a, double b)
        {
            A = a;
            B = b;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        public Intervald(IEnumerable<double> values)
        {
            A = B = values.First();

            foreach (var t in values.Skip(1))
                IncludeIncreasing(t);
        }


        /// <summary>
        /// Returns true if A is greater than B.
        /// </summary>
        public bool IsIncreasing
        {
            get { return B > A; }
        }


        /// <summary>
        /// Returns true if A is less than B.
        /// </summary>
        public bool IsDecreasing
        {
            get { return B < A; }
        }


        /// <summary>
        /// Returns positive if this interval is increasing, negative if it's decreasing, and zero if it's invalid.
        /// </summary>
        /// <returns></returns>
        public int Orientation
        {
            get { return Math.Sign(B - A); }
        }


        /// <summary>
        /// Returns true if A equals B.
        /// </summary>
        public bool IsValid
        {
            get { return A != B; }
        }


        /// <summary>
        /// Defined as B - A
        /// </summary>
        public double Length
        {
            get { return B - A; }
        }


        /// <summary>
        /// 
        /// </summary>
        public double Mid
        {
            get { return (A + B) * 0.5; }
        }


        /// <summary>
        /// 
        /// </summary>
        public double Min
        {
            get { return Math.Min(A, B); }
        }


        /// <summary>
        /// 
        /// </summary>
        public double Max
        {
            get { return Math.Max(A, B); }
        }


        /// <inheritdoc />
        public override string ToString()
        {
            return String.Format("{0} to {1}", A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ab"></param>
        public void Set(double ab)
        {
            A = B = ab;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void Set(double a, double b)
        {
            A = a;
            B = b;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool ApproxEquals(Intervald other, double tolerance = zMath.ZeroTolerance)
        {
            return 
                zMath.ApproxEquals(A, other.A, tolerance) &&
                zMath.ApproxEquals(B, other.B, tolerance);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public double Evaluate(double t)
        {
            return zMath.Lerp(A, B, t);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Normalize(double t)
        {
            return zMath.Normalize(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Clamp(double t)
        {
            if (IsDecreasing)
                return zMath.Clamp(t, B, A);
            else
                return zMath.Clamp(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Nearest(double t)
        {
            return zMath.Nearest(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Ramp(double t)
        {
            return zMath.Ramp(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double SmoothStep(double t)
        {
            return zMath.SmoothStep(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double SmootherStep(double t)
        {
            return zMath.SmootherStep(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public double Wrap(double t)
        {
            return zMath.Wrap(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Contains(double t)
        {
            if (IsDecreasing)
                return zMath.Contains(t, B, A);
            else
                return zMath.Contains(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool ContainsIncl(double t)
        {
            if (IsDecreasing)
                return zMath.ContainsIncl(t, B, A);
            else
                return zMath.ContainsIncl(t, A, B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public void Scale(double t)
        {
            A *= t;
            B *= t;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public void Translate(double t)
        {
            A += t;
            B += t;
        }


        /// <summary>
        /// Expands the interval on both sides by the given value
        /// </summary>
        /// <param name="t"></param>
        public void Expand(double t)
        {
            if (IsDecreasing)
            {
                A += t;
                B -= t;
            }
            else
            {
                A -= t;
                B += t;
            }
        }


        /// <summary>
        /// Expands the interval to include the given value.
        /// </summary>
        /// <param name="t"></param>
        public void Include(double t)
        {
            if (IsDecreasing)
                IncludeDecreasing(t);
            else
                IncludeIncreasing(t);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        internal void IncludeDecreasing(double t)
        {
            if (t > A) A = t;
            else if (t < B) B = t;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        internal void IncludeIncreasing(double t)
        {
            if (t > B) B = t;
            else if (t < A) A = t;
        }


        /// <summary>
        /// Expands this interval to include another
        /// </summary>
        /// <param name="other"></param>
        public void Include(Intervald other)
        {
            Include(other.A);
            Include(other.B);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        public void Include(IEnumerable<double> values)
        {
            Include(new Intervald(values));
        }


        /// <summary>
        /// 
        /// </summary>
        public void Reverse()
        {
            double t = A;
            A = B;
            B = t;
        }


        /// <summary>
        /// 
        /// </summary>
        public void MakeIncreasing()
        {
            if (IsDecreasing) Reverse();
        }


        /// <summary>
        /// 
        /// </summary>
        public void MakeDecreasing()
        {
            if (IsIncreasing) Reverse();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Deconstruct(out double a, out double b)
        {
            a = A;
            b = B;
        }
    }
}
