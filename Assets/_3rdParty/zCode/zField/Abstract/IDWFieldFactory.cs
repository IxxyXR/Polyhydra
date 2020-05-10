using System;
using System.Linq;

using zCode.zCore;

/*
 * Notes
 */

namespace zCode.zField
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class IDWFieldFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract IDWField3d<T> Create(double power, double epsilon = zMath.ZeroTolerance);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public IDWField3d<T> CreateCopy(IDWField3d<T> other)
        {
            var result = Create(other.Power, other.Epsilon);
            result.Objects.AddRange(other.Objects.Select(obj => obj.Duplicate()));
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public IDWField3d<T> CreateCopy<U>(IDWField3d<U> other, Func<U, T> converter)
        {
            var result = Create(other.Power, other.Epsilon);
            result.Objects.AddRange(other.Objects.Select(obj => obj.Convert(converter)));
            return result;
        }
    }
}
