using System.Drawing;
using UnityEngine;

/*
 * Notes
 */

namespace zCode.zCore
{
    /// <summary>
    /// 
    /// </summary>
    public static class ColorExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="other"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Color LerpTo(this Color c, Color other, double t)
        {
            int a = (int)(c.a + (other.a - c.a) * t);
            int r = (int)(c.r + (other.r - c.r) * t);
            int g = (int)(c.g + (other.g - c.g) * t);
            int b = (int)(c.b + (other.b - c.b) * t);
            return new Color(r, g, b, a);
        }
    }
}
