using UnityEngine;
using System.Collections.Generic;
using Cloner.Math.Trig;
using System.Runtime.CompilerServices;

namespace Cloner
{
	public class SinModifier : PointModifier
	{
		public enum Axis { X, Y, Z }

		public float speed;
		public float offset;
		public Axis along = Axis.X;
		public Axis by = Axis.Y;
		public Sin sin = new Sin () { amplitude = 1, frequency = 0.5f };

		private Vector3 axisOffset;
		private float speedOffset;

		public override List<Matrix4x4> Modify (List<Matrix4x4> points)
		{
			speedOffset += Time.deltaTime * speed;
			if (sin.frequency == 0f)
				sin.frequency = 0.0001f;

			switch (along)
			{
				case Axis.X:
					axisOffset = Vector3.right * offset / sin.frequency;
					break;
				case Axis.Y:
					axisOffset = Vector3.up * offset / sin.frequency;
					break;
				case Axis.Z:
					axisOffset = Vector3.forward * offset / sin.frequency;
					break;
			}

			for (int i = 0; i < points.Count; i++)
			{
				var samplePosition = transform.worldToLocalMatrix.MultiplyPoint3x4 ((Vector3)points[i].GetColumn (3) + axisOffset);
				points[i] *= Matrix4x4.Translate (Sin3D (samplePosition));
			}

			return points;
		}

		[MethodImplAttribute (MethodImplOptions.AggressiveInlining)]
		private Vector3 Sin3D (Vector3 sample)
		{
			var animatedOffset = offset + speedOffset;
			var byValue = 0f;

			switch (by)
			{
				case Axis.X:
					byValue = sample.x;
					break;
				case Axis.Y:
					byValue = sample.y;
					break;
				case Axis.Z:
					byValue = sample.z;
					break;
			}

			var value = sin.Solve (byValue, animatedOffset);

			switch (along)
			{
				case Axis.X:
					return new Vector3 (value, 0f, 0f);
				case Axis.Y:
					return new Vector3 (0f, value, 0f);
				default:
					return new Vector3 (0f, 0f, value);
			}
		}
	}
}