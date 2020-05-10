
/*
 * Notes
 */

#if USING_RHINO


using Rhino.Geometry;
using zCode.zCore;

namespace zCode.zRhino
{
    /// <summary>
    /// 
    /// </summary>
    public static class LineExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Interval3d ToInterval3d(this Line line)
        {
            return new Interval3d(line.From, line.To);
        }
    }
}

#endif
