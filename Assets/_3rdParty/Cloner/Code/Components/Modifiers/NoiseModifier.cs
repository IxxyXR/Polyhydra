using System.Collections.Generic;
using UnityEngine;

namespace Cloner
{
	public class NoiseModifier : PointModifier
	{
		public float speed = 0.1f;
		public float magnitude = 1f;
		public float frequency = 1f;
		[Range (1, 8)]
		public int octaves = 2;
		public float lacunarity = 2f;
		public float gain = 2f;
		[Space]
		public Vector3 scaleBias = Vector3.one;
		public Vector3 scaleEffect = Vector3.zero;
		public bool lookAlongDerivative = true;
		public bool local = true;
		public FastNoise.NoiseType noiseType = FastNoise.NoiseType.SimplexFractal;
		public FastNoise.Interp interpolation;

		private FastNoise noise = new FastNoise ();
		private float t;

		public override List<Matrix4x4> Modify (List<Matrix4x4> points)
		{
			if (frequency == 0f)
				frequency = 0.001f;
			if (magnitude == 0f)
				magnitude = 0.0001f;

			t += Time.deltaTime * speed;
			noise.SetNoiseType (noiseType);
			noise.SetInterp (interpolation);

			noise.SetFrequency (frequency);
			noise.SetFractalOctaves (octaves);
			noise.SetFractalLacunarity (lacunarity);
			noise.SetFractalGain (gain);

			var upRotation = Quaternion.Euler (90f, 0f, 0f);
			var worldToLocal = transform.worldToLocalMatrix;

			for (int i = 0; i < points.Count; i++)
			{
				Vector3 p = points[i].GetColumn (3);
				if (local)
					p = worldToLocal.MultiplyPoint3x4 (p);

				var x = noise.GetNoise (p.x + t, p.y + t, p.z + t);
				var y = noise.GetNoise (p.x + t + 1000f, p.y + t + 1000f, p.z + t + 1000f);
				var z = noise.GetNoise (p.x + t - 1000f, p.y + t - 1000f, p.z + t - 1000f);

				var derivative = new Vector3 (x, y, z);
				var absDerivative = new Vector3 (Mathf.Abs (x), Mathf.Abs (y), Mathf.Abs (z));
				if (derivative == Vector3.zero)
					derivative = new Vector3 (0f, 0.0001f, 0f);

				points[i] *= Matrix4x4.TRS (derivative * magnitude, (lookAlongDerivative) ? Quaternion.LookRotation (derivative) : Quaternion.identity, scaleBias + (Vector3.Scale (scaleEffect, absDerivative)));
			}

			return points;
		}
	}
}