using System;

/*
 * Notes
 */

namespace zCode.zDynamics
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Force
    {
        private double _strength;


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        public double Strength
        {
            get { return _strength; }
            set { _strength = value; }
        }
    }
}
